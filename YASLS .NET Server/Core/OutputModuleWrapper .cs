using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YASLS.NETServer.Configuration;
using YASLS.SDK.Library;

namespace YASLS.NETServer.Core
{
  class OutputModuleWrapper : IModule
  {
    // Wrapper internals
    protected YASLServer Server;
    public string ModuleKey { get; protected set; }
    protected Tuple<string, string>[] Attibutes;
    protected int AttributeCount;
    public int ModuleReloadTimeoutSeconds { get; set; } = 60;

    // Module related
    protected IOutputModule InputModule;
    public IProducerConsumerCollection<MessageDataItem> AttachedQueue { get; protected set; }
    protected JObject ModuleConfiguration;
    protected bool IsModuleConfigurationFromFile = false;
    protected string ConfigurationFilePath = null;
    protected ModuleDefinition ModuleDefinition;

    // Flow control
    protected Thread RunnerThread;
    public ManualResetEvent WorkCompleted { get; protected set; }
    protected CancellationTokenSource CancellationTokenSource = null;
    protected CancellationToken CancellationToken = CancellationToken.None;
    protected bool IsConfigurationReloadRequested = false;

    public OutputModuleWrapper(string outputModuleKey, ModuleDefinition moduleDefinition, YASLServer server)
    {
      Server = server;
      ModuleKey = outputModuleKey;
      ModuleDefinition = moduleDefinition;
      Attibutes = (moduleDefinition.Attributes ?? new Dictionary<string, string>()).Select(x => new Tuple<string, string>(x.Key, x.Value)).ToArray();
      AttributeCount = Attibutes.Length;
      WorkCompleted = new ManualResetEvent(false);

      if (!string.IsNullOrWhiteSpace(moduleDefinition.ConfigurationFilePath))
      {
        ConfigurationFilePath = moduleDefinition.ConfigurationFilePath;
        if (File.Exists(ConfigurationFilePath))
        {
          IsModuleConfigurationFromFile = true;
          ModuleConfiguration = JObject.Parse(File.ReadAllText(ConfigurationFilePath));
        }
        else
        {
          Server.Logger.LogEvent(this, Severity.Error, "InputModuleInit", $"External configuration file not found: {ConfigurationFilePath}.");
          IsModuleConfigurationFromFile = false;
          ModuleConfiguration = moduleDefinition.ConfigurationJSON;
        }
      }
      else
      {
        IsModuleConfigurationFromFile = false;
        ModuleConfiguration = moduleDefinition.ConfigurationJSON;
      }
      
      AttachedQueue = Server.QueueFactory.GetMessageQueue(InputModule);
    }

    private bool MessageAcceptor(MessageDataItem message)
    {
      for (int i = 0; i < AttributeCount; i++)
        message.AddAttribute(Attibutes[i].Item1, Attibutes[i].Item2);
      bool posted = AttachedQueue.TryAdd(message);
      if (!posted)
      {
        Server.Logger?.LogEvent(InputModule, Severity.Warning, ServerConstants.Reasons.QueueOverflow, $"Input message dropped from '{ModuleKey}'.");
        Server.HealthReporter?.SetModuleHealth(InputModule, ServerConstants.Components.AttachedQueue, HealthState.Warning, "Message dropped");
      }
      return posted;
    }

