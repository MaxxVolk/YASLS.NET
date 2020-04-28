using Library.ServiceManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using YASLS.Configuration;
using YASLS.SDK.Library;

namespace YASLS
{
  public class YASLServer : IModule
  {
    protected readonly Guid moduleId = Guid.Parse("{04DB4B9A-86D0-4A82-9FDA-CACC77FB82E8}");

    protected Dictionary<string, Tuple<MasterQueue, Thread>> MasterQueues = new Dictionary<string, Tuple<MasterQueue, Thread>>();
    protected Dictionary<string, Tuple<IInputModule, Thread>> InputModules = new Dictionary<string, Tuple<IInputModule, Thread>>();
    protected Dictionary<string, Tuple<Route, Thread>> Routes = new Dictionary<string, Tuple<Route, Thread>>();
    protected Dictionary<string, Tuple<IOutputModule, Thread>> OutputModules = new Dictionary<string, Tuple<IOutputModule, Thread>>();

    protected CancellationTokenSource TokenSource;

    protected ILogger internalLogger = new DebugConsoleLogger();
    protected IQueueFactory queueFactory;

    public YASLServer(ServerConfiguration serverConfiguration)
    {
      TokenSource = new CancellationTokenSource();
      queueFactory = new InMemoryConcurrentQueueFactory(internalLogger);

      #region Create Master Queues
      // create queues
      if (serverConfiguration.Queues != null)
        foreach (KeyValuePair<string, QueueDefinition> queueCfg in serverConfiguration.Queues)
          try
          {
            QueueDefinition queueDef = queueCfg.Value;
            MasterQueue newQueue = new MasterQueue(TokenSource.Token, queueDef.Attributes, internalLogger, null, queueFactory);
            MasterQueues.Add(queueCfg.Key, new Tuple<MasterQueue, Thread>(newQueue, new Thread(newQueue.GetWorker())));

            // reference check for future binding
            IEnumerable<string> missingInputReferences = queueDef.Inputs.Where(i => serverConfiguration.Inputs?.ContainsKey(i) == false);
            if (missingInputReferences.Any())
            {
              string missingList = "";
              foreach (string missingInput in missingInputReferences) missingList += missingInput + ", ";
              missingList = missingList.Substring(0, missingList.Length - 2);
              internalLogger?.LogEvent(this, Severity.Warning, "ServerInit", $"Queue '{queueCfg.Key}' has references to not existing Inputs: {missingList}");
            }

            // add Attribute Extractors to the queue
            if (queueCfg.Value.AttributeExtractors != null)
              foreach (KeyValuePair<string, ModuleDefinition> extractorCfg in queueCfg.Value.AttributeExtractors)
                try
                {
                  ModuleDefinition extractorDef = extractorCfg.Value;
                  IAttributeExtractorModule newExtractorModule = CreateModule<IAttributeExtractorModule>(extractorDef, serverConfiguration.Assemblies);
                  newExtractorModule.Initialize(extractorDef.ConfigurationJSON, extractorDef.Attributes);
                  newQueue.RegisterAttributeExtractor(newExtractorModule);
                }
                catch (Exception e)
                {
                  internalLogger?.LogEvent(this, Severity.Warning, "AttributeExtractorInit", $"Failed to load or initialize '{extractorCfg.Key ?? "<Unknown>"}' attribute parser module.", e);
                }
          }
          catch (Exception e)
          {
            internalLogger?.LogEvent(this, Severity.Error, "MasterQueueInit", $"Failed to create '{queueCfg.Key}' master queue.", e);
          }
      if (MasterQueues.Count == 0)
      {
        internalLogger?.LogEvent(this, Severity.Error, "ServerInit", "ERROR: Failed to create all queues or no queues defined. Server stops.");
        Environment.Exit(1);
      }
      #endregion

      #region Create Outputs
      // create outputs
      if (serverConfiguration.Outputs != null)
        foreach (KeyValuePair<string, ModuleDefinition> outputCfg in serverConfiguration.Outputs)
          try
          {
            ModuleDefinition outputDef = outputCfg.Value;
            IOutputModule newOutputModule = CreateModule<IOutputModule>(outputDef, serverConfiguration.Assemblies);
            newOutputModule.Initialize(outputDef.ConfigurationJSON, TokenSource.Token);
            OutputModules.Add(outputCfg.Key, new Tuple<IOutputModule, Thread>(newOutputModule, new Thread(new ModuleThreadWrapper(newOutputModule, TokenSource.Token).GetThreadEntry())));
          }
          catch (Exception e)
          {
            internalLogger?.LogEvent(this, Severity.Warning, "OutputInit", $"Failed to load or initialize '{outputCfg.Key ?? "<Unknown>"}' Output module.", e);
          }
      if (OutputModules.Count == 0)
      {
        internalLogger?.LogEvent(this, Severity.Error, "ServerInit", "ERROR: Failed to create all outputs or no outputs defined. Server stops.");
        Environment.Exit(1);
      }
      #endregion

      #region Create Routes
      // create routes
      if (serverConfiguration.Routing != null)
        foreach (KeyValuePair<string, RouteDefinition> routeCfg in serverConfiguration.Routing)
          try
          {
            RouteDefinition routeDef = routeCfg.Value;
            if (routeDef.Filters != null)
              // parse all filters: Filter and Parsers -- create a new instance for each module definition, Output -- link to existent.
              foreach (KeyValuePair<string, FilterDefinition> filterCfg in routeDef.Filters)
                try
                {
                  FilterDefinition filterDef = filterCfg.Value;
                  if (filterDef.Parser != null)
                  {
                    ModuleDefinition parserDef = filterDef.Parser;
                    IParserModule newParserModule = CreateModule<IParserModule>(parserDef, serverConfiguration.Assemblies);
                    newParserModule.Initialize(parserDef.ConfigurationJSON, TokenSource.Token);
                    filterDef.ParserModule = newParserModule;
                  }
                  if (filterDef.Filter != null)
                  {
                    ModuleDefinition filterModuleDef = filterDef.Filter;
                    IFilterModule filterModule = CreateModule<IFilterModule>(filterModuleDef, serverConfiguration.Assemblies);
                    filterModule.Initialize(filterModuleDef.ConfigurationJSON, TokenSource.Token);
                    filterDef.FilterModule = filterModule;
                  }
                  filterDef.OutputModules = new List<IOutputModule>();
                  if (filterDef.Output != null && filterDef.Output.Count > 0)
                  {
                    foreach (string outputRef in filterDef.Output)
                    {
                      if (OutputModules.ContainsKey(outputRef))
                        filterDef.OutputModules.Add(OutputModules[outputRef].Item1);
                      else
                        internalLogger?.LogEvent(this, Severity.Warning, "ServerInit", $"Warning: Output reference {outputRef} in the filter {filterCfg.Key} definition cannot be resolved.");
                    }
                  }
                  if (filterDef.OutputModules.Count == 0)
                    internalLogger?.LogEvent(this, Severity.Warning, "ServerInit", $"Warning: Filter {filterCfg.Key} has no Output defined or no Output resolved correctly.");
                }
                catch (Exception e)
                {
                  internalLogger?.LogEvent(this, Severity.Error, "FilterInit", $"Failed to create '{filterCfg.Key}' filter.", e);
                }
            // create a route object -- it's configuration is already parsed and contains module references.
            Route newRoute = new Route(routeCfg.Value, TokenSource.Token, internalLogger, null, queueFactory);
            Routes.Add(routeCfg.Key, new Tuple<Route, Thread>(newRoute, new Thread(newRoute.GetWorker())));

            // attach route to its queue
            MasterQueues[routeCfg.Value.InputQueue].Item1.RegisterRoute(newRoute);
          }
          catch (Exception e)
          {
            internalLogger?.LogEvent(this, Severity.Error, "RouteInit", $"Failed to create '{routeCfg.Key}' route.", e);
          }
      if (Routes.Count == 0)
      {
        internalLogger?.LogEvent(this, Severity.Error, "ServerInit", "ERROR: Failed to create all routes or no routes defined. Server stops.");
        Environment.Exit(1);
      }
      #endregion

      #region Create Inputs
      // create inputs
      if (serverConfiguration.Inputs != null)
        foreach (KeyValuePair<string, ModuleDefinition> inputCfg in serverConfiguration.Inputs)
          try
          {
            ModuleDefinition inputDef = inputCfg.Value;
            IInputModule newInputModule = CreateModule<IInputModule>(inputDef, serverConfiguration.Assemblies);
            newInputModule.Initialize(inputDef.ConfigurationJSON, TokenSource.Token, inputCfg.Value.Attributes, MasterQueues.Where(z => serverConfiguration.Queues.Where(x => x.Value.Inputs.Contains(inputCfg.Key)).Select(y => y.Key).Contains(z.Key)).Select(t => t.Value.Item1));
            InputModules.Add(inputCfg.Key, new Tuple<IInputModule, Thread>(newInputModule, new Thread(new ModuleThreadWrapper(newInputModule, TokenSource.Token).GetThreadEntry())));
          }
          catch (Exception e)
          {
            internalLogger?.LogEvent(this, Severity.Warning, "InputInit", $"Failed to load or initialize {inputCfg.Key ?? "<Unknown>"} Input module.", e);
          }
      if (InputModules.Count == 0)
        internalLogger?.LogEvent(this, Severity.Warning, "ServerInit", "WARNING: Failed to create all Inputs or no routes defined. Server starts but will do nothing.");
      #endregion
    }

