using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace YASLS.Configuration
{
  public abstract class ConfigurationPrincipalBase
  {
    [JsonIgnore]
    protected RootConfiguration RootConfiguration { get; private set; }

    [JsonProperty("Parameters", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> Parameters { get; set; }

    public void Bind(RootConfiguration rootConfiguration)
    {
      RootConfiguration = rootConfiguration;
      foreach (PropertyInfo propertyInfo in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
      {
        if (propertyInfo.PropertyType.IsSubclassOf(typeof(ConfigurationPrincipalBase)))
          ((ConfigurationPrincipalBase)propertyInfo.GetValue(this, null))?.Bind(rootConfiguration);
        if (propertyInfo.PropertyType.IsGenericType && propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>) && propertyInfo.PropertyType.GetGenericArguments()[0] == typeof(string))
        {
          IDictionary elementCollection = (IDictionary)propertyInfo.GetValue(this, null);
          if (elementCollection != null)
            foreach (DictionaryEntry dictionaryElement in elementCollection)
              if (dictionaryElement.Value.GetType().IsSubclassOf(typeof(ConfigurationPrincipalBase)))
                ((ConfigurationPrincipalBase)dictionaryElement.Value)?.Bind(rootConfiguration);
        }
      }
    }
  }
}
