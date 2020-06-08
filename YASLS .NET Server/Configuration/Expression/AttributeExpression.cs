using Newtonsoft.Json;

using System;

using YASLS.SDK.Library;

namespace YASLS.NETServer.Configuration
{
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
}