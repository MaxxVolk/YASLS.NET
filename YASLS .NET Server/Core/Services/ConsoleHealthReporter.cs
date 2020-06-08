using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YASLS.SDK.Library;

namespace YASLS.NETServer.Core
{
  public class ConsoleHealthReporter : IHealthReporter
  {
    public void SetModuleHealth(IModule sourceModule, string component, HealthState healthState, string message = null)
    {
      Console.WriteLine($"Module {sourceModule?.GetModuleDisplayName()} set component {component ?? ""} to {healthState} due to : {message ?? "no reason"}.");
    }

    public void SetPerformanceCounter(IModule sourceModule, string component, string counter, double value)
    {
      Console.WriteLine($"Module {sourceModule?.GetModuleDisplayName()} set component {component ?? ""} counter value {counter} to : {value:N2}.");
    }
  }
}
