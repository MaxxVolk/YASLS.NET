using Library.ServiceManagement;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using YASLS.NETServer.Configuration;
using YASLS.SDK.Library;

namespace YASLS.NETServer.Core
{
  partial class YASLServer
  {
    public ILogger Logger { get; protected set; }
    public IHealthReporter HealthReporter { get; protected set; }
    public IQueueFactory QueueFactory { get; protected set; }

    protected void InitilizeServices()
    {
      if (Environment.UserInteractive)
        Logger = new ConsoleLogger();
      else
        Logger = new FileLogger("", Severity.Debug);
      HealthReporter = new ConsoleHealthReporter();
      QueueFactory = new InMemoryConcurrentQueueFactory(Logger);
    }

    public I CreateModule<I>(ModuleDefinition moduleDefinition) where I : IModule
    {
      Assembly moduleAssembly = GetModuleAssembly(ServerConfiguration.Assemblies, moduleDefinition);
      Type moduleType = AssertationHelper.AssertNotNull(moduleAssembly.GetType(moduleDefinition.ManagedTypeName), () => new Exception($"Type {moduleDefinition.ManagedTypeName ?? "<Unknown>"} not found."));
      if (moduleType.GetInterface(typeof(I).FullName) == null)
        throw new Exception($"Module doesn't support the requested interface {typeof(I).FullName} interface.");
      I newModuleInstance = (I)Activator.CreateInstance(moduleType);
      if (moduleType.GetInterface(typeof(IServerBind).FullName) != null)
        ((IServerBind)newModuleInstance).RegisterServices(Logger, HealthReporter, null);
      return newModuleInstance;
    }

    protected static Assembly GetModuleAssembly(Dictionary<string, AssemblyDefinition> assemblies, ModuleDefinition moduleDefinition)
    {
      if (string.IsNullOrWhiteSpace(moduleDefinition.Assembly))
        return Assembly.GetExecutingAssembly();
      if (assemblies.Count == 0)
        throw new ApplicationException("Assembly collection is empty or not defined, unable to lookup for any assemblies.");
      KeyValuePair<string, AssemblyDefinition> assemblyConfiguration = assemblies.Where(a => a.Key == moduleDefinition.Assembly).FirstOrDefault();
      Exception loadByQualifiedNameException;
      try
      {
        return Assembly.Load(assemblyConfiguration.Value.AssemblyQualifiedName);
      }
      catch (Exception e)
      {
        loadByQualifiedNameException = e;
      }
      Exception loadByFileNameException;
      try
      {
        return Assembly.LoadFrom(assemblyConfiguration.Value.AssemblyFilePath);
      }
      catch (Exception e)
      {
        loadByFileNameException = e;
      }
      // if we are here, then both attempts thrown exceptions
      throw new AggregateException("Unable to located referenced assembly neither by Qualified Name nor File Path. See inner exceptions for details", new Exception[] { loadByQualifiedNameException, loadByFileNameException });
    }
  }
}
