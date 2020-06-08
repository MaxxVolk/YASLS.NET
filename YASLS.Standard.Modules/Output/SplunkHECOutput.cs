using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YASLS.SDK.Library;

namespace YASLS.Standard.Output
{
  class SplunkHECOutput : ModuleBase, IOutputModule
  {
    protected readonly Guid moduleId = Guid.Parse("{5C4F9283-326F-468F-9092-4559E83DA9D3}");
    protected CancellationToken token;
    protected MessageReceiver MessageReceiver;
    protected int maxBufferSize = 16000;
    protected int maxBatchSize = 50;

    protected SplunkHECOutputConfiguration outputConfiguration;

    public void Destroy() { }

    #region IModule Implementation
    public override string GetModuleDisplayName() => "Splunk HTTP Event Collector Output Module";

    public override Guid GetModuleId() => moduleId;
    #endregion

    public ThreadStart GetWorker(CancellationToken cancellationToken)
    {
      token = cancellationToken;
      return new ThreadStart(WorkerProc);
    }

    private void WorkerProc()
    {
      DateTime start = DateTime.Now;
      int counter = 0;
      int messageBatchCounter = 0;
      StringBuilder outputBuffer = new StringBuilder(maxBufferSize + maxBufferSize / 10); // 110%

      void FlushOutputBuffer(bool lastSubmission)
      {
        if (outputBuffer.Length == 0)
          return;
        bool sent = false;
        while (!sent)
          try
          {
            if (token.IsCancellationRequested && !lastSubmission)
              break;
            Task submitTask = SubmitHECEvent(outputBuffer);
            if (lastSubmission)
              submitTask.Wait(5 * 1000); // 5 seconds
            else
              submitTask.Wait(token);
            sent = true;
            break;
          }
          catch (Exception e)
          {
            sent = false;
            ServerLogger?.LogEvent(this, Severity.Warning, "SplunkHEC", "Failed to send request.", e);
            if (lastSubmission)
              break;
            continue;
          }
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
          FlushOutputBuffer(lastSubmission: true);
          break;
        }
        if (MessageReceiver.Invoke(out MessageDataItem newMessage))
          try
          {
            messageBatchCounter++;
            // root object & meta
            JObject hecRequestBody = new JObject
            {
              { "time", GetEPOCHTime(newMessage) },
              { "host", GetHost(newMessage) },
              { "source", GetSource(newMessage) },
              { "sourcetype", GetSourcetype(newMessage) },
              { "index", GetIndex(newMessage) }
            };
            // event other attributes
            JObject whereToAddAttributes;
            if (outputConfiguration.UseFields)
            {
              whereToAddAttributes = new JObject();
              hecRequestBody.Add("event", newMessage.Message);
            }
            else
              whereToAddAttributes = new JObject() { { "Message", newMessage.Message } };
            foreach (string attrName in newMessage.GetAttributeNames)
            {
              Variant attrValue = newMessage.GetAttributeAsVariant(attrName);
              switch (attrValue.Type)
              {
                case VariantType.Boolean:
                  whereToAddAttributes.Add(attrName, attrValue.BooleanValue);
                  break;
                case VariantType.DateTime:
                  whereToAddAttributes.Add(attrName, attrValue.DateTimeValue);
                  break;
                case VariantType.Float:
                  whereToAddAttributes.Add(attrName, attrValue.FloatValue);
                  break;
                case VariantType.Int:
                  whereToAddAttributes.Add(attrName, attrValue.IntValue);
                  break;
                case VariantType.String:
                  whereToAddAttributes.Add(attrName, attrValue.StringValue);
                  break;
              }
            }
            if (outputConfiguration.UseFields)
              hecRequestBody.Add("fields", whereToAddAttributes);
            else
              hecRequestBody.Add("event", whereToAddAttributes);

            outputBuffer.AppendLine(hecRequestBody.ToString());

            if (messageBatchCounter >= maxBatchSize || outputBuffer.Length >= maxBufferSize)
              FlushOutputBuffer(lastSubmission: false);

            // report performance
            counter++;
            if (counter > 100000)
            {
              double msgps = counter / DateTime.Now.Subtract(start).TotalSeconds;
              Console.WriteLine($"Rate: {msgps:N2} msg/sec.");
              ServerHealthReporter.SetPerformanceCounter(this, null, "MsgPerSecond", msgps);
              start = DateTime.Now;
              counter = 0;
            }
          }
          catch (Exception e)
          {
            ServerLogger?.LogEvent(this, Severity.Warning, "SplunkHEC", "Failed to create request.", e);
          }
        else
        {
          FlushOutputBuffer(lastSubmission: false);

          Thread.Sleep(DefaultIdleDelay);
        }
      }
    }

