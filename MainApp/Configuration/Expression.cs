using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using YASLS.Core;
using YASLS.SDK.Library;

namespace YASLS.Configuration
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
    public AttributeExpression Exists { get; set; }
    [JsonProperty]
    public SimpleExpression SimpleExpression { get; set; }
    [JsonProperty]
    public InExpression InExpression { get; set; }
    [JsonProperty]
    public RegExExpression RegExExpression { get; set; }

    internal void ResolveModules(IModuleResolver moduleResolver, CancellationToken cancellationToken)
    {
      // self
      if (ModuleExpression != null)
      {
        ModuleExpressionModule = moduleResolver.CreateModule<IFilterModule>(ModuleExpression);
        ModuleExpressionModule.LoadConfiguration(ModuleExpression.ConfigurationJSON, cancellationToken);
      }
      // then recurse
      And?.ForEach(x => x.ResolveModules(moduleResolver, cancellationToken));
      And?.ForEach(x => x.ResolveModules(moduleResolver, cancellationToken));
      Not?.ResolveModules(moduleResolver, cancellationToken);
    }

    [JsonProperty]
    public ModuleDefinition ModuleExpression { get; set; }

    [JsonIgnore]
    protected IFilterModule ModuleExpressionModule { get; set; } = null;

    public bool Evaluate(MessageDataItem dataItem)
    {
      if (And != null)
        return And.All(x => x?.Evaluate(dataItem) == true);
      if (Or != null)
        return And.Any(x => x?.Evaluate(dataItem) == true);
      if (Not != null)
        return !Not.Evaluate(dataItem);
      if (Exists != null)
        if (dataItem.AttributeExists(Exists.Name ?? ""))
          if (dataItem.GetAttributeAsVariant(Exists.Name).Type == Exists.Type)
            return true;
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

  public class RegularExpressionsExpression
  {
    [JsonProperty("Options", NullValueHandling = NullValueHandling.Ignore)]
    public int Options { get; set; } = 0;

    [JsonProperty("And", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> And { get; set; }

    [JsonProperty("Or", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> Or { get; set; }

    [JsonProperty("NotAll", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> NotAll { get; set; }

    [JsonProperty("NotAny", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> NotAny { get; set; }

    public bool Evaluate(string rawMessage)
    {
      if (rawMessage == null)
        return false;

      bool AndResult = true, OrResult = true, NotAllResult = true, NotAnyResult = true;
      if (And != null && And.Count > 0)
        AndResult = And.All(x => Regex.IsMatch(rawMessage, x, (RegexOptions)Options));
      if (Or != null && Or.Count > 0)
        OrResult = Or.Any(x => Regex.IsMatch(rawMessage, x, (RegexOptions)Options));
      if (NotAll != null && NotAll.Count > 0)
        NotAllResult = !NotAll.All(x => Regex.IsMatch(rawMessage, x, (RegexOptions)Options));
      if (NotAny != null && NotAny.Count > 0)
        NotAnyResult = !NotAny.Any(x => Regex.IsMatch(rawMessage, x, (RegexOptions)Options));

      return AndResult && OrResult && NotAllResult && NotAnyResult;
    }
  }

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
            return SecondValue.List.Any(x => x?.Value == firstValue);
          case InExpressionOperator.NotIn:
          case InExpressionOperator.InclusiveNotIn:
            return SecondValue.List.All(x => x?.Value != firstValue);
        }
      return false;
    }
  }

  public class InValueExpression
  {
    [JsonProperty]
    public List<ValueValueExpression> List { get; set; }

    [JsonProperty]
    public InRangeValueExpression Range { get; set; }
  }

  public class InRangeValueExpression
  {
    [JsonProperty]
    public ValueExpression StartValue { get; set; }
    [JsonProperty]
    public ValueExpression EndValue { get; set; }
  }

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

  public class ValueExpression
  {
    [JsonProperty("Attribute")]
    protected AttributeExpression Attribute { get; set; }

    [JsonProperty("Value")]
    protected ValueValueExpression Value { get; set; }

    
    public Variant GetValue(MessageDataItem dataItem)
    {
      if (Value != null)
        return Value.Value;
      if (Attribute != null)
      {
        var result = dataItem.GetAttributeAsVariant(Attribute.Name);
        if (result.Type != Attribute.Type)
          throw new InvalidCastException("Attribute type mismatch.");
        return result;
      }
      throw new InvalidOperationException("Value Expression is not defined.");
    }
  }

  public class AttributeExpression
  {
    [JsonProperty("Name")]
    public string Name { get; set; }

    [JsonProperty("Type")]
    protected string TypeStr { get; set; }
    [JsonIgnore]
    protected VariantType _Type;
    [JsonIgnore]
    protected bool TypeParsed = false;

    [JsonIgnore]
    public VariantType Type
    {
      get
      {
        if (TypeParsed) return _Type;
        _Type = (VariantType)Enum.Parse(typeof(VariantType), TypeStr);
        TypeParsed = true;
        return _Type;
      }
    }
  }

  public class ValueValueExpression
  {
    [JsonProperty("IntValue")]
    protected int? IntValue { get; set; }
    [JsonProperty("FloatValue")]
    protected double? FloatValue { get; set; }
    [JsonProperty("StringValue")]
    protected string StringValue { get; set; }
    [JsonProperty("DateTimeValue")]
    protected DateTime? DateTimeValue { get; set; }
    [JsonProperty("BooleanValue")]
    protected bool? BooleanValue { get; set; }


    [JsonIgnore]
    Variant _Value;
    [JsonIgnore]
    bool ValueParsed = false;
    [JsonIgnore]
    public Variant Value
    {
      get
      {
        if (ValueParsed)
          return _Value;
        if (StringValue!=null)
        {
          _Value = new Variant() { StringValue = StringValue, Type = VariantType.String };
          ValueParsed = true;
        }
        if (IntValue != null)
        {
          _Value = new Variant() { IntValue = IntValue ?? 0, Type = VariantType.Int };
          ValueParsed = true;
        }
        if (FloatValue != null)
        {
          _Value = new Variant() { FloatValue = FloatValue ?? 0, Type = VariantType.Float };
          ValueParsed = true;
        }
        if (DateTimeValue != null)
        {
          _Value = new Variant() { DateTimeValue = DateTimeValue ?? DateTime.MinValue, Type = VariantType.DateTime };
          ValueParsed = true;
        }
        if (BooleanValue != null)
        {
          _Value = new Variant() { BooleanValue = BooleanValue ?? false, Type = VariantType.Boolean };
          ValueParsed = true;
        }
        if (!ValueParsed)
        {
          // default to null string
          _Value = new Variant() { StringValue = null, Type = VariantType.String };
          ValueParsed = true;
        }
        return _Value;
      }
    }
  }
}