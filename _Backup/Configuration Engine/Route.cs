using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YASLS.Configuration
{
  public class Route : ConfigurationPrincipalBase
  {
    [JsonProperty("InputQueue")]
    public string InputQueue { get; set; }

    [JsonProperty]
    public Dictionary<string, Filter> Filters { get; set; }

    public void IngressMessage(MessageMetaInfo message)
    {

    }
  }
}
