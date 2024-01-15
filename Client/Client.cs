using System.Drawing.Printing;
using System.Net.Sockets;
using System.Text;

namespace client;

public class Client
{

    public enum ResponderStatus {OK, ERR_INPUT_FORMAT, ERR_OTHER};
    
    private bool connected; // is this "thread safe"?

    public bool isConnected { get {return connected;} }
    private const string eol = "\r\n";
    private Thread mainLoopThread;

    private TcpClient? client;
    private NetworkStream? stream;

    public EventHandler? imageUpdated;
    public EventHandler? onDisconnect;
    public EventHandler? onConnect;

    private ColorGradient colorGradient;

    private ChunkedBitmap myImage;
    public Image GetMyImage()  {return myImage.GetBitmap();}
    public Client()
    {
        mainLoopThread = new Thread(new ThreadStart(MainLoop));
        myImage = new ChunkedBitmap(200, 200);

        colorGradient = new ColorGradient(
            [0, 0.16, 0.33, 0.5, 0.66, 0.83, 1],
            [Color.Red, Color.Yellow, Color.Green, Color.Cyan, Color.Blue, Color.Magenta, Color.Red]);
    }

    private void MainLoop()
    {
        while (connected)
        {
            // Loop until disconnected
            if (client == null) throw new Exception("Client is null");
            if (stream == null) throw new Exception("Stream is null");

            if (stream.DataAvailable)
            {
                // We got a response from the server
                
                //Read 1 byte (status)
                ResponderStatus status = (ResponderStatus)stream.ReadByte();
                

                switch (status)
                {
                    case ResponderStatus.OK:
                        if (client.Available <= 8) break; // Keepalive, probably

                        // Read 8 bytes (chunk dims)
                        byte[] bytes = new byte[8];
                        stream.ReadExactly(bytes, 0, 8);

                        ushort width, height, leftc, topc;
                        width = (ushort)((bytes[0] << 8) | (bytes[1]));
                        height = (ushort)((bytes[2] << 8) | (bytes[3]));
                        leftc = (ushort)((bytes[4] << 8) | (bytes[5]));
                        topc = (ushort)((bytes[6] << 8) | (bytes[7]));

                        if (client.Available < width*height) throw new Exception("Not enough data in the stream for the specified chunk dimensions");
                        
                        // Read the rest of the data (a new packet should begin right after this)
                        bytes = new byte[(int)width*height];
                        stream.ReadExactly(bytes, 0, width*height);

                        Bitmap recievedImage = new Bitmap(width,height);

                        for (int i=0; i< width; i++)
                        {
                            for (int j=0; j<height; j++)
                            {
                                byte thisPixel = bytes[i+width*j];
                                if (thisPixel == 0)
                                {
                                    recievedImage.SetPixel(i, j, Color.Black);
                                }
                                else
                                {
                                    recievedImage.SetPixel(i, j, colorGradient.SampleGradient(thisPixel / 16.0));
                                }
                            }
                        }

                        
                        myImage.PutChunk(leftc, topc, recievedImage);
                        
                        imageUpdated?.Invoke(this, EventArgs.Empty);
                        break;
                    default:
                        byte[] msgbytes = new byte[256]; // some maximum message length
                        int k = 0; int c;
                        do { // Read bytes until 0 (end of string)
                            c = stream.ReadByte();
                            if (c==-1) throw new Exception("End of stream, mid-message");
                            msgbytes[k++] = (byte) c;
                        } while(c != 0);

                        string message = Encoding.UTF8.GetString(msgbytes, 0, k);
                        Console.WriteLine("Got Response: Server side error: " + message);
                        break;
                }
            }
        }
    }

    public void Connect(string host, ushort port)
    {
        // Connect (doesn't handle failed connection)
        client = new TcpClient(host, port);
        stream = client.GetStream();

        connected = true;


        // Loop to handle the tcp connection
        mainLoopThread = new Thread(new ThreadStart(MainLoop));
        mainLoopThread.Start();
        
        onConnect?.Invoke(this, EventArgs.Empty);

        // Should then return
    }

    public void Disconnect() // Should be called from main thread not mainloop thread
    {
        connected = false;
        stream?.Write(Encoding.UTF8.GetBytes("EXIT")); // Tell the server were disconnecting
        stream?.Flush();
        mainLoopThread.Join(); // mainLoopthread should return once connected is set to false
        stream?.Close();
        client?.Close();
        
        onDisconnect?.Invoke(this, EventArgs.Empty);
    }

    public void MakeRequest(double x, double y, double w, double h)
    {
        string command = x.ToString() + ";" + y.ToString() + ";" + w.ToString() + ";" + h.ToString() + eol;
        
        // This might fail if the server dies
        if (stream == null) throw new Exception("Stream is null");
        stream.Write(Encoding.UTF8.GetBytes(command));
    }

    

}