using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using VisitorSignInSystem.Manager.Views;
using VisitorSignInSystem.Manager.Helpers;

namespace VisitorSignInSystem.Manager.Services
{
    public class AuthenticateTab
    {
        public async Task<bool> Authenticate()
        {
            try
            {
                PasswordBox input = new PasswordBox()
                {
                    Name = "MaintainPasswordBox",
                    Height = 34,
                    Width = 240,
                    FontSize = 18,
                    Padding = new Thickness(4),
                    Margin = new Thickness(2),
                    PlaceholderText = "Enter password",
                    PasswordChar = "*",
                    PasswordRevealMode = PasswordRevealMode.Visible
                };

                input.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Red);
                input.BorderThickness = new Thickness(1);

                ContentDialog dialog = new ContentDialog()
                {
                    Name = "ContentDialog1",
                    Title = $"Unlock for editing",
                    PrimaryButtonText = "OK",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Primary,
                    Content = input,
                };

                ContentDialogResult result = await dialog.ShowAsyncQueue();
                if (result == ContentDialogResult.Primary)
                {
                    input = (PasswordBox)dialog.Content;
                    if (input.Password != "K8IsGreat!")
                    {
                        // authentication failed

                        dialog = new ContentDialog()
                        {
                            Title = $"Password is incorrect.",
                            CloseButtonText = "OK",
                            Content = "Access Denied!",
                        };

                        await dialog.ShowAsyncQueue();

                        // await new Windows.UI.Popups.MessageDialog($"Password is incorrect.", "Access Denied!").ShowAsync();
                        NavigationService.Navigate(typeof(MainPage), null);
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    // do nothing
                }
                //dialog = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return false;
        }
    }
}
