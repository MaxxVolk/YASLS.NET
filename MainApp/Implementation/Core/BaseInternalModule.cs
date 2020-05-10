using System;
using System.Reflection;
using YASLS.SDK.Library;

namespace YASLS.Core
{
  public abstract class BaseInternalModule : IModule
  {
    protected ILogger Logger;
    protected IHealthReporter HealthReporter;

    public BaseInternalModule(ILogger logger, IHealthReporter healthReporter)
    {
      Logger = logger;
      HealthReporter = healthReporter;
    }

    #region IModule Implementation
    public string GetModuleName() => GetType().FullName;

    public abstract string GetModuleDisplayName();

    public string GetModuleVendor() => "Core YASLS";

    public abstract Guid GetModuleId();

    public Version GetModuleVersion() => Assembly.GetAssembly(GetType()).GetName().Version;
    #endregion
  }
}
