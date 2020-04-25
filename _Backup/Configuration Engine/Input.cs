using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YASLS.SDK.Library;

namespace YASLS.Configuration
{
  public class Input : ConfigurationPrincipalBase
  {
    [JsonProperty("NetworkListeners")]
    public Dictionary<string, NetworkListener> NetworkListeners { get; set; }
  }

  public class NetworkListener: ConfigurationPrincipalBase
  {
    [JsonProperty]
    public int Port { get; set; } = 514;

    [JsonProperty]
    public int BufferSize { get; set; } = 16 * 1024;

    [JsonProperty]
    public string Protocol { get; set; } = "UDP";

    [JsonIgnore]
    protected Socket socket = null;

    [JsonIgnore]
    protected Thread workerThread = null;

    [JsonIgnore]
    protected CancellationToken cancellationToken;

    [JsonIgnore]
    protected CancellationTokenSource tokenSource;

    [JsonIgnore]
    protected List<YASLSQueue> OutputQueues = new List<YASLSQueue>();

    public void Start()
    {
      IPEndPoint lestenerEndpoint = new IPEndPoint(IPAddress.Any, Port);
      if (Protocol == "UDP")
      {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, BufferSize);
        socket.Bind(lestenerEndpoint);
      }
      if (Protocol == "TCP")
      {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(lestenerEndpoint);
        socket.Listen(int.MaxValue);
      }
      workerThread = new Thread(WorkerProc);
      tokenSource = new CancellationTokenSource();
      cancellationToken = tokenSource.Token;
      workerThread.Start();
    }

    protected void WorkerProc()
    {
      EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
      byte[] buffer = new byte[BufferSize];
      while (true)
      {
        if (socket.SocketType == SocketType.Dgram)
        {
          Task<SocketReceiveFromResult> udpAsyncResult = socket.ReceiveFromAsync(new ArraySegment<byte>(buffer), SocketFlags.None, remoteEndPoint);
          try
          {
            udpAsyncResult.Wait(cancellationToken);
          }
          catch(OperationCanceledException)
          {
            break;
          }
          int bytesReceived = udpAsyncResult.Result.ReceivedBytes;
          if (bytesReceived > 0)
          {
            MessageMetaInfo message = new MessageMetaInfo { RawMessage = Encoding.ASCII.GetString(buffer, 0, bytesReceived) };
            message.MetaData.Add("SenderIP", (udpAsyncResult.Result.RemoteEndPoint as IPEndPoint)?.Address.ToString());
            message.MetaData.Add("ReciveTimestamp", DateTime.Now);
            message.MetaData.Add("Channel", "UDP");
            foreach (YASLSQueue queue in OutputQueues)
              queue.Enqueue(message);
          }
        }
        if (socket.SocketType == SocketType.Stream)
        {
          Task<Socket> tcpAsyncAcceptResult = socket.AcceptAsync();
          try
          {
            tcpAsyncAcceptResult.Wait(cancellationToken);
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
              tcpAsyncReceiveResult.Wait(cancellationToken);
            }
            catch
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
          MessageMetaInfo message = new MessageMetaInfo { RawMessage = stringBuilder.ToString() };
          message.MetaData.Add("SenderIP", (tcpAsyncAcceptResult.Result.RemoteEndPoint as IPEndPoint)?.Address.ToString());
          message.MetaData.Add("ReciveTimestamp", DateTime.Now);
          message.MetaData.Add("Channel", "TCP");
          foreach (YASLSQueue queue in OutputQueues)
            queue.Enqueue(message);
        }
      }
    }

    public void Stop()
    {
      tokenSource.Cancel();
      workerThread.Join();
    }

    public void RegisterQueue(YASLSQueue queue)
    {
      OutputQueues.Add(queue);
    }
  }

  public class NetworkInput : IInputModule
  {
    protected int Port { get; set; } = 514;

    protected int BufferSize { get; set; } = 16 * 1024;

    protected string Protocol { get; set; } = "UDP";

    protected Socket socket = null;

    protected CancellationToken token;

    protected List<IQueueModule> OutputQueues = new List<IQueueModule>();

    protected void WorkerProc()
    {
      EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
      byte[] buffer = new byte[BufferSize];
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
            MessageDataItem message = new MessageDataItem { Message = Encoding.ASCII.GetString(buffer, 0, bytesReceived) };
            message.AddAttribute("SenderIP", (udpAsyncResult.Result.RemoteEndPoint as IPEndPoint)?.Address.ToString());
            message.AddAttribute("ReciveTimestamp", DateTime.Now);
            message.AddAttribute("Channel", "UDP");
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
            catch
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
          MessageDataItem message = new MessageDataItem { Message = stringBuilder.ToString() };
          message.AddAttribute("SenderIP", (tcpAsyncAcceptResult.Result.RemoteEndPoint as IPEndPoint)?.Address.ToString());
          message.AddAttribute("ReciveTimestamp", DateTime.Now);
          message.AddAttribute("Channel", "TCP");
          foreach (IQueueModule queue in OutputQueues)
            queue.Enqueue(message);
        }
      }
    }

    public void Initialize(JObject configuration, CancellationToken cancellationToken, IEnumerable<IQueueModule> queue)
    {
      Port = configuration["Port"].Value<int>();
      Protocol = configuration["Protocol"].Value<string>();

      IPEndPoint lestenerEndpoint = new IPEndPoint(IPAddress.Any, Port);
      if (Protocol == "UDP")
      {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, BufferSize);
        socket.Bind(lestenerEndpoint);
      }
      if (Protocol == "TCP")
      {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(lestenerEndpoint);
        socket.Listen(int.MaxValue);
      }
      token = cancellationToken;
      OutputQueues.AddRange(queue);
    }

    public ThreadStart GetWorker()
    {
      return new ThreadStart(WorkerProc);
    }

    public void Destroy()
    {
      socket.Close();
    }
  }
}
