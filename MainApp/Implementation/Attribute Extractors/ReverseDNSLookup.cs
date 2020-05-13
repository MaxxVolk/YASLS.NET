using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using YASLS.SDK.Library;

namespace YASLS
{
  public class ReverseDNSLookup : IAttributeExtractorModule
  {
    protected Dictionary<string, string> Attributes = new Dictionary<string, string>();
    protected string InputAttribute, OutputAttribute;
    protected readonly Guid moduleId = Guid.Parse("{C7A5838F-59D8-49C5-9941-35022ABFDA0D}");

    public void ExtractAttributes(MessageDataItem message)
    {
      foreach (KeyValuePair<string, string> extraAttr in Attributes)
        message.AddAttribute(extraAttr.Key, extraAttr.Value);
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

    public void LoadConfiguration(JObject configuration, Dictionary<string, string> attributes)
    {
      if (attributes != null && attributes.Count > 0)
        foreach (KeyValuePair<string, string> origAttr in attributes)
          Attributes.Add(origAttr.Key, origAttr.Value);
      InputAttribute = configuration["InputAttribute"]?.Value<string>() ?? throw new ArgumentOutOfRangeException("InputAttribute", "InputAttribute value is missing. Check 'ConfigurationJSON' section.");
      OutputAttribute = configuration["OutputAttribute"]?.Value<string>() ?? throw new ArgumentOutOfRangeException("OutputAttribute", "OutputAttribute value is missing. Check 'ConfigurationJSON' section.");
    }

    #region IModule Implementation
    public string GetModuleName() => GetType().FullName;

    public string GetModuleDisplayName() => "Host IP Reverse DNS Lookup Module";

    public string GetModuleVendor() => "Core YASLS";

    public Guid GetModuleId() => moduleId;

    public Version GetModuleVersion() => Assembly.GetAssembly(GetType()).GetName().Version;
    #endregion
  }
}
