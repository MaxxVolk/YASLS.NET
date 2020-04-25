using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YASLS.SDK.Library;

namespace YASLS.Configuration
{
  public class RouteDefinition
  {
    [JsonProperty("InputQueue")]
    public string InputQueue { get; set; }

    [JsonProperty]
    public Dictionary<string, FilterDefinition> Filters { get; set; }
  }

  public class FilterDefinition
  {
    [JsonProperty("AllMessages")]
    public bool AllMessages { get; set; } = false;

    [JsonProperty("StopIfMatched")]
    public bool StopIfMatched { get; set; } = false;

    [JsonProperty("Filter")]
    public ModuleDefinition Filter { get; set; }

    [JsonIgnore]
    public IFilterModule FilterModule;

    [JsonProperty("RegExp")]
    public JObject RegExp { get; set; } // to be parsed inside Filter managed module

    [JsonProperty("Parser")]
    public ModuleDefinition Parser { get; set; }

    [JsonIgnore]
    public IParserModule ParserModule;

    [JsonProperty("Attributes")]
    public Dictionary<string, string> Attributes { get; set; }

    [JsonProperty("Output")]
    public List<string> Output { get; set; }

    [JsonIgnore]
    public List<IOutputModule> OutputModules;
  }

}
