using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.SignalR.Client;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Windows.System;
using Windows.UI.Xaml.Media;
using Windows.UI.Core;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel.ExtendedExecution;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Xaml.Data;
using VisitorSignInSystem.Manager.Core.Models;
using VisitorSignInSystem.Manager.Services;
using Windows.UI.Xaml.Media.Imaging;

namespace VisitorSignInSystem.Manager.Views
{
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        public static MainPage Current;

        // enable locale if needed
        private CultureInfo enUS = new CultureInfo("en-US");

        // Creates a TextInfo based on the "en-US" culture.
        TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

        private ExtendedExecutionSession session = null;

        // Defined in Styles\_Colors.xaml
        static Brush paoRed = (Brush)App.Current.Resources["PaoSolidColorBrushRed"];
        static Brush paoSteelBlue = (Brush)App.Current.Resources["PaoSolidColorBrushSteelBlue"];
        static Brush paoGreenGrass = (Brush)App.Current.Resources["PaoSolidColorBrushGreenGrass"];
        static Brush paoGreen = (Brush)App.Current.Resources["PaoSolidColorBrushGreen"];
        static Brush paoBlack = (Brush)App.Current.Resources["PaoSolidColorBrushBlack"];
        static Brush paoBlue = (Brush)App.Current.Resources["PaoSolidColorBrushBlue"];
        static Brush paoIvory = (Brush)App.Current.Resources["PaoSolidColorBrushIvory"];
        static Brush paoOldBlue = (Brush)App.Current.Resources["PaoSolidColorBrushOldBlue"];
        static Brush paoWhite = (Brush)App.Current.Resources["PaoSolidColorBrushWhite"];
        static Brush paoDarkGoldenrod = (Brush)App.Current.Resources["PaoSolidColorBrushDarkGoldenrod"];
        static Brush paoGold = (Brush)App.Current.Resources["PaoSolidColorBrushGold"];
        static Brush paoDisabled = (Brush)App.Current.Resources["PaoSolidColorBrushDisabled"];
        static Brush paoEasyBlue = (Brush)App.Current.Resources["PaoSolidColorBrushEasyBlue"];

        // https://docs.microsoft.com/en-us/windows/apps/design/style/segoe-ui-symbol-font

        private const string SEGOE_MDL2_ASSET_WALKOPEN = "\xe805";
        private const string SEGOE_MDL2_ASSET_HEALTH = "\xe8b8";
        private const string SEGOE_MDL2_ASSET_COMPLETED = "\xe930";
        private const string SEGOE_MDL2_ASSET_CONTACTSOLID = "\xea8c";
        private const string SEGOE_MDL2_ASSET_BLOCKCONTACT = "\xe8f8";

        // Set max toast notification before reset
        private Int16 maxDesktopNotifications = 1;

        // Local, Applies to datagrid readonly
        public bool _isAuthenticated { get; set; }

        // SignalR hub for this page
        private HubConnection connection { get; set; }

        // Configuration to store local Windows data
        private static VsisConfiguration _vsisSettings;

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
        DataContextItem CurrentContext;

        /// <summary>
        /// Maintain operation
        /// </summary>
        public enum DataTrans
        {
            None,
            Add,
            Update,
            Delete
        }
        DataTrans _dataTransType;

        [Flags]
        private enum VisitorQueueChangesValidation
        {
            VisitCategoryId
        }

        // Collections for datagrids
        private ObservableCollection<VisitorQueue> visitorQueue = new ObservableCollection<VisitorQueue>();
        private ObservableCollection<AgentProfile> agentProfile = new ObservableCollection<AgentProfile>();
        private ObservableCollection<CounterDetail> counterDetail = new ObservableCollection<CounterDetail>();
        private ObservableCollection<AgentMetric> agentMetrics = new ObservableCollection<AgentMetric>();
        private ObservableCollection<GroupInfoCollection<NotificationItem>> NotifyGroups = new ObservableCollection<GroupInfoCollection<NotificationItem>>();
        private List<NotificationItem> agentsNotAvailableNotifications = new List<NotificationItem>();
        private List<NotificationItem> waitTimeNotifications = new List<NotificationItem>();

        private static CollectionViewSource groupedItems;

        public ObservableCollection<VisitorQueue> VisitorQueueItems
        {
            get
            {
                return visitorQueue;
            }
            set
            {
                Set(ref visitorQueue, value);
            }
        }
        public ObservableCollection<AgentProfile> AgentProfileItems
        {
            get
            {
                return agentProfile;
            }
            set
            {
                Set(ref agentProfile, value);
            }
        }
        public ObservableCollection<CounterDetail> CounterDetailItems
        {
            get
            {
                return counterDetail;
            }
            set
            {
                Set(ref counterDetail, value);
            }
        }
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

        // Combo lists
        private List<Category> CategoriesList;

        // Useful to show old category after update
        // in success dialog
        private string Old_VisitorEditCategory { get; set; }
        private int VisitorQueueSelectedEditIndex { get; set; }

        public MainPage()
        {
            InitializeComponent();
            BeginExtendedExecution();

            Current = this;

            this.Loaded += MainPage_Loaded;
            NotifyGroups.CollectionChanged += NotifyGroups_CollectionChanged;

            // Cache page
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
        }

