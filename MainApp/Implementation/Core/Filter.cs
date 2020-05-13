using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using YASLS.Configuration;
using YASLS.SDK.Library;

namespace YASLS.Core
{
  public class Filter : BaseInternalModule
  {
    protected readonly Guid moduleId = Guid.Parse("{3F119A41-CD4A-4488-B52B-42DF581BB38C}");
    protected Dictionary<string, string> Attributes = new Dictionary<string, string>();
    protected Expression FilterExpression = null;
    protected Parser Parser;
    protected bool stopIfMatched = false;

    // modules
    protected List<IAttributeExtractorModule> Extractors = new List<IAttributeExtractorModule>();

    public Filter(FilterDefinition cfg, ILogger logger, IHealthReporter healthReporter, CancellationToken cancellationToken, IModuleResolver moduleResolver) : base(logger, healthReporter)
    {
      FilterExpression = cfg.Expression?.ToObject<Expression>();
      FilterExpression?.ResolveModules(moduleResolver, cancellationToken);
      stopIfMatched = cfg.StopIfMatched;

      // add Parser
      Parser = new Parser(cfg.Parser, logger, healthReporter, cancellationToken, moduleResolver);

      // add Attribute Extractors to the parser
      if (cfg.AttributeExtractors != null)
        foreach (KeyValuePair<string, ModuleDefinition> extractorCfg in cfg.AttributeExtractors)
          try
          {
            ModuleDefinition extractorDef = extractorCfg.Value;
            IAttributeExtractorModule newExtractorModule = moduleResolver.CreateModule<IAttributeExtractorModule>(extractorDef);
            newExtractorModule.LoadConfiguration(extractorDef.ConfigurationJSON, extractorDef.Attributes);
            Extractors.Add(newExtractorModule);
          }
          catch (Exception e)
          {
            Logger?.LogEvent(this, Severity.Warning, "AttributeExtractorInit", $"Failed to load or initialize '{extractorCfg.Key ?? "<Unknown>"}' attribute parser module.", e);
          }

      if (cfg.Attributes != null && cfg.Attributes.Count > 0)
        foreach (KeyValuePair<string, string> attr in cfg.Attributes)
          Attributes.Add(attr.Key, attr.Value);
    }

    public bool ProcessMessageShouldStop(MessageDataItem message)
    {
      try
      {
        bool filterMatch = FilterExpression?.Evaluate(message) ?? true;

        if (filterMatch)
        {
          MessageDataItem sendingMessage = message.Clone();
          foreach (KeyValuePair<string, string> attr in Attributes)
            sendingMessage.AddAttribute(attr.Key, attr.Value);

          foreach (IAttributeExtractorModule extractor in Extractors)
            try
            {
              extractor.ExtractAttributes(sendingMessage);
            }
            catch (Exception e)
            {
              Logger?.LogEvent(this, Severity.Error, "ExtractorInvoke", $"Failed to execute extractor {extractor.GetType().FullName}.", e);
            }

          Parser.ParseMessage(sendingMessage);

          return stopIfMatched;
          //if (stopIfMatched) // && filterMatch
          //  return true; // shall stop
          //else
          //  return false; // shall proceed
        }
        else
          return false; // not matched => shall proceed
      }
      catch (Exception e)
      {
        Logger?.LogEvent(this, Severity.Error, "FilterAction", "Exception in filter", e);
        return false;
      }
    }

    #region IModule Implementation
    public override string GetModuleDisplayName() => "Main Filter Module";

    public override Guid GetModuleId() => moduleId;
    #endregion
  }
}
