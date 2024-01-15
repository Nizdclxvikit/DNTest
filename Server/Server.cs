using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;

namespace Server
{
    public enum ImageResponseStatus {OK, ERR_INPUT_FORMAT, ERR_OTHER};
    public class ImageResponse
    {
        
        public ImageResponseStatus status;
        public ushort width;
        public ushort height;

        public byte[] data;
        public string? message;
        private const string eol = "\r\n";

        public const byte chunkSize = 32; // Square only (32*32)
        public bool hasPartialChunkX { get {return width%chunkSize!=0; } }
        public bool hasPartialChunkY { get {return height%chunkSize!=0; } }
        public int numChunksX { get {return width / chunkSize + (hasPartialChunkX?1:0); } }
        public int numChunksY { get {return height / chunkSize + (hasPartialChunkY?1:0); } }
        public int numChunks { get {return numChunksX * numChunksY; } }
        

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
            byte[] packet;
            // What comes after depends on status
            switch (status)
            {
                case ImageResponseStatus.OK:
                    packet = new byte[9 + width * height];
                    ushort leftc = 0, topc = 0; // Send whole image, start at 0,0
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

        public byte[] FormulatePacketChunk(int chunk)
        {
            byte[] packet;
            
            

            switch (status)
            {
                case ImageResponseStatus.OK:
                    int cy = chunk / numChunksX;
                    int cx = chunk % numChunksX;
                    
                    ushort leftc = (ushort)(cx * chunkSize);
                    ushort topc = (ushort)(cy * chunkSize);
                    
                    byte cWidth = chunkSize;
                    if (width - leftc < chunkSize) cWidth = (byte)(width - leftc);
                    byte cHeight = chunkSize;
                    if (height - topc < chunkSize) cHeight = (byte)(height - topc);

                    packet = new byte[9 + cWidth * cHeight];
                    // Header
                    packet[0] = (byte)status;
                    packet[1] = (byte)((int)cWidth >> 8);
                    packet[2] = (byte)cWidth;
                    packet[3] = (byte)((int)cHeight >> 8);
                    packet[4] = (byte)cHeight;
                    packet[5] = (byte)(leftc >> 8);
                    packet[6] = (byte)leftc;
                    packet[7] = (byte)(topc >> 8);
                    packet[8] = (byte)topc;
                    // Data

                    for (int y = topc; y < topc+cHeight; y++)
                    {
                        Buffer.BlockCopy(data, leftc + width*y, packet, 9 + (y-topc)*cWidth, cWidth);
                    }
                    
                    break;


                default:
                    packet = new byte[1 + (message==null ? 0 : message.Length) + 1];
                    packet[0] = (byte) status;
                    if (message != null)
                    {
                        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                        Buffer.BlockCopy(messageBytes, 0, packet, 1, messageBytes.Length);
                        packet[message.Length + 2] = 0;
                    }
                    else
                    {
                        packet[1] = 0;
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
        private ImageResponse imageResponse;
        private bool[] sentChunks;

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
                    // Send an unsent chunk, if there are any
                    if (sentChunks != null && stream.CanWrite) // Not sure if canwrite will ever be false
                    {
                        bool foundUnsent = false;
                        int chunk;
                        for (chunk=0; chunk<sentChunks.Length; chunk++)
                        {
                            if (!sentChunks[chunk])
                            {
                                foundUnsent = true;
                                break;
                            }
                        }

                        if (foundUnsent)
                        {
                            // Formulate our response (a tcp packet, max 65535 bytes)
                            byte[] responseBytes = imageResponse.FormulatePacketChunk(chunk);

                            // Send it, if it fails just bail
                            if (!TryWrite(responseBytes, 0, responseBytes.Length)) break;
                            sentChunks[chunk] = true;
                            lastChecked = DateTime.Now; // Wrote something so reset timer
                            continue;
                        }
                    }

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

                // do the calculations, mark no chunks as sent
                imageResponse = GetResponse(bytes);
                sentChunks = new bool[imageResponse.numChunks];

  
  
            }

            // Disconnect
            stream.Close();
            client.Close();
            connected = false;
            Console.WriteLine("Client Disconnected.");
            
        }

        private ImageResponse GetResponse(byte[] input)
        {

            string str = Encoding.UTF8.GetString(input);
            
            string[] args = str.Split(";");

            if (args.Length == 6)
            {
                double x, y, w, h;
                int width, height;
                bool success = true;

                success &= double.TryParse(args[0], out x);
                success &= double.TryParse(args[1], out y);
                success &= double.TryParse(args[2], out w);
                success &= double.TryParse(args[3], out h);
                success &= int.TryParse(args[4], out width);
                success &= int.TryParse(args[5], out height);
                
                if (success) 
                {
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
