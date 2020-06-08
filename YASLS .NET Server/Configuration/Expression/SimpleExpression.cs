using Newtonsoft.Json;

using System;

using YASLS.SDK.Library;

namespace YASLS.NETServer.Configuration
{
  public enum SimpleExpressionOperator { Equal, NotEqual, Greater, Less, GreaterEqual, LessEqual }

  public class SimpleExpression
  {
    [JsonProperty]
    public ValueExpression FirstValue { get; set; }

    [JsonProperty]
    public ValueExpression SecondValue { get; set; }

    [JsonProperty("Operator")]
    protected string OperatorStr { get; set; }

    [JsonIgnore]
    protected SimpleExpressionOperator _Operator;
    [JsonIgnore]
    protected bool OperatorParsed = false;

    [JsonIgnore]
    public SimpleExpressionOperator Operator
    {
      get
      {
        if (OperatorParsed) return _Operator;
        _Operator = (SimpleExpressionOperator)Enum.Parse(typeof(SimpleExpressionOperator), OperatorStr);
        OperatorParsed = true;
        return _Operator;
      }
    }

    public bool Evaluate(MessageDataItem dataItem)
    {
      Variant firstValue = FirstValue.GetValue(dataItem);
      Variant secondValue = SecondValue.GetValue(dataItem);
      switch(Operator)
      {
        case SimpleExpressionOperator.Equal: return firstValue == secondValue;
        case SimpleExpressionOperator.NotEqual: return firstValue != secondValue;
        case SimpleExpressionOperator.Greater: return firstValue > secondValue;
        case SimpleExpressionOperator.GreaterEqual: return firstValue >= secondValue;
        case SimpleExpressionOperator.Less: return firstValue < secondValue;
        case SimpleExpressionOperator.LessEqual: return firstValue <= secondValue;
      }
      return false;
    }
  }
}