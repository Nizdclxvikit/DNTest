using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Server
{
    abstract class Responder
    {
        public abstract Response GetResponse(byte[] input);
    }
}
