using Newtonsoft.Json;

namespace YASLS.NETServer.Configuration
{
  public class InRangeValueExpression
  {
    [JsonProperty]
    public ValueExpression StartValue { get; set; }
    [JsonProperty]
    public ValueExpression EndValue { get; set; }
  }
}