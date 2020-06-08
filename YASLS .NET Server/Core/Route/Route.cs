using Newtonsoft.Json.Linq;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using YASLS.NETServer.Configuration;
using YASLS.SDK.Library;

namespace YASLS.NETServer.Core
{
  class Route : IModule
  {
    // Route internals
    protected YASLServer Server;
    protected string RouteKey;
    protected readonly Dictionary<string, Filter> Filters = new Dictionary<string, Filter>();
    protected Filter[] FilterRuntime;

    // Configuration
    protected bool ParallelFilters = true;

    // Flow control
    protected Thread RunnerThread;
    public ManualResetEvent WorkCompleted { get; protected set; }
    protected CancellationTokenSource CancellationTokenSource = null;
    protected CancellationToken CancellationToken = CancellationToken.None;
    protected bool IsConfigurationReloadRequested = false;

    public IProducerConsumerCollection<MessageDataItem> AttachedQueue { get; protected set; }

    public Route(string routeKey, RouteDefinition routeDefinition, Dictionary<string, OutputModuleWrapper> outputs, YASLServer server)
    {
      Server = server;
      RouteKey = routeKey;
      AttachedQueue = Server.QueueFactory.GetMessageQueue(this);
      WorkCompleted = new ManualResetEvent(false);

      foreach(KeyValuePair<string, FilterDefinition> filterCfg in routeDefinition.Filters)
      {
        Filters.Add(filterCfg.Key, new Filter(filterCfg.Key, filterCfg.Value, outputs, server));
      }
      FilterRuntime = Filters.Values.ToArray();
    }

    private void Runner()
    {
      CancellationTokenSource?.Dispose();
      CancellationTokenSource = new CancellationTokenSource();
      CancellationToken = CancellationTokenSource.Token;
      MessageDataItem message = null;
      while (true)
      {
        if (SpinWait.SpinUntil(()=> AttachedQueue.TryTake(out message), 1))
        {
          ProcessMessage(message);
          // assume there are more messages
          // NB! No need to test CancellationToken in this loop, because it's a kind of attempt to drain the queue. If not achieved in-time, 
          // will be terminated forcibly.
          while (AttachedQueue.TryTake(out message))
            ProcessMessage(message);
        }
        // no messages left
        if (CancellationToken.IsCancellationRequested)
          break;
      }

      // All done, but let's drain the queue -- if anything left it will never be completed
      while (AttachedQueue.Count > 0)
      {
        Thread.Sleep(5);
      }
      WorkCompleted.Set();
    }

    private void ProcessMessage(MessageDataItem message)
    {
      for (int f = 0; f < FilterRuntime.Length; f++)
        if (FilterRuntime[f].ProcessMessageShouldStop(message))
          break;
    }

    public void Start()
    {
      RunnerThread = new Thread(Runner);
      RunnerThread.Start();
    }

    public void Stop()
    {
      CancellationTokenSource.Cancel();
    }

    public void Abort()
    {
      RunnerThread.Abort();
    }

    #region IModule Implementation
    protected readonly Guid moduleId = Guid.Parse("{D0B57AAE-E862-451B-99C3-4E7BAC14FC01}");

    public string GetModuleDisplayName() => "YASLS .NET Server Route";

    public Guid GetModuleId() => moduleId;

    public string GetModuleName() => GetType().FullName;

    public string GetModuleVendor() => "YASLS";

    public Version GetModuleVersion() => Assembly.GetAssembly(GetType()).GetName().Version;

    public void LoadConfiguration(JObject configuration)
    {
      // never used
    }
    #endregion
  }
}
