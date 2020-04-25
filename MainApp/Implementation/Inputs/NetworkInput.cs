using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YASLS.SDK.Library;

namespace YASLS
{
  public class NetworkInput : IInputModule, IServerBind
  {
    protected int port = 514;
    protected int bufferSize = 16 * 1024;
    protected string protocol = "UDP";
    protected Socket socket = null;
    protected CancellationToken token;
    protected List<IQueueModule> OutputQueues = new List<IQueueModule>();
    protected Dictionary<string, string> Attributes = new Dictionary<string, string>();
    protected bool AddSenderIPAttribute = true, AddReciveTimestampAttribute = true;
    protected bool ReciveTimestampAttributeLocal = true;
    protected readonly Guid moduleId = Guid.Parse("{C4017BBE-0A88-4A3E-A719-9F4EC3878792}");
    protected ILogger logger = null;
    protected IHealthReporter healthReporter = null;
    private int z = 0;

    protected void WorkerProc()
    {
      try
      {
        IPEndPoint lestenerEndpoint = new IPEndPoint(IPAddress.Any, port);
        if (protocol == "UDP")
        {
          socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
          socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, bufferSize);
          socket.Bind(lestenerEndpoint);
        }
        if (protocol == "TCP")
        {
          socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
          socket.Bind(lestenerEndpoint);
          socket.Listen(int.MaxValue);
        }

        EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        byte[] buffer = new byte[bufferSize];
        while (true)
        {
          if (socket.SocketType == SocketType.Dgram)
          {
            Task<SocketReceiveFromResult> udpAsyncResult = socket.ReceiveFromAsync(new ArraySegment<byte>(buffer), SocketFlags.None, remoteEndPoint);
            try
            {
              udpAsyncResult.Wait(token);
            }
            catch (OperationCanceledException)
            {
              break;
            }
            int bytesReceived = udpAsyncResult.Result.ReceivedBytes;
            if (bytesReceived > 0)
            {
              MessageDataItem message = CreateMessageDataItem(Encoding.ASCII.GetString(buffer, 0, bytesReceived), udpAsyncResult.Result.RemoteEndPoint);
              foreach (IQueueModule queue in OutputQueues)
                queue.Enqueue(message);
            }
          }
          if (socket.SocketType == SocketType.Stream)
          {
            Task<Socket> tcpAsyncAcceptResult = socket.AcceptAsync();
            try
            {
              tcpAsyncAcceptResult.Wait(token);
            }
            catch
            {
              break;
            }
            Socket innerSocket = tcpAsyncAcceptResult.Result;
            bool isCancelled = false;
            StringBuilder stringBuilder = new StringBuilder();
            int bytesRead = 0;
            do
            {
              Task<int> tcpAsyncReceiveResult = innerSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
              try
              {
                tcpAsyncReceiveResult.Wait(token);
              }
              catch (OperationCanceledException)
              {
                isCancelled = true;
                break;
              }
              bytesRead = tcpAsyncReceiveResult.Result;
              if (bytesRead > 0)
                stringBuilder.Append(Encoding.ASCII.GetString(buffer, 0, tcpAsyncReceiveResult.Result));
            } while (bytesRead > 0);
            // propagate cancellation
            if (isCancelled)
              break;
            MessageDataItem message = CreateMessageDataItem(stringBuilder.ToString(), tcpAsyncAcceptResult.Result.RemoteEndPoint);
            foreach (IQueueModule queue in OutputQueues)
              queue.Enqueue(message);
          }
        }
      }
      finally
      {
        socket?.Close();
      }
    }

    protected MessageDataItem CreateMessageDataItem(string messageText, EndPoint endPoint)
    {
      MessageDataItem message = new MessageDataItem(messageText);
      if (AddSenderIPAttribute)
        message.AddAttribute("SenderIP", (endPoint as IPEndPoint)?.Address.ToString());
      if (AddReciveTimestampAttribute && ReciveTimestampAttributeLocal)
        message.AddAttribute("ReciveTimestamp", DateTime.Now);
      if (AddReciveTimestampAttribute && !ReciveTimestampAttributeLocal)
        message.AddAttribute("ReciveTimestamp", DateTime.UtcNow);
      foreach (KeyValuePair<string, string> extraAttr in Attributes)
        message.AddAttribute(extraAttr.Key, extraAttr.Value);
      return message;
    }

    public void Initialize(JObject configuration, CancellationToken cancellationToken, Dictionary<string, string> attributes, IEnumerable<IQueueModule> queue)
    {
      port = configuration["Port"].Value<int>();
      protocol = configuration["Protocol"].Value<string>();
      if (attributes != null && attributes.Count > 0)
        foreach (KeyValuePair<string, string> origAttr in attributes)
          Attributes.Add(origAttr.Key, origAttr.Value);
      AddSenderIPAttribute = configuration["AddSenderIPAttribute"]?.Value<bool>() ?? true;
      AddReciveTimestampAttribute = configuration["AddReciveTimestampAttribute"]?.Value<bool>() ?? true;
      ReciveTimestampAttributeLocal = (configuration["ReciveTimestampAttributeFormat"]?.Value<string>() == "Local");
      token = cancellationToken;
      OutputQueues.AddRange(queue);
    }

    public ThreadStart GetWorker() => new ThreadStart(WorkerProc);

    public void Destroy()
    {
      socket?.Close();
    }

    public string GetModuleName() => GetType().FullName;

    public string GetModuleDisplayName() => "Network Syslog Input Module";

    public string GetModuleVendor() => "Core YASLS";

    public Guid GetModuleId() => moduleId;

    public void RegisterServices(ILogger logger, IHealthReporter healthReporter, IQueueFactory factory)
    {
      this.logger = logger;
      this.healthReporter = healthReporter;
    }
  }
}
