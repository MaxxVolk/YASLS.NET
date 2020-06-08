using Newtonsoft.Json;

using System;
using System.Linq;

using YASLS.SDK.Library;

namespace YASLS.NETServer.Configuration
{
  public enum InExpressionOperator { In, NotIn, InclusiveIn, InclusiveNotIn }

  public class InExpression
  {
    [JsonProperty]
    public ValueExpression FirstValue { get; set; }

    [JsonProperty]
    public InValueExpression SecondValue { get; set; }

    [JsonProperty("Operator")]
    protected string OperatorStr { get; set; }

    [JsonIgnore]
    protected InExpressionOperator _Operator;
    [JsonIgnore]
    protected bool OperatorParsed = false;

    [JsonIgnore]
    public InExpressionOperator Operator
    {
      get
      {
        if (OperatorParsed) return _Operator;
        _Operator = (InExpressionOperator)Enum.Parse(typeof(InExpressionOperator), OperatorStr);
        OperatorParsed = true;
        return _Operator;
      }
    }

    public bool Evaluate(MessageDataItem dataItem)
    {
      Variant firstValue = FirstValue.GetValue(dataItem);
      if (SecondValue?.Range != null)
        switch (Operator)
        {
          case InExpressionOperator.In:
            return firstValue > SecondValue?.Range.StartValue.GetValue(dataItem) && firstValue < SecondValue?.Range.EndValue.GetValue(dataItem);
          case InExpressionOperator.InclusiveIn:
            return firstValue >= SecondValue?.Range.StartValue.GetValue(dataItem) && firstValue <= SecondValue?.Range.EndValue.GetValue(dataItem);
          case InExpressionOperator.NotIn:
            return firstValue <= SecondValue?.Range.StartValue.GetValue(dataItem) && firstValue >= SecondValue?.Range.EndValue.GetValue(dataItem);
          case InExpressionOperator.InclusiveNotIn:
            return firstValue < SecondValue?.Range.StartValue.GetValue(dataItem) && firstValue > SecondValue?.Range.EndValue.GetValue(dataItem);
        }
      if (SecondValue.List != null)
        switch (Operator)
        {
          case InExpressionOperator.In:
          case InExpressionOperator.InclusiveIn:
            return SecondValue.List.Any(x => x?.GetValue(dataItem) == firstValue);
          case InExpressionOperator.NotIn:
          case InExpressionOperator.InclusiveNotIn:
            return SecondValue.List.All(x => x?.GetValue(dataItem) != firstValue);
        }
      return false;
    }
  }
}