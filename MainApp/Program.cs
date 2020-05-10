using Newtonsoft.Json;
using System;
using System.IO;
using YASLS.Configuration;
using YASLS.Core;

namespace YASLS
{
  class Program
  {
    static void Main(string[] args)
    {
      ServerConfiguration serverConfiguration = JsonConvert.DeserializeObject<ServerConfiguration>(File.ReadAllText("ServerConfig.json"));

      YASLServer server = new YASLServer(serverConfiguration);
      server.Start();

      Console.ReadLine();

      server.Stop();

    }

  }
}
