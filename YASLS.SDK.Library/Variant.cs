using System;
using System.Collections.Generic;

namespace YASLS.SDK.Library
{
  public enum VariantType { Int, Float, String, DateTime, Boolean }
  public struct Variant : IEquatable<Variant>
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

    public override bool Equals(object obj)
    {
      return obj is Variant variant && Equals(variant);
    }

    public bool Equals(Variant other)
    {
      bool TypedCompare = false;
      switch (Type)
      {
        case VariantType.Boolean: TypedCompare = BooleanValue == other.BooleanValue; break;
        case VariantType.DateTime: TypedCompare = DateTimeValue == other.DateTimeValue; break;
        case VariantType.Float: TypedCompare = FloatValue == other.FloatValue; break;
        case VariantType.Int: TypedCompare = IntValue == other.IntValue; break;
        case VariantType.String: TypedCompare = StringValue == other.StringValue; break;
      }
      return Type == other.Type && TypedCompare;
    }

    public override int GetHashCode()
    {
      int hashCode = -938617308;
      hashCode = hashCode * -1521134295 + Type.GetHashCode();
      switch (Type)
      {
        case VariantType.Int: return hashCode * -1521134295 + IntValue.GetHashCode();
        case VariantType.Float: return hashCode * -1521134295 + FloatValue.GetHashCode();
        case VariantType.String: return hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(StringValue ?? "");
        case VariantType.DateTime: return hashCode * -1521134295 + DateTimeValue.GetHashCode();
        case VariantType.Boolean: return hashCode * -1521134295 + BooleanValue.GetHashCode();
      }
      return hashCode;
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
