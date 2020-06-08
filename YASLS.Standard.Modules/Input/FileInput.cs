using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using YASLS.SDK.Library;

namespace YASLS.Standard.Input
{
  class FileInput : ModuleBase, IInputModule
  {
    protected CancellationToken token;
    protected readonly Guid moduleId = Guid.Parse("{C4017BBE-0A88-4A3E-A719-9F4EC3878792}");
    protected MessageSender MessageSender;

    public override void LoadConfiguration(JObject configuration)
    {
    }

    protected void WorkerProc()
    {
      string[] allLines = File.ReadAllLines(@"C:\Temp\etc\storage.txt");
      while (true)
      {
        Thread.Sleep(50);
        if (token.IsCancellationRequested)
          break;

        foreach (string line in allLines)
        {
          MessageDataItem message = new MessageDataItem(line);
          message.AddAttribute("ReciveTimestamp", DateTime.Now);

          while (!MessageSender.Invoke(message, true))
          {
            Thread.Sleep(1);
          }

          if (token.IsCancellationRequested)
            break;
        }
      }
    }

    public ThreadStart GetWorker(CancellationToken cancellationToken)
    {
      token = cancellationToken;
      return new ThreadStart(WorkerProc);
    }

    public void Initialize()
    {

    }

    public void Destroy()
    {
      
    }

    #region IModule Implementation
    public override string GetModuleDisplayName() => "File Input Module";

    public override Guid GetModuleId() => moduleId;

    public void SetMessageSender(MessageSender whereToSendMessages)
    {
      MessageSender = whereToSendMessages;
    }
    #endregion
  }
}
