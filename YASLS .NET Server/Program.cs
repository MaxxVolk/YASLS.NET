using Newtonsoft.Json;
using System;
using System.IO;
using System.ServiceProcess;
using YASLS.NETServer.Configuration;
using YASLS.NETServer.Core;

namespace YASLS.NETServer
{
  class Program
  {
    static int Main(string[] args)
    {
      if (Environment.UserInteractive)
      {
        Console.WriteLine("YASLS .NET Server starting...");

        ServerConfiguration serverConfiguration = JsonConvert.DeserializeObject<ServerConfiguration>(File.ReadAllText(@"ServerConfig.json"));
        YASLServer server = new YASLServer(serverConfiguration);
        server.Start();

        Console.WriteLine("Press any key to stop...");
        Console.ReadKey(true);
        server.Stop(10 * 1000); 
      }
      else
      {
        ServiceBase[] ServicesToRun;
        ServicesToRun = new ServiceBase[]
        {
          new YASLSService()
        };
        ServiceBase.Run(ServicesToRun);
      }

      return 0;
    }
  }
}
