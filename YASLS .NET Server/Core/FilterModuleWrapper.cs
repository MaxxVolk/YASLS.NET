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
  class FilterModuleWrapper : IModule
  {

    #region IModule Implementation
    protected readonly Guid moduleId = Guid.Parse("{5904A555-7653-4AEA-A171-D8B97250FAF7}");

    public string GetModuleDisplayName() => "YASLS .NET Server Filter Module Wrapper";

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
