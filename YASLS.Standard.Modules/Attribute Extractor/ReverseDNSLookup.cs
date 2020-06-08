using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading;
using YASLS.SDK.Library;

namespace YASLS.Standard.AttributeExtractor
{
  public class ReverseDNSLookup : ModuleBase, IAttributeExtractorModule
  {
    protected string InputAttribute, OutputAttribute;
    protected readonly Guid moduleId = Guid.Parse("{C7A5838F-59D8-49C5-9941-35022ABFDA0D}");

    public void ExtractAttributes(MessageDataItem message)
    {
      if (message.AttributeExists(InputAttribute) && !message.AttributeExists(OutputAttribute))
        try
        {
          IPAddress hostIPAddress = IPAddress.Parse(message.GetAttributeAsString(InputAttribute));
          IPHostEntry hostInfo = Dns.GetHostEntry(hostIPAddress);
          message.AddAttribute(OutputAttribute, hostInfo.HostName);
        }
        catch
        {
          // none
        }
    }

    public override void LoadConfiguration(JObject configuration)
    {
      InputAttribute = configuration["InputAttribute"]?.Value<string>() ?? throw new ArgumentOutOfRangeException("InputAttribute", "InputAttribute value is missing. Check 'ConfigurationJSON' section.");
      OutputAttribute = configuration["OutputAttribute"]?.Value<string>() ?? throw new ArgumentOutOfRangeException("OutputAttribute", "OutputAttribute value is missing. Check 'ConfigurationJSON' section.");
    }

    #region IModule Implementation
    public override string GetModuleDisplayName() => "Host IP Reverse DNS Lookup Module";


    public override Guid GetModuleId() => moduleId;
    #endregion
  }
}
