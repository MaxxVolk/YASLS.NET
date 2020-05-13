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
    [JsonProperty("Expression")]
    public JObject Expression { get; set; } // later parsing to Expression when Filter object is created

  [JsonProperty("StopIfMatched")]
    public bool StopIfMatched { get; set; } = false;

    [JsonProperty]
    public Dictionary<string, ModuleDefinition> AttributeExtractors { get; set; }

    [JsonProperty("Parser")]
    public ParserDefinition Parser { get; set; }

    [JsonProperty("Attributes")]
    public Dictionary<string, string> Attributes { get; set; }
  }

  public class ParserDefinition
  {
    [JsonProperty("ParsingModule")]
    public ModuleDefinition ParsingModule { get; set; }

    [JsonIgnore]
    public IParserModule ParserModule;

    [JsonProperty("Output")]
    public List<string> Output { get; set; }

    [JsonIgnore]
    public List<IOutputModule> OutputModules;

    [JsonProperty("Attributes")]
    public Dictionary<string, string> Attributes { get; set; }
  }

}
