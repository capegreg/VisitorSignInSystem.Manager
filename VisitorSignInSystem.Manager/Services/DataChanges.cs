using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace VisitorSignInSystem.Manager.Services
{
    public static class DataChanges
    {
        /// <summary>
        /// Wait for the existing dialog to be closed before starting new one
        /// If a dialog is already in view, wait for it to be closed.
        /// Once user dismisses the existing dialog, the next one can be shown.
        /// </summary>
        private static TaskCompletionSource<ContentDialog> _contentDialogShowRequest;

        public enum DataTrans
        {
            None,
            Add,
            Update,
            Delete
        }

        public enum DataContextItem
        {
            Users,
            Departments,
            Counters,
            Categories,
            WaitTimes,
            GroupDevices,
            TransferTypes,
            VisitorsQueue
        }

        public static async Task<ContentDialogResult> ShowAsyncQueue(this ContentDialog dialog)
        {
            if (!Window.Current.Dispatcher.HasThreadAccess)
            {
                Console.WriteLine("This method can only be invoked from UI thread.");
                //throw new InvalidOperationException("This method can only be invoked from UI thread.");
            }

            while (_contentDialogShowRequest != null)
            {
                await _contentDialogShowRequest.Task;
            }

            var request = _contentDialogShowRequest = new TaskCompletionSource<ContentDialog>();
            var result = await dialog.ShowAsync();
            _contentDialogShowRequest = null;
            request.SetResult(dialog);
            return result;
        }

        public static async Task ChangeApplied(DataTrans trans, string d)
        {
            try
            {
                string ok = "was successfully"; // prefix space
                string au = "";
                string icon = "";

                switch (trans)
                {
                    case DataTrans.Add:
                        au = "";
                        if (d.IndexOf("Cannot") == -1)
                            au = "added";

                        break;
                    case DataTrans.Update:
                        au = "";
                        if (d.IndexOf("Cannot") == -1)
                            au = "updated";

                        break;
                    case DataTrans.Delete:
                        au = "";

                        if (d.IndexOf("Cannot") == -1)
                            au = "deleted";

                        break;
                }

                if (d.IndexOf("Cannot") == -1)
                {
                    icon = "success.png";
                }
                else
                {
                    icon = "warning.png";
                }

                string title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(au.ToLower());

                Grid grid = new Grid();

                ColumnDefinition column = new ColumnDefinition();
                column.Width = new GridLength(0, GridUnitType.Auto);                
                grid.ColumnDefinitions.Add(column);

                column = new ColumnDefinition();
                column.Width = new GridLength(0, GridUnitType.Auto);
                grid.ColumnDefinitions.Add(column);

                RowDefinition row = new RowDefinition();

                // add image to first column and message to second

                row.Height = new GridLength(0, GridUnitType.Auto);
                grid.RowDefinitions.Add(row);

                ImageSource source = new BitmapImage(new Uri($"ms-appx:///Assets/{icon}"));

                Image img = new Image() { Width = 64, Height = 64, Source = source };
                img.Margin = new Thickness(4);

                grid.Children.Add(img);
                Grid.SetColumn(img, 0);
                Grid.SetRow(img, 0);

                TextBlock tb = new TextBlock();
                tb.Margin = new Thickness(6);
                tb.MaxWidth = 350;
                tb.FontSize = 16;
                tb.FontWeight = Windows.UI.Text.FontWeights.SemiBold;
                tb.TextWrapping = TextWrapping.WrapWholeWords;
                tb.Text = $"{d} {ok} {au}.";

                grid.Children.Add(tb);
                Grid.SetColumn(tb, 1);
                Grid.SetRow(img, 0);

                ContentDialog dialog = new ContentDialog();
                dialog.Title = $"{title} {d}";
                dialog.CloseButtonText = "OK";
                //dialog.Background = new SolidColorBrush(Windows.UI.Colors.LawnGreen);
                dialog.Content = grid;

                await dialog.ShowAsyncQueue();
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static async Task<string> DataChangeResponse(DataContextItem dci, DataTrans trans, string reason_text, string description_text)
        {
            try
            {
                string return_message = "";
                string noun_text = "";

                switch (dci)
                {
                    case DataContextItem.Users:
                        noun_text = "User";
                        break;
                    case DataContextItem.Departments:
                        noun_text = "Department";
                        break;
                    case DataContextItem.Counters:
                        noun_text = "Counter";
                        break;
                    case DataContextItem.Categories:
                        noun_text = "Category";
                        break;
                    case DataContextItem.WaitTimes:
                        noun_text = "Wait Time Category";
                        break;
                    case DataContextItem.GroupDevices:
                        noun_text = "Device";
                        break;
                    case DataContextItem.TransferTypes:
                        noun_text = "Transfer Type";
                        break;
                    case DataContextItem.VisitorsQueue:
                        noun_text = "Visitors Queue";
                        break;
                }
                if (reason_text != "success")
                {
                    string au = "";
                    string verb_text = "";

                    switch (trans)
                    {
                        case DataTrans.Add:
                            if (reason_text.IndexOf("Cannot") == -1)
                                au = "Added";

                            verb_text = "Add";
                            break;
                        case DataTrans.Update:
                            if (reason_text.IndexOf("Cannot") == -1)
                                au = "Updated";
                            verb_text = "Update";
                            break;
                        case DataTrans.Delete:
                            if (reason_text.IndexOf("Cannot") == -1)
                                au = "Deleted";
                            verb_text = "Delete";
                            break;
                        default:
                            au = "";
                            verb_text = "";
                            break;
                    }
                    if (reason_text == "Required fields")
                    {
                        au = "Please provide a valid value for:\r";
                    }
                    ContentDialog dialog = new ContentDialog();
                    dialog.Title = $"{verb_text} {noun_text} {au}{description_text}";
                    dialog.CloseButtonText = "OK";
                    dialog.Content = $"Reason:\n{reason_text}";

                    await dialog.ShowAsyncQueue();
                    
                }
                else
                {
                    if (trans != DataTrans.None)
                        return_message = $"{noun_text} {description_text}";
                }
                return return_message;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return "";
        }

        public static async Task<bool> ConfirmDialog(string title, string content)
        {
            try
            {
                Grid grid = new Grid();

                ColumnDefinition column = new ColumnDefinition();
                column.Width = new GridLength(0, GridUnitType.Auto);
                grid.ColumnDefinitions.Add(column);

                column = new ColumnDefinition();
                column.Width = new GridLength(0, GridUnitType.Auto);
                grid.ColumnDefinitions.Add(column);

                RowDefinition row = new RowDefinition();

                // add image to first column and message to second

                row.Height = new GridLength(0, GridUnitType.Auto);
                grid.RowDefinitions.Add(row);

                ImageSource source = new BitmapImage(new Uri("ms-appx:///Assets/warning_shield.png"));

                Image img = new Image() { Width = 64, Height = 64, Source = source };
                img.Margin = new Thickness(4);

                grid.Children.Add(img);
                Grid.SetColumn(img, 0);
                Grid.SetRow(img, 0);

                TextBlock tb = new TextBlock();
                tb.Margin = new Thickness(6);
                tb.MaxWidth = 350;
                tb.FontSize = 16;
                tb.FontWeight = Windows.UI.Text.FontWeights.SemiBold;
                tb.TextWrapping = TextWrapping.WrapWholeWords;
                tb.Text = content;

                grid.Children.Add(tb);
                Grid.SetColumn(tb, 1);
                Grid.SetRow(img, 0);

                ContentDialog dialog = new ContentDialog();
                dialog.Title = $"{title}";
                dialog.PrimaryButtonText = "OK";
                dialog.CloseButtonText = "Cancel";
                dialog.DefaultButton = ContentDialogButton.Close;

                dialog.Content = grid;

                ContentDialogResult result = await dialog.ShowAsyncQueue();
                if (result == ContentDialogResult.Primary)
                {
                    return true;
                }
                else
                {
                    // Do nothing.
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return false;
        }

        /// <summary>
        /// Generic dialog
        /// </summary>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public static async Task GenericDialog(string title, string content)
        {
            try
            {
                Grid grid = new Grid();

                ColumnDefinition column = new ColumnDefinition();
                column.Width = new GridLength(0, GridUnitType.Auto);
                grid.ColumnDefinitions.Add(column);

                column = new ColumnDefinition();
                column.Width = new GridLength(0, GridUnitType.Auto);
                grid.ColumnDefinitions.Add(column);

                RowDefinition row = new RowDefinition();

                // add image to first column and message to second

                row.Height = new GridLength(0, GridUnitType.Auto);
                grid.RowDefinitions.Add(row);

                ImageSource source = new BitmapImage(new Uri("ms-appx:///Assets/other.png"));

                Image img = new Image() { Width = 64, Height = 64, Source = source };
                img.Margin = new Thickness(4);

                grid.Children.Add(img);
                Grid.SetColumn(img, 0);
                Grid.SetRow(img, 0);

                TextBlock tb = new TextBlock();
                tb.Margin = new Thickness(6);
                tb.MaxWidth = 350;
                tb.FontSize = 16;
                tb.TextAlignment = TextAlignment.Left;
                tb.FontWeight = Windows.UI.Text.FontWeights.SemiBold;
                tb.TextWrapping = TextWrapping.WrapWholeWords;
                tb.Text = content;

                grid.Children.Add(tb);
                Grid.SetColumn(tb, 1);
                Grid.SetRow(img, 0);

                ContentDialog dialog = new ContentDialog();
                dialog.Title = $"{title}";
                dialog.CloseButtonText = "OK";
                dialog.Content = grid;

                await dialog.ShowAsyncQueue();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
