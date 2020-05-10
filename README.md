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

## Standard Modules ##
### Released Modules ###
* Syslog Input Module (TCP and UDP transport supported, TLS support is planned)
* Console Debug Output Module
* Syslog Facility and Severity Code Attribute Extractor Module
* Reverse DNS Lookup Attribute Extractor Module
* Splunk HTTP Event Collector Output Module
* Regular Expression Parser Module (probably the only parser you need).
### Modules in planning / beta ###
* File Input Module
* File Output Module
* Web-hook Output Module
* VMware event ingress API Input Module

### Treading Model ###

* Each Input and Output module runs in its own thread.
* Each Queue runs in its own thread together with any Attribute Extractor Modules attached (i.e. attribute extractors running within the same thread as the main queue process).
* Each Route runs in its own thread together with optional Filter and Parser Modules attached.

### Module Instances ###

Input and Output modules running as single instances. When they referenced in Routes and Queues, then all message enqueue and dequeue operations performed to the same module instance, i.e. Input and Output modules should be capable with concurrency.
In opposite, Attribute Extractors, Parsers and Filters Modules running within a context of owning Queue or Route, and the server engine creates a separate instance for each Queue or Route. Therefore, Attribute Extractors, Parsers and Filters Modules shall not be thread safe, and can have different configuration for each instance.

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

## Standard Modules Description ##

### Regular Expression Parser Module ###

Example configuration:

```json
          "Parser": {
            "Assembly": null,
            "Type": "YASLS.RegExParser",
            "ConfigurationJSON": {
              "ParsingExpressions": [
                {
                  "MatchingRegEx": null,
                  "ParsingRegEx": "(?<name>(?:\"(?<name1>[^\"]*)\"|[^=,| ])*)\\s*=\\s*(?<value>(?:\"[^\"]*\"|[^=,| ])*)",
                  "StopIfMatched": false,
                  "MultiMatch": true,
                  "FieldSettings": [
                    {
                      "Input": {
                        "Group": "value",
                        "Type": "String",
                        "GroupToOutputAttribute": "name"
                      }
                    },
                    {
                  "MatchingRegEx": "\\<(?<PRIVAL>\\d{1,3})\\>(?<SYSLOGTIME>(\\w{3} \\d{1,2} \\d{1,2}:\\d{1,2}:\\d{1,2})) (?<HOST>\\w+) ",
                  "ParsingRegEx": "\\<(?<PRIVAL>\\d{1,3})\\>(?<SYSLOGTIME>(\\w{3} \\d{1,2} \\d{1,2}:\\d{1,2}:\\d{1,2})) (?<HOST>\\w+) ",
                  "FieldSettings": [
                    // keep the original message as it is
                    {
                      "Input": {
                        "Message": true
                      },
                      "OutputAttribute": null
                    },
                    // add attributes
                    {
                      "Input": {
                        "Group": "HOST",
                        "Type": "String"
                      },
                      "OutputAttribute": "Host"
                    },
                    {
                      "Input": {
                        "Group": "SYSLOGTIME",
                        "Type": "String"
                      },
                      "OutputAttribute": "SyslogTime"
                    }
                  ]
                }
                  ]
                }
              ],
              // for all messages
              "DefaultFieldSettings": [
                {
                  "Input": {
                    "Attribute": "Facility"
                  },
                  "OutputAttribute": "SyslogFacility"
                },
                {
                  "Input": {
                    "Attribute": "Severity"
                  },
                  "OutputAttribute": "SyslogSeverity"
                },
                {
                  "Input": {
                    "Value": "daas",
                    "Type": "String"
                  },
                  "OutputAttribute": "source"
                },
                {
                  "Input": {
                    "Value": "_json",
                    "Type": "String"
                  },
                  "OutputAttribute": "sourcetype"
                }
              ],
              // for not parsed messages
              "PassthroughFieldSettings": [
                // keep the original message as it is
                {
                  "Input": {
                    "Message": true
                  },
                  "OutputAttribute": null
                },
                // add attributes
                {
                  "Input": {
                    "Attribute": "SenderHostName"
                  },
                  "OutputAttribute": "Host"
                },
                {
                  "Input": {
                    "Attribute": "ReciveTimestamp",
                    "Type": "DateTime"
                  },
                  "OutputAttribute": "EventTimestamp"
                }
              ]
            }
          }
```

