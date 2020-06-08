using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Collections.Generic;

namespace YASLS.NETServer.Configuration
{
  public class FilterDefinition
  {
    [JsonProperty("Expression")]
    public JObject Expression { get; set; } // later parsing to Expression when Filter object is created

    [JsonProperty("StopIfMatched")]
    public bool StopIfMatched { get; set; } = false;


    [JsonProperty("Parser")]
    public ParserDefinition Parser { get; set; }

    [JsonProperty("Attributes")]
    public Dictionary<string, string> Attributes { get; set; }
  }

}
