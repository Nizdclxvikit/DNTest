using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.ComponentModel.DataAnnotations;

namespace Server
{
    public enum ImageResponseStatus {OK, ERR_INPUT_FORMAT, ERR_OTHER};
    public class ImageResponse
    {
        
        public ImageResponseStatus status; 
        public ushort width;
        public ushort height;

        public byte[] data;
        private ushort leftc = 0, topc = 0;
        public string? message;
        private const string eol = "\r\n";

        public ImageResponse(ImageResponseStatus newStatus, string? dMessage = null)
        {
            status = newStatus;
            data = Array.Empty<byte>();
            width = 0;
            height = 0;
            if (dMessage != null) message = dMessage;
        }

        public ImageResponse(ushort dWidth, ushort dHeight)
        {
            status = ImageResponseStatus.OK;
            width = dWidth;
            height = dHeight;
            data = new byte[width*height];
        }

        public byte[] FormulatePacket()
        {
            byte[] packet = {};
            // What comes after depends on status
            switch (status)
            {
                case ImageResponseStatus.OK:
                    packet = new byte[9 + width * height];
                    // Header
                    packet[0] = (byte)status;
                    packet[1] = (byte)(width >> 8);
                    packet[2] = (byte)width;
                    packet[3] = (byte)(height >> 8);
                    packet[4] = (byte)height;
                    packet[5] = (byte)(leftc >> 8);
                    packet[6] = (byte)leftc;
                    packet[7] = (byte)(topc >> 8);
                    packet[8] = (byte)topc;
                    // Data
                    Buffer.BlockCopy(data, 0, packet, 9, width*height);
                    break;
                default:
                    
                    packet = new byte[1 + (message==null ? 0 : message.Length)];
                    packet[0] = (byte) status;
                    if (message != null)
                    {
                        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                        Buffer.BlockCopy(messageBytes, 0, packet, 1, messageBytes.Length);
                    }
                    break;

            }
            return packet;
        }
    }

    class Server
    {

        private TcpListener TL;
    
        private NetworkStream stream;
        private bool connected = false;

        private static readonly byte[] keepAlive = {0};

        private ImageRenderer renderer;

        public Server() // This will never exit
        {

            IPAddress localaddr = IPAddress.Parse("127.0.0.1");
            int port = 80;
            TL = new TcpListener(localaddr, port);
            renderer = new MandelRenderer();

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
                byte[] bytes = new byte[client.Available];
                stream.Read(bytes, 0, bytes.Length);

                if (Encoding.UTF8.GetString(bytes) == "EXIT") break; //Disconnect

                // do the calculations
                ImageResponse response = GetResponse(bytes);

                // Formulate our response (a tcp packet, max 65535 bytes)
                byte[] fullResponse = response.FormulatePacket();

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

        public ImageResponse GetResponse(byte[] input)
        {

            string str = Encoding.UTF8.GetString(input);
            
            string[] args = str.Split(";");

            if (args.Length == 4)
            {
                double x, y, w, h;
                bool success = true;

                success &= double.TryParse(args[0], out x);
                success &= double.TryParse(args[1], out y);
                success &= double.TryParse(args[2], out w);
                success &= double.TryParse(args[3], out h);
                
                if (success) 
                {
                    

                    int width = 200, height = 200;
                    byte[] imageData = renderer.RenderArea(x, y, w, h, width, height);
                    
                    ImageResponse response = new ImageResponse((ushort)width, (ushort)height);
                    response.data = imageData;
                    

                    return response;
                }
                else return new ImageResponse(ImageResponseStatus.ERR_INPUT_FORMAT);
            }
            else
            {
                return new ImageResponse(ImageResponseStatus.ERR_INPUT_FORMAT);
            }

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
