using System;
using YASLS.SDK.Library;

namespace YASLS.NETServer.Core
{
  public class ConsoleLogger : ILogger
  {
    public void LogEvent(IModule sourceModule, Severity severity, string reason, string message, Exception exception = null)
    {
      Console.WriteLine($"Module '{sourceModule?.GetModuleDisplayName() ?? "<no module>"}' ({sourceModule?.GetModuleName() ?? ""}) by {sourceModule.GetModuleVendor() ?? ""} reported:");
      Console.WriteLine($"==> {severity}: {  message ?? "<No message>"}");
      if (exception != null)
        Console.WriteLine($"Exception: {exception?.Message ?? "<No exception>"}");
    }
  }
}
