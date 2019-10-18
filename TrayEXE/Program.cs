using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TrayEXE
{
    class Program
    {
        //A mutex to prevent the UWP part launch multiple instances of the EXE part.
        static Mutex mutex = new Mutex(true, "{4291b888-e528-4f46-a5fd-f7d669aad428}");

        static void Main(string[] args)
        {
            if (mutex.WaitOne(TimeSpan.Zero, true))
            {
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
                    Application.Exit();
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
                SendSIG("You launched the app from Start again.");
                return;
            }
        }

        static void SendSIG(string SIG)
        {
            //Communicate with the UWP part using command line arguments.
            //It can be done more elegantly by using AppServices,
            //but this is just a demo showing how to run a bundled EXE to show a tray icon for a UWP.
            ProcessStartInfo procStartInfo = new ProcessStartInfo("cmd", "/c TrayUWP.exe " + SIG);
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
}
