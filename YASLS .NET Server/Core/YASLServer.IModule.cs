using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using YASLS.NETServer.Configuration;
using YASLS.SDK.Library;

namespace YASLS.NETServer.Core
{
  partial class YASLServer : IModule
  {
    protected readonly Guid moduleId = Guid.Parse("{04DB4B9A-86D0-4A82-9FDA-CACC77FB82E8}");

    public string GetModuleDisplayName() => "YASLS .NET Server Core";

    public Guid GetModuleId() => moduleId;

    public string GetModuleName() => GetType().FullName;

    public string GetModuleVendor() => "YASLS";

    public Version GetModuleVersion() => Assembly.GetAssembly(GetType()).GetName().Version;

    public void LoadConfiguration(JObject configuration)
    {
      // never used
    }
  }
}
