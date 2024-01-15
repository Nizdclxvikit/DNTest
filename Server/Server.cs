using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.ComponentModel.DataAnnotations;

namespace Server
{
    class Server
    {

        private TcpListener TL;
        private Responder responder;
        private NetworkStream stream;
        private bool connected = false;

        private static readonly byte[] keepAlive = {0};
        private const string eol = "\r\n";

        public Server() // This will never exit
        {

            IPAddress localaddr = IPAddress.Parse("127.0.0.1");
            int port = 80;
            TL = new TcpListener(localaddr, port);
            responder = new ResponderFractal();

            TL.Start();
            Console.WriteLine("Listener started");

            while(true)
            {
                TcpClient client = TL.AcceptTcpClient(); // Blocks until something connects
                HandleConnection(client); // Returns when it disconnects
            }
        }

        private void HandleConnection(TcpClient client)
        {
            Console.WriteLine("A client connected.");
            connected = true;
            stream = client.GetStream();

            DateTime lastChecked = DateTime.Now;

            while (true) {
                // Wait for data available
                while (!stream.DataAvailable)
                {
                    // Send keepAlive every 2 seconds
                    if (DateTime.Now.Subtract(lastChecked) >= TimeSpan.FromSeconds(2))
                    {
                        lastChecked = DateTime.Now;
                        if (!TryWrite(keepAlive, 0, 1))
                        {
                            connected = false;
                            break;
                        }
                    }
                }
                if (!connected) break;
                
                // Read input data
                byte[] bytes = new byte[client.Available]; // Do we need to delete/deallocate this...?
                stream.Read(bytes, 0, bytes.Length);

                if (Encoding.UTF8.GetString(bytes) == "EXIT") break; //Disconnect

                // Let the responder do the calculations
                Response response = responder.GetResponse(bytes);

                // Formulate our response (a tcp packet, max 65535 bytes)
                byte[] responseBytes = {};

                // What comes after depends on status
                switch (response.status)
                {
                    case ResponderStatus.OK:
                        responseBytes = response.data;
                        break;
                    case ResponderStatus.ERR_INPUT_FORMAT:
                        responseBytes = Encoding.UTF8.GetBytes("Error with input format" + eol);
                        break;
                    case ResponderStatus.ERR_OTHER:
                        responseBytes = Encoding.UTF8.GetBytes("Error with something" + eol);
                        break;

                }

                // Concat the status to the beginning
                byte[] fullResponse = new byte[1 + responseBytes.Length];
                fullResponse[0] = (byte)response.status;
                Buffer.BlockCopy(responseBytes, 0, fullResponse, 1, responseBytes.Length);

                // Send it, if it fails just bail
                if (!TryWrite(fullResponse, 0, fullResponse.Length)) break;
                lastChecked = DateTime.Now; // Wrote something so reset timer
  
            }

            // Disconnect
            stream.Close();
            client.Close();
            connected = false;
            Console.WriteLine("Client Disconnected.");
            
        }


        private bool TryWrite(byte[] bytes, int offset, int count)
        {
            if (stream==null || !stream.CanWrite) return false;

            try {
                stream.Write(bytes, offset, count);
            }
            catch (IOException)
            {
                return false;
            }

            return true;
        }
    }
}
