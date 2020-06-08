using Newtonsoft.Json;

namespace YASLS.NETServer.Configuration
{
  public class ExistsExpression
  {
    [JsonProperty("Attribute")]
    public AttributeExpression Attribute { get; set; }
  }
}