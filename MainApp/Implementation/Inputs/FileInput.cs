using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using YASLS.SDK.Library;

namespace YASLS
{
  class FileInput : IInputModule, IServerBind
  {
    protected CancellationToken token;
    protected List<IQueueModule> OutputQueues = new List<IQueueModule>();
    protected Dictionary<string, string> Attributes = new Dictionary<string, string>();
    protected readonly Guid moduleId = Guid.Parse("{C4017BBE-0A88-4A3E-A719-9F4EC3878792}");
    protected ILogger logger = null;
    protected IHealthReporter healthReporter = null;

    public void Initialize(JObject configuration, CancellationToken cancellationToken, Dictionary<string, string> attributes, IEnumerable<IQueueModule> queue)
    {

      if (attributes != null && attributes.Count > 0)
        foreach (KeyValuePair<string, string> origAttr in attributes)
          Attributes.Add(origAttr.Key, origAttr.Value);
      token = cancellationToken;
      OutputQueues.AddRange(queue);
    }

    protected void WorkerProc()
    {
      foreach(var line in  File.ReadLines(@"C:\Temp\etc\test input.txt"))
      {
        MessageDataItem message = new MessageDataItem(line);
        foreach (KeyValuePair<string, string> extraAttr in Attributes)
          message.AddAttribute(extraAttr.Key, extraAttr.Value);
        message.AddAttribute("ReciveTimestamp", DateTime.Now);

        foreach (IQueueModule queue in OutputQueues)
          queue.Enqueue(message);

        Thread.Sleep(2);
        if (token.IsCancellationRequested)
          break;
      }

      while (true)
      {
        Thread.Sleep(50);
        if (token.IsCancellationRequested)
          break;
      }
    }

    public ThreadStart GetWorker() => new ThreadStart(WorkerProc);

    public void Destroy()
    {
      
    }

    public string GetModuleName() => GetType().FullName;

    public string GetModuleDisplayName() => "File Input Module";

    public string GetModuleVendor() => "Core YASLS";

    public Guid GetModuleId() => moduleId;

    public void RegisterServices(ILogger logger, IHealthReporter healthReporter, IQueueFactory factory)
    {
      this.logger = logger;
      this.healthReporter = healthReporter;
    }
  }
}