        private void NotifyGroups_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action.ToString().ToUpper() == "ADD")
                DoToastNotification();

            //Debug.WriteLine("Change type: " + e.Action);
            //if (e.NewItems != null)
            //{
            //    Debug.WriteLine("Items added: ");
            //    foreach (var item in e.NewItems)
            //    {
            //        Debug.WriteLine(item);
            //    }
            //}

            //if (e.OldItems != null)
            //{
            //    Debug.WriteLine("Items removed: ");
            //    foreach (var item in e.OldItems)
            //    {
            //        Debug.WriteLine(item);
            //    }
            //}

        }

        /// <summary>
        /// page loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadAppAll();
        }

        /// <summary>
        /// Keep in numerical order
        /// </summary>
        private enum VisitorStatusTypes
        {
            TakeCall,       // Get next visitor waiting
            AssignCounter,  // Counter where visitor should go
            AnnounceVisitor,// Post message to visitor on display
            MarkArrived,    // Mark that the visitor has arrived at counter
            EndCall         // Mark call closed
        };

        /// <summary>
        /// Received messages
        /// </summary>
        private void HubInvoked()
        {
            try
            {
                if (connection != null)
                {
                    connection.On("UserNotInRole", () =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UserNotInRole());
                    });
                    connection.On<string, string>("DataChangeAppliedMainPage", (m, d) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => DataChangeAppliedMainPage(m, d));
                    });
                    connection.On<List<VisitorQueue>>("VisitorQueueList", (m) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FillVisitorQueue(m));
                    });
                    connection.On<VisitorQueue>("VisitorQueueItem", (m) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => VisitorQueueItem(m));
                    });
                    connection.On<List<AgentProfile>>("AgentsStatusList", (m) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FillAgentsStatus(m));
                    });
                    connection.On<AgentProfile>("AgentsStatusItem", (m) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UpdateAgentsStatusItem(m));
                    });
                    connection.On<List<CounterDetail>>("AllCountersStatus", (m) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FillCountersStatus(m));
                    });
                    connection.On<CounterDetail>("CounterStatusMgr", (m) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UpdateCounterStatus(m));
                    });
                    connection.On<List<Category>>("CategoriesList", (m) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FillCategoriesList(m));
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Show dialog with server message
        /// </summary>
        /// <param name="reason_text"></param>
        /// <param name="descrption_text"></param>
        private async void DataChangeAppliedMainPage(string reason_text, string descrption_text)
        {
            try
            {
                //DataTransType = (DataTrans)((App)Application.Current).DataTransType;

                if (reason_text == "success")
                {
                    // do before clearing trans
                    await DataChanges.ChangeApplied((DataChanges.DataTrans)DataTransType, descrption_text);

                    DataTransType = DataTrans.None;
                    EnableEditControls(false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        //private void DataChangeApplied(string reason_text, string descrption_text)
        //{
        //    try
        //    {
        //        //  await DataChanges.ChangeApplied((DataChanges.DataTrans)DataTransType, descrption_text);

        //        string result_text = "";
        //        string result_glyph = "\xE72E";

        //        if (reason_text == "success")
        //        {
        //            DataTransType = DataTrans.None;
        //            EnableEditControls(false);

        //            result_text = "Record updated successfully.";
        //            result_glyph = "\xE8FB";

        //        }
        //        else
        //        {
        //            result_text = reason_text;
        //            result_glyph = "\xEA39";
        //        }

        //        _dataChangeResultTextBlock = result_text;
        //        _dataChangeResultGlyph = result_glyph;

        //        //button.IsTapEnabled = false;
        //        //button.Foreground = paoColor;
        //        //ToolTip tip = new ToolTip();
        //        //tip.Content = $"Select a row to delete";
        //        //ToolTipService.SetToolTip(button, tip);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //    }
        //}

        private async Task SendGetVisitorsMgr()
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("GetAllVisitorsMgr", _vsisSettings.AuthName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        // *******************************************************************
        // Add, Updates, Deletes must have return method included in Send
        // *******************************************************************
        string returnChangeAppliedPage = "DataChangeAppliedMainPage";

        private async Task SendPurgeVisitorInQueue(int id)
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("PurgeVisitorInQueue", _vsisSettings.AuthName, returnChangeAppliedPage, id, _vsisSettings.Location);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task SendUpdateVisitorCategory(int visitorId, ulong visitCategoryId)
        {
            try
            {
                if (connection != null)
                {
                    await connection.InvokeAsync("UpdateVisitorCategory", _vsisSettings.AuthName, returnChangeAppliedPage, visitorId, visitCategoryId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task SaveUpdateVisitorCategory()
        {
            try
            {
                VisitorQueue v = new VisitorQueue();

                bool hasSelectedItem = false;

                foreach (var obj in VisitorsDataGrid.SelectedItems)
                {
                    v = obj as VisitorQueue;
                    hasSelectedItem = true;
                }

                if (hasSelectedItem && v != null)
                {
                    VisitorQueueChangesValidation validate = VisitorQueueChangesValidation.VisitCategoryId;

                    if (v.VisitCategoryId > 0)
                        validate &= ~VisitorQueueChangesValidation.VisitCategoryId;

                    if (validate == 0)
                    {
                        var New_VisitorCategory = CategoriesList.FirstOrDefault(p => p.Id == v.VisitCategoryId).Description;
                        string msg = $"Change visitor {v.FullName} visit reason\nfrom '{Old_VisitorEditCategory}' to '{New_VisitorCategory}'?";
                        bool tf = await DataChanges.ConfirmDialog($"Update Visitor Reason", msg);
                        if (tf)
                            await SendUpdateVisitorCategory(v.Id, v.VisitCategoryId);
                    }
                    else
                    {
                        string m = await DataChanges.DataChangeResponse((DataChanges.DataContextItem)CurrentContext, (DataChanges.DataTrans)DataTransType, "Required fields", validate.ToString());
                        //if (success)
                        //{
                        DataTransType = DataTrans.None;
                        EnableEditControls(false);
                        //}
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void EnableEditControls(bool v)
        {
            if (v)
            {
                CancelRowButton.Visibility = Visibility.Visible;
                SaveRowButton.Visibility = Visibility.Visible;
            }
            else
            {
                CancelRowButton.Visibility = Visibility.Collapsed;
                SaveRowButton.Visibility = Visibility.Collapsed;

                DeleteRowButton.IsTapEnabled = false;
                DeleteRowButton.Foreground = paoDisabled;
            }
        }

        /// <summary>
        /// Set global flag if user is not a manager
        /// Server will invoke this message if user is
        /// not a manager.
        /// IsUserInRole default is true
        /// </summary>
        private async void UserNotInRole()
        {
            ((ShellPage)DataContext).IsUserInRole = false;
            await DataChanges.GenericDialog("User is not in role", $"{_vsisSettings.AuthFullName} requires Manager role.\nData in this app will not be available.");
        }

        /// <summary>
        /// Add agents
        /// </summary>
        /// <param name="agents"></param>
        private async void FillAgentsStatus(List<AgentProfile> agents)
        {
            try
            {
                if (agents != null)
                {
                    agentProfile.Clear();

                    // sort
                    agents = new List<AgentProfile>(agents.OrderByDescending(x => x.StatusName == "AVAILABLE").ThenBy(x => x.LastName));

                    foreach (var m in agents)
                    {
                        agentProfile.Add(new AgentProfile
                        {
                            AuthName = m.AuthName,
                            Categories = m.Categories,
                            CategoriesDescription = m.CategoriesDescription.Replace(",", "\n"),
                            Counter = m.Counter,
                            FullName = m.FullName,
                            Role = m.Role,
                            Active = m.Active,
                            Location = m.Location,
                            StatusName = m.StatusName,
                            VisitorName = m.VisitorName,
                            VisitorId = m.VisitorId,
                            DepartmentName = m.DepartmentName,
                            AgentStatusGlyph = m.StatusName.ToUpper() == "UNAVAILABLE" ? SEGOE_MDL2_ASSET_BLOCKCONTACT : SEGOE_MDL2_ASSET_CONTACTSOLID,
                            AgentStatusGlyphColor = m.StatusName.ToUpper() == "UNAVAILABLE" ? "Tomato" : "CadetBlue",
                            VisitorsToday = m.VisitorsToday,
                            VisitorsWtd = m.VisitorsWtd,
                            VisitorsMtd = m.VisitorsMtd,
                            VisitorsYtd = m.VisitorsYtd,
                            CallTimeToday = m.CallTimeToday,
                            CallTimeWtd = m.CallTimeWtd,
                            CallTimeMtd = m.CallTimeMtd,
                            CallTimeYtd = m.CallTimeYtd
                        });
                        //
                        CheckAgentsNotAvailableInCategory();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Update agent
        /// </summary>
        /// <param name="agent"></param>
        public async void UpdateAgentsStatusItem(AgentProfile agent)
        {
            try
            {
                if (agent != null)
                {
                    foreach (var m in agentProfile)
                    {
                        if (m.AuthName == agent.AuthName)
                        {
                            m.Categories = agent.Categories;
                            m.CategoriesDescription = agent.CategoriesDescription.Replace(",", "\n");
                            m.Counter = agent.Counter;
                            m.Role = agent.Role;
                            m.Active = agent.Active;
                            m.StatusName = agent.StatusName;
                            m.VisitorName = agent.VisitorName;
                            m.VisitorId = agent.VisitorId;
                            m.DepartmentName = agent.DepartmentName;
                            m.AgentStatusGlyph = agent.StatusName.ToUpper() == "UNAVAILABLE" ? SEGOE_MDL2_ASSET_BLOCKCONTACT : SEGOE_MDL2_ASSET_CONTACTSOLID;
                            m.AgentStatusGlyphColor = agent.StatusName.ToUpper() == "UNAVAILABLE" ? "Tomato" : "CadetBlue";
                            m.VisitorsToday = agent.VisitorsToday;
                            m.VisitorsWtd = agent.VisitorsWtd;
                            m.VisitorsMtd = agent.VisitorsMtd;
                            m.VisitorsYtd = agent.VisitorsYtd;
                            m.CallTimeToday = agent.CallTimeToday;
                            m.CallTimeWtd = agent.CallTimeWtd;
                            m.CallTimeMtd = agent.CallTimeMtd;
                            m.CallTimeYtd = agent.CallTimeYtd;
                        }

                        CheckAgentsNotAvailableInCategory();
                    }

                    // sort
                    agentProfile = new ObservableCollection<AgentProfile>(agentProfile.OrderByDescending(x => x.StatusName == "AVAILABLE")
                        .ThenBy(x => x.LastName));

                    AgentProfileItems = agentProfile;
                    AgentsDataGrid.ItemsSource = null;
                    AgentsDataGrid.ItemsSource = AgentProfileItems;

                    await Task.CompletedTask;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Add visitors
        /// </summary>
        /// <param name="visitors"></param>
        /// <returns></returns>
        public async void FillVisitorQueue(List<VisitorQueue> visitors)
        {
            try
            {
                if (visitors != null)
                {
                    visitorQueue.Clear();

                    foreach (var m in visitors)
                    {
                        await VisitorAdd(m);
                    }

                    VisitorsDataGrid.ItemsSource = null;
                    VisitorsDataGrid.ItemsSource = VisitorQueueItems;

                    var comboBoxColumn = VisitorsDataGrid.Columns.FirstOrDefault(x => x.Tag?.Equals("VisitCategory") == true) as DataGridComboBoxColumn;
                    if (comboBoxColumn != null)
                        if (CategoriesList.Count > 0)
                            comboBoxColumn.ItemsSource = CategoriesList;

                    CheckVisitorWaitTime();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Add one visitor to visitorQueue
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        private async Task VisitorAdd(VisitorQueue m)
        {
            try
            {
                // compute the WaitDuration
                DateTime current = DateTime.Now;
                TimeSpan elapsed = DateTime.Parse(current.ToString()).Subtract(DateTime.Parse(m.Created.ToString()));
                string wait_duration = "";

                // format to minutes without leading zeros
                if (m.CalledTime == null)
                    wait_duration = String.Format("{0}", elapsed.ToString(@"%m"));

                if (wait_duration == "0")
                    wait_duration = "";

                visitorQueue.Add(new VisitorQueue
                {
                    Id = m.Id,
                    CounterDescription = m.CounterDescription,
                    CalledTime = m.CalledTime,
                    CategoryDescription = m.CategoryDescription,
                    Created = m.Created,
                    MaxWaitTime = m.MaxWaitTime,
                    WaitDuration = wait_duration,
                    FirstName = m.FirstName,
                    LastName = m.LastName,
                    FullName = $"{m.FirstName} {m.LastName}",
                    IsHandicap = m.IsHandicap,
                    Kiosk = m.Kiosk,
                    Location = m.Location,
                    VisitCategoryId = m.VisitCategoryId,
                    //AgentIsAvailable = m.AgentIsAvailable,
                    VisitorHandicapGlyph = m.IsHandicap ? SEGOE_MDL2_ASSET_HEALTH : "",
                    VisitorHandicapGlyphColor = m.IsHandicap ? "LightSeaGreen" : "",
                    StatusName = textInfo.ToTitleCase(m.StatusName),
                    IsTransfer = m.IsTransfer,
                    VisitorTransferGlyph = m.IsTransfer ? SEGOE_MDL2_ASSET_WALKOPEN : "",
                    VisitorTransferGlyphColor = m.IsTransfer ? "Red" : ""
                });

                CheckAgentsNotAvailableInCategory();

                // add timer to wait duration
                timerx();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Update\Add\Remove visitor
        /// </summary>
        /// <param name="visitors"></param>
        /// <returns></returns>
        public async void VisitorQueueItem(VisitorQueue visitor)
        {
            try
            {
                if (visitor != null)
                {
                    if (visitor.StatusName != "CLOSED")
                    {
                        // Check if visitor exists
                        List<VisitorQueue> visitorItem = new ObservableCollection<VisitorQueue>(from item in visitorQueue
                                                                                                where item.Id == visitor.Id
                                                                                                select item).ToList();
                        if (visitorItem != null && visitorItem.Count == 0)
                            await VisitorAdd(visitor);

                        if (visitorItem != null && visitorItem.Count > 0)
                            await UpdateVisitor(visitor);
                    }

                    if (visitor.StatusName == "CLOSED")
                        visitorQueue.Remove(visitorQueue.Where(x => x.Id == visitor.Id).SingleOrDefault());

                    // sort
                    visitorQueue = new ObservableCollection<VisitorQueue>(visitorQueue.OrderByDescending(x => x.StatusName == "WAITING")
                        .ThenBy(x => x.Created));

                    VisitorQueueItems = visitorQueue;
                    VisitorsDataGrid.ItemsSource = null;
                    VisitorsDataGrid.ItemsSource = VisitorQueueItems;

                    CheckAgentsNotAvailableInCategory();

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Update visitorQueue
        /// </summary>
        /// <param name="visitor"></param>
        /// <returns></returns>
        private async Task UpdateVisitor(VisitorQueue visitor)
        {
            try
            {
                DateTime current = DateTime.Now;
                TimeSpan elapsed = DateTime.Parse(current.ToString()).Subtract(DateTime.Parse(visitor.Created.ToString()));
                string wait_duration = "";

                // format to minutes without leading zeros
                if (visitor.CalledTime == null)
                    wait_duration = String.Format("{0}", elapsed.ToString(@"%m"));

                if (wait_duration == "0")
                    wait_duration = "";

                VisitorQueue v = new ObservableCollection<VisitorQueue>(from item in visitorQueue
                                                                        where item.Id == visitor.Id
                                                                        select item).FirstOrDefault();
                if (v != null)
                {
                    v.Id = visitor.Id;
                    v.CounterDescription = visitor.CounterDescription;
                    v.CalledTime = visitor.CalledTime;
                    v.CategoryDescription = visitor.CategoryDescription;
                    v.Created = visitor.Created;
                    v.WaitDuration = wait_duration;
                    v.Kiosk = visitor.Kiosk;
                    v.StatusName = textInfo.ToTitleCase(visitor.StatusName);
                    v.IsTransfer = visitor.IsTransfer;
                    v.VisitorTransferGlyph = visitor.IsTransfer ? SEGOE_MDL2_ASSET_WALKOPEN : "";
                    v.VisitorTransferGlyphColor = visitor.IsTransfer ? "Red" : "";

                    // Refresh wait time notification
                    CheckVisitorWaitTime();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="visitor"></param>
        /// <returns></returns>
        private async Task UpdateWaitDurationExceeded(VisitorQueue visitor)
        {
            try
            {
                VisitorQueue v = new ObservableCollection<VisitorQueue>(from item in visitorQueue
                                                                        where item.Id == visitor.Id
                                                                        select item).FirstOrDefault();
                if (v != null)
                {
                    v.MaxWaitTime = visitor.MaxWaitTime;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Add data to CounterDataGrid
        /// </summary>
        /// <param name="counters"></param>
        private async void FillCountersStatus(List<CounterDetail> counters)
        {
            try
            {
                if (counters != null)
                {
                    counterDetail.Clear();

                    counters = new List<CounterDetail>(counters.OrderBy(x => x.CounterNumber));

                    foreach (var m in counters)
                    {
                        counterDetail.Add(new CounterDetail
                        {
                            Host = m.Host,
                            CounterNumber = m.CounterNumber,
                            Description = m.Description,
                            AgentFullName = m.AgentFullName,
                            VisitorId = m.VisitorId,
                            CounterStatusGlyph = m.CounterStatus.ToUpper() == "CLOSED" ? "" : SEGOE_MDL2_ASSET_COMPLETED,
                            //CounterStatusSymbolColor = m.CounterStatus.ToUpper() == "CLOSED" ? "Blue" : "Red",
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Fill the categories combo list
        /// DataGrid ComboBox Column requires
        /// the same field names so that the
        /// object is selected.
        /// </summary>
        /// <param name="m"></param>
        private void FillCategoriesList(List<Category> m)
        {
            try
            {
                List<Category> cat = new List<Category>();
                foreach (var item in m)
                {
                    cat.Add(new Category
                    {
                        Id = item.Id,
                        VisitCategoryId = item.Id,
                        Description = item.Description
                    });
                }
                CategoriesList = cat;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Updates entry in CountersDataGrid
        /// </summary>
        /// <param name="counter"></param>
        private async void UpdateCounterStatus(CounterDetail counter)
        {
            try
            {
                if (counter != null)
                {
                    CounterDetail c = new ObservableCollection<CounterDetail>(from item in counterDetail
                                                                              where item.Host == counter.Host
                                                                              select item).FirstOrDefault();

                    // First clear agent from any previous counter
                    foreach (var d in counterDetail)
                    {
                        if (d.AgentFullName == counter.AgentFullName)
                        {
                            c.AgentFullName = "";
                            c.VisitorId = 0;
                            c.CounterStatusGlyph = "";
                            break;
                        }
                    }

                    if (c != null)
                    {
                        c.Host = counter.Host;
                        c.CounterNumber = counter.CounterNumber;
                        c.Description = counter.Description;
                        c.AgentFullName = counter.AgentFullName;
                        c.VisitorId = counter.VisitorId;
                        c.CounterStatusGlyph = counter.CounterStatus.ToUpper() == "CLOSED" ? "" : SEGOE_MDL2_ASSET_COMPLETED;
                    }

                    CounterDetailItems = counterDetail;
                    CountersDataGrid.ItemsSource = null;
                    CountersDataGrid.ItemsSource = CounterDetailItems;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            await Task.CompletedTask;
        }

        private class Cats
        {
            public ulong Cat { get; set; }
            public string Desc { get; set; }
        }

        /// <summary>
        /// Toast notification content and display
        /// </summary>
        private void DoToastNotification()
        {
            try
            {
                if (_vsisSettings.ShowToastNotification)
                {
                    string title = "Manager Notifications";
                    string agent_notices = agentsNotAvailableNotifications.Count > 0 ? "One or more Agents are unavailable." : "";
                    string wait_time_notices = waitTimeNotifications.Count > 0 ? "One or more Visitors are waiting longer than usual." : "";
                    string content = $"{agent_notices}\n{wait_time_notices}";

                    if (maxDesktopNotifications == 1)
                    {
                        PopToast(title, content);
                        // disable further toast until max is reset
                        maxDesktopNotifications = 2;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Updates agent not available to service visitor notification
        /// </summary>
        private void CheckAgentsNotAvailableInCategory()
        {
            try
            {
                agentsNotAvailableNotifications.Clear();

                //agentsNotAvailableNotifications.Remove(
                //agentsNotAvailableNotifications.Where(x => x.MessageTag == v.VisitCategoryId.ToString()).SingleOrDefault());

                // Clone collections to prevent memory issue if changed
                List<VisitorQueue> vq = new List<VisitorQueue>(visitorQueue);
                List<AgentProfile> ap = new List<AgentProfile>(agentProfile.Where(x => x.VisitorId == 0 || x.VisitorId == null));
                // Categories covered by an Agent
                List<ulong> covered = new List<ulong>();

                // Create a category object with fields for diff
                List<Cats> vcats = (from p in vq
                                    select new Cats { Cat = p.VisitCategoryId, Desc = p.CategoryDescription }).ToList();

                if (agentProfile.Count > 0)
                {
                    // Check to add agent in category not availabe notification
                    foreach (var v in vq)
                    {
                        // Get all available agents
                        List<AgentProfile> agent = ap.Where(x => x.StatusName == "AVAILABLE").ToList();
                        foreach (var a in agent)
                        {
                            // Check if the agent is in the visitor category
                            ulong result = v.VisitCategoryId & a.Categories;
                            if (v.VisitCategoryId == result)
                            {
                                // Agent was found in visitor category
                                // add visitor category ex. 16, not agent category ex. 2046
                                covered.Add(v.VisitCategoryId);
                                break;
                            }
                        }
                    }
                }

                // Difference is not covered
                var diff = vcats.Where(x => !covered.Contains(x.Cat)).ToList();

                foreach (var item in diff)
                {
                    agentsNotAvailableNotifications.Add(new NotificationItem()
                    {
                        MessageType = "AgentNotAvailable",
                        GroupDescription = "No Agents are available",
                        MessageText = $"{item.Desc}",
                        MessageTag = item.ToString()
                    });
                }

                UpdateNotifications();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Updates wait time exceeded notification
        /// </summary>
        private void CheckVisitorWaitTime()
        {
            try
            {
                if (visitorQueue != null)
                {
                    waitTimeNotifications.Clear();

                    // Clone collections to prevent memory issue if changed
                    List<VisitorQueue> vq = new List<VisitorQueue>(visitorQueue.Where(x => x.StatusName == "WAITING"));

                    foreach (VisitorQueue v in vq)
                    {
                        DateTime current = DateTime.Now;
                        // Subtract current time from visitor created time to get elapsed time
                        TimeSpan elapsed = DateTime.Parse(current.ToString()).Subtract(DateTime.Parse(v.Created.ToString()));

                        if (v.CalledTime == null)
                            v.WaitDuration = String.Format("{0}", elapsed.ToString(@"%m"));

                        if (v.MaxWaitTime > 0)
                        {
                            // Show visitor in notifications block and send toast when max wait time reached
                            if (elapsed.Minutes >= v.MaxWaitTime)
                            {
                                string fullName = $"{v.FirstName} {v.LastName}";
                                string message = $"{fullName} signed in for {v.CategoryDescription}, {v.WaitDuration}+ minutes waiting.";

                                waitTimeNotifications.Add(new NotificationItem()
                                {
                                    MessageType = "WaitTimeExceeded",
                                    GroupDescription = "Wait Time Exceeded",
                                    MessageText = message,
                                    MessageTag = v.Id.ToString()
                                });
                            }
                        }
                    } // end foreach
                    if (waitTimeNotifications.Count > 0)
                        UpdateNotifications();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 60 seconds interval
        /// </summary>
        private void timerx()
        {
            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_Tick; ;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 60);
            dispatcherTimer.Start();
        }

        /// <summary>
        /// Updates the computed WaitDuration
        /// Format time to minutes without leading zeros
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DispatcherTimer_Tick(object sender, object e)
        {
            CheckVisitorWaitTime();
        }

        /// <summary>
        /// Add all notifications to NotifyDataGrid
        /// </summary>
        private void UpdateNotifications()
        {
            try
            {
                NotifyGroups.Clear();

                // Wait time notifications
                //
                var query = from item in waitTimeNotifications
                            group item by item.MessageType into g
                            select new { GroupName = g.Key, Items = g };

                foreach (var g in query)
                {
                    GroupInfoCollection<NotificationItem> info = new GroupInfoCollection<NotificationItem>();
                    info.Key = g.GroupName;
                    foreach (var item in g.Items)
                    {
                        info.Add(item);
                    }
                    NotifyGroups.Add(info);
                }

                // Agents not available notifications
                //
                List<NotificationItem> result = agentsNotAvailableNotifications.GroupBy(g => new { g.MessageType, g.MessageText })
                             .Select(g => g.First()).OrderBy(x => x.MessageText)
                             .ToList();

                query = from item in result
                        group item by item.MessageType into g
                        select new { GroupName = g.Key, Items = g };

                // build group for datagrid
                foreach (var g in query)
                {
                    GroupInfoCollection<NotificationItem> info = new GroupInfoCollection<NotificationItem>();
                    info.Key = g.GroupName;
                    foreach (var item in g.Items)
                    {
                        info.Add(item);
                    }
                    NotifyGroups.Add(info);
                }

                if (NotifyGroups.Count == 0)
                    maxDesktopNotifications = 1;

                groupedItems = new CollectionViewSource();
                groupedItems.IsSourceGrouped = true;
                groupedItems.Source = NotifyGroups;

                NotifyDataGrid.RowGroupHeaderPropertyNameAlternative = "";
                NotifyDataGrid.ItemsSource = null;
                NotifyDataGrid.ItemsSource = groupedItems.View;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Toast Builder
        /// </summary>
        /// <param name="title"></param>
        /// <param name="content"></param>
        private void PopToast(string title, string content)
        {
            try
            {
                // Generate the toast notification content and pop the toast
                new ToastContentBuilder()
                    .SetToastScenario(ToastScenario.Reminder)
                    .AddText(content)
                    .AddComboBox("snoozeTime", "5", ("1", "1 minute"),
                                                     ("5", "5 minutes"),
                                                     ("15", "15 minutes"))
                    .AddButton(new ToastButton()
                        .SetSnoozeActivation("snoozeTime"))
                    .AddButton(new ToastButton()
                        .SetDismissActivation())
                    .Show();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void NotifyDataGrid_LoadingRowGroup(object sender, DataGridRowGroupHeaderEventArgs e)
        {
            ICollectionViewGroup group = e.RowGroupHeader.CollectionViewGroup;
            NotificationItem item = group.GroupItems[0] as NotificationItem;
            e.RowGroupHeader.PropertyValue = item.GroupDescription;
        }

        private async void BeginExtendedExecution()
        {
            try
            {
                // The previous Extended Execution must be closed before a new one can be requested.
                // This code is redundant here because the sample doesn't allow a new extended
                // execution to begin until the previous one ends, but we leave it here for illustration.
                ClearExtendedExecution();

                var newSession = new ExtendedExecutionSession();
                newSession.Reason = ExtendedExecutionReason.Unspecified;
                newSession.Description = "Raising periodic toasts";
                newSession.Revoked += SessionRevoked;
                ExtendedExecutionResult result = await newSession.RequestExtensionAsync();

                switch (result)
                {
                    case ExtendedExecutionResult.Allowed:
                        //Current.NotifyUser("Extended execution allowed.", NotifyType.StatusMessage);
                        session = newSession;
                        break;

                    default:
                    case ExtendedExecutionResult.Denied:
                        //Current.NotifyUser("Extended execution denied.", NotifyType.ErrorMessage);
                        newSession.Dispose();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void ClearExtendedExecution()
        {
            if (session != null)
            {
                session.Revoked -= SessionRevoked;
                session.Dispose();
                session = null;
            }
        }

        private void EndExtendedExecution()
        {
            ClearExtendedExecution();
        }
                private async void SessionRevoked(object sender, ExtendedExecutionRevokedEventArgs args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                switch (args.Reason)
                {
                    case ExtendedExecutionRevokedReason.Resumed:
                        //Current.NotifyUser("Extended execution revoked due to returning to foreground.", NotifyType.StatusMessage);
                        break;

                    case ExtendedExecutionRevokedReason.SystemPolicy:
                        //Current.NotifyUser("Extended execution revoked due to system policy.", NotifyType.StatusMessage);
                        break;
                }

                EndExtendedExecution();
            });
        }

        /// <summary>
        /// UI initializing customization
        /// </summary>
        /// <returns></returns>
        private async Task LoadAppAll()
        {
            try
            {
                SetCurrentThemeStyle();

                _isAuthenticated = ((App)Application.Current).IsAuthenticated;
                EnableEditMode(_isAuthenticated);

                MainHeaderTitleTextBlock.Text = "VSIS Dashboard";
                MainHeaderSubTitleTextBlock.Text = "Visitor Queue, Agent Status, Counter Status, Notifications";

                // Entry point into vsis configuration
                await LoadSettings();
                await OpenConnection();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
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
                // Retrieve configuration for VSIS
                _vsisSettings = await GetLocalStorageSettings();

                if (_vsisSettings != null)
                {
                    if (_vsisSettings.Host == null)
                    {
                        this.Frame.Navigate(typeof(SettingsPage));
                    }

                    (string accountName, string fullName) = await GetWindowsAccount();

                    if (accountName.Length > 0)
                    {
                        _vsisSettings.AuthName = accountName;
                        _vsisSettings.AuthFullName = fullName;
                    }

                    ((App)Application.Current).VsisSettings = _vsisSettings;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Save user preferences
        /// </summary>
        private void SaveViewFactor()
        {
            try
            {
                Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                localSettings.Values["ZoomFactor"] = scrollvwr.ZoomFactor;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Get User local storage settings
        /// </summary>
        /// <returns>VsisConfiguration</returns>
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
                Object locationValue = localSettings.Values["Location"];
                if (locationValue != null && locationValue.ToString().Length > 0)
                {
                    c.Location = Convert.ToSByte(locationValue);
                }
                Object zoomFactorValue = localSettings.Values["ZoomFactor"];
                if (zoomFactorValue != null && zoomFactorValue.ToString().Length > 0)
                {
                    scrollvwr.ChangeView(0, 0, float.Parse(zoomFactorValue.ToString()));
                }
                Object EnableDesktopNotificationsValue = localSettings.Values["EnableDesktopNotifications"];
                if (EnableDesktopNotificationsValue != null)
                {
                    // exceed max to disable desktop notifications
                    c.ShowToastNotification = (bool)EnableDesktopNotificationsValue;

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return Task.FromResult(c); // return VsisConfiguration
        }

        /// <summary>
        /// Get Windows Account
        /// User Account Information must be enabled
        /// in package capabilities
        /// </summary>
        /// <returns></returns>
        private async Task<Tuple<string, string>> GetWindowsAccount()
        {
            try
            {
                IReadOnlyList<Windows.System.User> users = await Windows.System.User.FindAllAsync();

                var current = users.Where(p => p.AuthenticationStatus == UserAuthenticationStatus.LocallyAuthenticated &&
                                p.Type == UserType.LocalUser).FirstOrDefault();

                // other available properties

                //  var authenticationStatus = current.AuthenticationStatus;
                //  var nonRoamableId = current.NonRoamableId;
                //  var provider = await current.GetPropertyAsync(KnownUserProperties.ProviderName);
                //  var accountName = await current.GetPropertyAsync(KnownUserProperties.AccountName);
                //  var displayName = await current.GetPropertyAsync(KnownUserProperties.DisplayName);
                //  //var domainName = await current.GetPropertyAsync(KnownUserProperties.DomainName);
                //  var principalName = await current.GetPropertyAsync(KnownUserProperties.PrincipalName);
                //  var firstName = await current.GetPropertyAsync(KnownUserProperties.FirstName);
                //  var guestHost = await current.GetPropertyAsync(KnownUserProperties.GuestHost);
                //  var lastName = await current.GetPropertyAsync(KnownUserProperties.LastName);
                //  var sessionInitiationProtocolUri = await current.GetPropertyAsync(KnownUserProperties.SessionInitiationProtocolUri);
                //  var userType = current.Type;

                var domainName = await current.GetPropertyAsync(KnownUserProperties.DomainName);
                // example "MANATEEPAO.COM\\gbologna"

                string[] subs = domainName.ToString().Split('\\');

                if (subs != null && subs.Length > 1)
                {
                    var accountName = subs[1];
                    var fullName = await current.GetPropertyAsync(KnownUserProperties.DisplayName);

                    return Tuple.Create(accountName.ToString(), fullName.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return Tuple.Create("", "");
        }

        /// <summary>
        /// Open connection to SignalR vsisHub
        /// </summary>
        /// <returns></returns>
        private async Task OpenConnection()
        {
            try
            {
                // make sure global VsisConnection has a connection
                if (((App)Application.Current).VsisConnection != null &&
                    ((App)Application.Current).VsisConnection.State.ToString().ToUpper() == "CONNECTED")
                    return;

                if (connection != ((App)Application.Current).VsisConnection)
                    connection = null;


                if (_vsisSettings.Host != null)
                {
                    string conn = "";

                    ((App)Application.Current).VsisConnection = new HubConnectionBuilder()
                        .WithUrl(_vsisSettings.Host, options =>
                        {
                            options.UseDefaultCredentials = true;
                        })
                        .ConfigureLogging(logging =>
                        {
                            logging.SetMinimumLevel(LogLevel.Trace);
                        })
                        .WithAutomaticReconnect()
                        .Build();

                    connection = ((App)Application.Current).VsisConnection;

                    connection.HandshakeTimeout = TimeSpan.FromSeconds(90);
                    connection.Reconnecting += (error) =>
                    {
                        conn = connection.State.ToString();
                        if (connection == null)
                        {
                            conn = "Reconnecting";
                        }
                        else
                        {
                            conn = connection.State.ToString();
                        }

                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FormatActivityIndicator(Visibility.Visible, Visibility.Visible, true, conn));

                        return Task.CompletedTask;
                    };
                    connection.Reconnected += (connectionId) =>
                    {
                        SendJoinGroup();

                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FormatActivityIndicator(Visibility.Collapsed, Visibility.Visible, false, conn));

                        return Task.CompletedTask;
                    };

                    /*
                     * If the client doesn't successfully reconnect within its first four attempts, 
                     * the HubConnection will transition to the Disconnected state and fire the Closed event. 
                     * This provides an opportunity to attempt to restart the connection manually or inform 
                     * users the connection has been permanently lost.
                     * */
                    connection.Closed += (error) =>
                    {
                        connection.StopAsync();

                        if (connection.State != HubConnectionState.Connecting)
                            connection.DisposeAsync();

                        try
                        {
                            if (connection != null)
                            {
                                conn = connection.State.ToString();

                                connection.StartAsync();
                            }
                        }
                        catch (NullReferenceException)
                        {
                        }
                        catch (Exception ex)
                        {
                            // handle all exceptions
                            Console.WriteLine(ex.Message);
                        }

                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FormatActivityIndicator(Visibility.Visible, Visibility.Visible, true, conn));

                        return Task.CompletedTask;
                    };

                    try
                    {
                        // do all db calls after successful connection to server

                        await connection.StartAsync();
                        conn = connection.State.ToString();

                        if (connection == null)
                        {
                            conn = "Dead client";
                            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FormatActivityIndicator(Visibility.Visible, Visibility.Visible, true, conn));
                        }
                        else
                        {
                            // hide indicator when we are connected
                            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FormatActivityIndicator(Visibility.Collapsed, Visibility.Collapsed, false, ""));

                            // Server will respond with queue
                            SendJoinGroup();
                        }
                    }
                    catch (Exception)
                    {
                        // allow error to fall through

                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FormatActivityIndicator(Visibility.Visible, Visibility.Visible, true, "Disconnected"));

                        // attempt reconnect
                        await OpenConnection();
                    }

                    HubInvoked();
                }
            }
            catch (Exception)
            {
                string _state = "";
                if (connection == null)
                {
                    _state = "No Connection";
                }
                else
                {
                    _state = connection.State.ToString();
                }
            }
        } // end OpenConnection

        /// <summary>
        /// Join SignaR vsisHub
        /// </summary>
        private async void SendJoinGroup()
        {
            try
            {
                if (connection != null)
                {
                    // invoke procedures for SignalR server messages
                    await connection.InvokeAsync("JoinGroup", _vsisSettings.AuthName, 1, "Manager", _vsisSettings.AuthName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Update activity indicator control
        /// </summary>
        /// <param name="indicatorVisibility"></param>
        /// <param name="indicatorTextVisibility"></param>
        /// <param name="indicatorIsActive"></param>
        /// <param name="indicatorText"></param>
        private void FormatActivityIndicator(Visibility indicatorVisibility, Visibility indicatorTextVisibility, bool indicatorIsActive, string indicatorText)
        {
            try
            {
                // If called from the UI thread, then update immediately.
                // Otherwise, schedule a task on the UI thread to perform the update.
                if (Dispatcher.HasThreadAccess)
                {
                    if (indicatorText != null)
                    {
                        if (!indicatorIsActive)
                            indicatorText = "";

                        Brush brush = GetConnectionStateBrushColor(indicatorText);
                        ActivityIndicator.Foreground = brush;

                        ActivityIndicator.IsActive = indicatorIsActive;
                        ActivityIndicator.Visibility = indicatorVisibility;

                        ActivityIndicatorText.Text = indicatorText;
                        ActivityIndicatorText.Foreground = brush;
                        ActivityIndicatorText.Visibility = indicatorTextVisibility;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                // do not report
                // {"The application called an interface that was marshalled for a different thread.
                // (Exception from HRESULT: 0x8001010E (RPC_E_WRONG_THREAD))"}
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
                    VisitorsDataGrid.AlternatingRowBackground = altRowBg;
                    VisitorsDataGrid.BorderBrush = border;

                    AgentsDataGrid.AlternatingRowBackground = altRowBg;
                    AgentsDataGrid.BorderBrush = border;

                    CountersDataGrid.AlternatingRowBackground = altRowBg;
                    CountersDataGrid.BorderBrush = border;

                    NotifyDataGrid.AlternatingRowBackground = altRowBg;
                    NotifyDataGrid.BorderBrush = border;
                }

                if (altRowFg != null)
                {
                    VisitorsDataGrid.AlternatingRowForeground = altRowFg;
                    AgentsDataGrid.AlternatingRowForeground = altRowFg;
                    NotifyDataGrid.AlternatingRowForeground = altRowFg;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Formats connection UI control color
        /// </summary>
        /// <param name="connState"></param>
        /// <returns></returns>
        private Brush GetConnectionStateBrushColor(string connState)
        {
            Brush brush = paoBlack;
            try
            {
                switch (connState.ToUpper())
                {
                    case "DISCONNECTED":
                    case "DEAD CLIENT":
                    case "REFRESH STARTED":
                        brush = paoRed;
                        break;
                    case "CONNECTED":
                        brush = paoGreenGrass;
                        break;
                    case "RECONNECTING":
                        brush = paoSteelBlue;
                        break;
                    default:
                        brush = paoBlue;
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return brush;
        }

        #region property_handler
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

        #endregion property_handler

        #region Grid_sorting_events

        /// <summary>
        /// DataGrid sorting
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VisitorsDataGrid_Sorting(object sender, Microsoft.Toolkit.Uwp.UI.Controls.DataGridColumnEventArgs e)
        {
            try
            {
                if (e.Column.Tag != null)
                {
                    //Implement sort on the column
                    if (e.Column.SortDirection == null || e.Column.SortDirection == DataGridSortDirection.Descending)
                    {
                        switch (e.Column.Tag)
                        {
                            case "Id":
                                VisitorsDataGrid.ItemsSource = new ObservableCollection<VisitorQueue>(from item in visitorQueue
                                                                                                      orderby item.Id ascending
                                                                                                      select item);
                                break;
                            case "VisitorName":
                                VisitorsDataGrid.ItemsSource = new ObservableCollection<VisitorQueue>(from item in visitorQueue
                                                                                                      orderby item.FullName ascending
                                                                                                      select item);
                                break;
                            case "Transfer":
                                VisitorsDataGrid.ItemsSource = new ObservableCollection<VisitorQueue>(from item in visitorQueue
                                                                                                      orderby item.IsTransfer ascending
                                                                                                      select item);
                                break;
                            case "Handicap":
                                VisitorsDataGrid.ItemsSource = new ObservableCollection<VisitorQueue>(from item in visitorQueue
                                                                                                      orderby item.IsHandicap ascending
                                                                                                      select item);
                                break;
                            case "Reason":
                                VisitorsDataGrid.ItemsSource = new ObservableCollection<VisitorQueue>(from item in visitorQueue
                                                                                                      orderby item.CategoryDescription ascending
                                                                                                      select item);
                                break;
                            case "Status":
                                VisitorsDataGrid.ItemsSource = new ObservableCollection<VisitorQueue>(from item in visitorQueue
                                                                                                      orderby item.StatusName ascending
                                                                                                      select item);
                                break;
                            case "AssignedCounter":
                                VisitorsDataGrid.ItemsSource = new ObservableCollection<VisitorQueue>(from item in visitorQueue
                                                                                                      orderby item.AssignedCounter ascending
                                                                                                      select item);
                                break;
                            case "Created":
                                VisitorsDataGrid.ItemsSource = new ObservableCollection<VisitorQueue>(from item in visitorQueue
                                                                                                      orderby item.Created ascending
                                                                                                      select item);
                                break;
                            case "CalledTime":
                                VisitorsDataGrid.ItemsSource = new ObservableCollection<VisitorQueue>(from item in visitorQueue
                                                                                                      orderby item.CalledTime ascending
                                                                                                      select item);
                                break;
                            case "Waiting":
                                VisitorsDataGrid.ItemsSource = new ObservableCollection<VisitorQueue>(from item in visitorQueue
                                                                                                      orderby item.WaitDuration ascending
                                                                                                      select item);
                                break;
                        }
                        e.Column.SortDirection = DataGridSortDirection.Ascending;
                    }
                    else
                    {

                        switch (e.Column.Tag)
                        {

                            case "Id":
                                VisitorsDataGrid.ItemsSource = new ObservableCollection<VisitorQueue>(from item in visitorQueue
                                                                                                      orderby item.Id descending
                                                                                                      select item);
                                break;
                            case "VisitorName":
                                VisitorsDataGrid.ItemsSource = new ObservableCollection<VisitorQueue>(from item in visitorQueue
                                                                                                      orderby item.FullName descending
                                                                                                      select item);
                                break;
                            case "Transfer":
                                VisitorsDataGrid.ItemsSource = new ObservableCollection<VisitorQueue>(from item in visitorQueue
                                                                                                      orderby item.IsTransfer descending
                                                                                                      select item);
                                break;
                            case "Handicap":
                                VisitorsDataGrid.ItemsSource = new ObservableCollection<VisitorQueue>(from item in visitorQueue
                                                                                                      orderby item.IsHandicap descending
                                                                                                      select item);
                                break;
                            case "Reason":
                                VisitorsDataGrid.ItemsSource = new ObservableCollection<VisitorQueue>(from item in visitorQueue
                                                                                                      orderby item.CategoryDescription descending
                                                                                                      select item);
                                break;
                            case "Status":
                                VisitorsDataGrid.ItemsSource = new ObservableCollection<VisitorQueue>(from item in visitorQueue
                                                                                                      orderby item.StatusName descending
                                                                                                      select item);
                                break;
                            case "AssignedCounter":
                                VisitorsDataGrid.ItemsSource = new ObservableCollection<VisitorQueue>(from item in visitorQueue
                                                                                                      orderby item.AssignedCounter descending
                                                                                                      select item);
                                break;
                            case "Created":
                                VisitorsDataGrid.ItemsSource = new ObservableCollection<VisitorQueue>(from item in visitorQueue
                                                                                                      orderby item.Created descending
                                                                                                      select item);
                                break;
                            case "CalledTime":
                                VisitorsDataGrid.ItemsSource = new ObservableCollection<VisitorQueue>(from item in visitorQueue
                                                                                                      orderby item.CalledTime descending
                                                                                                      select item);
                                break;
                            case "Waiting":
                                VisitorsDataGrid.ItemsSource = new ObservableCollection<VisitorQueue>(from item in visitorQueue
                                                                                                      orderby item.WaitDuration descending
                                                                                                      select item);
                                break;
                        }
                        e.Column.SortDirection = DataGridSortDirection.Descending;
                    }

                    // add code to handle sorting by other columns as required

                    // Remove sorting indicators from other columns
                    foreach (var dgColumn in VisitorsDataGrid.Columns)
                    {
                        if (dgColumn.Tag != null && dgColumn.Tag.ToString() != e.Column.Tag.ToString())
                        {
                            dgColumn.SortDirection = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private void AgentsGrid_Sorting(object sender, DataGridColumnEventArgs e)
        {
            try
            {
                if (e.Column.Tag != null)
                {
                    //Implement sort on the column
                    if (e.Column.SortDirection == null || e.Column.SortDirection == DataGridSortDirection.Descending)
                    {
                        switch (e.Column.Tag)
                        {
                            case "FullName":
                                AgentsDataGrid.ItemsSource = new ObservableCollection<AgentProfile>(from item in agentProfile
                                                                                                    orderby item.FullName ascending
                                                                                                    select item);
                                break;
                            case "DepartmentName":
                                AgentsDataGrid.ItemsSource = new ObservableCollection<AgentProfile>(from item in agentProfile
                                                                                                    orderby item.DepartmentName ascending
                                                                                                    select item);
                                break;
                            case "Role":
                                AgentsDataGrid.ItemsSource = new ObservableCollection<AgentProfile>(from item in agentProfile
                                                                                                    orderby item.Role ascending
                                                                                                    select item);
                                break;

                            case "Categories":
                                AgentsDataGrid.ItemsSource = new ObservableCollection<AgentProfile>(from item in agentProfile
                                                                                                    orderby item.Categories ascending
                                                                                                    select item);
                                break;
                            case "Counter":
                                AgentsDataGrid.ItemsSource = new ObservableCollection<AgentProfile>(from item in agentProfile
                                                                                                    orderby item.Counter ascending
                                                                                                    select item);
                                break;
                            case "VisitorId":
                                AgentsDataGrid.ItemsSource = new ObservableCollection<AgentProfile>(from item in agentProfile
                                                                                                    orderby item.VisitorId ascending
                                                                                                    select item);
                                break;
                            case "AgentStatusGlyph":
                                AgentsDataGrid.ItemsSource = new ObservableCollection<AgentProfile>(from item in agentProfile
                                                                                                    orderby item.StatusName ascending
                                                                                                    select item);
                                break;
                            case "VisitorName":
                                AgentsDataGrid.ItemsSource = new ObservableCollection<AgentProfile>(from item in agentProfile
                                                                                                    orderby item.VisitorName ascending
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
                                AgentsDataGrid.ItemsSource = new ObservableCollection<AgentProfile>(from item in agentProfile
                                                                                                    orderby item.FullName descending
                                                                                                    select item);
                                break;
                            case "DepartmentName":
                                AgentsDataGrid.ItemsSource = new ObservableCollection<AgentProfile>(from item in agentProfile
                                                                                                    orderby item.DepartmentName descending
                                                                                                    select item);
                                break;
                            case "Role":
                                AgentsDataGrid.ItemsSource = new ObservableCollection<AgentProfile>(from item in agentProfile
                                                                                                    orderby item.Role descending
                                                                                                    select item);
                                break;
                            case "Categories":
                                AgentsDataGrid.ItemsSource = new ObservableCollection<AgentProfile>(from item in agentProfile
                                                                                                    orderby item.Categories descending
                                                                                                    select item);
                                break;
                            case "Counter":
                                AgentsDataGrid.ItemsSource = new ObservableCollection<AgentProfile>(from item in agentProfile
                                                                                                    orderby item.Counter descending
                                                                                                    select item);
                                break;
                            case "VisitorId":
                                AgentsDataGrid.ItemsSource = new ObservableCollection<AgentProfile>(from item in agentProfile
                                                                                                    orderby item.VisitorId descending
                                                                                                    select item);
                                break;
                            case "AgentStatusGlyph":
                                AgentsDataGrid.ItemsSource = new ObservableCollection<AgentProfile>(from item in agentProfile
                                                                                                    orderby item.StatusName descending
                                                                                                    select item);
                                break;
                            case "VisitorName":
                                AgentsDataGrid.ItemsSource = new ObservableCollection<AgentProfile>(from item in agentProfile
                                                                                                    orderby item.VisitorName descending
                                                                                                    select item);
                                break;
                        }
                        e.Column.SortDirection = DataGridSortDirection.Descending;
                    }

                    // add code to handle sorting by other columns as required

                    // Remove sorting indicators from other columns
                    foreach (var dgColumn in VisitorsDataGrid.Columns)
                    {
                        if (dgColumn.Tag != null && dgColumn.Tag.ToString() != e.Column.Tag.ToString())
                        {
                            dgColumn.SortDirection = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private void CountersDataGrid_Sorting(object sender, DataGridColumnEventArgs e)
        {
            try
            {
                if (e.Column.Tag != null)
                {
                    //Implement sort on the column
                    if (e.Column.SortDirection == null || e.Column.SortDirection == DataGridSortDirection.Descending)
                    {
                        switch (e.Column.Tag)
                        {
                            case "Host":
                                CountersDataGrid.ItemsSource = new ObservableCollection<CounterDetail>(from item in counterDetail
                                                                                                       orderby item.Host ascending
                                                                                                       select item);
                                break;
                            case "Description":
                                CountersDataGrid.ItemsSource = new ObservableCollection<CounterDetail>(from item in counterDetail
                                                                                                       orderby item.Description ascending
                                                                                                       select item);
                                break;

                            case "AgentFullName":
                                CountersDataGrid.ItemsSource = new ObservableCollection<CounterDetail>(from item in counterDetail
                                                                                                       orderby item.AgentFullName ascending
                                                                                                       select item);
                                break;
                            case "VisitorId":
                                CountersDataGrid.ItemsSource = new ObservableCollection<CounterDetail>(from item in counterDetail
                                                                                                       orderby item.VisitorId ascending
                                                                                                       select item);
                                break;
                            case "CounterStatus":
                                CountersDataGrid.ItemsSource = new ObservableCollection<CounterDetail>(from item in counterDetail
                                                                                                       orderby item.CounterStatus ascending
                                                                                                       select item);
                                break;
                        }
                        e.Column.SortDirection = DataGridSortDirection.Ascending;
                    }
                    else
                    {

                        switch (e.Column.Tag)
                        {

                            case "Host":
                                CountersDataGrid.ItemsSource = new ObservableCollection<CounterDetail>(from item in counterDetail
                                                                                                       orderby item.Host descending
                                                                                                       select item);
                                break;
                            case "Description":
                                CountersDataGrid.ItemsSource = new ObservableCollection<CounterDetail>(from item in counterDetail
                                                                                                       orderby item.Description descending
                                                                                                       select item);
                                break;

                            case "AgentFullName":
                                CountersDataGrid.ItemsSource = new ObservableCollection<CounterDetail>(from item in counterDetail
                                                                                                       orderby item.AgentFullName descending
                                                                                                       select item);
                                break;
                            case "VisitorId":
                                CountersDataGrid.ItemsSource = new ObservableCollection<CounterDetail>(from item in counterDetail
                                                                                                       orderby item.VisitorId descending
                                                                                                       select item);
                                break;
                            case "CounterStatus":
                                CountersDataGrid.ItemsSource = new ObservableCollection<CounterDetail>(from item in counterDetail
                                                                                                       orderby item.CounterStatus descending
                                                                                                       select item);
                                break;
                        }
                        e.Column.SortDirection = DataGridSortDirection.Descending;
                    }
                    // add code to handle sorting by other columns as required

                    // Remove sorting indicators from other columns
                    foreach (var dgColumn in VisitorsDataGrid.Columns)
                    {
                        if (dgColumn.Tag != null && dgColumn.Tag.ToString() != e.Column.Tag.ToString())
                        {
                            dgColumn.SortDirection = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        #endregion Grid_sorting_events

        /// <summary>
        /// Page scroll viewer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void scrollvwr_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            SaveViewFactor();
        }

        public DataTrans DataTransType
        {
            get
            {
                return _dataTransType;
            }
            set
            {
                this.Set<DataTrans>(ref _dataTransType, value);
            }
        }

        private string _glyph = "\xE72E";
        private Brush _glyphForeground = paoSteelBlue;
        private string _editModeTextBlock = "";

        public string Glyph
        {
            get
            {
                return _glyph;
            }
            set { this.Set<string>(ref _glyph, value); }
        }

        public Brush GlyphForeground
        {
            get
            {
                return _glyphForeground;
            }
            set
            {
                this.Set<Brush>(ref _glyphForeground, value);
            }
        }
        public string EditModeTextBlock
        {
            get
            {
                return _editModeTextBlock;
            }
            set { this.Set<string>(ref _editModeTextBlock, value); }
        }

        private void CancelRowButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            DoCancel();
        }

        private async void DoCancel()
        {
            DataTransType = DataTrans.None;
            EnableEditControls(false);
            await SendGetVisitorsMgr();
        }

        /// <summary>
        /// Delete operations
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void DeleteRowButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            // Visitor queue. You can should add support for other datagrid delete operations.
            try
            {
                if (VisitorsDataGrid.SelectedIndex > -1)
                {
                    VisitorQueue visitor = visitorQueue[VisitorsDataGrid.SelectedIndex];
                    bool tf = await DataChanges.ConfirmDialog($"Delete Visitor?", $"Are you sure you want to delete {visitor.FullName} from VSIS?");
                    if (tf)
                    {
                        DataTransType = DataTrans.Delete;
                        await SendPurgeVisitorInQueue(visitor.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void DataGridReadWrite()
        {
            VisitorsDataGrid.IsReadOnly = !_isAuthenticated;
        }

        private void EnableEditMode(bool tf)
        {
            try
            {
                if (tf)
                {
                    ((App)Application.Current).IsAuthenticated = true;
                    _isAuthenticated = true;
                    Glyph = "\xE785";

                    ReadOnlyLockIcon.Foreground = paoGreen;
                    ReadOnlyLockIcon.Visibility = Visibility.Visible;

                    DeleteRowButton.Visibility = Visibility.Visible;
                    DeleteRowButton.Foreground = paoDisabled;

                    EditModeTextBlock = "[EDIT MODE]";

                    CreateDataGridEvents();
                }
                else
                {
                    ((App)Application.Current).IsAuthenticated = false;
                    _isAuthenticated = false;
                    Glyph = "\xE72E";

                    ReadOnlyLockIcon.Foreground = paoSteelBlue;
                    DeleteRowButton.Visibility = Visibility.Collapsed;
                    EditModeTextBlock = "";

                    RemoveDataGridEvents();

                    ToolTip tip = new ToolTip();
                    tip.Content = $"Select a row to delete";
                    ToolTipService.SetToolTip(DeleteRowButton, tip);
                }
                DataGridReadWrite();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void CreateDataGridEvents()
        {
            DeleteRowButton.Tapped += DeleteRowButton_Tapped;
            VisitorsDataGrid.BeginningEdit += DataGrid_BeginningEdit;
            VisitorsDataGrid.SelectionChanged += DataGrid_SelectionChanged;
        }

        private void RemoveDataGridEvents()
        {
            DeleteRowButton.Tapped -= DeleteRowButton_Tapped;
            VisitorsDataGrid.BeginningEdit -= DataGrid_BeginningEdit;
            VisitorsDataGrid.SelectionChanged -= DataGrid_SelectionChanged;
        }

        //private void DataGrid_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        //{
        //    if (CancelRowButton.Visibility == Visibility.Visible)
        //        if (e.Key == VirtualKey.Escape)
        //        {
        //            e.Handled = true;
        //            DoCancel();
        //        }
        //}

        //[Conditional("DEBUG")]
        private async void AuthenticateUser()
        {
            try
            {
                if (!_isAuthenticated)
                {
                    AuthenticateTab auth = new AuthenticateTab();
                    bool tf = await auth.Authenticate();
                    if (tf)
                    {
                        ((App)Application.Current).IsAuthenticated = true;
                        _isAuthenticated = true;
                        Glyph = "\xE785";

                        ReadOnlyLockIcon.Foreground = paoGreen;
                        EditModeTextBlock = "[EDIT MODE]";
                        EnableEditMode(true);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        /// <summary>
        /// Toggle authentication state
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReadOnlyLockIcon_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            try
            {
                // leave edit mode
                if (_isAuthenticated)
                {
                    DoCancel();
                    EnableEditMode(false);
                }
                else
                {
                    AuthenticateUser();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// BeginningEdit for all datagrids
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            try
            {
                if (DataTransType == DataTrans.None)
                {
                    string row = "";
                    bool edit_row = false;

                    VisitorQueue visitor = visitorQueue[VisitorsDataGrid.SelectedIndex];
                    if (visitor.Id > 0)
                    {
                        edit_row = true;
                        row = $"{visitor.Id}";
                    }

                    if (edit_row)
                    {
                        DataTransType = DataTrans.Update;

                        EnableEditControls(true);

                        ToolTip tip = new ToolTip();
                        tip.Content = $"Save row {row}";
                        ToolTipService.SetToolTip(SaveRowButton, tip);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_isAuthenticated)
                {
                    if (DataTransType != DataTrans.Update &&
                        DataTransType != DataTrans.Delete)
                    {
                        DataTransType = DataTrans.None;

                        string row = "";

                        if (VisitorsDataGrid.SelectedIndex > -1)
                        {
                            VisitorQueue visitor = visitorQueue[VisitorsDataGrid.SelectedIndex];
                            if (visitor != null)
                                row = visitor.FullName;
                        }

                        var grid = sender as DataGrid;
                        if (grid == null || grid.SelectedItem == null)
                            return;

                        EnableEditControls(false);

                        // enable delete
                        DeleteRowButton.IsTapEnabled = true;
                        DeleteRowButton.Foreground = paoEasyBlue;

                        ToolTip tip = new ToolTip();
                        tip.Content = $"Delete {row}";
                        ToolTipService.SetToolTip(DeleteRowButton, tip);
                    }

                    if (DataTransType == DataTrans.Update)
                    {
                        SaveRowButton.Visibility = Visibility.Visible;
                        CancelRowButton.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Save the visitor changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SaveRowButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            try
            {
                if (VisitorsDataGrid.SelectedItems.Count == 1)
                {
                    switch (DataTransType)
                    {
                        case DataTrans.Update:
                            VisitorQueue visitor = visitorQueue[VisitorsDataGrid.SelectedIndex];
                            if (visitor != null)
                            {
                                bool tf = VisitorQueueSelectedEditIndex == VisitorsDataGrid.SelectedIndex;
                                if (!tf)
                                {
                                    tf = await DataChanges.ConfirmDialog($"It appears that your selected another visitor.", $"Are your sure you want to update {visitor.FullName}?");
                                }
                                if (tf)
                                    await SaveUpdateVisitorCategory();
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async void DataGrid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            try
            {
                int index = e.Row.GetIndex();
                VisitorQueueSelectedEditIndex = index;

                if (e.Column.Tag != null)
                {
                    switch (e.Column.Tag)
                    {
                        case "VisitCategory":

                            CurrentContext = DataContextItem.VisitorsQueue;

                            if (index == VisitorsDataGrid.SelectedIndex)
                            {
                                //Style style = new Style();
                                //style.TargetType = typeof(DataGridCell);
                                //style.Setters.Add(new Setter(DataGridCell.BackgroundProperty, new SolidColorBrush(Windows.UI.Colors.Red)));
                                //e.Column.CellStyle = style;

                                //ComboBox d = e.Column.GetCellContent(e.Row) as ComboBox;
                                //if (d != null)
                                //    d.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);

                                VisitorQueue visitor = visitorQueue[VisitorsDataGrid.SelectedIndex];
                                if (visitor != null)
                                {
                                    if (visitor.StatusName != "WAITING")
                                    {
                                        await DataChanges.GenericDialog($"Reason Change Not Allowed", $"{visitor.FullName} status is {visitor.StatusName}." +
                                            $" A visit reason can only be changed when the visitor status is 'WAITING'.");

                                        (sender as DataGrid).CancelEdit(DataGridEditingUnit.Cell);

                                        DoCancel();
                                    }
                                    else
                                    {
                                        Old_VisitorEditCategory = visitor.CategoryDescription;
                                    }
                                }
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

    public class GroupInfoCollection<T> : ObservableCollection<T>
    {
        public object Key { get; set; }

        public new IEnumerator<T> GetEnumerator()
        {
            return (IEnumerator<T>)base.GetEnumerator();
        }
    }

}
