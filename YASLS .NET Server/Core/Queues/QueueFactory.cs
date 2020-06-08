using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using YASLS.SDK.Library;

namespace YASLS.NETServer.Core
{
  class InMemoryConcurrentQueueFactory : IQueueFactory
  {
    protected List<ModuleQueueInformation> Queues = new List<ModuleQueueInformation>();
    protected ILogger internalLogger;

    public InMemoryConcurrentQueueFactory(ILogger logger)
    {
      internalLogger = logger;
    }

    public IProducerConsumerCollection<MessageDataItem> GetMessageQueue(IModule sourceModulem)
    {
      InMemoryConcurrentQueue newQueue = new InMemoryConcurrentQueue(internalLogger, sourceModulem);
      Queues.Add(new ModuleQueueInformation { AssignedQueue = newQueue, OwningModule = sourceModulem });
      return newQueue;
    }
  }

  public class ModuleQueueInformation
  {
    public IModule OwningModule;
    public IProducerConsumerCollection<MessageDataItem> AssignedQueue;
  }
}
