using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using YASLS.SDK.Library;

namespace YASLS
{
  class RegExParser : IParserModule, IServerBind
  {
    protected readonly Guid moduleId = Guid.Parse("{977D4BAD-B613-44EA-AA39-052C09AC142D}");
    protected CancellationToken token;
    protected ILogger logger = null;
    protected IHealthReporter healthReporter = null;
    protected RegExParserConfiguration config;

    public string GetModuleDisplayName() => "Regular Expression Parser";

    public Guid GetModuleId() => moduleId;

    public string GetModuleName() => GetType().FullName;

    public string GetModuleVendor() => "Core YASLS";

    public void Initialize(JObject configuration, CancellationToken cancellationToken)
    {
      token = cancellationToken;
      config = configuration.ToObject<RegExParserConfiguration>();
    }

    public MessageDataItem Parse(MessageDataItem message)
    {
      // result -- keep it as separate vars to allow modifications and overrides
      string outMessage = null;
      Dictionary<string, Variant> outAttributes = new Dictionary<string, Variant>();
      bool AnyMatches = false;

      if (config.DefaultFieldSettings != null)
        foreach (FieldSetting attrCfg in config.DefaultFieldSettings)
          try
          {
            attrCfg.SetField(null, message, ref outMessage, outAttributes);
          }
          catch (Exception e)
          {
            logger?.LogEvent(this, Severity.Warning, "Parser", $"Failed to parse field {attrCfg.OutputAttribute ?? "<UNKNOWN>"}", e);
          }

      foreach (ParsingExpression expr in config.ParsingExpressions)
        if (string.IsNullOrEmpty(expr.MatchingRegEx) || Regex.IsMatch(message.Message, expr.MatchingRegEx))
        {
          MatchCollection matches = Regex.Matches(message.Message, expr.ParsingRegEx, expr.RegexOptions);
          if (matches.Count > 0)
          {
            AnyMatches = true;
            foreach (Match match in matches)
            {
              if (match.Success)
              {
                if (expr.FieldSettings != null)
                  foreach (FieldSetting attrCfg in expr.FieldSettings)
                    try
                    {
                      attrCfg.SetField(match.Groups, message, ref outMessage, outAttributes);
                    }
                    catch (Exception e)
                    {
                      logger?.LogEvent(this, Severity.Warning, "Parser", $"Failed to parse field {attrCfg.OutputAttribute ?? "<UNKNOWN>"}", e);
                    }
              }
            }
          }
          else
          {
            if (expr.StopIfMatched)
              logger?.LogEvent(this, Severity.Warning, "Parser", "Matching expression matched in a stopping expression, but the parsing expression didn't match.");
          }
          // exit from loop of ParsingExpressions
          if (expr.StopIfMatched)
            break;
        }

      if (AnyMatches)
      {
        MessageDataItem result = new MessageDataItem(outMessage ?? "");
        foreach (KeyValuePair<string, Variant> newAttribute in outAttributes)
          result.AddAttribute(newAttribute.Key, newAttribute.Value);
        return result;
      }
      else
      {
        if (config.PassthroughFieldSettings != null)
          foreach (FieldSetting attrCfg in config.PassthroughFieldSettings)
            try
            {
              attrCfg.SetField(null, message, ref outMessage, outAttributes);
            }
            catch (Exception e)
            {
              logger?.LogEvent(this, Severity.Warning, "Parser", $"Failed to parse field {attrCfg.OutputAttribute ?? "<UNKNOWN>"}", e);
            }

        MessageDataItem result = new MessageDataItem(outMessage ?? "");
        foreach (KeyValuePair<string, Variant> newAttribute in outAttributes)
          result.AddAttribute(newAttribute.Key, newAttribute.Value);
        return result;
        // return message.Clone();
      }
    }

    public void RegisterServices(ILogger logger, IHealthReporter healthReporter, IQueueFactory queueFactory)
    {
      this.logger = logger;
      this.healthReporter = healthReporter;
    }
  }

  [JsonObject]
  public class RegExParserConfiguration
  {
    [JsonProperty]
    public bool KeepOriginalMessageAsAttribute { get; set; }

    [JsonProperty]
    public List<ParsingExpression> ParsingExpressions { get; set; }

    [JsonProperty]
    public List<FieldSetting> DefaultFieldSettings { get; set; }

