using Newtonsoft.Json;

using System;

using YASLS.SDK.Library;

namespace YASLS.NETServer.Configuration
{
  public enum RegExExpressionOperator { Matches, NotMatches }

  public class RegExExpression
  {
    [JsonProperty]
    public ValueExpression FirstValue { get; set; }

    [JsonProperty]
    public RegularExpressionsExpression RegularExpressions { get; set; }

    [JsonProperty("Operator")]
    protected string OperatorStr { get; set; }

    [JsonIgnore]
    protected RegExExpressionOperator _Operator;
    [JsonIgnore]
    protected bool OperatorParsed = false;

    [JsonIgnore]
    public RegExExpressionOperator Operator
    {
      get
      {
        if (OperatorParsed) return _Operator;
        _Operator = (RegExExpressionOperator)Enum.Parse(typeof(RegExExpressionOperator), OperatorStr);
        OperatorParsed = true;
        return _Operator;
      }
    }

    public bool Evaluate(MessageDataItem dataItem)
    {
      Variant firstValue = FirstValue.GetValue(dataItem);
      string message = null;
      switch (firstValue.Type)
      {
        case VariantType.Boolean: message = firstValue.BooleanValue.ToString(); break;
        case VariantType.DateTime: message = firstValue.DateTimeValue.ToString("o"); break;
        case VariantType.Float: message = firstValue.FloatValue.ToString(); break;
        case VariantType.Int: message = firstValue.IntValue.ToString(); break;
        case VariantType.String: message = firstValue.StringValue; break;
      }

      if (Operator == RegExExpressionOperator.Matches)
        return RegularExpressions.Evaluate(message);
      if (Operator == RegExExpressionOperator.NotMatches)
        return ! RegularExpressions.Evaluate(message);

      return false;
    }
  }
}