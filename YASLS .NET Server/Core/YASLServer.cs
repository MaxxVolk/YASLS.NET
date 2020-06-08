using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using YASLS.NETServer.Configuration;
using YASLS.SDK.Library;

namespace YASLS.NETServer.Core
{
  partial class YASLServer
  {
    private ServerConfiguration ServerConfiguration { get; }

    private readonly Dictionary<string, InputModuleWrapper> Inputs = new Dictionary<string, InputModuleWrapper>();
    private readonly Dictionary<string, Route> Routes = new Dictionary<string, Route>();
    private readonly Dictionary<string, OutputModuleWrapper> Outputs = new Dictionary<string, OutputModuleWrapper>();
    private MessageMixer MessageMixer;

    public YASLServer(ServerConfiguration serverConfiguration)
    {
      ServerConfiguration = serverConfiguration;

      InitilizeServices();

      ValidateServerConfiguration();

      foreach (KeyValuePair<string, ModuleDefinition> inputCfg in ServerConfiguration.Inputs)
        Inputs.Add(inputCfg.Key, new InputModuleWrapper(inputCfg.Key, inputCfg.Value, this));
      foreach (KeyValuePair<string, ModuleDefinition> outputCfg in ServerConfiguration.Outputs)
        Outputs.Add(outputCfg.Key, new OutputModuleWrapper(outputCfg.Key, outputCfg.Value, this));
      foreach (KeyValuePair<string, RouteDefinition> routeCfg in ServerConfiguration.Routing)
        Routes.Add(routeCfg.Key, new Route(routeCfg.Key, routeCfg.Value, Outputs, this));
      MessageMixer = new MessageMixer(Inputs, Routes, ServerConfiguration.Routing, this);
    }

    public void Start()
    {
      Logger.LogEvent(this, Severity.Verbose, "ServerStart", "Starting output modules...");
      foreach (KeyValuePair<string, OutputModuleWrapper> output in Outputs)
        output.Value.Start();

      Logger.LogEvent(this, Severity.Verbose, "ServerStart", "Starting routes...");
      foreach (KeyValuePair<string, Route> route in Routes)
        route.Value.Start();

      Logger.LogEvent(this, Severity.Verbose, "ServerStart", "Starting message mixer...");
      MessageMixer.Start();

      Logger.LogEvent(this, Severity.Verbose, "ServerStart", "Starting input modules...");
      foreach (KeyValuePair<string, InputModuleWrapper> input in Inputs)
        input.Value.Start();
    }

    public void Stop(int gracefulShutdownTimeout)
    {
      Logger.LogEvent(this, Severity.Verbose, "ServerStop", "Stopping input modules...");
      foreach (InputModuleWrapper input in Inputs.Values)
        input.Stop();
      Logger.LogEvent(this, Severity.Verbose, "ServerStop", "Waiting input modules to drain their queues...");
      if (!WaitHandle.WaitAll(Inputs.Values.Select(x => x.WorkCompleted).ToArray(), gracefulShutdownTimeout))
      {
        Logger.LogEvent(this, Severity.Warning, "ServerStop", "Some input queues hasn't been drained gracefully. Shutting down inputs.");
        foreach (InputModuleWrapper input in Inputs.Values)
          input.Abort();
      }

      Logger.LogEvent(this, Severity.Verbose, "ServerStart", "Stopping routes...");
      foreach (KeyValuePair<string, Route> route in Routes)
        route.Value.Stop();
      if (!WaitHandle.WaitAll(Routes.Values.Select(x => x.WorkCompleted).ToArray(), gracefulShutdownTimeout))
      {
        Logger.LogEvent(this, Severity.Warning, "ServerStop", "Some routes hasn't been drained gracefully. Shutting down routes.");
        foreach (Route route in Routes.Values)
          route.Abort();
      }

      Logger.LogEvent(this, Severity.Verbose, "ServerStop", "Stopping output modules...");
      foreach (OutputModuleWrapper output in Outputs.Values)
        output.Stop();
      Logger.LogEvent(this, Severity.Verbose, "ServerStop", "Waiting output modules to drain their queues...");
      if (!WaitHandle.WaitAll(Outputs.Values.Select(x => x.WorkCompleted).ToArray(), gracefulShutdownTimeout))
      {
        Logger.LogEvent(this, Severity.Warning, "ServerStop", "Some output queues hasn't been drained gracefully. Shutting down outputs.");
        foreach (OutputModuleWrapper output in Outputs.Values)
          output.Abort();
      }

      MessageMixer.Stop();
    }

    private void ValidateServerConfiguration()
    {
      // check empty configuration
      if (ServerConfiguration.Inputs == null || ServerConfiguration.Inputs.Count == 0)
      {
        Logger.LogEvent(this, Severity.Fatal, "Configuration", "No input modules defined. Server stops.");
        throw new ConfigurationFatalException("No input modules defined. Server stops.");
      }
      if (ServerConfiguration.Outputs == null || ServerConfiguration.Outputs.Count == 0)
      {
        Logger.LogEvent(this, Severity.Fatal, "Configuration", "No output modules defined. Server stops.");
        throw new ConfigurationFatalException("No output modules defined. Server stops.");
      }
      if (ServerConfiguration.Routing == null || ServerConfiguration.Routing.Count == 0)
      {
        Logger.LogEvent(this, Severity.Fatal, "Configuration", "No routes defined. Server stops.");
        throw new ConfigurationFatalException("No routes defined. Server stops.");
      }
      // configuration normalization
      if (ServerConfiguration.Assemblies == null)
        ServerConfiguration.Assemblies = new Dictionary<string, AssemblyDefinition>();
    }
  }
}