    protected I CreateModule<I>(ModuleDefinition moduleDefinition, Dictionary<string, AssemblyDefinition> assemblies) where I: IModule
    {
      Assembly moduleAssembly = GetModuleAssembly(moduleDefinition, assemblies);
      Type moduleType = AssertationHelper.AssertNotNull(moduleAssembly.GetType(moduleDefinition.ManagedTypeName), () => new Exception($"Type {moduleDefinition.ManagedTypeName ?? "<Unknown>"} not found."));
      if (moduleType.GetInterface(typeof(I).FullName) == null)
        throw new Exception($"Module doesn't support the requested interface {typeof(I).FullName} interface.");
      I newModuleInstance = (I)Activator.CreateInstance(moduleType);
      if (moduleType.GetInterface(typeof(IServerBind).FullName) != null)
        ((IServerBind)newModuleInstance).RegisterServices(internalLogger, null, queueFactory, null);
      return newModuleInstance;
    }

    public void Start()
    {
      // start inputs
      foreach (Tuple<IInputModule, Thread> inputModule in InputModules.Values)
      {
        inputModule.Item2.Start();
      }

      // start queues
      foreach (Tuple<MasterQueue, Thread> masterQueue in MasterQueues.Values)
      {
        masterQueue.Item2.Start();
      }

      // start routes
      foreach (Tuple<Route, Thread> route in Routes.Values)
      {
        route.Item2.Start();
      }

      // start outputs
      foreach (Tuple<IOutputModule, Thread> outputModule in OutputModules.Values)
      {
        outputModule.Item2.Start();
      }
    }

