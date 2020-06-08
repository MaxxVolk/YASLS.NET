using Newtonsoft.Json;

namespace YASLS.NETServer.Configuration
{
  public class AssemblyDefinition
  {
    [JsonProperty("AssemblyQualifiedName")]
    public string AssemblyQualifiedName { get; set; }

    [JsonProperty("AssemblyFilePath")]
    public string AssemblyFilePath { get; set; }
  }
}
