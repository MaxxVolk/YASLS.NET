using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using YASLS.Configuration;
using YASLS.SDK.Library;

namespace YASLS
{
  public class Route: IModule
  {
    protected Dictionary<string, Filter> Filters = new Dictionary<string, Filter>();
    protected readonly Guid moduleId = Guid.Parse("{E1A7E7CA-F1B7-4623-942A-05C0D7C0F667}");
    protected IMessageQueue Messages;
    protected CancellationToken token;
    protected ILogger eventLogger;
    protected IHealthReporter healthReporter;
    protected int maxMessageCount = 100;

    public Route(RouteDefinition cfg, CancellationToken cancellationToken, ILogger logger, IHealthReporter healthReporter, IQueueFactory queueFactory)
    {
      token = cancellationToken;
      eventLogger = logger;
      this.healthReporter = healthReporter;
      foreach (KeyValuePair<string, FilterDefinition> filterDef in cfg.Filters)
      {
        Filters.Add(filterDef.Key, new Filter(filterDef.Value, logger));
      }
      Messages = queueFactory.GetMessageQueue(this);
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
          foreach (KeyValuePair<string, Filter> filterRef in Filters)
            if (filterRef.Value.ProcessMessageShouldStop(newMessage))
              break;
          continue; // sleep only if the queue is empty
        }
        else
        {
          Thread.Sleep(5);
        }
      }
    }

    public ThreadStart GetWorker() => new ThreadStart(WorkerProc);

    public string GetModuleName() => GetType().FullName;

    public string GetModuleDisplayName() => "Main Route Module";

    public string GetModuleVendor() => "Core YASLS";

    public Guid GetModuleId() => moduleId;
  }

  public class Filter
  {
    protected bool AllMessages = false;
    protected bool StopIfMatched = false;
    protected IFilterModule FilterModule;
    protected RegExpFilter RegExp;
    protected IParserModule ParserModule;
    protected Dictionary<string, string> Attributes = new Dictionary<string, string>();
    protected List<IOutputModule> OutputModules = new List<IOutputModule>();
    protected ILogger Logger;

    public Filter (FilterDefinition filterDefinition, ILogger logger)
    {
      AllMessages = filterDefinition.AllMessages;
      StopIfMatched = filterDefinition.StopIfMatched;
      FilterModule = filterDefinition.FilterModule;
      RegExp = filterDefinition.RegExp?.ToObject<RegExpFilter>();
      ParserModule = filterDefinition.ParserModule;
      Logger = logger;
      if (filterDefinition.OutputModules != null && filterDefinition.OutputModules.Count > 0)
        OutputModules.AddRange(filterDefinition.OutputModules);
      if (filterDefinition.Attributes != null && filterDefinition.Attributes.Count > 0)
        foreach (KeyValuePair<string, string> attr in filterDefinition.Attributes)
          Attributes.Add(attr.Key, attr.Value);
    }

    public bool ProcessMessageShouldStop(MessageDataItem message)
    {
      try
      {
        bool filterMatch = AllMessages;
        if (!filterMatch)
          filterMatch = RegExp?.IsMatch(message) ?? false;
        if (!filterMatch && FilterModule != null)
          filterMatch = FilterModule.IsMatch(message);
        if (filterMatch)
        {
          MessageDataItem sendingMessage = message.Clone();
          foreach (KeyValuePair<string, string> attr in Attributes)
            sendingMessage.AddAttribute(attr.Key, attr.Value);
          MessageDataItem parsedMessage = null;
          try
          {
            parsedMessage = ParserModule?.Parse(sendingMessage);
          }
          catch (Exception e)
          {
            Logger?.LogEvent(ParserModule, Severity.Error, "Parsing", "Failed to parse message.", e);
            parsedMessage = null;
          }
          foreach (IOutputModule module in  OutputModules)
          {
            module.Enqueue(parsedMessage ?? sendingMessage);
          }
          if (StopIfMatched) // && filterMatch
            return true; // shall stop
          else
            return false; // still proceed
        }
        else
          return false;
      }
      catch (Exception e)
      {
        Logger?.LogEvent(Guid.Empty, Severity.Error, "FilterAction", "Exception in filter", e);
        return false;
      }
      
    }
  }


  public class RegExpFilter
  {
    [JsonProperty("InputAttribute")]
    public string InputAttribute { get; set; }

    [JsonProperty("Conditions")]
    public RegExpFilterCondition Conditions { get; set; }

    public bool IsMatch(MessageDataItem message)
    {
      {
        if (string.IsNullOrEmpty(InputAttribute))
          return Conditions?.Evaluate(message.Message) ?? false;
        else
        {
          if (message.AttributeExists(InputAttribute))
            return Conditions?.Evaluate(message.GetAttributeAsVariant(InputAttribute).ToString()) ?? false;
        }
        return false;
      }
    }
  }

  public class RegExpFilterCondition
  {
    [JsonProperty("Options", NullValueHandling = NullValueHandling.Ignore)]
    public int Options { get; set; } = 0;

    [JsonProperty("And", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> And { get; set; }

    [JsonProperty("Or", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> Or { get; set; }

    [JsonProperty("NotAll", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> NotAll { get; set; }

    [JsonProperty("NotAny", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> NotAny { get; set; }

    public bool Evaluate(string rawMessage)
    {
      if (rawMessage == null)
        return false;

      bool AndResult = true, OrResult = true, NotAllResult = true, NotAnyResult = true;
      if (And != null && And.Count > 0)
        AndResult = And.All(x => Regex.IsMatch(rawMessage, x, (RegexOptions)Options));
      if (Or != null && Or.Count > 0)
        OrResult = Or.Any(x => Regex.IsMatch(rawMessage, x, (RegexOptions)Options));
      if (NotAll != null && NotAll.Count > 0)
        NotAllResult = !NotAll.All(x => Regex.IsMatch(rawMessage, x, (RegexOptions)Options));
      if (NotAny != null && NotAny.Count > 0)
        NotAnyResult = !NotAny.Any(x => Regex.IsMatch(rawMessage, x, (RegexOptions)Options));

      return AndResult && OrResult && NotAllResult && NotAnyResult;
    }
  }
}
