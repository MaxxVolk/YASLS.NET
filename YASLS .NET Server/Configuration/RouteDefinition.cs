using Newtonsoft.Json;
using System.Collections.Generic;

namespace YASLS.NETServer.Configuration
{
  public class RouteDefinition
  {
    [JsonProperty("Inputs")]
    public List<string> Inputs { get; set; }

    [JsonProperty]
    public Dictionary<string, FilterDefinition> Filters { get; set; }
  }
}
