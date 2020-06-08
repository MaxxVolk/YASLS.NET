using Newtonsoft.Json;

using System.Collections.Generic;

namespace YASLS.NETServer.Configuration
{
  public class ParserDefinition
  {
    [JsonProperty("ParsingModule")]
    public ModuleDefinition ParsingModule { get; set; }

    [JsonProperty]
    public Dictionary<string, ModuleDefinition> AttributeExtractors { get; set; }

    [JsonProperty("Output")]
    public List<string> Output { get; set; }

    [JsonProperty("Attributes")]
    public Dictionary<string, string> Attributes { get; set; }
  }

}
