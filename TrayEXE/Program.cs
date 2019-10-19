using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using Windows.Foundation.Collections;
using Windows.ApplicationModel.AppService;
using System.IO.Pipes;
using System.IO;
using Windows.Storage;

namespace TrayEXE
{
    class Program
    {
        //A mutex to prevent the UWP part from launching multiple instances of the EXE part.
        static Mutex mutex = new Mutex(true, "{4291b888-e528-4f46-a5fd-f7d669aad428}");

        static AppServiceConnection connection;
        static bool isServiceOpen = false;
        static string currentSIG = "nothing";

        static Thread pipeThread = new Thread(new ThreadStart(pipeTask));

        static void Main(string[] args)
        {
            if (mutex.WaitOne(TimeSpan.Zero, true))
            {
                //Create an AppService connection
                CreateConnection().Wait();
                //Start the pipe server
                pipeThread.Start();
                //Construct and show the tray icon and its context menu
                NotifyIcon trayIcon = new NotifyIcon();
                trayIcon.Text = "UWP Tray Icon";
                trayIcon.Icon = new Icon(SystemIcons.Application, 40, 40);
                trayIcon.Click += (s, e) =>
                {
                    if ((e as MouseEventArgs).Button == MouseButtons.Left)
                        SendSIG("You clicked the tray icon.");
                };
                ContextMenu trayMenu = new ContextMenu();
                trayMenu.MenuItems.Add("Menu item 1", (s, e) => SendSIG("You clicked menu item 1."));
                trayMenu.MenuItems.Add("Menu item 2", (s, e) => SendSIG("You clicked menu item 2."));
                trayMenu.MenuItems.Add("Menu item 3", (s, e) => SendSIG("You clicked menu item 3."));
                trayMenu.MenuItems.Add("-");
                trayMenu.MenuItems.Add("Exit", (s, e) =>
                {
                    SendSIG("Exit");
                    Environment.Exit(0);
                }
                );
                trayIcon.ContextMenu = trayMenu;
                trayIcon.Visible = true;
                Application.Run();
                mutex.ReleaseMutex();
            }
            else
            {
                //There is already an instance running
                var settings = ApplicationData.Current.LocalSettings;
                //Tell the original instance to re-establish connection
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "ArgPipe", PipeDirection.Out))
                {
                    //Connect to the pipe or wait until the pipe is available.
                    pipeClient.Connect();
                    using (StreamWriter sw = new StreamWriter(pipeClient))
                    {
                        sw.AutoFlush = true;
                        sw.Write((string)settings.Values["Parameter"]);
                    }
                }
                settings.Values["Parameter"] = "nothing";
                Environment.Exit(0);
            }
        }

        //Use pipes to communicate between instances
        static async void pipeTask()
        {
            while (true)
            {
                using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("ArgPipe", PipeDirection.In))
                {
                    //Wait for a client to connect
                    pipeServer.WaitForConnection();
                    try
                    {
                        using (StreamReader sr = new StreamReader(pipeServer))
                        {
                            var input = await sr.ReadToEndAsync();
                            Console.WriteLine(input);
                            if (input == "sync" || input == "nothing")
                            {
                                await CreateConnection();
                                if (input == "nothing") currentSIG = "You launched the app from Start again.";
                                //Sync signal
                                var task = connection.SendMessageAsync(new ValueSet()
                                {
                                    new KeyValuePair<string, object>("signal", currentSIG)
                                }).AsTask();
                                task.Wait();
                                currentSIG = "nothing";
                            }
                        }
                    }
                    //Swallow the IOException that is raised if the pipe is broken
                    //or disconnected.
                    catch (IOException) { }
                }
            }
        }

        static void SendSIG(string SIG)
        {
            //Communicate with the UWP part using AppService
            if (isServiceOpen == true)
            {
                //Cannot use await, or SendMessageAsync will never return.
                //I don't know why.
                var task = connection.SendMessageAsync(new ValueSet()
                {
                    new KeyValuePair<string, object>("signal", SIG)
                }).AsTask();
                task.Wait();
                //Sometimes Connection_ServiceClosed won't be called when you close the UWP.
                if (task.Result.Status == AppServiceResponseStatus.Failure)
                {
                    isServiceOpen = false;
                    SendSIG(SIG);
                }
            }
            else
            {
                //If AppService is closed, it means the UWP part has terminated.
                //Use command line to launch the UWP part and tell it to sync.
                if (SIG == "Exit") Environment.Exit(0);
                currentSIG = SIG;
                ProcessStartInfo procStartInfo = new ProcessStartInfo("cmd", "/c TrayUWP.exe sync");
                procStartInfo.UseShellExecute = false;
                procStartInfo.CreateNoWindow = true;
                using (Process process = new Process())
                {
                    process.StartInfo = procStartInfo;
                    process.Start();
                    process.WaitForExit();
                }
            }
        }

        static async Task CreateConnection()
        {
            connection = new AppServiceConnection();
            connection.AppServiceName = "CommunicationService";
            connection.PackageFamilyName = Windows.ApplicationModel.Package.Current.Id.FamilyName;
            connection.RequestReceived += Connection_RequestReceived;
            connection.ServiceClosed += Connection_ServiceClosed;
            var res = await connection.OpenAsync();
            if (res == AppServiceConnectionStatus.Success) isServiceOpen = true;
            else isServiceOpen = false;
        }

        private static void Connection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            isServiceOpen = false;
        }

        private static async void Connection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var messageDeferral = args.GetDeferral();
            try
            {
                string key = args.Request.Message.First().Key;
                string value = args.Request.Message.First().Value.ToString();
                if ((key, value) == ("request", "ping"))
                    await args.Request.SendResponseAsync(new ValueSet()
                    {
                        new KeyValuePair<string, object>("response", "From TrayEXE: Pong!")
                    });
            }
            finally
            {
                messageDeferral.Complete();
            }
        }
    }
}
