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
  class Filter : IModule
  {
    // Filter internals
    protected YASLServer Server;
    protected string FilterKey;

    
    protected Expression FilterExpression = null;
    protected Parser Parser;

    protected bool StopIfMatched = false;

    public Filter(string filterKey, FilterDefinition filterDefinition, Dictionary<string, OutputModuleWrapper> outputs, YASLServer server)
    {
      Server = server;
      FilterKey = filterKey;
      StopIfMatched = filterDefinition.StopIfMatched;

      FilterExpression = filterDefinition.Expression?.ToObject<Expression>();
      // FilterExpression?.ResolveModules($"{rootPath}\\Expression", cancellationToken, moduleCreators, bindInfo);

      Parser = new Parser(filterDefinition.Parser, outputs, Server);
    }

    public bool ProcessMessageShouldStop(MessageDataItem message)
    {
      try
      {
        bool filterMatch = FilterExpression?.Evaluate(message) ?? true;

        if (filterMatch)
        {
          Parser.ParseMessage(message);

          return StopIfMatched;
          //if (stopIfMatched && filterMatch)
          //  return true; // shall stop
          //else
          //  return false; // shall proceed
        }
        else
          return false; // not matched => shall proceed
      }
      catch (Exception e)
      {
        Server.Logger?.LogEvent(this, Severity.Error, "FilterAction", "Exception in filter", e);
        return false;
      }
    }

    #region IModule Implementation
    protected readonly Guid moduleId = Guid.Parse("{3F119A41-CD4A-4488-B52B-42DF581BB38C}");

    public string GetModuleDisplayName() => "YASLS .NET Server Filter";

    public Guid GetModuleId() => moduleId;

    public string GetModuleName() => GetType().FullName;

    public string GetModuleVendor() => "YASLS";

    public Version GetModuleVersion() => Assembly.GetAssembly(GetType()).GetName().Version;

    public void LoadConfiguration(JObject configuration)
    {
      // never used
    }
    #endregion
  }
}
