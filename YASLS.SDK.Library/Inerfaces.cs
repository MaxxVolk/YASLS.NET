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
    /// Implementation of this method should return machine readable module name, typically full type name.
    /// </summary>
    /// <returns>Machine readable module name</returns>
    string GetModuleName();
    /// <summary>
    /// Implementation of this method should return human readable module name.
    /// </summary>
    /// <returns>Human readable module name</returns>
    string GetModuleDisplayName();
    /// <summary>
    /// Implementation of this method should return module vendor name.
    /// </summary>
    /// <returns>Module vendor name</returns>
    string GetModuleVendor();
    /// <summary>
    /// Implementation of this method should return a unique ID associated with this module.
    /// </summary>
    /// <returns>Module unique ID</returns>
    Guid GetModuleId();
    /// <summary>
    /// Implementation of this method should return current module version.
    /// </summary>
    /// <returns></returns>
    Version GetModuleVersion();
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
    /// <summary>
    /// Implementation if this method should allocate all used resources. This method can be executed multiple time if the module needs restart.
    /// </summary>
    void Initialize();
    /// <summary>
    /// Implementation if this method should free up all used resources. This method can be executed multiple time if the module needs restart.
    /// </summary>
    void Destroy();
  }

  /// <summary>
  /// Input modules ingress messages/events for further processing by other modules.
  /// </summary>
  public interface IInputModule : IThreadModule
  {
    /// <summary>
    /// Implementation if this method should initialize and verify module's configuration. This method shall not create any resources or 
    /// initiate objects implementing <c>IDisposable</c> interface.
    /// </summary>
    /// <param name="configuration">Content of 'ConfigurationJSON' object in JSON module definition.</param>
    /// <param name="cancellationToken">Cancellation token used by server to signal module thread to finish work.</param>
    /// <param name="attributes">Attributes associated with the module. Normally, module implementation shall add them to outgoing message.</param>
    /// <param name="queue">List of queues, where module shall send messages to.</param>
    void LoadConfiguration(JObject configuration, CancellationToken cancellationToken, Dictionary<string, string> attributes, IEnumerable<IServerMasterQueue> queue);
  }

  /// <summary>
  /// Output module sends messages/event outside of the server engine (for example to a file, or an API, etc.).
  /// </summary>
  public interface IOutputModule : IThreadModule
  {
    /// <summary>
    /// Implementation if this method should initialize and verify module's configuration. This method shall not create any resources or 
    /// initiate objects implementing <c>IDisposable</c> interface.
    /// </summary>
    /// <param name="configuration">Content of 'ConfigurationJSON' object in JSON module definition.</param>
    /// <param name="cancellationToken">Cancellation token used by server to signal module thread to finish work.</param>
    void LoadConfiguration(JObject configuration, CancellationToken cancellationToken);
    /// <summary>
    /// This method accepts inbound messages/events.
    /// </summary>
    /// <param name="message">Inbound message/event.</param>
    void Enqueue(MessageDataItem message);
  }

  /// <summary>
  /// Parser module is to re/structure an inbound message. It can completely replace or drop the inbound message.
  /// </summary>
  public interface IParserModule : IModule
  {
    /// <summary>
    /// Implementation if this method should initialize and verify module's configuration. This method shall not create any resources or 
    /// initiate objects implementing <c>IDisposable</c> interface.
    /// </summary>
    /// <param name="configuration">Content of 'ConfigurationJSON' object in JSON module definition.</param>
    /// <param name="cancellationToken">Cancellation token used by server to signal module thread to finish work.</param>
    void LoadConfiguration(JObject configuration, CancellationToken cancellationToken);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message">Inbound message/event.</param>
    /// <returns>Parsed message/event, new message/event, or null of the message is dropped.</returns>
    MessageDataItem Parse(MessageDataItem message);
  }

  /// <summary>
  /// Filter module determines if the current router branch is applicable for message processing.
  /// </summary>
  public interface IFilterModule : IModule
  {
    /// <summary>
    /// Implementation if this method should initialize and verify module's configuration. This method shall not create any resources or 
    /// initiate objects implementing <c>IDisposable</c> interface.
    /// </summary>
    /// <param name="configuration">Content of 'ConfigurationJSON' object in JSON module definition.</param>
    /// <param name="cancellationToken">Cancellation token used by server to signal module thread to finish work.</param>
    void LoadConfiguration(JObject configuration, CancellationToken cancellationToken);
    /// <summary>
    /// Returns true if an inbound message matches current route branch conditions.
    /// </summary>
    /// <param name="message">Inbound message/event.</param>
    /// <returns><see langword="true"/> if message/event should be processed further.</returns>
    bool IsMatch(MessageDataItem message);
  }

  /// <summary>
  /// Attribute Extractor module is to extract standard information from message. Attribute Extractor cannot change or replace
  /// message/event, but add attributes.
  /// </summary>
  public interface IAttributeExtractorModule : IModule
  {
    /// <summary>
    /// Implementation if this method should initialize and verify module's configuration. This method shall not create any resources or 
    /// initiate objects implementing <c>IDisposable</c> interface.
    /// </summary>
    /// <param name="configuration">Content of 'ConfigurationJSON' object in JSON module definition.</param>
    /// <param name="attributes"></param>
    void LoadConfiguration(JObject configuration, Dictionary<string, string> attributes);
    /// <summary>
    /// Analyses an inbound message and extract standard information into message attributes.
    /// </summary>
    /// <param name="message">Processed message.</param>
    void ExtractAttributes(MessageDataItem message);
  }

  /// <summary>
  /// Optional module interface. A module can implement this interface to provide configuration verification capabilities. 
  /// When implemented, the server will call the validation method prior to module initialization and then report user upon verification results.
  /// If verification fails, module initialization will be skipped.
  /// </summary>
  public interface IConfigurationVerifier
  {
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration">Content of 'ConfigurationJSON' object in JSON module definition.</param>
    /// <param name="validationErrors"></param>
    /// <returns>Validation results.</returns>
    bool IsValid(JObject configuration, out Dictionary<string, string> validationErrors);
  }

  /// <summary>
  /// Optional module interface. A module can implement this interface, if it needs to use server-provided logging, monitoring, and queue mechanism capabilities.
  /// </summary>
  public interface IServerBind
  {
    /// <summary>
    /// Called by server core, when module is initializing. Some references can be null.
    /// </summary>
    /// <param name="logger">Server-provided logger, if implemented. Otherwise null. Module can ignore this parameter if has no use.</param>
    /// <param name="healthReporter">Server-provided health reported, if implemented. Otherwise null. Module can ignore this parameter if has no use.</param>
    /// <param name="queueFactory">Server-provided queue factory, if implemented. Otherwise null. Module can ignore this parameter if has no use.</param>
    void RegisterServices(ILogger logger, IHealthReporter healthReporter, IQueueFactory queueFactory, IPersistentDataStore persistentStore);
  }
  #endregion

  #region Server Interfaces
  /// <summary>
  /// Server-core implemented interface providing Input modules with message/event sink point.
  /// </summary>
  public interface IServerMasterQueue
  {
    /// <summary>
    /// Sends a message/event into server's queue.
    /// </summary>
    /// <param name="message">Inbound message.</param>
    void Enqueue(MessageDataItem message);
  }
  #endregion

  #region Support Interfaces
  /// <summary>
  /// Logging event severity. Refer to <seealso cref="ILogger"/>
  /// </summary>
  public enum Severity { Debug, Informational, Warning, Error }

  /// <summary>
  /// Server-provided interface for modules need to log their own events.
  /// </summary>
  public interface ILogger
  {
    /// <summary>
    /// Logs a module event or error into current logging channel. <paramref name="sourceModule"/> and <paramref name="reason"/> might be used for event suppression.
    /// </summary>
    /// <param name="sourceModule">Self module reference.</param>
    /// <param name="severity">Event severity.</param>
    /// <param name="reason">Module component or workflow part identifier.</param>
    /// <param name="message">Human readable message.</param>
    /// <param name="exception">An exception if associated with this event, otherwise null.</param>
    void LogEvent(IModule sourceModule, Severity severity, string reason, string message, Exception exception = null);
  }

  /// <summary>
  /// Health state for a module component. Refer to <seealso cref="IHealthReporter"/>
  /// </summary>
  public enum HealthState { Healthy = 0, Warning = 1, Critical = 2, Unknown = -1 }

  /// <summary>
  /// Server-provided interface for modules to report their components health status to an external monitoring system.
  /// </summary>
  public interface IHealthReporter
  {
    /// <summary>
    /// Set module component health state to report module health to the core server and/or an external monitoring system.
    /// </summary>
    /// <param name="sourceModule">Self module reference.</param>
    /// <param name="component">Module component or workflow part identifier.</param>
    /// <param name="healthState">Component health state.</param>
    /// <param name="message">Optional message to associate with the health state.</param>
    void SetModuleHealth(IModule sourceModule, string component, HealthState healthState, string message = null);
    /// <summary>
    /// Set module component counter value to report module performance to the core server and/or an external monitoring system.
    /// </summary>
    /// <param name="sourceModule">Self module reference.</param>
    /// <param name="component">Module component or workflow part identifier.</param>
    /// <param name="counter">Counter name.</param>
    /// <param name="value">Counter value.</param>
    void SetPerformanceCounter(IModule sourceModule, string component, string counter, double value);
  }

  /// <summary>
  /// Server-provided interface to use within any module, which needs an internal queue.
  /// </summary>
  public interface IQueueFactory
  {
    /// <summary>
    /// Creates a new queue for the source module.
    /// </summary>
    /// <param name="sourceModule"></param>
    /// <returns>An object reference implementing <seealso cref="IMessageQueue"/> interface.</returns>
    IMessageQueue GetMessageQueue(IModule sourceModule);
  }

  /// <summary>
  /// Message/event queue interface if provided by the server.
  /// </summary>
  public interface IMessageQueue
  {
    void Enqueue(MessageDataItem message);
    bool TryDequeue(out MessageDataItem message);
    bool TryPeek(out MessageDataItem result);
    bool IsEmpty { get; }
  }

  /// <summary>
  /// Server provided interface for any module, which needs a persistent store for working data.
  /// </summary>
  public interface IPersistentDataStore
  {
    /// <summary>
    /// Creates module's own temporary storage folder.
    /// </summary>
    /// <param name="module">Self module reference.</param>
    /// <returns>Path to the temporary storage.</returns>
    string GetTempFilePath(IModule module);
    /// <summary>
    /// Saves module state object to a persistent storage.
    /// </summary>
    /// <param name="module">Self module reference.</param>
    /// <param name="stateObjectId">State object unique ID within module's scope.</param>
    /// <param name="state">State object.</param>
    /// <returns></returns>
    bool WriteModuleState(IModule module, string stateObjectId, object state);
    /// <summary>
    /// Reads state object from the persistent storage. The object is not preserved if the following changes: module version, module reference in server configuration.
    /// </summary>
    /// <param name="module">Self module reference.</param>
    /// <param name="stateObjectId">State object unique ID within module's scope.</param>
    /// <returns>State object.</returns>
    object ReadModuleState(IModule module, string stateObjectId);
  }
  #endregion
}
