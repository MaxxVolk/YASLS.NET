using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using YASLS.SDK.Library;

namespace YASLS.NETServer.Core
{
  /// <summary>
  /// Server-provided interface to use within any module, which needs an internal queue.
  /// </summary>
  public interface IQueueFactory
  {
    /// <summary>
    /// Creates a new queue for the source module.
    /// </summary>
    /// <param name="sourceModule"></param>
    /// <returns>An object reference implementing <seealso cref="IMessageQueue"/> interface.</returns>
    //IMessageQueue GetMessageQueue(IModule sourceModule);
    IProducerConsumerCollection<MessageDataItem> GetMessageQueue(IModule sourceModule);
  }
}
