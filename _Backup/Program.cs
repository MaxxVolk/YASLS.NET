using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using YASLS.Configuration;
using YASLS.SDK.Library;

namespace MainApp
{
  class Program
  {
    static void Main(string[] args)
    {
      ServerConfiguration serverConfiguration = JsonConvert.DeserializeObject<ServerConfiguration>(File.ReadAllText("ServerConfig.json"));

      YASLServer server = new YASLServer(serverConfiguration);
      server.Start();

      Console.ReadLine();

      server.Stop();


      //RootConfiguration Cfg = JsonConvert.DeserializeObject<RootConfiguration>(File.ReadAllText("ServerConfig.json"));
      //Cfg.Bind();

      //foreach (NetworkListener listener in Cfg.Inputs.NetworkListeners.Select(x => x.Value))
      //  listener.Start();
      //foreach (YASLSQueue queue in Cfg.Queues.Select(q => q.Value))
      //  queue.Start();

      //Console.ReadLine();

      //foreach (NetworkListener listener in Cfg.Inputs.NetworkListeners.Select(x => x.Value))
      //  listener.Stop();
      //foreach (YASLSQueue queue in Cfg.Queues.Select(q => q.Value))
      //  queue.Stop();
    }

  }

  public class YASLServer
  {
    protected Dictionary<string, Tuple<MasterQueue, Thread>> masterQueues = new Dictionary<string, Tuple<MasterQueue, Thread>>();
    protected Dictionary<string, Tuple<IInputModule, Thread>> inputModules = new Dictionary<string, Tuple<IInputModule, Thread>>();

    protected CancellationTokenSource TokenSource;

    public YASLServer(ServerConfiguration serverConfiguration)
    {
      TokenSource = new CancellationTokenSource();

      // create inputs
      foreach (KeyValuePair<string, QueueDefinition> queueCfg in serverConfiguration.Queues)
      {
        MasterQueue newQueue = new MasterQueue(TokenSource.Token);
        masterQueues.Add(queueCfg.Key, new Tuple<MasterQueue, Thread>(newQueue, new Thread(newQueue.GetWorker())));
      }

      // create queues
      foreach (KeyValuePair<string, InputDefinition> inputCfg in serverConfiguration.Inputs)
      {
        InputDefinition inputDef = inputCfg.Value;
        Type inputModuleType = GetModuleAssembly(inputDef).GetType(inputDef.ManagedTypeName);
        IInputModule newModule = (IInputModule)Activator.CreateInstance(inputModuleType);
        newModule.Initialize(inputDef.ConfigurationJSON, TokenSource.Token, masterQueues.Where(z => serverConfiguration.Queues.Where(x => x.Value.Inputs.Contains(inputCfg.Key)).Select(y => y.Key).Contains(z.Key)).Select(t => t.Value.Item1));
        inputModules.Add(inputCfg.Key, new Tuple<IInputModule, Thread>(newModule, new Thread(newModule.GetWorker())));
      }


    }

    public void Start()
    {
      // start inputs
      foreach (Tuple<IInputModule, Thread> inputModule in inputModules.Values)
      {
        inputModule.Item2.Start();
      }

      // start queues
      foreach (Tuple<MasterQueue, Thread> masterQueues in masterQueues.Values)
      {
        masterQueues.Item2.Start();
      }
    }

    public void Stop()
    {
      TokenSource.Cancel();
      foreach (Tuple<IInputModule, Thread> inputModule in inputModules.Values)
      {
        inputModule.Item2.Join();
        inputModule.Item1.Destroy();
      }
      foreach (Tuple<MasterQueue, Thread> masterQueues in masterQueues.Values)
      {
        masterQueues.Item2.Join();
      }
    }

    protected Assembly GetModuleAssembly(InputDefinition inputDef)
    {
      if (string.IsNullOrWhiteSpace(inputDef.Assembly))
        return Assembly.GetExecutingAssembly();
      throw new NotImplementedException();
    }
  }

  public class MasterQueue : IQueueModule
  {
    protected ConcurrentQueue<MessageDataItem> Messages = new ConcurrentQueue<MessageDataItem>();
    protected CancellationToken token;

    public MasterQueue(CancellationToken cancellationToken)
    {
      token = cancellationToken;
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
          Console.WriteLine($"Received at {newMessage.GetAttributeAsDateTime("ReciveTimestamp"):yyyy-MM-dd HH:mm:ss} from {newMessage.GetAttributeAsString("SenderIP")} via {newMessage.GetAttributeAsString("Channel")}: {newMessage.Message}");
          continue; // sleep only if the queue is empty
        }
        else
        {
          Thread.Sleep(5);
        }
      }
    }

    public ThreadStart GetWorker()
    {
      return new ThreadStart(WorkerProc);
    }
  }
}
