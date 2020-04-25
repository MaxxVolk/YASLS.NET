using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace YASLS.Configuration
{
  public class Filter : ConfigurationPrincipalBase
  {
    [JsonProperty]
    public bool Any { get; set; } = false;

    [JsonProperty]
    public bool StopIfMatched { get; set; } = true;

    [JsonProperty]
    public RegExpFilter RegExp { get; set; }
  }

  public class RegExpFilter : ConfigurationPrincipalBase
  {
    [JsonProperty("Options", NullValueHandling = NullValueHandling.Ignore)]
    public int Options { get; set; } = 0;

    [JsonProperty("And", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> And { get; set; }

    [JsonProperty("Or", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> Or { get; set; }

    [JsonProperty("NotAll", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> NotAll { get; set; }

    [JsonProperty("NotAny", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> NotAny { get; set; }

    [JsonProperty("Always", NullValueHandling = NullValueHandling.Ignore)]
    public bool? Always { get; set; }

    public bool Evaluate(string rawMessage)
    {
      bool AndResult = true, OrResult = true, NotAllResult = true, NotAnyResult = true;
      if (And != null && And.Count > 0)
        AndResult = And.All(x => Regex.IsMatch(rawMessage, x, (RegexOptions)Options));
      if (Or != null && Or.Count > 0)
        OrResult = Or.Any(x => Regex.IsMatch(rawMessage, x, (RegexOptions)Options));
      if (NotAll != null && NotAll.Count > 0)
        NotAllResult = !NotAll.All(x => Regex.IsMatch(rawMessage, x, (RegexOptions)Options));
      if (NotAny != null && NotAny.Count > 0)
        NotAnyResult = !NotAny.Any(x => Regex.IsMatch(rawMessage, x, (RegexOptions)Options));

      return (Always == true) || (AndResult && OrResult && NotAllResult && NotAnyResult);
    }
  }
}