    private void Runner()
    {
      ThreadStart ModuleEntryPoint = null;
      bool finishedSuccessfully = true;
      int moduleFailureCount = 0;
      DateTime failureResetTimer = DateTime.UtcNow;
      do
      {
        // create and initialize module
        while (ModuleEntryPoint == null)
          try
          {
            CancellationTokenSource?.Dispose();
            CancellationTokenSource = new CancellationTokenSource();
            CancellationToken = CancellationTokenSource.Token;
            InputModule = Server.CreateModule<IOutputModule>(ModuleDefinition);
            InputModule.LoadConfiguration(ModuleConfiguration);
            InputModule.SetMessageReceiver(MessageDonor);
            InputModule.Initialize();
            ModuleEntryPoint = InputModule.GetWorker(CancellationToken);
            failureResetTimer = DateTime.UtcNow;
          }
          catch (Exception e)
          {
            Server.Logger?.LogEvent(this, Severity.Error, "InputModule", $"Failed to re/create '{ModuleKey}'. Next retry in {ModuleReloadTimeoutSeconds} seconds.", e);
            ModuleEntryPoint = null;
            // pause before next attempt
            for (int waitCount = 0; waitCount < 100; waitCount++)
            {
              if (CancellationToken.IsCancellationRequested)
                break;
              Thread.Sleep(ModuleReloadTimeoutSeconds * 10); // effectively * 1000, i.e. seconds => milliseconds
            }
            if (CancellationToken.IsCancellationRequested && IsConfigurationReloadRequested)
              continue;
            // if canceled not for reload, and still unable to create, just mark completed and exit
            if (CancellationToken.IsCancellationRequested)
            {
              WorkCompleted.Set();
              return;
            }
          }
        // working
        try
        {
          ModuleEntryPoint.Invoke(); // ModuleEntryPoint != null, see above, except if canceled 
          if (IsConfigurationReloadRequested)
          {
            IsConfigurationReloadRequested = false;
            try { InputModule.Destroy(); } catch { } // to avoid Invoke() catcher
            ModuleEntryPoint = null; // to initiate module load
          }
          finishedSuccessfully = true;
          if (CancellationToken.IsCancellationRequested)
            break;
        }
        catch (OperationCanceledException)
        {
          break;
        }
        catch (Exception e)
        {
          if (DateTime.UtcNow.Subtract(failureResetTimer).TotalSeconds > 10 * ModuleReloadTimeoutSeconds)
          {
            failureResetTimer = DateTime.UtcNow;
            moduleFailureCount = 0;
          }
          Server.Logger.LogEvent(InputModule, Severity.Error, "OutputModule", $"Unexpected output module failure. That's happened {moduleFailureCount + 1} times in the row.", e);
          finishedSuccessfully = false;
          Server.Logger.LogEvent(this, Severity.Error, "OutputModule", $"Recreating output module. Next retry in {ModuleReloadTimeoutSeconds * moduleFailureCount} seconds.");
          if (moduleFailureCount > 0)
            Thread.Sleep(ModuleReloadTimeoutSeconds * moduleFailureCount * 1000); // pause before next attempt
          // re-initialize the module
          try { InputModule.Destroy(); } catch { }
          ModuleEntryPoint = null;
          moduleFailureCount++;
        }
      } while (!finishedSuccessfully && !CancellationToken.IsCancellationRequested);

      // All done, but let's drain the queue
      if (AttachedQueue.Count > 0)
      {
        Server.Logger?.LogEvent(this, Severity.Warning, "OutputModule", $"Output module '{ModuleKey}' finished, but it's queue is not empty. Start draining.");
        CancellationTokenSource?.Dispose();
        CancellationTokenSource = new CancellationTokenSource(); // refresh token
        CancellationToken = CancellationTokenSource.Token;
        if (ModuleEntryPoint != null) // previous initialization was successful 
        {
          Thread drainThread = new Thread(InputModule.GetWorker(CancellationToken));
          drainThread.Start();
          while (AttachedQueue.Count > 0)
          {
            Server.Logger?.LogEvent(this, Severity.Warning, "OutputModule", $"Draining. Messages left {AttachedQueue.Count}.");
            Thread.Sleep(1000);
          }
          Server.Logger?.LogEvent(this, Severity.Warning, "OutputModule", $"Output module '{ModuleKey}' finished draining.");
          CancellationTokenSource.Cancel();
        }
      }
      WorkCompleted.Set();
    }

    private bool MessageDonor(out MessageDataItem message)
    {
      if (AttachedQueue.TryTake(out message))
      {
        for (int i = 0; i < AttributeCount; i++)
          message.AddAttribute(Attibutes[i].Item1, Attibutes[i].Item2);
        return true;
      }
      else
      {
        message = null;
        return false;
      }
    }

    public void Start()
    {
      RunnerThread = new Thread(Runner);
      RunnerThread.Start();
    }

    public void Stop()
    {
      CancellationTokenSource.Cancel();
    }

    public void Abort()
    {
      RunnerThread.Abort();
    }

    public bool ReloadConfiguration()
    {
      // only external configuration reload is supported
      if (!IsModuleConfigurationFromFile)
        return false;
      // reload configuration
      if (File.Exists(ConfigurationFilePath)) // =! null because IsModuleConfigurationFromFile == true
      {
        IsModuleConfigurationFromFile = true;
        ModuleConfiguration = JObject.Parse(File.ReadAllText(ConfigurationFilePath));
      }
      else
      {
        Server.Logger.LogEvent(this, Severity.Error, "ReloadConfiguration", $"External configuration file not found: {ConfigurationFilePath}.");
        return false;
      }
      IsConfigurationReloadRequested = true;
      CancellationTokenSource.Cancel();
      return true;
    }

    #region IModule Implementation
    protected readonly Guid moduleId = Guid.Parse("{0F8EA251-DD8F-4E46-B853-C041BDA88238}");

    public string GetModuleDisplayName() => "YASLS .NET Server Input Module Wrapper";

    public Guid GetModuleId() => moduleId;

    public string GetModuleName() => GetType().FullName;

    public string GetModuleVendor() => "YASLS";

    public Version GetModuleVersion() => Assembly.GetAssembly(GetType()).GetName().Version;

    public void LoadConfiguration(JObject configuration)
    {
      // never used
    }
    #endregion
  }
}
