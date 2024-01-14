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

        TcpListener TL;
        Responder responder;

        public Server()
        {

            System.Net.IPAddress localaddr = IPAddress.Parse("127.0.0.1");
            int port = 80;
            TL = new TcpListener(localaddr, port);
            responder = new ResponderFractal();

            TL.Start();
            Console.WriteLine("Listener started");

            HandleConnection(); // Blocks until something connects
        }

        void HandleConnection()
        {
            TcpClient client = TL.AcceptTcpClient();
            NetworkStream stream = client.GetStream();

            Console.WriteLine("A client connected.");

            const string eol = "\r\n";

            while (true) {
                while (!stream.DataAvailable); // Wait for data available
                

                byte[] bytes = new byte[client.Available]; // Do we need to delete/deallocate this...?
                stream.Read(bytes, 0, bytes.Length);


                Response response = responder.GetResponse(bytes);

                
            


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

                byte[] fullResponse = new byte[1 + responseBytes.Length];
                fullResponse[0] = (byte)response.status;
                Buffer.BlockCopy(responseBytes, 0, fullResponse, 1, responseBytes.Length);

                stream.Write(fullResponse, 0, fullResponse.Length);
  
            }

            
            Console.WriteLine("Client Disconnected.");
            
        }
    }
}
