using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using YASLS.SDK.Library;

namespace YASLS
{
  public class FacilityCodeExtractor : IAttributeExtractorModule
  {
    protected Dictionary<string, string> Attributes = new Dictionary<string, string>();
    protected int DefaultFacility, DefaultSeverity;
    protected bool AddDefaultIfNoPRI, OutText, OutCode;
    protected readonly Guid moduleId = Guid.Parse("{B6BBE72C-6A69-4FEE-81A8-52F836D6F829}");
    protected string FacilityCodeAttribute, SeverityCodeAttribute, FacilityTextAttribute, SeverityTextAttribute;

    public void ExtractAttributes(MessageDataItem message)
    {
      foreach (KeyValuePair<string, string> extraAttr in Attributes)
        message.AddAttribute(extraAttr.Key, extraAttr.Value);
      try
      {
        if (string.IsNullOrWhiteSpace(message.Message) && AddDefaultIfNoPRI)
        {
          AddSyslogPRI(message, facility: DefaultFacility, severity: DefaultSeverity);
          return;
        }
        int maxSubLen = message.Message.Length > 20 ? 20 : message.Message.Length;
        string rawMessageHead = message.Message.Substring(0, maxSubLen).Trim();
        if (string.IsNullOrWhiteSpace(rawMessageHead) && AddDefaultIfNoPRI)
        {
          AddSyslogPRI(message, facility: DefaultFacility, severity: DefaultSeverity);
          return;
        }
        if (rawMessageHead[0] == '<')
        {
          if (int.TryParse(rawMessageHead.Substring(1, rawMessageHead.IndexOf('>') - 1), out int priValue))
          {
            int facility = priValue >> 3;
            int severity = priValue & 7;
            AddSyslogPRI(message, facility: facility, severity: severity);
            return;
          }
        }
        if (AddDefaultIfNoPRI)
          AddSyslogPRI(message, facility: DefaultFacility, severity: DefaultSeverity);
        return;
      }
      catch
      {
        {
          if (!message.AttributeExists("Facility") && !message.AttributeExists("Severity"))
            AddSyslogPRI(message, facility: DefaultFacility, severity: DefaultSeverity);
        }
      }
    }

    protected readonly string[] FacilityText = { "kernel ", "user", "mail", "system", "security", "syslogd", "printer", "network", "UUCP", "clock", "authorization", "FTP", "NTP",
      "audit", "alert", "note2", "local0", "local1", "local2", "local3", "local4", "local5", "local6", "local7" };
    protected readonly string[] SeverityText = { "Emergency", "Alert", "Critical", "Error", "Warning", "Notice", "Informational", "Debug" };


    protected void AddSyslogPRI(MessageDataItem message, int facility, int severity)
    {
      if (OutCode)
      {
        message.AddAttribute(FacilityCodeAttribute, facility);
        message.AddAttribute(SeverityCodeAttribute, severity);
      }
      if (OutText)
      {
        if (facility >= 0 && facility < FacilityText.Length)
          message.AddAttribute(FacilityTextAttribute, FacilityText[facility]);
        else
          message.AddAttribute(FacilityTextAttribute, $"Unknown facility code{facility}");
        if (severity >= 0 && severity < SeverityText.Length)
          message.AddAttribute(SeverityTextAttribute, SeverityText[severity]);
        else
          message.AddAttribute(SeverityTextAttribute, $"Unknown severity code{severity}");
      }
    }

    public void LoadConfiguration(JObject configuration, Dictionary<string, string> attributes)
    {
      if (attributes != null && attributes.Count > 0)
        foreach (KeyValuePair<string, string> origAttr in attributes)
          Attributes.Add(origAttr.Key, origAttr.Value);
      AddDefaultIfNoPRI = configuration["AddDefaultIfNoPRI"]?.Value<bool>() ?? true;

      OutCode = configuration["OutCode"]?.Value<bool>() ?? false;
      OutText = configuration["OutText"]?.Value<bool>() ?? true;

      DefaultFacility = configuration["DefaultFacility"]?.Value<int>() ?? 1;
      DefaultSeverity = configuration["DefaultSeverity"]?.Value<int>() ?? 5;

      FacilityCodeAttribute = configuration["FacilityCodeAttribute"]?.Value<string>() ?? "FacilityCode";
      SeverityCodeAttribute = configuration["SeverityCodeAttribute"]?.Value<string>() ?? "SeverityCode";
      FacilityTextAttribute = configuration["FacilityTextAttribute"]?.Value<string>() ?? "Facility";
      SeverityTextAttribute = configuration["SeverityTextAttribute"]?.Value<string>() ?? "Severity";
    }

    #region IModule Implementation
    public string GetModuleName() => GetType().FullName;

    public string GetModuleDisplayName() => "Facility Code Extractor Module";

    public string GetModuleVendor() => "Core YASLS";

    public Guid GetModuleId() => moduleId;

    public Version GetModuleVersion() => Assembly.GetAssembly(GetType()).GetName().Version;
    #endregion
  }
}
