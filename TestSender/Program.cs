using SyslogNet.Client;
using SyslogNet.Client.Serialization;
using SyslogNet.Client.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestSender
{
  class Program
  {
    static void Main(string[] args)
    {
      ISyslogMessageSerializer serializer = new SyslogRfc5424MessageSerializer();
      ISyslogMessageSender sender = new SyslogUdpSender("127.0.0.1", 514);
      Random random = new Random();

      while(!Console.KeyAvailable)
      {
        SyslogMessage msg = new SyslogMessage(
                                DateTimeOffset.Now,
                                Facility.UserLevelMessages,
                                Severity.Error,
                                Environment.MachineName,
                                "AppName",
                                "ProcId",
                                "Alert",
                                $"Test message from {args[0] ?? "none"} at {DateTime.Now}");
        sender.Send(msg, serializer);
        Thread.Sleep(random.Next(0, 20));
      }
    }
  }
}
