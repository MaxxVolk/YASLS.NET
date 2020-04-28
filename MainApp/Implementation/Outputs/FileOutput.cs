using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YASLS.SDK.Library;

namespace YASLS
{
  class FileOutput : IOutputModule, IServerBind
  {
    protected readonly Guid moduleId = Guid.Parse("{A8AB8740-7D5A-4628-98A5-5221F7D04C06}");
    protected CancellationToken token;
    protected ILogger logger = null;
    protected IHealthReporter healthReporter = null;
    protected IMessageQueue Messages;
    protected int maxMessageCount = 100;

    protected FileOutputConfiguration outputConfiguration;

    public void Destroy() { }

    public void Enqueue(MessageDataItem message)
    {
      Messages.Enqueue(message);
    }

    #region IModule Implementation
    public string GetModuleName() => GetType().FullName;

    public string GetModuleDisplayName() => "File Output Module";

    public string GetModuleVendor() => "Core YASLS";

    public Guid GetModuleId() => moduleId;

    public Version GetModuleVersion() => Assembly.GetAssembly(GetType()).GetName().Version;
    #endregion

    public ThreadStart GetWorker() => new ThreadStart(WorkerProc);

    private void WorkerProc()
    {
      string fullPath = Path.Combine(outputConfiguration.Path, outputConfiguration.RotationSettings.FileNameTemplate);
      while (true)
      {
        if (token.IsCancellationRequested)
          break;
        if (Messages.TryDequeue(out MessageDataItem newMessage))
        {
          switch (outputConfiguration.Mode)
          {
            case "DataItemAsJSON":
              JObject outData = new JObject
              {
                { "Message", newMessage.Message }
              };
              foreach (string attrName in newMessage.GetAttributeNames)
              {
                Variant attrValue = newMessage.GetAttributeAsVariant(attrName);
                switch (attrValue.Type)
                {
                  case VariantType.Boolean:
                    outData.Add(attrName, attrValue.BooleanValue);
                    break;
                  case VariantType.DateTime:
                    outData.Add(attrName, attrValue.DateTimeValue);
                    break;
                  case VariantType.Float:
                    outData.Add(attrName, attrValue.FloatValue);
                    break;
                  case VariantType.Int:
                    outData.Add(attrName, attrValue.IntValue);
                    break;
                  case VariantType.String:
                    outData.Add(attrName, attrValue.StringValue);
                    break;
                }
              }
              File.AppendAllText(fullPath, outData.ToString(Formatting.None) + "\r\n");
              break;
          }

          
          continue; // sleep only if the queue is empty
        }
        else
        {
          Thread.Sleep(5);
        }
      }
    }

    public void Initialize(JObject configuration, CancellationToken cancellationToken)
    {
      token = cancellationToken;
      outputConfiguration = configuration.ToObject<FileOutputConfiguration>();
    }

    #region IServerBind Implementation
    public void RegisterServices(ILogger logger, IHealthReporter healthReporter, IQueueFactory factory, IPersistentDataStore persistentStore)
    {
      this.logger = logger;
      this.healthReporter = healthReporter;
      Messages = factory.GetMessageQueue(this);
    }
    #endregion
  }

  public class FileOutputConfiguration
  {
    [JsonProperty]
    public string Mode { get; set; }

    [JsonProperty]
    public string Path { get; set; }

    [JsonProperty]
    public FileOutputRotationConfiguration RotationSettings { get; set; }
  }

  public class FileOutputRotationConfiguration
  {
    [JsonProperty]
    public string FileNameTemplate { get; set; }

    [JsonProperty]
    public bool Rotation { get; set; }
  }
}
