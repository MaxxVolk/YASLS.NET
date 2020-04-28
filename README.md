# YASLS.NET
## Overview ##

YASLS.NET is a modular message/event processing server. Its modular architecture provides flexibility and great extendibility. Built initially as a syslog server and parsing solution, it can play the following roles by combining standard and 3rd party extension modules.

* Simple syslog server saving messages to files.
* Logging system gateway.
* Message unification solution.
* Event parser.
* Log based alerting system.
* Event splitter/router.

## Architecture ##

YASLS.NET server features built in message queues and a message router. The message router on its own supports a built in regular expression filter. All other modules are attached to these core elements. There are the following modules types:

* Input Modules
* Output Modules
* Attribute Extractor Modules
* Filter Modules
* Parser Modules

The data flow within the server is the following:

==> Input Modules (M):(N) Queues => (1):(K) Routers => (1):(1) Filter (Always/RegExp/Filter Module) => (1):(1) Parser => (R):(L) Output Modules

## Configuration ##

Server configuration file is a JSON structured file. For users convenience, a JSON schema is supplied with the server. 

Server configuration starts from defining input modules. Each module definition (including other module types) consist of:
* .NET Class Library reference
* Managed type name (the type must support interfaces defined for particular module type)
* Link to external module configuration file (not implemented) == OR ==
* Embedded module configuration (schema varies on module implementation)
* List of static attributes. Module implementation must add them to all processed messages.

Next configuration section is a set of output modules. Output modules send events to external storages/systems. Examples of destinations are flat or structure files, web-hooks, alerting system, logging systems such as Splunk HTTP Event Collector, etc.

Then one or more queues shall be defined. Each queue can be assigned with multiple input modules. At the same time, the same input module can be associated with multiple queue. In other words, each queue can receive event from any combination of defined input modules. Each queue may contain one or more attribute extractor modules. They are for quick extraction of some very standard information bits from bypassing messages, such as host names, time stamps, syslog facility and priority values, etc.

While queues can intermix events from different inputs, the next element -- route, can only be attached to one input queue. However, each queue can send events to any number of routes. Each route do the following:
* Filter inbound messages/events for further processing by either:
  * Apply "Always" condition.
  * Apply Regular Expression filter, which may consist of a multiple regular expressions combined by AND / OR / ANY OF / NONE OF logical glue.
  * Apply custom filtering module.
* Parse message/event with an optional parsing module. NB! Parsing module's output not necessarily one-to-one for input and output messages. A parsing module can drop some or all events, and output events may not be directly related to input. Possible scenarios include statistic analysis of inbound flow with just summary output, or valve type flow control on an external signal, etc.
* Send events to one or more output modules. Like input modules and queues, any number of routes can send events to any number of output modules in any combination.