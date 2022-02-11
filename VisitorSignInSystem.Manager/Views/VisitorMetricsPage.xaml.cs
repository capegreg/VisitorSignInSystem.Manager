using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using VisitorSignInSystem.Manager.Core.Models;
using VisitorSignInSystem.Manager.Services;
using Windows.UI.Core;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Windows.UI.Xaml.Media;
using Windows.Storage;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace VisitorSignInSystem.Manager.Views
{
    public sealed partial class VisitorMetricsPage : Page, INotifyPropertyChanged
    {
        // SignalR hub for this page
        private HubConnection connection { get; set; }

        // Configuration to store local Windows data
        //private static VsisConfiguration VsisSettings;

        private ElementTheme _elementTheme = ThemeSelectorService.Theme;

        public ElementTheme ElementTheme
        {
            get { return _elementTheme; }

            set { Set(ref _elementTheme, value); }
        }

        Brush paoBlack = (Brush)App.Current.Resources["PaoSolidColorBrushBlack"];
        Brush paoIvory = (Brush)App.Current.Resources["PaoSolidColorBrushIvory"];
        Brush paoOldBlue = (Brush)App.Current.Resources["PaoSolidColorBrushOldBlue"];
        Brush paoWhite = (Brush)App.Current.Resources["PaoSolidColorBrushWhite"];

        // Collections for datagrids
        private ObservableCollection<AgentMetric> agentMetric = new ObservableCollection<AgentMetric>();
        private ObservableCollection<CategoryMetric> categoryMetric = new ObservableCollection<CategoryMetric>();

        public ObservableCollection<AgentMetric> AgentMetricItems
        {
            get
            {
                return agentMetric;
            }
            set
            {
                Set(ref agentMetric, value);
            }
        }
        public ObservableCollection<CategoryMetric> CategoryMetricItems
        {
            get
            {
                return categoryMetric;
            }
            set
            {
                Set(ref categoryMetric, value);
            }
        }

        public VisitorMetricsPage()
        {
            this.InitializeComponent();
            this.Loaded += VisitorMetricsPage_Loaded;
        }

        private async void VisitorMetricsPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                connection = ((App)Application.Current).VsisConnection;

                // TODO: MainPage is startup page and openconnection.
                // Should move OpenConnection to interface
                if (connection != null)
                {
                    SetCurrentThemeStyle();
                    HubInvoked();
                    await SendGetVisitorMetrics();

                    HeaderTitleTextBlock.Text = "Visitor Statistics";
                    HeaderSubTitleTextBlock.Text = "Agent Counts, Visitor Counts";
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Received messages
        /// </summary>
        private void HubInvoked()
        {
            if (connection != null)
            {
                connection.On<List<AgentMetric>>("AgentMetric", (m) =>
                {
                    _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FillAgentMetric(m));
                });
                connection.On<List<CategoryMetric>>("CategoryMetrics", (m) =>
                {
                    _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FillCategoryMetric(m));
                });

            }
        }

        /// <summary>
        /// Update control best style to current selected theme
        /// </summary>
        private void SetCurrentThemeStyle()
        {
            try
            {
                Brush altRowBg = null, altRowFg = null, border = null;

                switch (ThemeSelectorService.Theme)
                {
                    case ElementTheme.Default:
                    case ElementTheme.Light:
                        altRowBg = paoIvory;
                        altRowFg = paoBlack;
                        border = paoOldBlue;
                        break;
                    case ElementTheme.Dark:
                        altRowBg = paoOldBlue;
                        altRowFg = paoWhite;
                        border = altRowBg;
                        break;
                }

                if (altRowBg != null)
                {

                    AgentMetricDataGrid.AlternatingRowBackground = altRowBg;
                    AgentMetricDataGrid.BorderBrush = border;

                    CategoryMetricDataGrid.AlternatingRowBackground = altRowBg;
                    CategoryMetricDataGrid.BorderBrush = border;
                }

                if (altRowFg != null)
                {
                    AgentMetricDataGrid.AlternatingRowForeground = altRowFg;
                    CategoryMetricDataGrid.AlternatingRowForeground = altRowFg;
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Initial load of metrics
        /// </summary>
        /// <returns></returns>
        private async Task SendGetVisitorMetrics()
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("GetVisitorMetrics");
            }
            catch (Exception) {}
        }

        private async void FillAgentMetric(List<AgentMetric> agent_metric)
        {
            agentMetric = new ObservableCollection<AgentMetric>(agent_metric);

            AgentMetricItems = agentMetric;
            AgentMetricDataGrid.ItemsSource = null;
            AgentMetricDataGrid.ItemsSource = AgentMetricItems;

            await Task.CompletedTask;
        }

        private async void FillCategoryMetric(List<CategoryMetric> category_metric)
        {
            categoryMetric = new ObservableCollection<CategoryMetric>(category_metric);

            CategoryMetricItems = categoryMetric;
            CategoryMetricDataGrid.ItemsSource = null;
            CategoryMetricDataGrid.ItemsSource = CategoryMetricItems;

            await Task.CompletedTask;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Set<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return;
            }

            storage = value;
            OnPropertyChanged(propertyName);
        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #region Grid_sorting_events

        private void AgentMetricDataGrid_Sorting(object sender, DataGridColumnEventArgs e)
        {
            if (e.Column.Tag != null)
            {
                //Implement sort on the column
                if (e.Column.SortDirection == null || e.Column.SortDirection == DataGridSortDirection.Descending)
                {
                    switch (e.Column.Tag)
                    {
                        case "FullName":
                            AgentMetricDataGrid.ItemsSource = new ObservableCollection<AgentMetric>(from item in agentMetric
                                                                                                        orderby item.FullName ascending
                                                                                                        select item);
                            break;
                    }
                    e.Column.SortDirection = DataGridSortDirection.Ascending;
                }
                else
                {

                    switch (e.Column.Tag)
                    {

                        case "FullName":
                            AgentMetricDataGrid.ItemsSource = new ObservableCollection<AgentMetric>(from item in agentMetric
                                                                                                    orderby item.FullName descending
                                                                                                        select item);
                            break;
                    }
                    e.Column.SortDirection = DataGridSortDirection.Descending;
                }
                // add code to handle sorting by other columns as required

                // Remove sorting indicators from other columns
                foreach (var dgColumn in AgentMetricDataGrid.Columns)
                {
                    if (dgColumn.Tag != null && dgColumn.Tag.ToString() != e.Column.Tag.ToString())
                    {
                        dgColumn.SortDirection = null;
                    }
                }
            }
        }
        #endregion Grid_sorting_events
    }
}
