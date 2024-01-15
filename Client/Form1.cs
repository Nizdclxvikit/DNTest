using System.Diagnostics;
using System.Drawing.Printing;
using System.Net.Sockets;
using System.Numerics;
using System.Text;

namespace client;

public class ClientWindow : Form
{
    //private RichTextBox myConsole;
    private PictureBox renderArea;
    private Button connectButton;

    private TextBox ipText;
    private TextBox portText;

    private Client client;

    double x=0, y=0, w=1.5, h=1.5;
    private const double zoomFactor = 2.0;

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

        Bitmap MyImage = new Bitmap("D:\\Github\\DNTest\\Client\\image.jpg");
        renderArea = new PictureBox
        {
            Location = new Point(0, 0),
            Name = "RenderArea",
            Size = new Size(600, 600),
            SizeMode = PictureBoxSizeMode.StretchImage,
            ClientSize = new Size(600, 600),
            Image = (Image) MyImage
        };
        renderArea.MouseClick += new MouseEventHandler(RA_LeftClick);

        
        ipText = new TextBox
        {
            Name = "IPText",
            Text = "127.0.0.1",
            Location = new Point(610, 10),
            Size = new Size(80, 24)
        };

        portText = new TextBox
        {
            Name = "IPText",
            Text = "80",
            Location = new Point(610, 50),
            Size = new Size(80, 24)
        };

        connectButton = new Button
        {
            Name = "ConnectButton",
            Text = "Connect",
            Location = new Point(610, 90),
            Size = new Size(80, 24)
        };
        connectButton.Click += new System.EventHandler(ConnectButtonPress);


        // Add to the form's control collection,
        // so that it will appear on the form.
        Controls.Add(renderArea);
        Controls.Add(ipText);
        Controls.Add(portText);
        Controls.Add(connectButton);


        client = new Client();
        client.imageUpdated += new EventHandler(ImageUpdatedCB);
        client.onConnect += new EventHandler(OnConnect);
        client.onDisconnect += new EventHandler(OnDisconnect);
        

        this.FormClosed += new FormClosedEventHandler(OnFormClosed);
    }

    private void ConnectButtonPress(object? sender, EventArgs e)
    {
        if (!client.isConnected)
        {
            ushort port;
            if (!ushort.TryParse(portText.Text, out port))
            {
                Console.WriteLine("Invalid Port format");
                return;
            }

            client.Connect(ipText.Text, port);
            client.MakeRequest(x, y, w, h); // Fractal Area
        }
        else
        {
            client.Disconnect();
        }
    }

    private void ImageUpdatedCB(object? sender, EventArgs e)
    {
        if (InvokeRequired)
        {
            // If we are in the side thread, call this function from the main thread instead
            Invoke(new Action<object, EventArgs>(ImageUpdatedCB), sender, e);
            return;
        }

        // In the main thread for sure
        UpdateRenderArea(client.GetMyImage());
    }

    private void OnConnect(object? sender, EventArgs e)
    {
        connectButton.Text = "Disconnect";
        connectButton.Refresh();
    }
    private void OnDisconnect(object? sender, EventArgs e)
    {
        connectButton.Text = "Connect";
        connectButton.Refresh();
        UpdateRenderArea(new Bitmap("D:\\Github\\DNTest\\Client\\image.jpg"));
    }

    private void UpdateRenderArea(Image newImage)
    {
        renderArea.Image = newImage;
        renderArea.Refresh();
    }

    private void RA_LeftClick(object? sender, MouseEventArgs e)
    {
        if (!client.isConnected) return;
        x = RA_PointToX(e.Location);
        y = RA_PointToY(e.Location);
        switch (e.Button)
        {
            case MouseButtons.Left:
                w /= zoomFactor;
                h /= zoomFactor;
                client.MakeRequest(x,y,w,h);
                break;
            case MouseButtons.Right:
                w *= zoomFactor;
                h *= zoomFactor;
                client.MakeRequest(x,y,w,h);
                break;
        }
        
        
    }
    
    private double RA_PointToX(Point point)
    {
        double newX = x - w/2;
        newX += w * point.X/(double)renderArea.Width;
        return newX;
    }

    private double RA_PointToY(Point point)
    {
        double newY = y + h/2;
        newY -= h * point.Y/(double)renderArea.Height;
        return newY;
    }

    private void OnFormClosed(object? sender, FormClosedEventArgs e)
    {
        client?.Disconnect();
    }
    

}
