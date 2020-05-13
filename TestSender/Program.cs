using SyslogNet.Client;
using SyslogNet.Client.Serialization;
using SyslogNet.Client.Transport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestSender
{
  class Program
  {
    static void Main(string[] args)
    {
      SendTCP();
    }

    private static void SendTCPDebug()
    {
      Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
      IPEndPoint localhost = new IPEndPoint(IPAddress.Loopback, 514);
      Encoding ascii = Encoding.ASCII;
      socket.Connect(localhost);

      string testMsg = "BdddddddddddddddddddddddddddddddE\r\nBggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggE\r\n";
      socket.Send(ascii.GetBytes(testMsg));

      Console.ReadLine();

      testMsg = "TdddddddddddddddddddddddddddddddE\r\nBggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggE\r\n";
      socket.Send(ascii.GetBytes(testMsg));

      Console.ReadLine();
    }

    static void SendTCP()
    {
      Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
      IPEndPoint localhost = new IPEndPoint(IPAddress.Loopback, 514);
      socket.Connect(localhost);
      
      byte[] allLines = File.ReadAllBytes(@"C:\Temp\etc\storage.txt");
      DateTime now = DateTime.Now;
      socket.Send(allLines);
      TimeSpan elapsed = DateTime.Now.Subtract(now);
      Console.WriteLine($"Events per second: {allLines.Length / elapsed.TotalSeconds:N2}");

      Console.ReadLine();
    }

    static void SendUDP()
    {
      Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
      socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, 4096);
      IPEndPoint localhost = new IPEndPoint(IPAddress.Loopback, 514);
      Encoding ascii = Encoding.ASCII;
      
      string[] allLines = File.ReadAllLines(@"C:\Temp\etc\storage.txt");
      DateTime now = DateTime.Now;
      foreach (string line in allLines)
        socket.SendTo(ascii.GetBytes(line), SocketFlags.None, localhost);
      TimeSpan elapsed = DateTime.Now.Subtract(now);
      Console.WriteLine($"Events per second: {allLines.Length / elapsed.TotalSeconds:N2}");


    }

    static void SendAPI()
    {
      //ISyslogMessageSerializer serializer = new SyslogRfc5424MessageSerializer();
      //ISyslogMessageSender sender = new SyslogUdpSender("127.0.0.1", 514);
      //Random random = new Random();

      //while(!Console.KeyAvailable)
      //{
      //  SyslogMessage msg = new SyslogMessage(
      //                          DateTimeOffset.Now,
      //                          Facility.UserLevelMessages,
      //                          Severity.Error,
      //                          Environment.MachineName,
      //                          "AppName",
      //                          "ProcId",
      //                          "Alert",
      //                          $"Test message from {args[0] ?? "none"} at {DateTime.Now}");
      //  sender.Send(msg, serializer);
      //  Thread.Sleep(random.Next(0, 20));
      //}
    }
  }
}
