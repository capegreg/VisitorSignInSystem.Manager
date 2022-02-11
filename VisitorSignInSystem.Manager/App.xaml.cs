using System;

using VisitorSignInSystem.Manager.Core.Helpers;
using VisitorSignInSystem.Manager.Services;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.Foundation.Collections;

namespace VisitorSignInSystem.Manager
{
    public sealed partial class App : Application
    {
        private Lazy<ActivationService> _activationService;
        
        private ActivationService ActivationService
        {
            get { return _activationService.Value; }
        }

        public VsisConfiguration VsisSettings { get; set; }
        public HubConnection VsisConnection { get; set; }

        // Global, Applies to datagrid readonly
        public bool IsAuthenticated { get; set; }

        //public enum DataTrans
        //{
        //    None,
        //    Add,
        //    Update,
        //    Delete
        //}
        //public DataTrans DataTransType { get; set; }

        public App()
        {
            InitializeComponent();

            EnteredBackground += App_EnteredBackground;
            Resuming += App_Resuming;
            UnhandledException += OnAppUnhandledException;

            // Deferred execution until used. Check https://docs.microsoft.com/dotnet/api/system.lazy-1 for further info on Lazy<T> class.
            _activationService = new Lazy<ActivationService>(CreateActivationService);

            VsisSettings = new VsisConfiguration();
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            if (!args.PrelaunchActivated)
            {
                await ActivationService.ActivateAsync(args);
            }
        }

        protected override async void OnActivated(IActivatedEventArgs e)
        {
            await ActivationService.ActivateAsync(e);

            //// Handle notification activation
            //if (e is ToastNotificationActivatedEventArgs toastActivationArgs)
            //{
            //    // Obtain the arguments from the notification
            //    ToastArguments args = ToastArguments.Parse(toastActivationArgs.Argument);

            //    // Obtain any user input (text boxes, menu selections) from the notification
            //    ValueSet userInput = toastActivationArgs.UserInput;

            //    // TODO: Show the corresponding content
            //}


        }

        private void OnAppUnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // TODO WTS: Please log and handle the exception as appropriate to your scenario
            // For more info see https://docs.microsoft.com/uwp/api/windows.ui.xaml.application.unhandledexception
        }

        /// <summary>
        /// Set startup page
        /// </summary>
        /// <returns></returns>
        private ActivationService CreateActivationService()
        {
            return new ActivationService(this, typeof(Views.MainPage), new Lazy<UIElement>(CreateShell));
        }

        private UIElement CreateShell()
        {
            return new Views.ShellPage();
        }

        private async void App_EnteredBackground(object sender, EnteredBackgroundEventArgs e)
        {
            var deferral = e.GetDeferral();
            await Singleton<SuspendAndResumeService>.Instance.SaveStateAsync();
            deferral.Complete();
        }

        private void App_Resuming(object sender, object e)
        {
            Singleton<SuspendAndResumeService>.Instance.ResumeApp();
        }

        protected override async void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            await ActivationService.ActivateAsync(args);
        }
    }
}