    [JsonProperty]
    public List<FieldSetting> PassthroughFieldSettings { get; set; }
  }

  [JsonObject]
  public class ParsingExpression
  {
    [JsonProperty]
    public string MatchingRegEx { get; set; }

    [JsonProperty]
    public string ParsingRegEx { get; set; }

    [JsonProperty("RegExOptions")]
    protected List<string> RegExOptionsList { get; set; }

    [JsonProperty]
    public bool StopIfMatched { get; set; }

    [JsonIgnore]
    private RegexOptions ParsedRegexOptions = RegexOptions.None;
    [JsonIgnore]
    private bool IsRegexOptionsParsed = false;
    [JsonIgnore]
    public RegexOptions RegexOptions
    {
      get
      {
        if (IsRegexOptionsParsed)
          return ParsedRegexOptions;

        if (RegExOptionsList == null || RegExOptionsList.Count == 0)
        {
          ParsedRegexOptions = RegexOptions.None;
          IsRegexOptionsParsed = true;
          return ParsedRegexOptions;
        }

        ParsedRegexOptions = RegexOptions.None;
        foreach (string option in RegExOptionsList)
          if (Enum.TryParse(option, out RegexOptions newOption))
            ParsedRegexOptions |= newOption;
        IsRegexOptionsParsed = true;
        return ParsedRegexOptions;
      }
    }

    [JsonProperty]
    public List<FieldSetting> FieldSettings { get; set; }
  }

  [JsonObject]
  public class FieldSetting
  {
    [JsonProperty]
    public FieldInputSettings Input { get; set; }

    [JsonProperty]
    public string OutputAttribute { get; set; } // null if message body

