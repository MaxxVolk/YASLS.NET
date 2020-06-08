using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YASLS.SDK.Library;

namespace YASLS.Standard.Output
{
  public class ConsoleOutput : ModuleBase, IOutputModule
  {
    protected readonly Guid moduleId = Guid.Parse("{A72FFDB6-F3AF-433F-9FE2-D1D23DA00281}");
    protected CancellationToken token;
    protected ILogger logger = null;
    protected IHealthReporter healthReporter = null;
    protected MessageReceiver MessageReceiver;

    #region IOutputModule Implementation
    public override void LoadConfiguration(JObject configuration)
    {
    }

    #endregion

    #region IThreadModule Implementation
    public ThreadStart GetWorker(CancellationToken cancellationToken)
    {
      token = cancellationToken;
      return new ThreadStart(WorkerProc);
    }

    public void Initialize() { }

    public void Destroy() { }
    #endregion

    private void WorkerProc()
    {
      while (true)
      {
        if (token.IsCancellationRequested)
          break;
        try
        {
          if (MessageReceiver.Invoke(out MessageDataItem newMessage))
          {
            Console.WriteLine(newMessage.Message);
            foreach (string attrName in newMessage.GetAttributeNames)
              Console.WriteLine($"{attrName:30}: {newMessage.GetAttributeAsVariant(attrName)}");
            continue;
          }
          else
          {
            Thread.Sleep(DefaultIdleDelay);
          }
        }
        catch (Exception e)
        {
          logger?.LogEvent(this, Severity.Error, "ConsoleWrite", "Unknown exception in Console Output Module.", e);
          continue;
        }
      }
    }

    #region IModule Implementation
    public override string GetModuleDisplayName() => "Console Output Module";

    public override Guid GetModuleId() => moduleId;

    public void SetMessageReceiver(MessageReceiver whereGetMessages)
    {
      MessageReceiver = whereGetMessages;
    }
    #endregion
  }
}
