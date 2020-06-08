using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using YASLS.NETServer.Configuration;
using YASLS.SDK.Library;

namespace YASLS.NETServer.Core
{
  class MessageMixer : IModule
  {
    // MessageMixer internals
    protected YASLServer Server;
    protected Tuple<InputModuleWrapper, Route[]>[] Mixer;
    protected MessageDataItem[] Forwarders;

    // Flow control
    protected Thread RunnerThread;
    public ManualResetEvent WorkCompleted { get; protected set; }
    protected CancellationTokenSource CancellationTokenSource = null;
    protected CancellationToken CancellationToken = CancellationToken.None;

    public MessageMixer(Dictionary<string, InputModuleWrapper> inputs, Dictionary<string, Route> routes, Dictionary<string, RouteDefinition> routing, YASLServer server)
    {
      Server = server;
      WorkCompleted = new ManualResetEvent(false);
      List<Tuple<InputModuleWrapper, Route[]>> PreMixer = new List<Tuple<InputModuleWrapper, Route[]>>();
      foreach (KeyValuePair<string, InputModuleWrapper> input in inputs)
      {
        IEnumerable<string> assignedRouteNames = routing.Where(r => r.Value.Inputs?.Contains(input.Key) == true).Select(n=>n.Key);
        if (assignedRouteNames == null || !assignedRouteNames.Any())
          Server.Logger?.LogEvent(this, Severity.Warning, "MixerInit", $"Input {input.Key} is not used in any route. It will overflow and drop message if sent.");
        else
          PreMixer.Add(new Tuple<InputModuleWrapper, Route[]>(input.Value, (new List<Route>(routes.Where(r => assignedRouteNames.Contains(r.Key)).Select(p => p.Value))).ToArray()));
      }
      Mixer = PreMixer.ToArray();
      Forwarders = new MessageDataItem[PreMixer.Max(l => l.Item2.Length)];
    }

    private void Runner()
    {
      CancellationTokenSource = new CancellationTokenSource();
      CancellationToken = CancellationTokenSource.Token;
      SpinWait emptyCycle = new SpinWait();
      while (true)
      {
        bool anyMsg = false;
        for (int i = 0; i < Mixer.Length; i++)
        {
          MessageDataItem inboundMessage;
          if (Mixer[i].Item1.AttachedQueue.TryTake(out inboundMessage))
          {
            anyMsg = true;
            Forwarders[0] = inboundMessage;
            for (int r = 1; r < Mixer[i].Item2.Length; r++)
              Forwarders[r] = inboundMessage.Clone();
            for (int r = 0; r< Mixer[i].Item2.Length; r++)
            {
              while (!Mixer[i].Item2[r].AttachedQueue.TryAdd(Forwarders[r]))
                emptyCycle.SpinOnce();
            }
          }
        }
        if (anyMsg)
          continue;
        if (CancellationToken.IsCancellationRequested)
          break;
        emptyCycle.SpinOnce();
      }

      WorkCompleted.Set();
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
    protected readonly Guid moduleId = Guid.Parse("{CE5FFBE7-9254-4D7C-A100-47B198C2B2BD}");

    public string GetModuleDisplayName() => "YASLS .NET Server Message Mixer";

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
