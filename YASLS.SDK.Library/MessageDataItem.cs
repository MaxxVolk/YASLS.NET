using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YASLS.SDK.Library
{
  public class MessageDataItem
  {
    public string Message { get; }

    protected Dictionary<string, Variant> _attributes = new Dictionary<string, Variant>();

    public MessageDataItem(string message) => Message = message;

    #region Add*
    public void AddAttribute(string name, string value)
    {
      if (_attributes.ContainsKey(name))
        throw new ArgumentException("Key already exists.", "name");
      else
        _attributes.Add(name, new Variant { Type = VariantType.String, StringValue = value });
    }

    public void AddAttribute(string name, double value)
    {
      if (_attributes.ContainsKey(name))
        throw new ArgumentException("Key already exists.", "name");
      else
        _attributes.Add(name, new Variant { Type = VariantType.Float, FloatValue = value });
    }

    public void AddAttribute(string name, long value)
    {
      if (_attributes.ContainsKey(name))
        throw new ArgumentException("Key already exists.", "name");
      else
        _attributes.Add(name, new Variant { Type = VariantType.Int, IntValue = value });
    }

    public void AddAttribute(string name, Variant value)
    {
      if (_attributes.ContainsKey(name))
        throw new ArgumentException("Key already exists.", "name");
      else
        _attributes.Add(name, value);
    }

    public void AddAttribute(string name, bool value)
    {
      if (_attributes.ContainsKey(name))
        throw new ArgumentException("Key already exists.", "name");
      else
        _attributes.Add(name, new Variant { Type = VariantType.Boolean, BooleanValue = value });
    }

    public void AddAttribute(string name, DateTime value)
    {
      if (_attributes.ContainsKey(name))
        throw new ArgumentException("Key already exists.", "name");
      else
        _attributes.Add(name, new Variant { Type = VariantType.DateTime, DateTimeValue = value });
    }
    #endregion

    #region Get*
    public long GetAttributeAsInt(string name) => _attributes[name].Type == VariantType.Int ? _attributes[name].IntValue : throw new ArgumentException("Invalid attribute type.");
    public double GetAttributeAsFloat(string name) => _attributes[name].Type == VariantType.Float ? _attributes[name].FloatValue : throw new ArgumentException("Invalid attribute type.");
    public string GetAttributeAsString(string name) => _attributes[name].Type == VariantType.String ? _attributes[name].StringValue : throw new ArgumentException("Invalid attribute type.");
    public DateTime GetAttributeAsDateTime(string name) => _attributes[name].Type == VariantType.DateTime ? _attributes[name].DateTimeValue : throw new ArgumentException("Invalid attribute type.");
    public bool GetAttributeAsBoolean(string name) => _attributes[name].Type == VariantType.Boolean ? _attributes[name].BooleanValue : throw new ArgumentException("Invalid attribute type.");
    public Variant GetAttributeAsVariant(string name) => _attributes[name];
    #endregion

    #region Update*
    public void UpdateAttribute(string name, long value)
    {
      if (_attributes[name].Type == VariantType.Int)
        _attributes[name] = new Variant { Type = VariantType.Int, IntValue = value };
      else
        throw new ArgumentException("Invalid attribute type.");
    }

    public void UpdateAttribute(string name, double value)
    {
      if (_attributes[name].Type == VariantType.Float)
        _attributes[name] = new Variant { Type = VariantType.Float, FloatValue = value };
      else
        throw new ArgumentException("Invalid attribute type.");
    }

    public void UpdateAttribute(string name, string value)
    {
      if (_attributes[name].Type == VariantType.String)
        _attributes[name] = new Variant { Type = VariantType.String, StringValue = value };
      else
        throw new ArgumentException("Invalid attribute type.");
    }

    public void UpdateAttribute(string name, DateTime value)
    {
      if (_attributes[name].Type == VariantType.DateTime)
        _attributes[name] = new Variant { Type = VariantType.DateTime, DateTimeValue = value };
      else
        throw new ArgumentException("Invalid attribute type.");
    }

    public void UpdateAttribute(string name, bool value)
    {
      if (_attributes[name].Type == VariantType.Boolean)
        _attributes[name] = new Variant { Type = VariantType.Boolean, BooleanValue = value };
      else
        throw new ArgumentException("Invalid attribute type.");
    }
    #endregion

    public IEnumerable<string> GetAttributeNames => _attributes.Keys;

    public bool AttributeExists(string name) => _attributes.ContainsKey(name);

    public MessageDataItem Clone()
    {
      MessageDataItem result = new MessageDataItem(Message);
      foreach (var attr in _attributes)
        result._attributes.Add(attr.Key, attr.Value);
      return result;
    }
  }

  public enum VariantType { Int, Float, String, DateTime, Boolean }

  public struct Variant
  {
    public VariantType Type;
    public long IntValue;
    public double FloatValue;
    public string StringValue;
    public DateTime DateTimeValue;
    public bool BooleanValue;

    public override string ToString()
    {
      switch (this.Type)
      {
        case VariantType.Boolean:
          return BooleanValue.ToString();
        case VariantType.DateTime:
          return DateTimeValue.ToString("yyyy-MM-dd HH:mm:ss");
        case VariantType.Float:
          return FloatValue.ToString();
        case VariantType.Int:
          return IntValue.ToString();
        case VariantType.String:
          return StringValue;
      }
      return "<Unknown Value Type>";
    }

    public static bool operator ==(Variant a, Variant b)
    {
      if (a == null || b == null)
        return false;
      if (a == null && b == null)
        return true;
      if (a.Type != b.Type)
        throw new InvalidCastException("Cannot compare Variant of different type");
      switch (a.Type)
      {
        case VariantType.String: return a.StringValue == b.StringValue;
        case VariantType.Boolean: return a.BooleanValue == b.BooleanValue;
        case VariantType.DateTime: return a.DateTimeValue == b.DateTimeValue;
        case VariantType.Float: return a.FloatValue == b.FloatValue;
        case VariantType.Int: return a.IntValue == b.IntValue;
      }
      return false;
    }

    public static bool operator !=(Variant a, Variant b)
    {
      if (a == null && b == null)
        return false;
      if (a == null || b == null)
        return true;
      if (a.Type != b.Type)
        throw new InvalidCastException("Cannot compare Variant of different type");
      switch (a.Type)
      {
        case VariantType.String: return a.StringValue != b.StringValue;
        case VariantType.Boolean: return a.BooleanValue != b.BooleanValue;
        case VariantType.DateTime: return a.DateTimeValue != b.DateTimeValue;
        case VariantType.Float: return a.FloatValue != b.FloatValue;
        case VariantType.Int: return a.IntValue != b.IntValue;
      }
      return false;
    }

    public static bool operator >=(Variant a, Variant b)
    {
      if (a == null || b == null)
        return false;
      if (a.Type != b.Type)
        throw new InvalidCastException("Cannot compare Variant of different type");
      switch (a.Type)
      {
        case VariantType.String: return a.StringValue.CompareTo(b.StringValue) > 0 || a.StringValue.CompareTo(b.StringValue) == 0;
        case VariantType.Boolean: return (a.BooleanValue && !b.BooleanValue) || (a.BooleanValue == b.BooleanValue) ? true : false;
        case VariantType.DateTime: return a.DateTimeValue >= b.DateTimeValue;
        case VariantType.Float: return a.FloatValue >= b.FloatValue;
        case VariantType.Int: return a.IntValue >= b.IntValue;
      }
      return false;
    }

    public static bool operator <=(Variant a, Variant b)
    {
      if (a == null || b == null)
        return false;
      if (a.Type != b.Type)
        throw new InvalidCastException("Cannot compare Variant of different type");
      switch (a.Type)
      {
        case VariantType.String: return a.StringValue.CompareTo(b.StringValue) < 0 || a.StringValue.CompareTo(b.StringValue) == 0;
        case VariantType.Boolean: return (!a.BooleanValue && b.BooleanValue) || (a.BooleanValue == b.BooleanValue) ? true : false;
        case VariantType.DateTime: return a.DateTimeValue <= b.DateTimeValue;
        case VariantType.Float: return a.FloatValue <= b.FloatValue;
        case VariantType.Int: return a.IntValue <= b.IntValue;
      }
      return false;
    }

    public static bool operator >(Variant a, Variant b)
    {
      if (a == null || b == null)
        return false;
      if (a.Type != b.Type)
        throw new InvalidCastException("Cannot compare Variant of different type");
      switch (a.Type)
      {
        case VariantType.String: return a.StringValue.CompareTo(b.StringValue) > 0;
        case VariantType.Boolean: return a.BooleanValue && !b.BooleanValue ? true : false;
        case VariantType.DateTime: return a.DateTimeValue > b.DateTimeValue;
        case VariantType.Float: return a.FloatValue > b.FloatValue;
        case VariantType.Int: return a.IntValue > b.IntValue;
      }
      return false;
    }

    public static bool operator <(Variant a, Variant b)
    {
      if (a == null || b == null)
        return false;
      if (a.Type != b.Type)
        throw new InvalidCastException("Cannot compare Variant of different type");
      switch (a.Type)
      {
        case VariantType.String: return a.StringValue.CompareTo(b.StringValue) < 0;
        case VariantType.Boolean: return !a.BooleanValue && b.BooleanValue ? true : false;
        case VariantType.DateTime: return a.DateTimeValue < b.DateTimeValue;
        case VariantType.Float: return a.FloatValue < b.FloatValue;
        case VariantType.Int: return a.IntValue < b.IntValue;
      }
      return false;
    }
  }
}
