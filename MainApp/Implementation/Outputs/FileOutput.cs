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
  public enum FileOutputOutputMode { JSON, CSV, TSV, XML }

  class FileOutput : IOutputModule, IServerBind
  {
    protected readonly Guid moduleId = Guid.Parse("{A8AB8740-7D5A-4628-98A5-5221F7D04C06}");
    protected CancellationToken token;
    protected ILogger logger = null;
    protected IHealthReporter healthReporter = null;
    protected IMessageQueue Messages;
    protected int maxMessageCount = 100;
    protected FileStream fileStream;
    protected StreamWriter streamWriter;
    protected int maxBufferSize = 32000;
    private int counter = 0;

    protected FileOutputConfiguration outputConfiguration;

    public void Destroy() 
    {
      try
      {
        fileStream?.Flush();
        fileStream.Dispose();
      }
      catch { }
    }

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

    public ThreadStart GetWorker()
    {
      switch (outputConfiguration.Mode)
      {
        case FileOutputOutputMode.JSON:
          return new ThreadStart(WorkerProcJSON);
        case FileOutputOutputMode.CSV:
          separator = ',';
          return new ThreadStart(WorkerProcCTSV);
        case FileOutputOutputMode.TSV:
          separator = '\t';
          return new ThreadStart(WorkerProcCTSV);
        case FileOutputOutputMode.XML:
          return new ThreadStart(WorkerProcXML);
      }
      throw new Exception("This must not happen!!!");
    }

    private void WorkerProcXML()
    {
      throw new NotImplementedException();
    }

    private char separator = ',';
    private void WorkerProcCTSV()
    {
      DateTime start = DateTime.Now;
      while (true)
      {
        if (token.IsCancellationRequested)
          break;
        if (Messages.TryDequeue(out MessageDataItem newMessage))
        {
          if (counter == 0)
            start = DateTime.Now;
          Console.WriteLine(counter++);
          if (!string.IsNullOrEmpty(newMessage.Message))
            streamWriter.Write(QuoteString(newMessage.Message));
          foreach (string attrName in newMessage.GetAttributeNames)
          {
            streamWriter.Write(separator);
            Variant attrValue = newMessage.GetAttributeAsVariant(attrName);
            switch (attrValue.Type)
            {
              case VariantType.Boolean:
                streamWriter.Write(QuoteString(attrValue.BooleanValue.ToString()));
                break;
              case VariantType.DateTime:
                streamWriter.Write(QuoteString(attrValue.DateTimeValue.ToString("o")));
                break;
              case VariantType.Float:
                streamWriter.Write(QuoteString(attrValue.FloatValue.ToString()));
                break;
              case VariantType.Int:
                streamWriter.Write(QuoteString(attrValue.IntValue.ToString()));
                break;
              case VariantType.String:
                streamWriter.Write(QuoteString(attrValue.StringValue ?? ""));
                break;
            }
          }
          streamWriter.WriteLine();

          continue; // sleep only if the queue is empty
        }

        Thread.Sleep(5);
        // flush any outstanding streams
        Console.WriteLine($"Rate: {counter / DateTime.Now.Subtract(start).TotalSeconds:N2} msg/sec.");
      }
    }

    private void WorkerProcCTSV_old()
    {

      while (true)
      {
        if (token.IsCancellationRequested)
          break;
        if (Messages.TryDequeue(out MessageDataItem newMessage))
        {
          Console.WriteLine(counter++);
          if (!string.IsNullOrEmpty(newMessage.Message))
            streamWriter.Write(QuoteString(newMessage.Message));
          foreach (string attrName in newMessage.GetAttributeNames)
          {
            streamWriter.Write(separator);
            Variant attrValue = newMessage.GetAttributeAsVariant(attrName);
            switch (attrValue.Type)
            {
              case VariantType.Boolean:
                streamWriter.Write(QuoteString(attrValue.BooleanValue.ToString()));
                break;
              case VariantType.DateTime:
                streamWriter.Write(QuoteString(attrValue.DateTimeValue.ToString("o")));
                break;
              case VariantType.Float:
                streamWriter.Write(QuoteString(attrValue.FloatValue.ToString()));
                break;
              case VariantType.Int:
                streamWriter.Write(QuoteString(attrValue.IntValue.ToString()));
                break;
              case VariantType.String:
                streamWriter.Write(QuoteString(attrValue.StringValue ?? ""));
                break;
            }
          }
          streamWriter.WriteLine();

          continue; // sleep only if the queue is empty
        }

        Thread.Sleep(5);
        // flush any outstanding streams
        streamWriter.Flush();
        fileStream.Flush();
        streamWriter.Dispose(); // The StreamWriter object calls Dispose() on the provided Stream object when StreamWriter.Dispose is called.
        fileStream = new FileStream(outputConfiguration.GetFileName(), FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read | FileShare.Delete, maxBufferSize);
        streamWriter = new StreamWriter(fileStream, Encoding.UTF8);
      }
    }

    private string QuoteString(string input) => "\"" + input.Replace("\"", "\"\"") + "\"";

    private void WorkerProcJSON()
    {
      while (true)
      {
        if (token.IsCancellationRequested)
          break;
        if (Messages.TryDequeue(out MessageDataItem newMessage))
        {
          JObject outData = new JObject
          {
            { "Message", newMessage.Message ?? "" }
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
                outData.Add(attrName, attrValue.StringValue ?? "");
                break;
            }
          }
          string fullPath = outputConfiguration.GetFileName();
          File.AppendAllText(fullPath, outData.ToString(Formatting.None) + "\r\n");

          continue; // sleep only if the queue is empty
        }
        Thread.Sleep(5);
      }
    }

    public void LoadConfiguration(JObject configuration, CancellationToken cancellationToken)
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

    public void Initialize()
    {
      fileStream = new FileStream(outputConfiguration.GetFileName(), FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read | FileShare.Delete, maxBufferSize);
      streamWriter = new StreamWriter(fileStream, Encoding.UTF8);
    }
    #endregion
  }

  public class FileOutputConfiguration
  {
    [JsonProperty("Mode")]
    protected string ModeStr { get; set; }
    [JsonIgnore]
    protected FileOutputOutputMode _Mode;
    [JsonIgnore]
    protected bool ModeParsed = false;

    [JsonIgnore]
    public FileOutputOutputMode Mode
    {
      get
      {
        if (ModeParsed) return _Mode;
        _Mode = (FileOutputOutputMode)Enum.Parse(typeof(FileOutputOutputMode), ModeStr);
        ModeParsed = true;
        return _Mode;
      }
    }

    [JsonProperty("Path")]
    public string BasePath { get; set; }

    [JsonProperty]
    public FileOutputRotationConfiguration RotationSettings { get; set; }

    public string GetFileName()
    {
      return Path.Combine(BasePath, RotationSettings.FileNameTemplate); // temporary stub
    }
  }

  public class FileOutputRotationConfiguration
  {
    [JsonProperty]
    public string FileNameTemplate { get; set; }

    [JsonProperty]
    public bool Rotation { get; set; }
  }
}
