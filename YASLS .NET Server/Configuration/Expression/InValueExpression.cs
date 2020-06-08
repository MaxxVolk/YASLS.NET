using Newtonsoft.Json;

using System.Collections.Generic;

namespace YASLS.NETServer.Configuration
{
  public class InValueExpression
  {
    [JsonProperty]
    public List<ValueExpression> List { get; set; }

    [JsonProperty]
    public InRangeValueExpression Range { get; set; }
  }
}