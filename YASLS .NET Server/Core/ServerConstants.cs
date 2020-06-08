using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YASLS.NETServer.Core
{
  class ServerConstants
  {
    internal class Reasons
    {
      public const string QueueOverflow = "QueueOverflow";
      public const string ServerInit = "ServerInit";
    }

    internal class Components
    {
      public const string AttachedQueue = "AttachedQueue";
    }
  }
}
