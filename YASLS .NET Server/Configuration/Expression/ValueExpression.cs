using Newtonsoft.Json;

using System;

using YASLS.SDK.Library;

namespace YASLS.NETServer.Configuration
{
  public class ValueExpression
  {
    [JsonProperty("Attribute")]
    protected AttributeExpression Attribute { get; set; }

    [JsonProperty("Value")]
    protected ValueValueExpression Value { get; set; }

    [JsonProperty("Message")]
    protected bool Message { get; set; }

    public Variant GetValue(MessageDataItem dataItem)
    {
      if (Message)
        return new Variant() { StringValue = dataItem.Message, Type = VariantType.String };
      if (Value != null)
        return Value.Value;
      if (Attribute != null)
      {
        Variant result = dataItem.GetAttributeAsVariant(Attribute.Name);
        if (result.Type != Attribute.Type)
          throw new InvalidCastException("Attribute type mismatch.");
        return result;
      }
      throw new InvalidOperationException("Value Expression is not defined.");
    }
  }
}