    public void Stop()
    {
      TokenSource.Cancel();
      foreach (Tuple<IInputModule, Thread> inputModule in InputModules.Values)
      {
        inputModule.Item2.Join();
        inputModule.Item1.Destroy();
      }
      foreach (Tuple<MasterQueue, Thread> masterQueues in MasterQueues.Values)
      {
        masterQueues.Item2.Join();
      }
      foreach (Tuple<Route, Thread> route in Routes.Values)
      {
        route.Item2.Join();
      }
      foreach (Tuple<IOutputModule, Thread> outputModule in OutputModules.Values)
      {
        outputModule.Item2.Join();
      }
    }

    protected Assembly GetModuleAssembly(ModuleDefinition moduleDefinition, Dictionary<string, AssemblyDefinition> assemblies)
    {
      if (string.IsNullOrWhiteSpace(moduleDefinition.Assembly))
        return Assembly.GetExecutingAssembly();
      if (assemblies == null || assemblies.Count == 0)
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

    #region IModule Implementation
    public string GetModuleName() => GetType().FullName;

    public string GetModuleDisplayName() => "Main YASLS Server Module";

    public string GetModuleVendor() => "Core YASLS";

    public Guid GetModuleId() => moduleId;

    public Version GetModuleVersion() => Assembly.GetAssembly(GetType()).GetName().Version;
    #endregion
  }

  public class ModuleThreadWrapper
  {
    protected Guid moduleId;
    protected ThreadStart moduleEntryPoint;
    protected CancellationToken cancellationToken;

    public ModuleThreadWrapper (IThreadModule module, CancellationToken token)
    {
      moduleId = module.GetModuleId();
      moduleEntryPoint = module.GetWorker();
      cancellationToken = token;
    }

    public ThreadStart GetThreadEntry()
    {
      return new ThreadStart(ExceptionCapturer);
    }

    private void ExceptionCapturer()
    {
      bool finishedSuccessfully = true;
      do
      {
        try
        {
          moduleEntryPoint.Invoke();
          finishedSuccessfully = true;
          if (cancellationToken.IsCancellationRequested)
            break;
        }
        catch (OperationCanceledException)
        {
          break;
        }
        catch (Exception e)
        {
          Console.WriteLine($"Module {moduleId} failed with exception {e.Message}");
          finishedSuccessfully = false;
          Console.WriteLine("Restarting.");
        }
      } while (!finishedSuccessfully && !cancellationToken.IsCancellationRequested);
      
    }
  }

  public class DebugConsoleLogger : ILogger
  {
    public void LogEvent(Guid moduleId, Severity severity, string reason, string message, Exception exception = null)
    {
      Console.WriteLine(message ?? "<No message>");
      Console.WriteLine(exception?.Message ?? "<No exception>");
    }

    public void LogEvent(IModule sourceModulem, Severity severity, string reason, string message, Exception exception = null)
    {
      Console.WriteLine(message ?? "<No message>");
      Console.WriteLine(exception?.Message ?? "<No exception>");
    }
  }
}
