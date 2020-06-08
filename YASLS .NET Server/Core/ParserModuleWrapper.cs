using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using YASLS.SDK.Library;

namespace YASLS.NETServer.Core
{
  class ParserModuleWrapper : IModule
  {

    #region IModule Implementation
    protected readonly Guid moduleId = Guid.Parse("{F148E2C0-CB4A-4642-B04A-50B4D46A3C63}");

    public string GetModuleDisplayName() => "YASLS .NET Server Parser Module Wrapper";

    public Guid GetModuleId() => moduleId;

    public string GetModuleName() => GetType().FullName;

    public string GetModuleVendor() => "YASLS";

    public Version GetModuleVersion() => Assembly.GetAssembly(GetType()).GetName().Version;

    public void LoadConfiguration(JObject configuration)
    {
      // never used
    }
    #endregion
  }
}
