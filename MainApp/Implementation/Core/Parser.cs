using System;
using System.Collections.Generic;
using System.Threading;
using YASLS.Configuration;
using YASLS.SDK.Library;

namespace YASLS.Core
{
  public class Parser : BaseInternalModule
  {
    protected readonly Guid moduleId = Guid.Parse("{B0760569-8AE8-4373-9C92-D3C94D19C4C1}");
    protected Dictionary<string, string> Attributes = new Dictionary<string, string>();
    
    protected List<IOutputModule> OutputModules = new List<IOutputModule>();
    protected IParserModule ParserModule;

    public Parser(ParserDefinition cfg, ILogger logger, IHealthReporter healthReporter, CancellationToken cancellationToken, IModuleResolver moduleResolver) : base(logger, healthReporter)
    {
      if (cfg.ParsingModule != null)
      {
        ParserModule = moduleResolver.CreateModule<IParserModule>(cfg.ParsingModule);
        ParserModule.LoadConfiguration(cfg.ParsingModule.ConfigurationJSON, cancellationToken);
      }
      foreach (string outputRefName in cfg.Output)
      {
        IOutputModule output = moduleResolver.GetModuleReference<IOutputModule>(outputRefName);
        if (output != null)
          OutputModules.Add(output);
        else
          Logger?.LogEvent(this, Severity.Warning, "ParserInit", $"Failed to link referenced Output Module: {outputRefName}. The module not found.");
      }
    }

    public void ParseMessage(MessageDataItem message)
    {
      MessageDataItem parsedMessage = null;
      try
      {
        parsedMessage = ParserModule?.Parse(message) ?? message;
      }
      catch (Exception e)
      {
        Logger?.LogEvent(this, Severity.Error, "Parsing", "Failed to parse message.", e);
        parsedMessage = null;
      }
      foreach (IOutputModule module in OutputModules)
      {
        module.Enqueue(parsedMessage ?? message);
      }
    }

    public override string GetModuleDisplayName() => "Base Internal Parser Module";
    public override Guid GetModuleId() => moduleId;
  }
}
