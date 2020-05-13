using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YASLS.Core;
using YASLS.SDK.Library;

namespace YASLS
{
  public class KeyValuePairExtractor : BaseInternalModule, IAttributeExtractorModule
  {
    protected Dictionary<string, string> Attributes = new Dictionary<string, string>();
    protected readonly Guid moduleId = Guid.Parse("");

    enum State { Skipping, KeyBegin, Key,  }

    public KeyValuePairExtractor(ILogger logger, IHealthReporter healthReporter) : base(logger, healthReporter)
    {
    }

    public void ExtractAttributes(MessageDataItem message)
    {
      foreach (KeyValuePair<string, string> extraAttr in Attributes)
        message.AddAttribute(extraAttr.Key, extraAttr.Value);
      try
      {
        if (message.Message.IndexOf('=') == -1)
          return;

        // start state machine
        int pos = 0;
        int len = message.Message.Length;
        while (pos < len)
        {

          pos++;
        }

        return;
      }
      catch (Exception e)
      {
        Logger?.LogEvent(this, Severity.Warning, "KVExtractor", "Error while extracting key-value pairs.", e);
      }
    }

    static char NextChar() => 'd';

    public override string GetModuleDisplayName() => "Key-Value Pair Attribute Extractor";

    public override Guid GetModuleId() => moduleId;

    public void LoadConfiguration(JObject configuration, Dictionary<string, string> attributes)
    {
      if (attributes != null && attributes.Count > 0)
        foreach (KeyValuePair<string, string> attr in attributes)
          Attributes.Add(attr.Key, attr.Value);
    }
  }
}