    private async Task SubmitHECEvent(StringBuilder body)
    {
      HttpWebRequest Request = WebRequest.CreateHttp(outputConfiguration.URL);
      Request.Method = "POST";
      Request.ContentType = "application/json";
      Request.Accept = "application/json";
      Request.Headers.Add("Authorization", $"Splunk {outputConfiguration.Token}");
      UTF8Encoding encoding = new UTF8Encoding();
      byte[] byteBody = encoding.GetBytes(body.ToString());
      Request.ContentLength = byteBody.Length;
      using (Stream dataStream = Request.GetRequestStream())
        dataStream.Write(byteBody, 0, byteBody.Length);

      using (WebResponse Response = await Request.GetResponseAsync())
      {
        using (Stream ResponseStream = Response.GetResponseStream())
        {
          using (StreamReader Reader = new StreamReader(ResponseStream, Encoding.UTF8))
          {
            string result = await Reader.ReadToEndAsync();
            Console.WriteLine(result);
            if (!(JToken.Parse(result)["text"].ToString() == "Success"))
              throw new Exception($"Splunk HEC returned non-success.");
          }
        }
      }
    }

    private string GetIndex(MessageDataItem msg)
    {
      try
      {
        return msg.GetAttributeAsString(outputConfiguration.FieldMappings.IndexAttribute);
      }
      catch
      {
        if (outputConfiguration.EventMetadataDefaults.Index != null)
          return outputConfiguration.EventMetadataDefaults.Index;

        ServerLogger?.LogEvent(this, Severity.Warning, "SplunkHEC", "Failed to get index from message and no default index set, using main index instead.");
        return "main";
      }
    }

    private string GetSourcetype(MessageDataItem msg)
    {
      try
      {
        return msg.GetAttributeAsString(outputConfiguration.FieldMappings.SourcetypeAttribute);
      }
      catch
      {
        if (outputConfiguration.EventMetadataDefaults.Sourcetype != null)
          return outputConfiguration.EventMetadataDefaults.Sourcetype;

        ServerLogger?.LogEvent(this, Severity.Warning, "SplunkHEC", "Failed to get sourcetype from message and no default sourcetype set, using _json instead.");
        return "_json";
      }
    }

    private string GetSource(MessageDataItem msg)
    {
      try
      {
        return msg.GetAttributeAsString(outputConfiguration.FieldMappings.SourceAttribute);
      }
      catch
      {
        if (outputConfiguration.EventMetadataDefaults.Source != null)
          return outputConfiguration.EventMetadataDefaults.Source;

        ServerLogger?.LogEvent(this, Severity.Warning, "SplunkHEC", "Failed to get source from message and no default source set, using none instead.");
        return "none";
      }
    }

    private string GetHost(MessageDataItem msg)
    {
      try
      {
        return msg.GetAttributeAsString(outputConfiguration.FieldMappings.HostAttribute);
      }
      catch
      {
        if (outputConfiguration.EventMetadataDefaults.Host != null)
          return outputConfiguration.EventMetadataDefaults.Host;

        ServerLogger?.LogEvent(this, Severity.Warning, "SplunkHEC", "Failed to get host from message and no default host, using localhost instead.");
        return "localhost";
      }
    }

    private readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private string GetEPOCHTime(MessageDataItem msg)
    {
      if (outputConfiguration.EventMetadataDefaults.UseCurrentTime)
        return DateTime.UtcNow.Subtract(epoch).TotalSeconds.ToString("F3");
      try
      {
        DateTime msgTime = msg.GetAttributeAsDateTime(outputConfiguration.FieldMappings.TimeAttribute);
        if (msgTime.Kind == DateTimeKind.Local)
          msgTime = msgTime.ToUniversalTime();
        return msgTime.Subtract(epoch).TotalSeconds.ToString("F3");
      }
      catch
      {
        ServerLogger?.LogEvent(this, Severity.Warning, "SplunkHEC", "Failed to get time from message, using current time instead.");
        return DateTime.Now.Subtract(epoch).TotalSeconds.ToString("F3");
      }
    }

    public override void LoadConfiguration(JObject configuration)
    {
      outputConfiguration = configuration.ToObject<SplunkHECOutputConfiguration>();
    }

    public void SetMessageReceiver(MessageReceiver whereGetMessages)
    {
      MessageReceiver = whereGetMessages;
    }

    public void Initialize()
    {
      
    }
  }

  public class SplunkHECOutputConfiguration
  {
    [JsonProperty]
    public string URL { get; set; }

    [JsonProperty]
    public string Token { get; set; }

    [JsonProperty]
    public bool UseFields { get; set; }

    [JsonProperty]
    public EventMetadataDefaults EventMetadataDefaults { get; set; }

    [JsonProperty]
    public EventMetadataMappings FieldMappings { get; set; }
  }

  public class EventMetadataMappings
  {
    [JsonProperty]
    public string HostAttribute { get; set; }

    [JsonProperty]
    public string TimeAttribute { get; set; }

    [JsonProperty]
    public string SourceAttribute { get; set; }

    [JsonProperty]
    public string SourcetypeAttribute { get; set; }

    [JsonProperty]
    public string IndexAttribute { get; set; }
  }

  public class EventMetadataDefaults
  {
    [JsonProperty]
    public string Host { get; set; }

    [JsonProperty]
    public string Source { get; set; }

    [JsonProperty]
    public string Sourcetype { get; set; }

    [JsonProperty]
    public string Index { get; set; }

    [JsonProperty]
    public bool UseCurrentTime { get; set; } = false;
  }
}
