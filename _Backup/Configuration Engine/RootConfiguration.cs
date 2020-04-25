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
  public class RootConfiguration
  {
    [JsonProperty("Inputs", NullValueHandling = NullValueHandling.Ignore)]
    public Input Inputs { get; set; }

    [JsonProperty("Queues", NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, YASLSQueue> Queues { get; set; }

    //[JsonProperty("Parsers", NullValueHandling = NullValueHandling.Ignore)]
    //public Dictionary<string, Parser> DeploymentClients { get; set; }

    //[JsonProperty("ExternalFilters", NullValueHandling = NullValueHandling.Ignore)]
    //public Dictionary<string, ExternalFilter> ExtraFiles { get; set; }

    //[JsonProperty("Outputs", NullValueHandling = NullValueHandling.Ignore)]
    //public Dictionary<string, Output> Outputs { get; set; }

    [JsonProperty("Routing", NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, Route> Routing { get; set; }

    public void Bind()
    {
      foreach (PropertyInfo propertyInfo in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
      {
        if (propertyInfo.PropertyType.IsSubclassOf(typeof(ConfigurationPrincipalBase)))
          ((ConfigurationPrincipalBase)propertyInfo.GetValue(this, null))?.Bind(this);
        if (propertyInfo.PropertyType.IsGenericType && propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>) && propertyInfo.PropertyType.GetGenericArguments()[0] == typeof(string))
        {
          IDictionary elementCollection = (IDictionary)propertyInfo.GetValue(this, null);
          if (elementCollection != null)
            foreach (DictionaryEntry dictionaryElement in elementCollection)
              if (dictionaryElement.Value.GetType().IsSubclassOf(typeof(ConfigurationPrincipalBase)))
                ((ConfigurationPrincipalBase)dictionaryElement.Value)?.Bind(this);
        }
      }

      // specific bindings
      foreach (YASLSQueue queue in Queues.Select(x => x.Value))
        foreach (string inputName in queue.Inputs)
        {
          if (Inputs.NetworkListeners.ContainsKey(inputName))
            Inputs.NetworkListeners[inputName].RegisterQueue(queue);
        }
      foreach (Route route in Routing.Select(r => r.Value))
      {
        //route.RegisterQueue();
      }
    }
  }

  public class ServerConfiguration
  {
    [JsonProperty]
    public Dictionary<string, InputDefinition> Inputs { get; set; }

    [JsonProperty]
    public Dictionary<string, QueueDefinition> Queues { get; set; }
  }

  public class InputDefinition
  {
    [JsonProperty("Assembly")]
    public string Assembly { get; set; }
    [JsonProperty("Type")]
    public string ManagedTypeName { get; set; }
    [JsonProperty("ConfigurationFilePath")]
    public string ConfigurationFilePath { get; set; }
    [JsonProperty("ConfigurationJSON")]
    public JObject ConfigurationJSON { get; set; }
  }

  public class QueueDefinition
  {
    [JsonProperty]
    public List<string> Inputs { get; set; }
  }
}
