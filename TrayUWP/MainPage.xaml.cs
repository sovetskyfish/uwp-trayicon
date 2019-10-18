﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace TrayUWP
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            //Run the bundled EXE to show a tray icon.
            //You must configure your Package.appxmanifest correctly.
            if (string.IsNullOrEmpty(e.Parameter as string))
                _ = FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();

            //Receive the commands.
            blk.Text = string.IsNullOrEmpty(e.Parameter as string) ? "You just launched this app." : (e.Parameter as string);
            if (e.Parameter as string == "Exit") Application.Current.Exit();
        }
    }
}
