using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using YASLS.Configuration;
using YASLS.SDK.Library;

namespace YASLS.Core
{
  public class Route: BaseInternalModule
  {
    protected Dictionary<string, Filter> Filters = new Dictionary<string, Filter>();
    protected readonly Guid moduleId = Guid.Parse("{E1A7E7CA-F1B7-4623-942A-05C0D7C0F667}");
    protected IMessageQueue Messages;
    protected CancellationToken token;

    public Route(RouteDefinition cfg, CancellationToken cancellationToken, ILogger logger, IHealthReporter healthReporter, IQueueFactory queueFactory, IModuleResolver moduleResolver):base(logger, healthReporter)
    {
      token = cancellationToken;
      foreach (KeyValuePair<string, FilterDefinition> filterDef in cfg.Filters)
      {
        Filters.Add(filterDef.Key, new Filter(filterDef.Value, logger, healthReporter, cancellationToken, moduleResolver));
      }
      Messages = queueFactory.GetMessageQueue(this);
    }

    public void Enqueue(MessageDataItem message)
    {
      Messages.Enqueue(message);
    }

    protected void WorkerProc()
    {
      while (true)
      {
        if (token.IsCancellationRequested)
          break;
        if (Messages.TryDequeue(out MessageDataItem newMessage))
        {
          foreach (KeyValuePair<string, Filter> filterRef in Filters)
            if (filterRef.Value.ProcessMessageShouldStop(newMessage))
              break;
          continue; // sleep only if the queue is empty
        }
        else
        {
          Thread.Sleep(5);
        }
      }
    }

    #region IModule Implementation
    public ThreadStart GetWorker() => new ThreadStart(WorkerProc);

    public override string GetModuleDisplayName() => "Main Route Module";

    public override Guid GetModuleId() => moduleId;
    #endregion
  }
}
