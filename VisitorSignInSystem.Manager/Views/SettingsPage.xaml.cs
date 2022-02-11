using Microsoft.Toolkit.Uwp.UI.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using VisitorSignInSystem.Manager.Helpers;
using VisitorSignInSystem.Manager.Services;
using Windows.ApplicationModel;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace VisitorSignInSystem.Manager.Views
{
    // TODO WTS: Add other settings as necessary. For help see https://github.com/Microsoft/WindowsTemplateStudio/blob/release/docs/UWP/pages/settings-codebehind.md
    // TODO WTS: Change the URL for your privacy policy in the Resource File, currently set to https://YourPrivacyUrlGoesHere
    public sealed partial class SettingsPage : Page, INotifyPropertyChanged
    {
        private ElementTheme _elementTheme = ThemeSelectorService.Theme;

        public ElementTheme ElementTheme
        {
            get { return _elementTheme; }

            set { Set(ref _elementTheme, value); }
        }

        private string _versionDescription;

        public string VersionDescription
        {
            get { return _versionDescription; }

            set { Set(ref _versionDescription, value); }
        }

        private string _user;

        public string User
        {
            get { return _user; }

            set { Set(ref _user, value); }
        }


        public SettingsPage()
        {
            InitializeComponent();
        }

        private async void SettingsUI_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadSettings();
        }

        /// <summary>
        /// 
        ///     Note: Configuration depends on communication with the server. Most of the application
        ///             settings will start in LoadSettings and not page load.
        /// 
        /// 1. Gets local storage settings and determines if the app is running as an agent or a counter
        ///     by checking CounterName. If no counter name is present, user is an agent.
        ///     
        /// 3. The value of ClientGroupName is the client\server messaging name. This can be set to
        ///     either a counter name or Windows User Id. Examples:
        ///     Counter:    ClientGroupName = pao-pc104
        ///     Agent:      ClientGroupName = ksmith
        ///     
        /// 4. When it is not possible to determine a ClientGroupName, as when the app is launched
        ///     for the first time, a guid will be used. The guid will enable one-to-one communication 
        ///     with the server, and establish configuration data.
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task LoadSettings()
        {
            try
            {
                VsisConfiguration vsisSettings = await GetLocalStorageSettings();

                var users = await Windows.System.User.FindAllAsync(UserType.LocalUser);
                var name = await users.FirstOrDefault().GetPropertyAsync(KnownUserProperties.AccountName);
                var fullName = await users.FirstOrDefault().GetPropertyAsync(KnownUserProperties.DisplayName);

                string about = $"{fullName}\n{name}";

                YourInfoUserText.Text = about;

                if (vsisSettings != null)
                {
                    VsisHost.Text= vsisSettings.Host;
                    VsisLocationTextBox.Text = vsisSettings.Location.ToString();

                    ToolTip tp = new ToolTip();
                    tp.Content = $"{vsisSettings.Host}";
                    tp.Visibility = Visibility.Visible;
                    ToolTipService.SetToolTip(VsisHost, tp);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await InitializeAsync();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);

            SaveLocalStorageSettings();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task InitializeAsync()
        {
            VersionDescription = GetVersionDescription();
            await Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private string GetVersionDescription()
        {
            var appName = "AppDisplayName".GetLocalized();
            var package = Package.Current;
            var packageId = package.Id;
            var version = packageId.Version;

            return $"{appName} - {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ThemeChanged_CheckedAsync(object sender, RoutedEventArgs e)
        {
            var param = (sender as RadioButton)?.CommandParameter;

            if (param != null)
            {
                await ThemeSelectorService.SetThemeAsync((ElementTheme)param);
            }
        }

        /// <summary>
        /// Get User local storage settings
        /// </summary>
        /// <returns></returns>
        private Task<VsisConfiguration> GetLocalStorageSettings()
        {
            VsisConfiguration c = new VsisConfiguration();

            try
            {
                Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                Object hostValue = localSettings.Values["Host"];
                if (hostValue != null && hostValue.ToString().Length > 0)
                {
                    c.Host = hostValue.ToString();
                }
                else
                {
                    c.Host = "http://vsistest.manateepao.com:5000/vsisHub";
                }
                Object locationValue = localSettings.Values["Location"];
                if(locationValue != null && locationValue.ToString().Length > 0)
                {
                    sbyte result = Convert.ToSByte(locationValue);
                    if (result == 0)
                        result = 1;
                    c.Location = result;
                }
                else
                {
                    c.Location = 1;
                }
                Object zoomFactorValue = localSettings.Values["ZoomFactor"];
                if (zoomFactorValue != null && zoomFactorValue.ToString().Length > 0)
                {
                    ZoomFactorText.Text = zoomFactorValue.ToString();
                }
                Object enableDesktopNotificationsValue = localSettings.Values["EnableDesktopNotifications"];
                if (enableDesktopNotificationsValue != null)
                {
                    EnableDesktopNotifications.IsOn = (bool)enableDesktopNotificationsValue;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return Task.FromResult(c);
        }

        /// <summary>
        /// Save to local storage settings
        /// </summary>
        private void SaveLocalStorageSettings()
        {
            try
            {
                Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

                if (VsisHost.Text.Length > 0)
                {
                    if (localSettings.Values["Host"] != null)
                    {
                        if (localSettings.Values["Host"].ToString().Length > 0 && localSettings.Values["Host"].ToString() != VsisHost.Text)
                        {
                            // connection host changed
                            ((App)Application.Current).VsisConnection = null;
                        }                        
                    }
                    localSettings.Values["Host"] = VsisHost.Text;
                    //
                    localSettings.Values["EnableDesktopNotifications"] = EnableDesktopNotifications.IsOn;
                    //
                    localSettings.Values["Location"] = VsisLocationTextBox.Text;
                    //
                    localSettings.Values["ZoomFactor"] = ZoomFactorText.Text;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="storage"></param>
        /// <param name="value"></param>
        /// <param name="propertyName"></param>
        private void Set<T>(ref T storage, T value, [CallerMemberName]string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return;
            }

            storage = value;
            OnPropertyChanged(propertyName);
        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void ZoomFactorButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            try
            {
                Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

                // if (localSettings.Containers.ContainsKey("ZoomFactor"))
                // {
                ZoomFactorText.Text = "1.0";
                SaveLocalStorageSettings();
                GetLocalStorageSettings();
                // }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    } // end class
} // end namespace
