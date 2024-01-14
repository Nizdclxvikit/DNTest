using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Data.SqlTypes;

namespace Server
{
    public enum ResponderStatus {OK, ERR_INPUT_FORMAT, ERR_OTHER};
    public struct Response
    {
        public Response(ResponderStatus newStatus)
        {
            status = newStatus;
            data = Array.Empty<byte>();
        }
        public ResponderStatus status; 
        public byte[] data;
    }
}
