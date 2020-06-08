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

namespace YASLS.Standard.Output
{
  public enum FileOutputOutputMode { JSON, CSV, TSV, XML }

  class FileOutput : ModuleBase, IOutputModule
  {
    protected readonly Guid moduleId = Guid.Parse("{A8AB8740-7D5A-4628-98A5-5221F7D04C06}");
    protected CancellationToken token;
    protected MessageReceiver MessageReceiver;
    protected int maxBufferSize = 32000;
    protected int maxBatchSize = 100;
    protected Encoding fileEncoding = Encoding.UTF8;

    protected FileOutputConfiguration outputConfiguration;

    public void Destroy() 
    {
    }

    public void Initialize()
    {
    }


    #region IModule Implementation
    public override string GetModuleDisplayName() => "File Output Module";

    public override Guid GetModuleId() => moduleId;
    #endregion

    public ThreadStart GetWorker(CancellationToken cancellationToken)
    {
      token = cancellationToken;
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
      int counter = 0;
      int messageBatchCounter = 0;
      StringBuilder outputBuffer = new StringBuilder(maxBufferSize + maxBufferSize / 10); // 110%

      void FlushOutputBuffer()
      {
        if (outputBuffer.Length == 0)
          return;
        File.AppendAllText(outputConfiguration.GetFileName(), outputBuffer.ToString(), fileEncoding);
        outputBuffer.Clear();
        messageBatchCounter = 0;
      }
      /*
       * Buffer flush rules:
       * 1. Flush if buffer size is over maxBufferSize, OR, if buffer contains mother than maxBatchSize messages
       * 2. If there is no messages in the input queue left
      */
      while (true)
      {
        if (token.IsCancellationRequested)
        {
          FlushOutputBuffer();
          break;
        }
        if (MessageReceiver.Invoke(out MessageDataItem newMessage))
        {
          messageBatchCounter++;
          if (!string.IsNullOrEmpty(newMessage.Message))
            outputBuffer.Append(QuoteString(newMessage.Message));
          foreach (string attrName in newMessage.GetAttributeNames)
          {
            outputBuffer.Append(separator);
            Variant attrValue = newMessage.GetAttributeAsVariant(attrName);
            switch (attrValue.Type)
            {
              case VariantType.Boolean:
                outputBuffer.Append(QuoteString(attrValue.BooleanValue.ToString()));
                break;
              case VariantType.DateTime:
                outputBuffer.Append(QuoteString(attrValue.DateTimeValue.ToString("o")));
                break;
              case VariantType.Float:
                outputBuffer.Append(QuoteString(attrValue.FloatValue.ToString()));
                break;
              case VariantType.Int:
                outputBuffer.Append(QuoteString(attrValue.IntValue.ToString()));
                break;
              case VariantType.String:
                outputBuffer.Append(QuoteString(attrValue.StringValue ?? ""));
                break;
            }
          }
          outputBuffer.AppendLine();

          if (messageBatchCounter >= maxBatchSize || outputBuffer.Length >= maxBufferSize)
            FlushOutputBuffer();

          // report performance
          counter++;
          if (counter > 100000)
          {
            double msgps = counter / DateTime.Now.Subtract(start).TotalSeconds;
            ServerHealthReporter.SetPerformanceCounter(this, null, "MsgPerSecond", msgps);
            start = DateTime.Now;
            counter = 0;
          }
        }
        else
        {
          // flush any outstanding data if nothing else to do
          FlushOutputBuffer();

          Thread.Sleep(DefaultIdleDelay);
        }
      }
    }

    private string QuoteString(string input) => "\"" + input.Replace("\"", "\"\"") + "\"";

    private void WorkerProcJSON()
    {
      while (true)
      {
        if (token.IsCancellationRequested)
          break;
        if (MessageReceiver.Invoke(out MessageDataItem newMessage))
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
        Thread.Sleep(DefaultIdleDelay);
      }
    }

    public override void LoadConfiguration(JObject configuration)
    {
      outputConfiguration = configuration.ToObject<FileOutputConfiguration>();
    }

    public void SetMessageReceiver(MessageReceiver whereGetMessages)
    {
      MessageReceiver = whereGetMessages;
    }
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
