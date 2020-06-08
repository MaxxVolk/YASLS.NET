using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace YASLS.NETServer.Core
{
  public class ConfigurationException : Exception
  {
    public ConfigurationException()
      : base()
    {
    }

    public ConfigurationException(string Message)
      : base(Message)
    {
    }

    public ConfigurationException(String message, Exception innerException)
      : base(message, innerException)
    {
    }

    protected ConfigurationException(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }
  }

  [Serializable]
  public class ConfigurationFatalException : ConfigurationException
  {
    public ConfigurationFatalException()
      : base()
    {
    }

    public ConfigurationFatalException(string Message)
      : base(Message)
    {
    }

    public ConfigurationFatalException(String message, Exception innerException)
      : base(message, innerException)
    {
    }

    protected ConfigurationFatalException(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }
  }
}