    public void SetField(GroupCollection groups, MessageDataItem inputMessage, ref string outMessage, Dictionary<string, Variant> outAttributes)
    {
      if (!string.IsNullOrEmpty(Input?.Group))
      {
        string dynamicOutputAttribute;
        if (string.IsNullOrEmpty(Input.GroupToOutputAttribute))
          dynamicOutputAttribute = OutputAttribute;
        else
          dynamicOutputAttribute = groups[Input.GroupToOutputAttribute].Value;
        switch (Input.Type)
        {
          case VariantType.Boolean:
            if (bool.TryParse(groups[Input.Group]?.Value, out bool newBoolValue))
            {
              if (string.IsNullOrEmpty(dynamicOutputAttribute))
                outMessage = newBoolValue.ToString();
              else
                outAttributes[dynamicOutputAttribute] = new Variant { BooleanValue = newBoolValue, Type = VariantType.Boolean };
            }
            return;
          case VariantType.DateTime:
            if (string.IsNullOrEmpty(Input.Format))
            {
              if (DateTime.TryParse(groups[Input.Group]?.Value, out DateTime newDateTimeValue))
                if (string.IsNullOrEmpty(dynamicOutputAttribute))
                  outMessage = newDateTimeValue.ToString();
                else
                  outAttributes[dynamicOutputAttribute] = new Variant { DateTimeValue = newDateTimeValue, Type = VariantType.DateTime };
            }
            else
            {
              if (DateTime.TryParseExact(groups[Input.Group]?.Value, Input.Format, null, DateTimeStyles.AssumeLocal, out DateTime newDateTimeValue))
                if (string.IsNullOrEmpty(dynamicOutputAttribute))
                  outMessage = newDateTimeValue.ToString();
                else
                  outAttributes[dynamicOutputAttribute] = new Variant { DateTimeValue = newDateTimeValue, Type = VariantType.DateTime };
            }
            return;
          case VariantType.Float:
            if (float.TryParse(groups[Input.Group].Value, out float newFloatValue))
              if (string.IsNullOrEmpty(dynamicOutputAttribute))
                outMessage = (newFloatValue.ToString());
              else
                outAttributes[dynamicOutputAttribute] = new Variant { FloatValue = newFloatValue, Type = VariantType.Float };
            return;
          case VariantType.Int:
            if (int.TryParse(groups[Input.Group].Value, out int newIntValue))
              if (string.IsNullOrEmpty(dynamicOutputAttribute))
                outMessage = (newIntValue.ToString());
              else
                outAttributes[dynamicOutputAttribute] = new Variant { IntValue = newIntValue, Type = VariantType.Int };
            return;
          case VariantType.String:
            if (string.IsNullOrEmpty(dynamicOutputAttribute))
              outMessage = groups[Input.Group]?.Value ?? "";
            else
              outAttributes[dynamicOutputAttribute] = new Variant { StringValue = groups[Input.Group]?.Value, Type = VariantType.String };
            return;
        }
      }
      if (Input?.Message == true)
      {
        if (string.IsNullOrEmpty(OutputAttribute))
          outMessage = inputMessage.Message;
        else
          outAttributes[OutputAttribute] = new Variant { StringValue = inputMessage.Message, Type = VariantType.String };
        return;
      }
      if (!string.IsNullOrEmpty(Input?.Attribute))
      {
        if (string.IsNullOrEmpty(OutputAttribute))
        {
          if (inputMessage.AttributeExists(Input.Attribute))
            outMessage = inputMessage.GetAttributeAsVariant(Input.Attribute).ToString();
        }
        else
        {
          if (inputMessage.AttributeExists(Input.Attribute))
            outAttributes[OutputAttribute] = inputMessage.GetAttributeAsVariant(Input.Attribute);
        }
        return;
      }
      if (Input?.Value != null)
        switch (Input.Type)
        {
          case VariantType.Boolean:
            if (bool.TryParse(Input.Value, out bool newBoolValue))
            {
              if (string.IsNullOrEmpty(OutputAttribute))
                outMessage = newBoolValue.ToString();
              else
                outAttributes[OutputAttribute] = new Variant { BooleanValue = newBoolValue, Type = VariantType.Boolean };
            }
            return;
          case VariantType.DateTime:
            if (string.IsNullOrEmpty(Input.Format))
            {
              if (DateTime.TryParse(Input.Value, out DateTime newDateTimeValue))
                if (string.IsNullOrEmpty(OutputAttribute))
                  outMessage = newDateTimeValue.ToString();
                else
                  outAttributes[OutputAttribute] = new Variant { DateTimeValue = newDateTimeValue, Type = VariantType.DateTime };
            }
            else
            {
              if (DateTime.TryParseExact(Input.Value, Input.Format, null, DateTimeStyles.AssumeLocal, out DateTime newDateTimeValue))
                if (string.IsNullOrEmpty(OutputAttribute))
                  outMessage = newDateTimeValue.ToString();
                else
                  outAttributes[OutputAttribute] = new Variant { DateTimeValue = newDateTimeValue, Type = VariantType.DateTime };
            }
            return;
          case VariantType.Float:
            if (float.TryParse(Input.Value, out float newFloatValue))
              if (string.IsNullOrEmpty(OutputAttribute))
                outMessage = (newFloatValue.ToString());
              else
                outAttributes[OutputAttribute] = new Variant { FloatValue = newFloatValue, Type = VariantType.Float };
            return;
          case VariantType.Int:
            if (int.TryParse(Input.Value, out int newIntValue))
              if (string.IsNullOrEmpty(OutputAttribute))
                outMessage = (newIntValue.ToString());
              else
                outAttributes[OutputAttribute] = new Variant { IntValue = newIntValue, Type = VariantType.Int };
            return;
          case VariantType.String:
            if (string.IsNullOrEmpty(OutputAttribute))
              outMessage = Input.Value;
            else
              outAttributes[OutputAttribute] = new Variant { StringValue = Input.Value, Type = VariantType.String };
            return;
        }
    }
  }

  public class FieldInputSettings
  {
    [JsonProperty]
    public string Group { get; set; }

    [JsonProperty]
    public bool? Message { get; set; }

    [JsonProperty]
    public string Attribute { get; set; }

    [JsonProperty]
    public string Value { get; set; }

    [JsonProperty]
    public string Format { get; set; }

    [JsonProperty]
    public string GroupToOutputAttribute { get; set; }

    [JsonProperty("Type")]
    protected string TypeStr { get; set; }

    [JsonIgnore]
    private bool IsTypeParsed = false;
    [JsonIgnore]
    private VariantType _Type = VariantType.String;
    [JsonIgnore]
    public VariantType Type
    {
      get
      {
        if (IsTypeParsed)
          return _Type;
        if (!Enum.TryParse(TypeStr, out _Type))
          _Type = VariantType.String;
        IsTypeParsed = true;
        return _Type;
      }
    }
  }
}
