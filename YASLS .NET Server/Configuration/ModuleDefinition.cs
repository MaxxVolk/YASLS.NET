using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Collections.Generic;

namespace YASLS.NETServer.Configuration
{
  public class ModuleDefinition
  {
    [JsonProperty("Assembly")]
    public string Assembly { get; set; }

    [JsonProperty("Type")]
    public string ManagedTypeName { get; set; }

    [JsonProperty("ConfigurationFilePath")]
    public string ConfigurationFilePath { get; set; }

    [JsonProperty("ConfigurationJSON")]
    public JObject ConfigurationJSON { get; set; }

    [JsonProperty("Attributes")]
    public Dictionary<string, string> Attributes { get; set; }
  }
}
