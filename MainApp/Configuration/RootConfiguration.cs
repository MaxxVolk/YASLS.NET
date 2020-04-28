using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace YASLS.Configuration
{
  public class ServerConfiguration
  {
    [JsonProperty]
    public Dictionary<string, ModuleDefinition> Inputs { get; set; }

    [JsonProperty]
    public Dictionary<string, QueueDefinition> Queues { get; set; }

    [JsonProperty]
    public Dictionary<string, ModuleDefinition> Outputs { get; set; }

    [JsonProperty]
    public Dictionary<string, RouteDefinition> Routing { get; set; }

    [JsonProperty]
    public Dictionary<string, AssemblyDefinition> Assemblies { get; set; }
  }

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

  public class AssemblyDefinition
  {
    [JsonProperty("AssemblyQualifiedName")]
    public string AssemblyQualifiedName { get; set; }

    [JsonProperty("AssemblyFilePath")]
    public string AssemblyFilePath { get; set; }
  }
}
