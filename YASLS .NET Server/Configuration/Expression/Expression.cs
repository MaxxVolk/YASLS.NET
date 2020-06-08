using Newtonsoft.Json;

using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading;
using YASLS.SDK.Library;

namespace YASLS.NETServer.Configuration
{
  public class Expression
  {
    [JsonProperty]
    public List<Expression> And { get; set; }
    [JsonProperty]
    public List<Expression> Or { get; set; }
    [JsonProperty]
    public Expression Not { get; set; }
    [JsonProperty]
    public ExistsExpression Exists { get; set; }
    [JsonProperty]
    public SimpleExpression SimpleExpression { get; set; }
    [JsonProperty]
    public InExpression InExpression { get; set; }
    [JsonProperty]
    public RegExExpression RegExExpression { get; set; }

    //internal void ResolveModules(string rootPath, CancellationToken cancellationToken, ModuleCreators moduleCreators, ServerBindInfo bindInfo)
    //{
    //  // self
    //  if (ModuleExpressionDef != null)
    //  {
    //    ModuleExpression = moduleCreators.FilterCreator.Invoke("", $"{rootPath}\\ModuleExpression\\{ModuleExpressionDef.ManagedTypeName}", ModuleExpressionDef, null, bindInfo);
    //  }
    //  // then recurse
    //  And?.ForEach(x => x.ResolveModules($"{rootPath}\\And", cancellationToken, moduleCreators,bindInfo));
    //  Or?.ForEach(x => x.ResolveModules($"{rootPath}\\Or", cancellationToken, moduleCreators, bindInfo));
    //  Not?.ResolveModules($"{rootPath}\\Not", cancellationToken, moduleCreators, bindInfo);
    //}

    //[JsonIgnore]
    //public ModuleInfo<IFilterModule> ModuleExpression;

    [JsonIgnore]
    protected IFilterModule ModuleExpressionRuntime;

    [JsonProperty("ModuleExpression")]
    public ModuleDefinition ModuleExpressionDef { get; set; }

    [JsonIgnore]
    protected IFilterModule ModuleExpressionModule { get; set; } = null;

    public bool Evaluate(MessageDataItem dataItem)
    {
      if (And != null)
        return And.All(x => x?.Evaluate(dataItem) == true);
      if (Or != null)
        return Or.Any(x => x?.Evaluate(dataItem) == true);
      if (Not != null)
        return !Not.Evaluate(dataItem);
      if (Exists != null)
      {
        if (Exists.Attribute != null)
          if (dataItem.AttributeExists(Exists.Attribute.Name ?? ""))
            if (dataItem.GetAttributeAsVariant(Exists.Attribute.Name).Type == Exists.Attribute.Type)
              return true;
      }
      if (SimpleExpression != null)
        return SimpleExpression.Evaluate(dataItem);
      if (InExpression != null)
        return InExpression.Evaluate(dataItem);
      if (RegExExpression != null)
        return RegExExpression.Evaluate(dataItem);
      if (ModuleExpressionModule != null)
        return ModuleExpressionModule.IsMatch(dataItem);

      return false;
    }
  }
}