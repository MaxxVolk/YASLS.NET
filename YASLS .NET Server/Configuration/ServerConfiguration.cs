using Newtonsoft.Json;
using System.Collections.Generic;

namespace YASLS.NETServer.Configuration
{
  public class ServerConfiguration
  {
    [JsonProperty]
    public Dictionary<string, ModuleDefinition> Inputs { get; set; }

    [JsonProperty]
    public Dictionary<string, ModuleDefinition> Outputs { get; set; }

    [JsonProperty]
    public Dictionary<string, RouteDefinition> Routing { get; set; }

    [JsonProperty]
    public Dictionary<string, AssemblyDefinition> Assemblies { get; set; }
  }
}
