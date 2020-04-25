using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YASLS.SDK.Library;

namespace YASLS
{
  class InMemoryConcurrentQueueFactory : IQueueFactory
  {
    protected List<ModuleQueueInformation> Queues = new List<ModuleQueueInformation>();
    protected ILogger internalLogger;

    public InMemoryConcurrentQueueFactory(ILogger logger)
    {
      internalLogger = logger;
    }

    public IMessageQueue GetMessageQueue(IModule sourceModulem)
    {
      InMemoryConcurrentQueue newQueue = new InMemoryConcurrentQueue(internalLogger, sourceModulem);
      Queues.Add(new ModuleQueueInformation { AssignedQueue = newQueue, OwningModule = sourceModulem });
      return newQueue;
    }
  }

  public class ModuleQueueInformation
  {
    public IModule OwningModule;
    public IMessageQueue AssignedQueue;
  }

  public class InMemoryConcurrentQueue : IMessageQueue
  {
    protected ConcurrentQueue<MessageDataItem> Implementation = new ConcurrentQueue<MessageDataItem>();
    protected ILogger internalLogger;
    protected IModule ownerModule;

    public InMemoryConcurrentQueue(ILogger logger, IModule sourceModulem)
    {
      internalLogger = logger;
      ownerModule = sourceModulem;
    }

    public bool IsEmpty => Implementation.IsEmpty;

    public void Enqueue(MessageDataItem message)
    {
      if (Implementation.Count > 10000)
        internalLogger?.LogEvent(ownerModule, Severity.Error, "QueueOverflow", "Module queue is full. Dropping events.");
      else
      Implementation.Enqueue(message);
    }

    public bool TryDequeue(out MessageDataItem message) => Implementation.TryDequeue(out message);

    public bool TryPeek(out MessageDataItem message) => TryPeek(out message);
  }
}
