using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Threading;

namespace YASLS.SDK.Library
{
  #region Module Interfaces
  /// <summary>
  /// Base interface for all modules. Provides module identity.
  /// </summary>
  public interface IModule
  {
    /// <summary>
    /// Implementation of this module should return machine readable module name, typically full type name.
    /// </summary>
    /// <returns>Machine readable module name</returns>
    string GetModuleName();
    /// <summary>
    /// Implementation of this module should return human readable module name.
    /// </summary>
    /// <returns>Human readable module name</returns>
    string GetModuleDisplayName();
    /// <summary>
    /// Implementation of this module should return module vendor name.
    /// </summary>
    /// <returns>Module vendor name</returns>
    string GetModuleVendor();
    /// <summary>
    /// Returns a unique ID associated with this module.
    /// </summary>
    /// <returns>Module unique ID</returns>
    Guid GetModuleId();
  }

  /// <summary>
  /// Base interface for modules, which run in their own thread.
  /// </summary>
  public interface IThreadModule : IModule
  {
    /// <summary>
    /// Implementation of this module should return an entry point for module working thread.
    /// </summary>
    /// <returns>Module thread entry point</returns>
    ThreadStart GetWorker();
  }

  /// <summary>
  /// Input modules ingress messages/events for further processing by other modules.
  /// </summary>
  public interface IInputModule : IThreadModule
  {
    /// <summary>
    /// Implementation if this method should initialize and verify module's configuration. **This ** 
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="attributes"></param>
    /// <param name="queue"></param>
    void Initialize(JObject configuration, CancellationToken cancellationToken, Dictionary<string, string> attributes, IEnumerable<IQueueModule> queue);
    void Destroy();
  }

  public interface IOutputModule : IThreadModule
  {
    void Initialize(JObject configuration, CancellationToken cancellationToken);
    void Enqueue(MessageDataItem message);
    void Destroy();
  }

  public interface IParserModule : IModule
  {
    void Initialize(JObject configuration, CancellationToken cancellationToken);
    MessageDataItem Parse(MessageDataItem message);
  }

  public interface IFilterModule : IModule
  {
    void Initialize(JObject configuration, CancellationToken cancellationToken);
    bool IsMatch(MessageDataItem message);
  }

  public interface IAttributeExtractorModule : IModule
  {
    void Initialize(JObject configuration, Dictionary<string, string> attributes);
    void ExtractAttributes(MessageDataItem message);
  }
  #endregion

  #region Server Interfaces
  public interface IQueueModule
  {
    void Enqueue(MessageDataItem message);
  }

  public interface IServerBind
  {
    void RegisterServices(ILogger logger, IHealthReporter healthReporter, IQueueFactory queueFactory);
  }

  public enum Severity { Debug, Informational, Warning, Error }

  public interface ILogger
  {
    void LogEvent(Guid moduleId, Severity severity, string reason, string message, Exception exception = null);
    void LogEvent(IModule sourceModulem, Severity severity, string reason, string message, Exception exception = null);
  }

  public enum HealthState { Healthy = 0, Warning = 1, Critical = 2, Unknown = -1 }

  public interface IHealthReporter
  {
    void SetModuleHealth(Guid moduleId, string component, HealthState healthState);
  }

  public interface IQueueFactory
  {
    IMessageQueue GetMessageQueue(IModule sourceModulem);
  }

  public interface IMessageQueue
  {
    void Enqueue(MessageDataItem message);
    bool TryDequeue(out MessageDataItem message);
    bool TryPeek(out MessageDataItem result);
    bool IsEmpty { get; }
  }
  #endregion

  #region Support Interfaces

  #endregion
}
