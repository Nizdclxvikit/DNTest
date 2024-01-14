using System.Drawing.Printing;
using System.Net.Sockets;
using System.Text;

namespace client;

public class ClientWindow : Form
{
    //private RichTextBox myConsole;
    private PictureBox renderArea;
    private Button connectButton;

    private TextBox ipText;
    private TextBox portText;

    private bool connected = false;

    public enum ResponderStatus {OK, ERR_INPUT_FORMAT, ERR_OTHER};
    
    private ColorGradient colorGradient;

    public ClientWindow()
    {
        // Set the form's caption, which will appear in the title bar.
        this.Text = "Client Window";
        this.Size = new Size(800, 600);

        
        /*myConsole = new RichTextBox();
        myConsole.Location = new Point(600, 0);
        myConsole.Name = "Console";
        myConsole.Size = new Size(200, 500);
        myConsole.Text = "Text and stuff...";
        myConsole.ReadOnly = true;*/

        renderArea = new PictureBox();
        renderArea.Location = new Point(0, 0);
        renderArea.Name = "RenderArea";
        renderArea.Size = new Size(600,600);

        ipText = new TextBox();
        ipText.Name = "IPText";
        ipText.Text = "127.0.0.1";
        ipText.Location = new Point(610, 10);
        ipText.Size = new Size(80, 24);

        portText = new TextBox();
        portText.Name = "IPText";
        portText.Text = "80";
        portText.Location = new Point(610, 50);
        portText.Size = new Size(80, 24);

        connectButton = new Button();
        connectButton.Location = new Point(610, 90);
        connectButton.Size = new Size(80, 24);
        connectButton.Name = "ConnectButton";
        connectButton.Text = "Connect";
        connectButton.Click += new System.EventHandler(ConnectButtonPress);

        // Stretches the image to fit the pictureBox.
        renderArea.SizeMode = PictureBoxSizeMode.StretchImage ;
        Bitmap MyImage = new Bitmap("D:\\Github\\DNTest\\Client\\image.jpg");
        renderArea.ClientSize = new Size(600, 600);
        renderArea.Image = (Image) MyImage ;

        // Add to the form's control collection,
        // so that it will appear on the form.
        Controls.Add(renderArea);
        Controls.Add(ipText);
        Controls.Add(portText);
        Controls.Add(connectButton);


        colorGradient = new ColorGradient(
            new double[]{0, 0.16, 0.33, 0.5, 0.66, 0.83, 1},
            new Color[]{Color.Red, Color.Orange, Color.Yellow, Color.Green, Color.Blue, Color.Magenta, Color.Red}
            );
    }

    private void ConnectButtonPress(object sender, EventArgs e)
    {
        if (connected)
        {

        }
        else
        {
            // string[] ipSplit = ipText.Text.Split('.');

            // if (ipSplit.Length != 4)
            // {
            //     Console.WriteLine("Invalid IP format");
            //     return;
            // }

            // byte[] ipAddr = new byte[4];
            // bool success = true;
            // for (int i=0; i<4; i++)
            // {
            //     success &= byte.TryParse(ipSplit[i], out ipAddr[i]);
            // }

            // if (!success)
            // {
            //     Console.WriteLine("Failed to parse IP");
            //     return;
            // }

            ushort port;
            if (!ushort.TryParse(portText.Text, out port))
            {
                Console.WriteLine("Invalid Port format");
                return;
            }

            HandleConnection(ipText.Text, port);
        }
    }

    private void HandleConnection(string host, ushort port)
    {
        // Connect
        TcpClient client = new TcpClient(host, port);
        NetworkStream stream = client.GetStream();

        const string eol = "\r\n";

        stream.Write(Encoding.UTF8.GetBytes("-1.4;0;0.1;0.1" + eol));
        while (!stream.DataAvailable);

        byte[] bytes = new byte[client.Available]; // Do we need to delete/deallocate this...?
        stream.Read(bytes, 0, bytes.Length);

        switch ((ResponderStatus)bytes[0])
        {
            case ResponderStatus.OK:
                ushort width, height;
                width = (ushort)((bytes[1] << 8) | (bytes[2]));
                height = (ushort)((bytes[3] << 8) | (bytes[4]));

                Bitmap recievedImage = new Bitmap(width,height);

                for (int i=0; i< width; i++)
                {
                    for (int j=0; j<height; j++)
                    {
                        byte thisPixel = bytes[5+i+height*j];
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

                renderArea.Image = recievedImage;
                renderArea.Refresh();
                break;
            default:
                string message = Encoding.UTF8.GetString(bytes, 1, bytes.Length-1);
                Console.WriteLine("Got Response: Server side error: " + message);
                break;
        }

        client.Close();
    }

}
