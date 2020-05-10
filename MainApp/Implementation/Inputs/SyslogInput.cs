using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YASLS.SDK.Library;

namespace YASLS
{
  public enum SyslogTransportProtocol { UDP, TCP, TLS }

  public enum SyslogReciveTimestampFormat { UTC, Local }

  public class SyslogInput : IInputModule, IServerBind
  {
    protected int port = 514;
    protected int bufferSize = 16 * 1024;
    protected SyslogTransportProtocol protocol = SyslogTransportProtocol.UDP;
    protected Socket socket = null;
    protected CancellationToken token;
    protected List<IServerMasterQueue> OutputQueues = new List<IServerMasterQueue>();
    protected Dictionary<string, string> Attributes = new Dictionary<string, string>();
    protected bool AddSenderIPAttribute = true, AddReciveTimestampAttribute = true;
    protected bool ReciveTimestampAttributeLocal = true;
    protected readonly Guid moduleId = Guid.Parse("{C4017BBE-0A88-4A3E-A719-9F4EC3878792}");
    protected ILogger logger = null;
    protected IHealthReporter healthReporter = null;
    private int z = 0;
    protected char[] TrailerChars = { (char)0x00, '\r', '\n' };

    #region UDP
    protected void UDPWorkerProc()
    {
      try
      {
        EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        byte[] buffer = new byte[bufferSize];
        while (true)
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
            MessageDataItem message = CreateMessageDataItem(Encoding.ASCII.GetString(buffer, 0, bytesReceived).Trim(TrailerChars), udpAsyncResult.Result.RemoteEndPoint);
            foreach (IServerMasterQueue queue in OutputQueues)
              queue.Enqueue(message);
          }
        }
      }
      finally
      {
        socket?.Close();
      }
    }
    #endregion

    #region TCP
    protected void TCPWorkerProc()
    {
      try
      {
        EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        while (true)
        {
          Task<Socket> tcpAsyncAcceptResult = socket.AcceptAsync();
          try
          {
            tcpAsyncAcceptResult.ContinueWith(new Func<Task<Socket>, object, Task>(SingleHostReceiver), new ConnectionState { }, token);
            tcpAsyncAcceptResult.Wait(token);
          }
          catch (OperationCanceledException)
          {
            break;
          }
        }
      }
      finally
      {
        socket?.Close();
      }
    }

    private async Task SingleHostReceiver(Task<Socket> connectTask, object state)
    {
      Socket innerSocket = connectTask.Result;
      bool isCancelled = false;

      int bytesRead = 0;
      StringBuilder stringBuilder = new StringBuilder();
      byte[] buffer = new byte[bufferSize];
      do
      {
        try
        {
          bytesRead = await innerSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
        }
        catch (OperationCanceledException)
        {
          isCancelled = true;
          break;
        }
        if (bytesRead > 0)
        {
          string nextPortion = Encoding.ASCII.GetString(buffer, 0, bytesRead);
          int tailPos = nextPortion.IndexOfAny(TrailerChars);
          if (tailPos >= 0)
          {
            string thisMsg = nextPortion.Substring(0, tailPos).Trim(TrailerChars);
            stringBuilder.Append(thisMsg);

            MessageDataItem message = CreateMessageDataItem(stringBuilder.ToString(), innerSocket.RemoteEndPoint);
            foreach (IServerMasterQueue queue in OutputQueues)
              queue.Enqueue(message);
            stringBuilder.Clear();

            if (tailPos < nextPortion.Length - 1)
            {
              string nextMsg = nextPortion.Substring(tailPos, nextPortion.Length - tailPos).Trim(TrailerChars);
              if (!string.IsNullOrEmpty(nextMsg))
                stringBuilder.Append(nextMsg);
            }
          }
          else
            stringBuilder.Append(nextPortion);
        }

      } while (bytesRead > 0);
      innerSocket?.Close();
    }


    #endregion

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

    #region IInputModule
    public void LoadConfiguration(JObject configuration, CancellationToken cancellationToken, Dictionary<string, string> attributes, IEnumerable<IServerMasterQueue> queue)
    {
      port = configuration["Port"].Value<int>();
      string strProtocol = configuration["Protocol"]?.Value<string>();
      if (!string.IsNullOrWhiteSpace(strProtocol))
        if (!Enum.TryParse(strProtocol, out protocol))
          logger?.LogEvent(this, Severity.Warning, "SyslogInit", "Invalid protocol or no protocol specified. UDP is used by default. Allowed values TCP, UDP, and TLS.");
      if (attributes != null && attributes.Count > 0)
        foreach (KeyValuePair<string, string> origAttr in attributes)
          Attributes.Add(origAttr.Key, origAttr.Value);
      AddSenderIPAttribute = configuration["AddSenderIPAttribute"]?.Value<bool>() ?? true;
      AddReciveTimestampAttribute = configuration["AddReciveTimestampAttribute"]?.Value<bool>() ?? true;
      ReciveTimestampAttributeLocal = configuration["ReciveTimestampAttributeFormat"]?.Value<string>() == "Local";
      token = cancellationToken;
      OutputQueues.AddRange(queue);
    }
    #endregion

    #region IThreadModule
    public ThreadStart GetWorker()
    {
      switch (protocol)
      {
        case SyslogTransportProtocol.UDP:
          return new ThreadStart(UDPWorkerProc);
        case SyslogTransportProtocol.TCP:
          return new ThreadStart(TCPWorkerProc);
      }
      throw new NotImplementedException("Protocol is not implemented.");
    }

    public void Initialize()
    {
      switch (protocol)
      {
        case SyslogTransportProtocol.UDP:
          IPEndPoint udpLestenerEndpoint = new IPEndPoint(IPAddress.Any, port);
          socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
          socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, bufferSize);
          socket.Bind(udpLestenerEndpoint);
          break;
        case SyslogTransportProtocol.TCP:
          IPEndPoint tcpLestenerEndpoint = new IPEndPoint(IPAddress.Any, port);
          socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
          socket.Bind(tcpLestenerEndpoint);
          socket.Listen(int.MaxValue);
          break;
        default:
          throw new NotImplementedException("Protocol is not implemented.");
      }
    }

    public void Destroy()
    {
      socket?.Close();
    }
    #endregion

    #region IModule Implementation
    public string GetModuleName() => GetType().FullName;

    public string GetModuleDisplayName() => "Network Syslog Input Module";

    public string GetModuleVendor() => "Core YASLS";

    public Guid GetModuleId() => moduleId;
    public Version GetModuleVersion() => Assembly.GetAssembly(GetType()).GetName().Version;
    #endregion

    #region IServerBind Implementation
    public void RegisterServices(ILogger logger, IHealthReporter healthReporter, IQueueFactory factory, IPersistentDataStore persistentStore)
    {
      this.logger = logger;
      this.healthReporter = healthReporter;
    }
    #endregion
  }

  internal class ConnectionState
  {
    internal Socket workSocket = null;
    internal const int BufferSize = 16384;
    internal byte[] buffer = new byte[BufferSize];
    internal StringBuilder stringBuilder = new StringBuilder();
    internal EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
  }

  public class SyslogInputConfiguration
  {
    [JsonProperty("Port")]
    public int Port { get; set; }

    [JsonProperty("Protocol")]
    protected string ProtocolStr { get; set; }

    [JsonIgnore]
    private SyslogTransportProtocol _Protocol;
    [JsonIgnore]
    private bool ProtocolParsed = false;
    [JsonIgnore]
    public SyslogTransportProtocol Protocol
    {
      get
      {
        if (ProtocolParsed) return _Protocol;
        _Protocol = (SyslogTransportProtocol)Enum.Parse(typeof(SyslogTransportProtocol), ProtocolStr);
        ProtocolParsed = true;
        return _Protocol;
      }
    }
  }
}
