using Newtonsoft.Json;

using System;

using YASLS.SDK.Library;

namespace YASLS.NETServer.Configuration
{
  public class ValueValueExpression
  {
    [JsonProperty("IntValue")]
    protected int? IntValue { get; set; }
    [JsonProperty("FloatValue")]
    protected double? FloatValue { get; set; }
    [JsonProperty("StringValue")]
    protected string StringValue { get; set; }
    [JsonProperty("DateTimeValue")]
    protected DateTime? DateTimeValue { get; set; }
    [JsonProperty("BooleanValue")]
    protected bool? BooleanValue { get; set; }


    [JsonIgnore]
    Variant _Value;
    [JsonIgnore]
    bool ValueParsed = false;
    [JsonIgnore]
    public Variant Value
    {
      get
      {
        if (ValueParsed)
          return _Value;
        if (StringValue!=null)
        {
          _Value = new Variant() { StringValue = StringValue, Type = VariantType.String };
          ValueParsed = true;
        }
        if (IntValue != null)
        {
          _Value = new Variant() { IntValue = IntValue ?? 0, Type = VariantType.Int };
          ValueParsed = true;
        }
        if (FloatValue != null)
        {
          _Value = new Variant() { FloatValue = FloatValue ?? 0, Type = VariantType.Float };
          ValueParsed = true;
        }
        if (DateTimeValue != null)
        {
          _Value = new Variant() { DateTimeValue = DateTimeValue ?? DateTime.MinValue, Type = VariantType.DateTime };
          ValueParsed = true;
        }
        if (BooleanValue != null)
        {
          _Value = new Variant() { BooleanValue = BooleanValue ?? false, Type = VariantType.Boolean };
          ValueParsed = true;
        }
        if (!ValueParsed)
        {
          // default to null string
          _Value = new Variant() { StringValue = null, Type = VariantType.String };
          ValueParsed = true;
        }
        return _Value;
      }
    }
  }
}