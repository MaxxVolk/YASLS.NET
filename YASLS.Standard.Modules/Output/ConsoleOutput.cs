﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YASLS.SDK.Library;

namespace YASLS.Standard.Modules
{
  public class ConsoleOutput : IOutputModule, IServerBind
  {
    protected readonly Guid moduleId = Guid.Parse("{A72FFDB6-F3AF-433F-9FE2-D1D23DA00281}");
    protected CancellationToken token;
    protected ILogger logger = null;
    protected IHealthReporter healthReporter = null;
    protected IMessageQueue Messages;
    protected int maxMessageCount = 100;


    #region IOutputModule Implementation
    public void LoadConfiguration(JObject configuration, CancellationToken cancellationToken)
    {
      token = cancellationToken;
    }

    public void Enqueue(MessageDataItem message)
    {
      Messages.Enqueue(message);
    }
    #endregion

    #region IThreadModule Implementation
    public ThreadStart GetWorker() => new ThreadStart(WorkerProc);

    public void Initialize() { }

    public void Destroy() { }
    #endregion

    private void WorkerProc()
    {
      while (true)
      {
        if (token.IsCancellationRequested)
          break;
        try
        {
          if (Messages.TryDequeue(out MessageDataItem newMessage))
          {
            Console.WriteLine(newMessage.Message);
            foreach (string attrName in newMessage.GetAttributeNames)
              Console.WriteLine($"{attrName:30}: {newMessage.GetAttributeAsVariant(attrName)}");
            continue; // sleep only if the queue is empty
          }
          else
          {
            Thread.Sleep(5);
          }
        }
        catch (Exception e)
        {
          logger?.LogEvent(this, Severity.Error, "ConsoleWrite", "Unknown exception in Console Output Module.", e);
          continue;
        }
      }
    }

    #region IServerBind Implementation
    public void RegisterServices(ILogger logger, IHealthReporter healthReporter, IQueueFactory factory, IPersistentDataStore persistentStore)
    {
      this.logger = logger;
      this.healthReporter = healthReporter;
      Messages = factory.GetMessageQueue(this);
    }
    #endregion

    #region IModule Implementation
    public string GetModuleName() => GetType().FullName;

    public string GetModuleDisplayName() => "Console Output Module";

    public string GetModuleVendor() => "Core YASLS";

    public Guid GetModuleId() => moduleId;

    public Version GetModuleVersion() => Assembly.GetAssembly(GetType()).GetName().Version;
    #endregion
  }
}
