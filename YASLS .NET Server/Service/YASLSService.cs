using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using YASLS.NETServer.Configuration;
using YASLS.NETServer.Core;

namespace YASLS.NETServer
{
  class YASLSService : ServiceBase
  {
    private System.ComponentModel.IContainer components = null;
    private YASLServer server;

    public YASLSService()
    {
      InitializeComponent();
      ServerConfiguration serverConfiguration = JsonConvert.DeserializeObject<ServerConfiguration>(File.ReadAllText(@"ServerConfig.json"));
      server = new YASLServer(serverConfiguration);
    }

    protected override void OnStart(string[] args)
    {
      
    }

    protected override void OnStop()
    {
      
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
      // 
      // YASLSService
      // 
      this.ServiceName = "YASLServer";

    }
  }
}
