using Newtonsoft.Json.Linq;
using System;
using System.Reflection;
using System.Threading;

namespace YASLS.SDK.Library
{
  public abstract class ModuleBase : IServerBind, IModule
  {
    protected ILogger ServerLogger;
    protected IHealthReporter ServerHealthReporter;
    protected IPersistentDataStore ServerPersistentStore;
    protected int DefaultIdleDelay = 1;

    public abstract void LoadConfiguration(JObject configuration);

    #region IServerBind Implementation
    public void RegisterServices(ILogger logger, IHealthReporter healthReporter, IPersistentDataStore persistentStore)
    {
      ServerLogger = logger;
      ServerHealthReporter = healthReporter;
      ServerPersistentStore = persistentStore;
    }
    #endregion

    #region IModule Implementation
    public string GetModuleName() => GetType().FullName;

    public abstract string GetModuleDisplayName();

    public string GetModuleVendor() => "Core YASLS";

    public abstract Guid GetModuleId();

    public Version GetModuleVersion() => Assembly.GetAssembly(GetType()).GetName().Version;
    #endregion
  }
}
