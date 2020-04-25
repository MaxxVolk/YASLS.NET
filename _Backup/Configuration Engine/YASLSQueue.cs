using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YASLS.Configuration
{
  public class YASLSQueue : ConfigurationPrincipalBase
  {
    [JsonProperty]
    public List<string> Inputs { get; set; }

    [JsonIgnore]
    protected ConcurrentQueue<MessageMetaInfo> Messages = new ConcurrentQueue<MessageMetaInfo>();

    [JsonIgnore]
    protected CancellationToken cancellationToken;

    [JsonIgnore]
    protected CancellationTokenSource tokenSource;

    [JsonIgnore]
    protected List<Tuple<Route, Thread>> routerWorkers = null;
    Thread workerThread;

    public void Enqueue(MessageMetaInfo message)
    {
      Messages.Enqueue(message);
    }

    public void Start()
    {
      workerThread = new Thread(WorkerProc);
      tokenSource = new CancellationTokenSource();
      cancellationToken = tokenSource.Token;
      workerThread.Start();
    }

    protected void WorkerProc()
    {
      while (true)
      {
        if (cancellationToken.IsCancellationRequested)
          break;
        if (Messages.TryDequeue(out MessageMetaInfo newMessage))
        {
          Console.WriteLine($"Received at {((DateTime)newMessage.MetaData["ReciveTimestamp"]).ToString("yyyy-MM-dd HH:mm:ss")} from {newMessage.MetaData["SenderIP"].ToString()} via {newMessage.MetaData["Channel"].ToString()}: {newMessage.RawMessage}");
          continue; // sleep only if the queue is empty
        }
        else
        {
          Thread.Sleep(5);
        }
      }
    }

    public void Stop()
    {
      tokenSource.Cancel();
      workerThread.Join();
    }

    public void RegisterRoute(Route route)
    {

    }
  }

  public class MessageMetaInfo
  {
    public string RawMessage;
    public Dictionary<string, object> MetaData = new Dictionary<string, object>();
  }
}
