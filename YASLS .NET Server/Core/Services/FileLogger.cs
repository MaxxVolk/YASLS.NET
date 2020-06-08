using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YASLS.SDK.Library;

namespace YASLS.NETServer.Core
{
  class FileLogger : ILogger
  {
    protected string FilePath;
    protected Severity MinSeverity;

    public FileLogger(string filePath, Severity minimumSeverity)
    {
      FilePath = filePath;
      MinSeverity = minimumSeverity;
    }

    public void LogEvent(IModule sourceModule, Severity severity, string reason, string message, Exception exception = null)
    {
      if ((int)severity < (int)MinSeverity) // 0 => Debug
        return;
      LogWriteVerbose(FilePath, exception == null ? message : $"{message}\r\n{exception.Message}", reason ?? "<no component>", sourceModule?.GetModuleName() ?? "<no module>", Thread.CurrentThread.ManagedThreadId);
    }

    public static void LogWriteVerbose(string filePath, string debugInfo, string component, string source, int thread)
    {
      // example:
      // <![LOG[Message]LOG]!><time="14:11:02.040-780" date="03-27-2020" component="CcmExec" context="" type="1" thread="5400" file="powerstatemanager.cpp:1065">

      DateTime localNow = DateTime.Now;
      DateTime utcNow = DateTime.UtcNow;
      int UTCOffset = (int)Math.Truncate(utcNow.Subtract(localNow).TotalMinutes);
      string strUTCOffset;
      if (UTCOffset < 0)
        strUTCOffset = UTCOffset.ToString("D");
      else
        strUTCOffset = "+" + UTCOffset.ToString("D");
      string formattedMessage = "<![LOG[" + debugInfo + "]LOG]!>";
      // time="17:23:36.867-720" date="04-20-2016"
      string strTime = $"time=\"{localNow:HH:mm:ss.fff}{strUTCOffset}\"";
      string strDate = $"date=\"{localNow:MM-dd-yyyy}\"";
      string strLogFileName = Path.Combine(filePath, $"ServerLog-{localNow:yyyy-MM-dd}.log");
      formattedMessage += $"<{strTime} {strDate} component=\"{component}\" context=\"\" type=\"1\" thread=\"{thread}\" file=\"{source}\">\r\n";
      // need that due to multi-threading nature
      for (int attemptCounter = 0; attemptCounter < 10; attemptCounter++)
      {
        try
        {
          File.AppendAllText(strLogFileName, formattedMessage);
          break; // exit the loop if the write op is successful
        }
        catch (Exception)
        {
          Thread.Sleep(1); // wait a bit and repeat
        }
      }
    }
  }
}
