using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YASLS.SDK.Library;

namespace YASLS.Standard.AttributeExtractor
{
  public class KeyValuePairExtractor : ModuleBase, IAttributeExtractorModule
  {
    protected readonly Guid moduleId = Guid.Parse("{4B002D99-EEE8-42AB-ADE3-E35A1933B7E4}");

    enum State { Skipping, KeyBegin, Key,  }

    public void ExtractAttributes(MessageDataItem message)
    {
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
        ServerLogger?.LogEvent(this, Severity.Warning, "KVExtractor", "Error while extracting key-value pairs.", e);
      }
    }

    static char NextChar() => 'd';

    public override string GetModuleDisplayName() => "Key-Value Pair Attribute Extractor";

    public override Guid GetModuleId() => moduleId;

    public override void LoadConfiguration(JObject configuration)
    {
    }
  }
}
