using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace TrayUWP
{
    /// <summary>
    /// 提供特定于应用程序的行为，以补充默认的应用程序类。
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// 初始化单一实例应用程序对象。这是执行的创作代码的第一行，
        /// 已执行，逻辑上等同于 main() 或 WinMain()。
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        //Command line arguments
        string cmdArgs = null;

        //The connection
        public static AppServiceConnection Connection = null;

        BackgroundTaskDeferral appServiceDeferral = null;

        //Initializes the app service on the host process 
        protected override void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            base.OnBackgroundActivated(args);
            if (args.TaskInstance.TriggerDetails is AppServiceTriggerDetails)
            {
                appServiceDeferral = args.TaskInstance.GetDeferral();
                args.TaskInstance.Canceled += OnTaskCanceled;

                AppServiceTriggerDetails details = args.TaskInstance.TriggerDetails as AppServiceTriggerDetails;
                Connection = details.AppServiceConnection;
                Connection.RequestReceived += Connection_RequestReceived;
            }
        }

        Frame rootFrame;

        private async void Connection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var messageDeferral = args.GetDeferral();
            try
            {
                string value = args.Request.Message.First().Value.ToString();
                //Send a response, prevent the non-UWP part from waiting.
                await args.Request.SendResponseAsync(new ValueSet() { new KeyValuePair<string, object>("response", value) });
                if (value == "Exit")
                {
                    Current.Exit();
                }
                await rootFrame.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    rootFrame.Navigate(typeof(MainPage), value, new SuppressNavigationTransitionInfo());
                });
            }
            finally
            {
                //Complete the deferral, telling the system we have completed the response.
                messageDeferral.Complete();
            }
        }

        //Associate the cancellation handler with the background task 
        private void OnTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            if (this.appServiceDeferral != null)
            {
                //Complete the service deferral.
                this.appServiceDeferral.Complete();
            }
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            //When activated by command line
            if (args.Kind == ActivationKind.CommandLineLaunch)
            {
                var commandLine = args as CommandLineActivatedEventArgs;
                if (commandLine != null)
                {
                    var operation = commandLine.Operation;
                    cmdArgs = operation.Arguments;
                }
            }

            rootFrame = Window.Current.Content as Frame;

            if (rootFrame == null)
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                Window.Current.Content = rootFrame;
            }

            rootFrame.Navigate(typeof(MainPage), cmdArgs);

            Window.Current.Activate();
            base.OnActivated(args);
        }

        //LaunchFullTrustProcess with a string passed via local settings container
        public static async Task LaunchFullTrustProcess(string Parameter = "nothing")
        {
            var settings = ApplicationData.Current.LocalSettings;
            settings.Values["Parameter"] = Parameter;
            await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
        }

        /// <summary>
        /// 在应用程序由最终用户正常启动时进行调用。
        /// 将在启动应用程序以打开特定文件等情况下使用。
        /// </summary>
        /// <param name="e">有关启动请求和过程的详细信息。</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            rootFrame = Window.Current.Content as Frame;

            // 不要在窗口已包含内容时重复应用程序初始化，
            // 只需确保窗口处于活动状态
            if (rootFrame == null)
            {
                // 创建要充当导航上下文的框架，并导航到第一页
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: 从之前挂起的应用程序加载状态
                }

                // 将框架放在当前窗口中
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // 当导航堆栈尚未还原时，导航到第一页，
                    // 并通过将所需信息作为导航参数传入来配置
                    // 参数
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // 确保当前窗口处于活动状态
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// 导航到特定页失败时调用
        /// </summary>
        ///<param name="sender">导航失败的框架</param>
        ///<param name="e">有关导航失败的详细信息</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// 在将要挂起应用程序执行时调用。  在不知道应用程序
        /// 无需知道应用程序会被终止还是会恢复，
        /// 并让内存内容保持不变。
        /// </summary>
        /// <param name="sender">挂起的请求的源。</param>
        /// <param name="e">有关挂起请求的详细信息。</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: 保存应用程序状态并停止任何后台活动
            deferral.Complete();
        }
    }
}
