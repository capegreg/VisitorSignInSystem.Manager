using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using VisitorSignInSystem.Manager.Core.Models;
using Windows.UI.Core;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace VisitorSignInSystem.Manager.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AgentMetricsPage : Page, INotifyPropertyChanged
    {

        private HubConnection connection { get; } = ((App)Application.Current).VsisConnection;

        private ObservableCollection<AgentMetric> agentMetrics = new ObservableCollection<AgentMetric>();

        public ObservableCollection<AgentMetric> AgentMetricsItems
        {
            get
            {
                return agentMetrics;
            }
            set
            {
                Set(ref agentMetrics, value);
            }
        }


        public AgentMetricsPage()
        {
            this.InitializeComponent();
            //HubInvoked();
        }

        private void HubInvoked()
        {
            connection.On<List<AgentMetric>>("AgentMetricList", (m) =>
            {
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FillVisitorMetric(m));
            });

        }

        private async void FillVisitorMetric(List<AgentMetric> metrics)
        {
            agentMetrics.Clear();

            foreach (var m in metrics)
            {
                agentMetrics.Add(new AgentMetric
                {
                   AuthName=m.AuthName

                });
            }
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









    }
}
