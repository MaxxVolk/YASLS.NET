using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YASLS.Configuration
{
  public class QueueDefinition
  {
    [JsonProperty]
    public List<string> Inputs { get; set; }

    [JsonProperty]
    public Dictionary<string, ModuleDefinition> AttributeExtractors { get; set; }

    [JsonProperty("Attributes")]
    public Dictionary<string, string> Attributes { get; set; }
  }
}
