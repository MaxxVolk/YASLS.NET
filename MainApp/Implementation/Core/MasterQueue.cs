using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using YASLS.SDK.Library;

namespace YASLS
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

    public MasterQueue(CancellationToken cancellationToken, Dictionary<string, string> attributes, ILogger logger, IHealthReporter healthReporter, IQueueFactory queueFactory)
    {
      token = cancellationToken;
      eventLogger = logger;
      this.healthReporter = healthReporter;
      if (attributes != null && attributes.Count > 0)
        foreach (KeyValuePair<string, string> origAttr in attributes)
          Attributes.Add(origAttr.Key, origAttr.Value);
      Messages = queueFactory.GetMessageQueue(this);
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

    public void RegisterAttributeExtractor(IAttributeExtractorModule attributeExtractor)
    {
      Extractors.Add(attributeExtractor);
    }

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
