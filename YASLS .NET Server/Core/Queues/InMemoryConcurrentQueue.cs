using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

using YASLS.SDK.Library;

namespace YASLS.NETServer.Core
{
  public class InMemoryConcurrentQueue : IProducerConsumerCollection<MessageDataItem>
  {
    protected ConcurrentQueue<MessageDataItem> Implementation = new ConcurrentQueue<MessageDataItem>();
    protected ILogger internalLogger;
    protected IModule ownerModule;

    public InMemoryConcurrentQueue(ILogger logger, IModule sourceModulem)
    {
      internalLogger = logger;
      ownerModule = sourceModulem;
    }

    public int Count => ((ICollection)Implementation).Count;

    public object SyncRoot => ((ICollection)Implementation).SyncRoot;

    public bool IsSynchronized => ((ICollection)Implementation).IsSynchronized;

    public void CopyTo(MessageDataItem[] array, int index)
    {
      ((IProducerConsumerCollection<MessageDataItem>)Implementation).CopyTo(array, index);
    }

    public void CopyTo(Array array, int index)
    {
      ((ICollection)Implementation).CopyTo(array, index);
    }

    public IEnumerator<MessageDataItem> GetEnumerator()
    {
      return ((IEnumerable<MessageDataItem>)Implementation).GetEnumerator();
    }

    public MessageDataItem[] ToArray()
    {
      return ((IProducerConsumerCollection<MessageDataItem>)Implementation).ToArray();
    }

    public bool TryAdd(MessageDataItem item)
    {
      if (Implementation.Count > 10000)
        return false;
      return ((IProducerConsumerCollection<MessageDataItem>)Implementation).TryAdd(item);
    }

    public bool TryTake(out MessageDataItem item)
    {
      return ((IProducerConsumerCollection<MessageDataItem>)Implementation).TryTake(out item);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return ((IEnumerable)Implementation).GetEnumerator();
    }
  }
}
