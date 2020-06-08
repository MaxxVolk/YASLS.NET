using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using YASLS.NETServer.Configuration;
using YASLS.SDK.Library;

namespace YASLS.NETServer.Core
{
  class Parser : IModule
  {
    // Parser internals
    protected YASLServer Server;
    protected OutputModuleWrapper[] Outputs;

    public Parser(ParserDefinition parserDefinition, Dictionary<string, OutputModuleWrapper> outputs, YASLServer server)
    {
      Server = server;

      List<OutputModuleWrapper> PreOutputs = new List<OutputModuleWrapper>();
      foreach (string outputName in parserDefinition.Output)
        PreOutputs.Add(outputs[outputName]);
      Outputs = PreOutputs.ToArray();
    }

    public void ParseMessage(MessageDataItem message)
    {
      for (int o = 0; o < Outputs.Length; o++)
        Outputs[o].AttachedQueue.TryAdd(message.Clone());
    }

    #region IModule Implementation
    protected readonly Guid moduleId = Guid.Parse("{B0760569-8AE8-4373-9C92-D3C94D19C4C1}");

    public string GetModuleDisplayName() => "YASLS .NET Server Parser";

    public Guid GetModuleId() => moduleId;

    public string GetModuleName() => GetType().FullName;

    public string GetModuleVendor() => "YASLS";

    public Version GetModuleVersion() => Assembly.GetAssembly(GetType()).GetName().Version;

    public void LoadConfiguration(JObject configuration)
    {
      // never used
    }
    #endregion
  }
}
