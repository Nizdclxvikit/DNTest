namespace client;

static class Program
{
    [STAThread]
    static void Main()
    {
        // see https://aka.ms/applicationconfiguration.
        //ApplicationConfiguration.Initialize();
        Application.EnableVisualStyles();
        Application.Run(new ClientWindow());
    }    
}