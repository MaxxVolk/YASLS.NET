using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using YASLS.Configuration;
using YASLS.SDK.Library;

namespace YASLS.Core
{
  public class MasterQueue : IServerMasterQueue, IModule
  {
    protected readonly Guid moduleId = Guid.Parse("{72E29AEC-E94B-4295-A491-0167516C5F4B}");
    protected IMessageQueue Messages;
    protected CancellationToken token;
    protected Dictionary<string, string> Attributes = new Dictionary<string, string>();
    protected List<IAttributeExtractorModule> Extractors = new List<IAttributeExtractorModule>();
    protected List<Route> Routes = new List<Route>();
    protected ILogger eventLogger;
    protected IHealthReporter healthReporter;

    public MasterQueue(QueueDefinition cfg, CancellationToken cancellationToken, ILogger logger, IHealthReporter healthReporter, IQueueFactory queueFactory, IModuleResolver moduleResolver)
    {
      token = cancellationToken;
      eventLogger = logger;
      this.healthReporter = healthReporter;
      if (cfg.Attributes != null && cfg.Attributes.Count > 0)
        foreach (KeyValuePair<string, string> origAttr in cfg.Attributes)
          Attributes.Add(origAttr.Key, origAttr.Value);
      Messages = queueFactory.GetMessageQueue(this);

      // add Attribute Extractors to the queue
      if (cfg.AttributeExtractors != null)
        foreach (KeyValuePair<string, ModuleDefinition> extractorCfg in cfg.AttributeExtractors)
          try
          {
            ModuleDefinition extractorDef = extractorCfg.Value;
            IAttributeExtractorModule newExtractorModule = moduleResolver.CreateModule<IAttributeExtractorModule>(extractorDef);
            newExtractorModule.LoadConfiguration(extractorDef.ConfigurationJSON, extractorDef.Attributes);
            Extractors.Add(newExtractorModule);
          }
          catch (Exception e)
          {
            eventLogger?.LogEvent(this, Severity.Warning, "AttributeExtractorInit", $"Failed to load or initialize '{extractorCfg.Key ?? "<Unknown>"}' attribute parser module.", e);
          }
    }

    public void Enqueue(MessageDataItem message)
    {
      Messages.Enqueue(message.Clone());
    }

    protected void WorkerProc()
    {
      while (true)
      {
        if (token.IsCancellationRequested)
          break;
        if (Messages.TryDequeue(out MessageDataItem newMessage))
        {
          foreach (KeyValuePair<string, string> extraAttr in Attributes)
            newMessage.AddAttribute(extraAttr.Key, extraAttr.Value);
          foreach (IAttributeExtractorModule extractor in Extractors)
            try
            {
              extractor.ExtractAttributes(newMessage);
            }
            catch (Exception e)
            {
              eventLogger?.LogEvent(this, Severity.Error, "ExtractorInvoke", $"Failed to execute extractor {extractor.GetType().FullName}.", e);
            }
          foreach (Route route in Routes)
          {
            MessageDataItem rMsg = newMessage.Clone();
            route.Enqueue(rMsg);
          }
          continue; // sleep only if the queue is empty
        }
        else
        {
          Thread.Sleep(5);
        }
      }
    }

    public ThreadStart GetWorker() => new ThreadStart(WorkerProc);

    public void RegisterRoute(Route route)
    {
      Routes.Add(route);
    }

    #region IModule Implementation
    public string GetModuleName() => GetType().FullName;

    public string GetModuleDisplayName() => "Main Queue Module";

    public string GetModuleVendor() => "Core YASLS";

    public Guid GetModuleId() => moduleId;

    public Version GetModuleVersion() => Assembly.GetAssembly(GetType()).GetName().Version;
    #endregion
  }
}
