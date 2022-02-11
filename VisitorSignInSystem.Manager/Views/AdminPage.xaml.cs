using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Globalization;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Microsoft.AspNetCore.SignalR.Client;
using Windows.UI.Core;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Windows.UI.Xaml.Media;
using VisitorSignInSystem.Manager.Core.Models;
using VisitorSignInSystem.Manager.Services;
using VisitorSignInSystem.Manager.Helpers;
using Windows.UI.Xaml.Media.Imaging;

// TODO: add connection indicator

namespace VisitorSignInSystem.Manager.Views
{
    public sealed partial class AdminPage : Page, INotifyPropertyChanged
    {
        // Enable locale if needed
        private CultureInfo enUS = new CultureInfo("en-US");

        // Creates a TextInfo based on the "en-US" culture.
        TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

        // Application configuration settings local object
        private static VsisConfiguration VsisSettings;

        // SignalR hub for this page
        private HubConnection connection { get; set; }

        // Local, Applies to datagrid readonly
        public bool _isAuthenticated { get; set; }

        // Custom colors
        static Brush paoEasyBlue = (Brush)App.Current.Resources["PaoSolidColorBrushEasyBlue"];
        static Brush paoSteelBlue = (Brush)App.Current.Resources["PaoSolidColorBrushSteelBlue"];
        static Brush paoDisabled = (Brush)App.Current.Resources["PaoSolidColorBrushDisabled"];
        static Brush paoGreen = (Brush)App.Current.Resources["PaoSolidColorBrushGreen"];

        // *******************************************************************************************

        #region ValidationFlags

        private string _HeaderTitleTextBlock = "";
        private string _HeaderSubTitleTextBlock = "";
        private string _TitleTextBlock = "";
        private string _SubTitleTextBlock = "";
        private double _SubTitleTextBlockMaxWidth;
        private string _SubTextBlock = "";
        private string _glyph = "\xE72E";
        private Brush _glyphForeground = paoSteelBlue;
        private string _editModeTextBlock = "";

        //private string _dataChangeResultGlyph = "\xE72E";
        //private Brush _dataChangeResultGlyphForeground = paoSteelBlue;
        //private string _dataChangeResultTextBlock = "";

        /// <summary>
        /// Add\Update user validation bitwise operation
        /// double last value to add new check
        /// </summary>
        [Flags]
        private enum UserChangesValidation
        {
            AuthName = 2,
            FullName = 4,
            LastName = 8,
            Department = 16,
            Categories = 32,
            Role = 64,
            Location = 128
        }
        /// <summary>
        /// Add\Update counter validation bitwise operation
        /// double last value to add new check
        /// </summary>
        [Flags]
        private enum CounterChangesValidation
        {
            Host = 2,
            CounterNumber = 4,
            Description = 8,
            DisplayDescription = 16,
            Location = 32,
            Floor = 64,
            Category = 128,
            Icon = 256
        }
        /// <summary>
        /// Add\Update wait time validation bitwise operation
        /// double last value to add new check
        /// </summary>
        [Flags]
        private enum WaitTimeChangesValidation
        {
            Mail = 2,
            Category = 4,
            MaxWaitTime = 8
        }
        /// <summary>
        /// Add\Update category validation bitwise operation
        /// double last value to add new check
        /// </summary>
        [Flags]
        private enum CategoryChangesValidation
        {
            Id = 2,
            Description = 4,
            DepartmentId = 8,
            Active = 16,
            Icon = 32,
            Location = 64
        }
        /// <summary>
        /// Add\Update group device validation bitwise operation
        /// double last value to add new check
        /// </summary>
        [Flags]
        private enum GroupDeviceChangesValidation
        {
            Kind = 2,
            Name = 4,
            Description = 8,
            Location = 16
        }
        /// <summary>
        /// Add\Update department validation bitwise operation
        /// double last value to add new check
        /// </summary>
        [Flags]
        private enum DepartmentChangesValidation
        {
            Id = 2,
            DepartmentName = 4,
            Symbol = 8,
            SymbolType = 16,
            OrderBy = 32
        }
        /// <summary>
        /// Add\Update department validation bitwise operation
        /// double last value to add new check
        /// </summary>
        [Flags]
        private enum TransferTypesChangesValidation
        {
            Id = 2,
            Department = 4,
            Description = 8,
            Icon = 16,
            Location = 32
        }
        #endregion ValidationFlags

        // *******************************************************************************************

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
        DataContextItem CurrentVisiblePanel;

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

        /// <summary>
        /// AdminPage Main
        /// </summary>
        public AdminPage()
        {
            this.InitializeComponent();
            this.Loaded += AdminPage_Loaded;
        }

        /// <summary>
        /// AdminPage loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void AdminPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                VsisSettings = ((App)Application.Current).VsisSettings;
                connection = ((App)Application.Current).VsisConnection;
                _isAuthenticated = ((App)Application.Current).IsAuthenticated;
                EnableEditMode(_isAuthenticated);

                if (connection == null || connection.State.ToString() != "Connected")
                {
                    await DataChanges.GenericDialog("Error: Connection Lost", "A connection with the server was not found.\nPlease report " +
                        "this to the IT Dept.\nYou will be redirected to the main page.");

                    this.Frame.Navigate(typeof(MainPage));
                }
                else
                {
                    HubInvoked();

                    // prefetch icon list
                    await SendGetIconList();

                    // default panel
                    await ShowPanel(DataContextItem.Users);
                    UserListAppBarToggleButton.IsChecked = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        // *******************************************************************************************

        #region PageCollectionObjects

        // DataGrid lists
        private ObservableCollection<VsisUser> vsisUsersCollection;
        private ObservableCollection<WaitTimeNotify> waitTimesCollection;
        private ObservableCollection<Category> categoryCollection;
        private ObservableCollection<Counter> counterCollection;
        private ObservableCollection<GroupDevices> groupDevicesCollection;
        private ObservableCollection<Department> departmentCollection;
        private ObservableCollection<Transfers> transferTypesCollection;

        public ObservableCollection<VsisUser> VsisUserItems
        {
            get
            {
                return vsisUsersCollection;
            }
            set
            {
                Set(ref vsisUsersCollection, value);
            }
        }
        public ObservableCollection<WaitTimeNotify> WaitTimesItems
        {
            get
            {
                return waitTimesCollection;
            }
            set
            {
                Set(ref waitTimesCollection, value);
            }
        }
        public ObservableCollection<Category> CategoryItems
        {
            get
            {
                return categoryCollection;
            }
            set
            {
                Set(ref categoryCollection, value);
            }
        }
        public ObservableCollection<Counter> CounterItems
        {
            get
            {
                return counterCollection;
            }
            set
            {
                Set(ref counterCollection, value);
            }
        }
        public ObservableCollection<GroupDevices> GroupDevicesItems
        {
            get
            {
                return groupDevicesCollection;
            }
            set
            {
                Set(ref groupDevicesCollection, value);
            }
        }
        public ObservableCollection<Department> DepartmentItems
        {
            get
            {
                return departmentCollection;
            }
            set
            {
                Set(ref departmentCollection, value);
            }
        }
        public ObservableCollection<Transfers> TransferTypeItems
        {
            get
            {
                return transferTypesCollection;
            }
            set
            {
                Set(ref transferTypesCollection, value);
            }
        }

        // Combo lists
        private List<DepartmentDetail> DepartmentList;
        private static List<Roles> RoleList;
        private static List<IconInventory> IconList;
        private static List<GroupDevices> GroupDeviceList;

        #endregion PageCollectionObjects

        // *******************************************************************************************

        /// <summary>
        /// Received messages
        /// </summary>
        private void HubInvoked()
        {
            try
            {
                if (connection != null)
                {
                    connection.On<string, string>("DataChangeAppliedAdminPage", (m, d) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => DataChangeAppliedAdminPage(m, d));
                    });
                    connection.On<List<VsisUser>>("VsisUsersList", (m) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FillUsers(m));
                    });
                    connection.On<List<DepartmentDetail>>("DepartmentDetailList", (m) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FillDepartmentDetailList(m));
                    });
                    connection.On<List<Department>>("DepartmentList", (m) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FillDepartments(m));
                    });
                    connection.On<List<WaitTimeNotify>>("WaitTimeNotifyList", (m) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FillWaitTimeNotifyGrid(m));
                    });
                    connection.On<List<Category>>("CategoriesList", (m) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FillCategoriesGrid(m));
                    });
                    connection.On<List<IconInventory>>("IconList", (m) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FillIconList(m));
                    });
                    connection.On<List<Counter>>("CountersList", (m) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FillCounterGrid(m));
                    });
                    connection.On<List<GroupDevices>>("GroupDeviceList", (m) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FillGroupDeviceGrid(m));
                    });
                    connection.On<List<Transfers>>("TransferTypesForManager", (m) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FillTransferTypes(m));
                    });

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        // *******************************************************************************************

        #region SignalRSends

        // Get
        private async Task SendGetUsers()
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("GetUsers", VsisSettings.AuthName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private async Task SendGetDepartmentDetailList()
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("GetDepartmentDetailList", VsisSettings.AuthName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private async Task SendGetDepartmentList()
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("GetDepartmentList", VsisSettings.AuthName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private async Task SendGetWaitTimeNotifyList()
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("GetWaitTimesList", VsisSettings.AuthName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private async Task SendGetIconList()
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("GetIconList", VsisSettings.AuthName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private async Task SendGetCategories()
        {
            try
            {
                if (connection != null)
                {
                    await connection.InvokeAsync("GetAnyCategoriesList", VsisSettings.AuthName);
                    await connection.InvokeAsync("GetAnyCategoriesIconsList", VsisSettings.AuthName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private async Task SendGetCounterList()
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("GetCounterList", VsisSettings.AuthName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private async Task SendGetGroupDeviceList()
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("GetGroupDeviceList", VsisSettings.AuthName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private async Task SendGetTransferTypesForManager()
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("GetTransferTypesForManager", VsisSettings.AuthName, returnChangeAppliedPage);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        // *******************************************************************
        // Add, Updates, Deletes must have return method included in Send
        // *******************************************************************
        string returnChangeAppliedPage = "DataChangeAppliedAdminPage";

        // Add
        private async Task SendAddUser(VsisUser user)
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("AddUser", VsisSettings.AuthName, returnChangeAppliedPage, user);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private async Task SendAddCounter(Counter counter)
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("AddCounter", VsisSettings.AuthName, returnChangeAppliedPage, counter);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private async Task SendAddWaitTime(WaitTimeNotify w)
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("AddWaitTime", VsisSettings.AuthName, returnChangeAppliedPage, w);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private async Task SendAddCategory(Category category)
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("AddCategory", VsisSettings.AuthName, returnChangeAppliedPage, category);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private async Task SendAddGroupDevice(GroupDevices devices)
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("AddGroupDevice", VsisSettings.AuthName, returnChangeAppliedPage, devices);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private async Task SendAddDepartment(Department dept)
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("AddDepartment", VsisSettings.AuthName, returnChangeAppliedPage, dept);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private async Task SendAddTransferType(Transfers transfer)
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("AddTransferType", VsisSettings.AuthName, returnChangeAppliedPage, transfer);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        // Update
        private async Task SendUpdateCounter(Counter counter)
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("UpdateCounter", VsisSettings.AuthName, returnChangeAppliedPage, counter);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private async Task SendUpdateWaitTime(WaitTimeNotify w)
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("UpdateWaitTime", VsisSettings.AuthName, returnChangeAppliedPage, w);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private async Task SendUpdateUser(VsisUser user)
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("UpdateUser", VsisSettings.AuthName, returnChangeAppliedPage, user);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private async Task SendUpdateCategory(Category category)
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("UpdateCategory", VsisSettings.AuthName, returnChangeAppliedPage, category);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private async Task SendUpdateGroupDevice(GroupDevices devices)
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("UpdateGroupDevice", VsisSettings.AuthName, returnChangeAppliedPage, devices);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private async Task SendUpdateDepartment(Department dept)
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("UpdateDepartment", VsisSettings.AuthName, returnChangeAppliedPage, dept);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private async Task SendUpdateTransferType(Transfers transfer)
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("UpdateTransferType", VsisSettings.AuthName, returnChangeAppliedPage, transfer);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        // Delete
        private async Task SendDeleteCounter(string host)
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("DeleteCounter", VsisSettings.AuthName, returnChangeAppliedPage, host);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private async Task SendDeleteWaitTime(string mail)
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("DeleteWaitTime", VsisSettings.AuthName, returnChangeAppliedPage, mail);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private async Task SendDeleteUser(string authName)
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("DeleteUser", VsisSettings.AuthName, returnChangeAppliedPage, authName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private async Task SendDeleteCategory(ulong id)
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("DeleteCategory", VsisSettings.AuthName, returnChangeAppliedPage, id);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private async Task SendDeleteGroupDevice(int id)
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("DeleteGroupDevice", VsisSettings.AuthName, returnChangeAppliedPage, id);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private async Task SendDeleteDepartment(int id)
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("DeleteDepartment", VsisSettings.AuthName, returnChangeAppliedPage, id);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private async Task SendDeleteTransferType(ulong id)
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("DeleteTransferType", VsisSettings.AuthName, returnChangeAppliedPage, id);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        #endregion SignalRSends

        // *******************************************************************************************

        /// <summary>
        /// Show dialog with server message
        /// </summary>
        /// <param name="reason_text"></param>
        /// <param name="descrption_text"></param>
        private async void DataChangeAppliedAdminPage(string reason_text, string descrption_text, string return_method_name)
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

        #region FillMethods

        private void FillDepartmentDetailList(List<DepartmentDetail> m)
        {
            DepartmentList = new List<DepartmentDetail>(m);
        }

        private void FillIconList(List<IconInventory> m)
        {
            IconList = new List<IconInventory>(m);

            //foreach (var item in m)
            //{
            //    IconList.Add(new IconInventory
            //    {
            //        Id = item.Id,
            //        Icon = $"/Assets/" + item.Icon,
            //        ControlType = item.ControlType,
            //        Description = item.Description
            //    });
            //}
        }

        private async Task FillGroupDeviceKindList()
        {
            GroupDeviceList = new List<GroupDevices>();

            foreach (var item in groupDevicesCollection.GroupBy(p => p.Kind).Select(x => x.FirstOrDefault()))
            {
                GroupDeviceList.Add(new GroupDevices
                {
                    Kind = item.Kind
                });
            }
            await Task.CompletedTask;
        }

        #endregion FillMethods

        // *******************************************************************************************

        /// <summary>
        /// Add button controls that have common enable\disable states
        /// true = edit mode
        /// </summary>
        /// <param name="v"></param>
        private void EnableEditControls(bool v)
        {
            try
            {
                if (v)
                {
                    SaveRowButton.Visibility = Visibility.Visible;
                    CancelRowButton.Visibility = Visibility.Visible;

                    AddRowButton.IsTapEnabled = false;
                    AddRowButton.Foreground = paoDisabled;
                }
                else
                {
                    SaveRowButton.Visibility = Visibility.Collapsed;
                    CancelRowButton.Visibility = Visibility.Collapsed;

                    AddRowButton.IsTapEnabled = true;
                    AddRowButton.Foreground = paoEasyBlue;

                    DeleteRowButton.IsTapEnabled = false;
                    DeleteRowButton.Foreground = paoDisabled;

                    ToolTip tip = new ToolTip();
                    tip.Content = $"Select a row to delete";
                    ToolTipService.SetToolTip(DeleteRowButton, tip);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Load users datagrid
        /// </summary>
        /// <param name="users"></param>
        public async void FillUsers(List<VsisUser> users)
        {
            try
            {
                if (users != null)
                {
                    vsisUsersCollection = new ObservableCollection<VsisUser>(users.OrderBy(x => x.FullName));

                    VsisUserItems = vsisUsersCollection;
                    VsisUsersDataGrid.ItemsSource = null;
                    VsisUsersDataGrid.ItemsSource = VsisUserItems;

                    var comboBoxColumn = VsisUsersDataGrid.Columns.FirstOrDefault(x => x.Tag?.Equals("Department") == true) as DataGridComboBoxColumn;
                    if (comboBoxColumn != null)
                    {
                        comboBoxColumn.ItemsSource = DepartmentList;
                    }
                    comboBoxColumn = VsisUsersDataGrid.Columns.FirstOrDefault(x => x.Tag?.Equals("Roles") == true) as DataGridComboBoxColumn;
                    if (comboBoxColumn != null)
                    {
                        comboBoxColumn.ItemsSource = await GetRoles();
                    }

                    if (vsisUsersCollection != null)
                    {
                        SubTextBlock = $"There are {vsisUsersCollection.Count} existing users: " +
                            $"{vsisUsersCollection.Where(p => p.Role == "Agent").Count()} Agents, and {vsisUsersCollection.Where(p => p.Role == "Manager").Count()} Managers.";

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
        /// Load Wait Times datagrid
        /// </summary>
        /// <param name="waits"></param>
        public async void FillWaitTimeNotifyGrid(List<WaitTimeNotify> waits)
        {
            try
            {
                if (waits != null)
                {
                    waitTimesCollection = new ObservableCollection<WaitTimeNotify>(waits.OrderBy(x => x.Category));

                    WaitTimesItems = waitTimesCollection;
                    WaitNotifyDataGrid.ItemsSource = null;
                    WaitNotifyDataGrid.ItemsSource = WaitTimesItems;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Load Categories datagrid
        /// </summary>
        /// <param name="cats"></param>
        public async void FillCategoriesGrid(List<Category> cats)
        {
            try
            {
                if (cats != null)
                {
                    categoryCollection = new ObservableCollection<Category>(cats);

                    CategoryItems = categoryCollection;
                    CategoriesDataGrid.ItemsSource = null;
                    CategoriesDataGrid.ItemsSource = CategoryItems;

                    var comboBoxColumn = CategoriesDataGrid.Columns.FirstOrDefault(x => x.Tag?.Equals("Icon") == true) as DataGridComboBoxColumn;
                    if (comboBoxColumn != null)
                    {
                        comboBoxColumn.ItemsSource = IconList;
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
        /// Load Counters datagrid
        /// </summary>
        /// <param name="counter"></param>
        public async void FillCounterGrid(List<Counter> counter)
        {
            try
            {
                if (counter != null)
                {
                    counterCollection = new ObservableCollection<Counter>(counter);

                    CounterItems = counterCollection;
                    CountersDataGrid.ItemsSource = null;
                    CountersDataGrid.ItemsSource = CounterItems;

                    if (IconList.Count > 0)
                    {
                        var comboBoxColumn = CountersDataGrid.Columns.FirstOrDefault(x => x.Tag?.Equals("Icon") == true) as DataGridComboBoxColumn;
                        if (comboBoxColumn != null)
                            comboBoxColumn.ItemsSource = IconList;
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
        /// Load Group Devices datagrid
        /// </summary>
        /// <param name="devices"></param>
        public async void FillGroupDeviceGrid(List<GroupDevices> devices)
        {
            try
            {
                if (devices != null)
                {
                    groupDevicesCollection = new ObservableCollection<GroupDevices>(devices);

                    await FillGroupDeviceKindList();

                    GroupDevicesItems = groupDevicesCollection;
                    GroupDevicesDataGrid.ItemsSource = null;
                    GroupDevicesDataGrid.ItemsSource = GroupDevicesItems;

                    if (GroupDeviceList != null)
                    {
                        var comboBoxColumn = GroupDevicesDataGrid.Columns.FirstOrDefault(x => x.Tag?.Equals("Kind") == true) as DataGridComboBoxColumn;
                        if (comboBoxColumn != null)
                        {
                            comboBoxColumn.ItemsSource = GroupDeviceList;
                        }
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
        /// Load Transfer Types datagrid
        /// </summary>
        /// <param name="devices"></param>
        public async void FillTransferTypes(List<Transfers> reasons)
        {
            try
            {
                if (reasons != null)
                {
                    transferTypesCollection = new ObservableCollection<Transfers>(reasons);

                    TransferTypeItems = transferTypesCollection;
                    TransferTypesDataGrid.ItemsSource = null;
                    TransferTypesDataGrid.ItemsSource = TransferTypeItems;

                    if (IconList.Count > 0)
                    {
                        var comboBoxColumn = TransferTypesDataGrid.Columns.FirstOrDefault(x => x.Tag?.Equals("Icon") == true) as DataGridComboBoxColumn;
                        if (comboBoxColumn != null)
                            comboBoxColumn.ItemsSource = IconList;
                    }
                    if (DepartmentList.Count > 0)
                    {
                        var comboBoxColumn = TransferTypesDataGrid.Columns.FirstOrDefault(x => x.Tag?.Equals("Department") == true) as DataGridComboBoxColumn;
                        if (comboBoxColumn != null)
                            comboBoxColumn.ItemsSource = DepartmentList;
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
        /// Load Department datagrid
        /// </summary>
        /// <param name="departments"></param>
        public async void FillDepartments(List<Department> departments)
        {
            try
            {
                if (departments != null)
                {
                    departmentCollection = new ObservableCollection<Department>(departments.OrderBy(x => x.OrderBy));

                    DepartmentItems = departmentCollection;
                    DepartmentsDataGrid.ItemsSource = null;
                    DepartmentsDataGrid.ItemsSource = DepartmentItems;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// TODO: get data from server if list changes frequently
        /// Name, Description
        /// Admin   SYSADMIN
        /// Agent Agent
        /// Manager Manager
        /// </summary>
        public async Task<IEnumerable<Roles>> GetRoles()
        {
            RoleList = new List<Roles>();

            Roles role = new Roles();
            role.Role = "Admin";
            role.Description = "SYSADMIN";
            RoleList.Add(role);

            role = new Roles();
            role.Role = "Agent";
            role.Description = "Agent";
            RoleList.Add(role);

            role = new Roles();
            role.Role = "Manager";
            role.Description = "Manager";
            RoleList.Add(role);

            await Task.CompletedTask;

            return RoleList;
        }

        /// <summary>
        /// Property changed events
        /// </summary>
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

        /// <summary>
        /// Update page labels for selected data operation
        /// </summary>
        /// <param name="DataContextItem dci"></param>
        private async Task ShowPanel(DataContextItem dci)
        {
            try
            {
                CurrentVisiblePanel = dci;

                StackContainer.Visibility = Visibility.Visible;

                string title = "";                

                switch (dci)
                {
                    case DataContextItem.Users:

                        HideAllDataGrids();
                        VsisUsersDataGrid.Visibility = Visibility.Visible;

                        // fetch department combo data first
                        await SendGetDepartmentDetailList();
                        await SendGetUsers();

                        HeaderTitleTextBlock = "Maintain Users";
                        HeaderSubTitleTextBlock = "Add, Update, Delete User Accounts";
                        SubTextSymbol.Text = "\xE716";
                        break;

                    case DataContextItem.Counters:

                        HeaderTitleTextBlock = "Maintain Counters";
                        HeaderSubTitleTextBlock = "Add, Update, Delete Counters";
                        SubTextSymbol.Text = "\xE770";
                        SubTextBlock = "Manage counters";
                        title = "Counters";
                        break;

                    case DataContextItem.Categories:

                        HeaderTitleTextBlock = "Maintain Categories";
                        HeaderSubTitleTextBlock = "Add, Update, Delete Categories";
                        SubTextSymbol.Text = "\xE74C";
                        SubTextBlock = "Manage visitor categories";
                        title = "Categories";
                        break;

                    case DataContextItem.WaitTimes:

                        HeaderTitleTextBlock = "Maintain Wait Time Notifications";
                        HeaderSubTitleTextBlock = "Add, Update, Delete Category Wait Time Notifications";
                        SubTextSymbol.Text = "\xE781";
                        SubTextBlock = "Set manager email notifications";
                        title = "Category Wait Time Notifications";
                        break;

                    case DataContextItem.GroupDevices:

                        HeaderTitleTextBlock = "Maintain Devices";
                        HeaderSubTitleTextBlock = "Add, Update, Delete Group Devices";
                        SubTextSymbol.Text = "\xE703";
                        SubTextBlock = "Manage system devices";
                        title = "Group Devices";
                        break;

                    case DataContextItem.Departments:

                        HeaderTitleTextBlock = "Maintain Departments";
                        HeaderSubTitleTextBlock = "Add, Update, Delete Departments";
                        SubTextSymbol.Text = "\xE81E";
                        SubTextBlock = "Manage departments";
                        title = "Departments";
                        break;

                    case DataContextItem.TransferTypes:

                        HeaderTitleTextBlock = "Maintain Transfer Types";
                        HeaderSubTitleTextBlock = "Add, Update, Delete Transfer Types";
                        SubTextSymbol.Text = "\xE805";
                        SubTextBlock = "Manage Transfer Types";
                        title = "Transfer Types";
                        break;
                }

                SubTitleTextBlock = "";
                SubTitleTextBlock = PanelTitleTextArea(dci);

                SubTitleTextBlockMaxWidth = 0;
                SubTitleTextBlockMaxWidth = PanelTitleTextMaxWidth(dci);

                TitleTextBlock = title;

                AddDeleteButtonStack.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        //
        // Button tapped events
        // TODO: refactor to a shared event
        //

        private async void WaitTimesList_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ToggleAppBarButtons();
            EnableEditControls(false);
            AppBarToggleButton btn = (AppBarToggleButton)sender;
            btn.IsChecked = true;
            HideAllDataGrids();
            WaitNotifyDataGrid.Visibility = Visibility.Visible;
            await ShowPanel(DataContextItem.WaitTimes);
            await SendGetWaitTimeNotifyList();
        }

        private async void UserList_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ToggleAppBarButtons();
            EnableEditControls(false);
            AppBarToggleButton btn = (AppBarToggleButton)sender;
            btn.IsChecked = true;
            //            AppBarButton btn = (AppBarButton)sender;
            //          UpdateButtonStyle(btn);
            await ShowPanel(DataContextItem.Users);
        }

        private void ToggleAppBarButtons()
        {
            UserListAppBarToggleButton.IsChecked = false;
            WaitTimesAppBarToggleButton.IsChecked = false;
            CategoryAppBarToggleButton.IsChecked = false;
            CountersAppBarToggleButton.IsChecked = false;
            DevicesAppBarToggleButton.IsChecked = false;
            DepartmentAppBarToggleButton.IsChecked = false;
            TransferTypesAppBarToggleButton.IsChecked = false;
        }

        //private void UpdateButtonStyle(AppBarButton button)
        //{
        //    button.BorderBrush = paoEasyBlue;
        //    button.BorderThickness = new Thickness(1);
        //}

        private async void CategoryList_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ToggleAppBarButtons();
            EnableEditControls(false);
            AppBarToggleButton btn = (AppBarToggleButton)sender;
            btn.IsChecked = true;
            HideAllDataGrids();
            CategoriesDataGrid.Visibility = Visibility.Visible;
            await ShowPanel(DataContextItem.Categories);
            await SendGetCategories();
        }

        private async void CountersList_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ToggleAppBarButtons();
            EnableEditControls(false);
            AppBarToggleButton btn = (AppBarToggleButton)sender;
            btn.IsChecked = true;
            HideAllDataGrids();
            CountersDataGrid.Visibility = Visibility.Visible;
            await ShowPanel(DataContextItem.Counters);
            await SendGetCounterList();
        }
        private async void GroupDevicesList_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ToggleAppBarButtons();
            EnableEditControls(false);
            AppBarToggleButton btn = (AppBarToggleButton)sender;
            btn.IsChecked = true;
            HideAllDataGrids();
            GroupDevicesDataGrid.Visibility = Visibility.Visible;
            await ShowPanel(DataContextItem.GroupDevices);
            await SendGetGroupDeviceList();
        }
        private async void DepartmentList_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ToggleAppBarButtons();
            EnableEditControls(false);
            AppBarToggleButton btn = (AppBarToggleButton)sender;
            btn.IsChecked = true;
            HideAllDataGrids();
            DepartmentsDataGrid.Visibility = Visibility.Visible;
            await ShowPanel(DataContextItem.Departments);
            await SendGetDepartmentList();
        }
        private async void TransferTypesAppBarToggleButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ToggleAppBarButtons();
            EnableEditControls(false);
            AppBarToggleButton btn = (AppBarToggleButton)sender;
            btn.IsChecked = true;
            HideAllDataGrids();
            TransferTypesDataGrid.Visibility = Visibility.Visible;
            await ShowPanel(DataContextItem.TransferTypes);
            await SendGetTransferTypesForManager();
        }

        //
        // Button to add, save, delete, cancel datagrid tapped events
        //

        private void CancelRowButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            DoCancel();
        }

        private async void DoCancel()
        {
            try
            {
                DataTransType = DataTrans.None;

                EnableEditControls(false);

                SubTitleTextBlock = PanelTitleTextArea(CurrentVisiblePanel);

                switch (CurrentVisiblePanel)
                {
                    case DataContextItem.Users:
                        await SendGetUsers();
                        break;
                    case DataContextItem.Counters:
                        await SendGetCounterList();
                        break;
                    case DataContextItem.Categories:
                        await SendGetCategories();
                        break;
                    case DataContextItem.WaitTimes:
                        await SendGetWaitTimeNotifyList();
                        break;
                    case DataContextItem.GroupDevices:
                        await SendGetGroupDeviceList();
                        break;
                    case DataContextItem.Departments:
                        await SendGetDepartmentList();
                        break;
                    case DataContextItem.TransferTypes:
                        await SendGetTransferTypesForManager();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        private async void DeleteRowButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                bool tf = false;

                // DataTrans gets changed in selection change event,
                // so assign DataTrans.Delete on confirm

                switch (CurrentVisiblePanel)
                {
                    case DataContextItem.Users:

                        if (VsisUsersDataGrid.SelectedIndex > -1)
                        {
                            VsisUser user = vsisUsersCollection[VsisUsersDataGrid.SelectedIndex];
                            string msg = $"Are you sure you want to delete user {user.FullName}? {Environment.NewLine}Note: Deleting a user account does not delete their user statistics.";
                            tf = await DataChanges.ConfirmDialog($"Delete User?", msg);
                            if (tf)
                            {
                                DataTransType = DataTrans.Delete;
                                await SendDeleteUser(user.AuthName);
                            }
                        }
                        break;

                    case DataContextItem.Counters:

                        if (CountersDataGrid.SelectedIndex > -1)
                        {
                            Counter counter = counterCollection[CountersDataGrid.SelectedIndex];
                            tf = await DataChanges.ConfirmDialog($"Delete Counter?", $"Are you sure you want to delete counter {counter.Host}?");
                            if (tf)
                            {
                                DataTransType = DataTrans.Delete;
                                await SendDeleteCounter(counter.Host);
                            }
                        }
                        break;

                    case DataContextItem.Categories:

                        if (CategoriesDataGrid.SelectedIndex > -1)
                        {
                            Category category = categoryCollection[CategoriesDataGrid.SelectedIndex];
                            tf = await DataChanges.ConfirmDialog($"Delete Category?", $"Are you sure you want to delete {category.Description}?{Environment.NewLine}Did you read the warning?");
                            if (tf)
                            {
                                DataTransType = DataTrans.Delete;
                                await SendDeleteCategory(category.Id);
                            }
                        }
                        break;

                    case DataContextItem.WaitTimes:

                        if (CategoriesDataGrid.SelectedIndex > -1)
                        {
                            WaitTimeNotify w = waitTimesCollection[CategoriesDataGrid.SelectedIndex];
                            tf = await DataChanges.ConfirmDialog($"Delete Category Wait Time?", $"Are you sure you want to delete wait time category {w.Category}?");
                            if (tf)
                            {
                                DataTransType = DataTrans.Delete;
                                await SendDeleteWaitTime(w.Mail);
                            }
                        }
                        break;

                    case DataContextItem.GroupDevices:

                        if (GroupDevicesDataGrid.SelectedIndex > -1)
                        {
                            GroupDevices g = groupDevicesCollection[GroupDevicesDataGrid.SelectedIndex];
                            tf = await DataChanges.ConfirmDialog($"Delete Device?", $"Are you sure you want to delete device {g.Description}?");
                            if (tf)
                            {
                                DataTransType = DataTrans.Delete;
                                await SendDeleteGroupDevice(g.Id);
                            }
                        }
                        break;

                    case DataContextItem.Departments:

                        if (DepartmentsDataGrid.SelectedIndex > -1)
                        {
                            Department d = departmentCollection[DepartmentsDataGrid.SelectedIndex];
                            tf = await DataChanges.ConfirmDialog($"Delete Department?", $"Are you sure you want to delete department {d.DepartmentName}?");
                            if (tf)
                            {
                                DataTransType = DataTrans.Delete;
                                await SendDeleteDepartment(d.Id);
                            }
                        }
                        break;

                    case DataContextItem.TransferTypes:

                        if (TransferTypesDataGrid.SelectedIndex > -1)
                        {
                            Transfers t = transferTypesCollection[TransferTypesDataGrid.SelectedIndex];
                            tf = await DataChanges.ConfirmDialog($"Delete Transfer Type?", $"Are you sure you want to delete Transfer Type {t.Description}?");
                            if (tf)
                            {
                                DataTransType = DataTrans.Delete;
                                await SendDeleteTransferType(t.Id);
                            }
                        }
                        break;
                }
                SubTitleTextBlock = PanelTitleTextArea(CurrentVisiblePanel);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void AddRowButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                DataTransType = DataTrans.Add;
                CancelRowButton.Visibility = Visibility.Visible;

                // for creating binary values
                ulong max_id = 0;

                switch (CurrentVisiblePanel)
                {
                    case DataContextItem.Users:

                        VsisUser u = new VsisUser();
                        u.AuthName = "";
                        u.FullName = "";
                        u.LastName = "";
                        u.Department = 0;
                        u.DepartmentName = "";
                        u.Categories = 0;
                        u.Role = "";
                        u.Active = false;
                        u.Location = 1;
                        u.Created = DateTime.Now;
                        vsisUsersCollection.Add(u);
                        VsisUsersDataGrid.ScrollIntoView(u, null);
                        break;

                    case DataContextItem.Counters:

                        Counter counter = new Counter();
                        counter.Host = "";
                        counter.CounterNumber = "";
                        counter.Description = "";
                        counter.DisplayDescription = "";
                        counter.Location = 1;
                        counter.Floor = "";
                        counter.IsHandicap = false;
                        counter.IsAvailable = false;
                        counter.Icon = "";
                        counter.Created = DateTime.Now;
                        counterCollection.Add(counter);
                        CountersDataGrid.ScrollIntoView(counter, null);
                        break;

                    case DataContextItem.Categories:

                        foreach (var item in categoryCollection)
                        {
                            if (item.Id > max_id)
                                max_id = item.Id;
                        }

                        Category c = new Category();

                        // increment next ulong value
                        c.Id = max_id * 2;
                        c.Description = "";
                        c.DepartmentId = 0;
                        c.Location = 1;
                        c.Active = false;
                        c.Created = DateTime.Now;

                        categoryCollection.Add(c);
                        CategoriesDataGrid.ScrollIntoView(c, null);
                        break;

                    case DataContextItem.WaitTimes:

                        WaitTimeNotify w = new WaitTimeNotify();
                        waitTimesCollection.Add(w);
                        WaitNotifyDataGrid.ScrollIntoView(w, null);
                        break;

                    case DataContextItem.GroupDevices:

                        GroupDevices d = new GroupDevices();
                        d.Location = 1;
                        d.Created = DateTime.Now;
                        groupDevicesCollection.Add(d);
                        GroupDevicesDataGrid.ScrollIntoView(d, null);
                        break;

                    case DataContextItem.Departments:

                        Department dept = new Department();

                        // suggest new department Id
                        dept.Id = departmentCollection.Select(p => p.Id).Max() + 1;
                        dept.OrderBy = departmentCollection.Select(p => p.OrderBy).Max() + 1;
                        dept.Created = DateTime.Now;
                        departmentCollection.Add(dept);
                        DepartmentsDataGrid.ScrollIntoView(dept, null);
                        break;

                    case DataContextItem.TransferTypes:

                        Transfers t = new Transfers();

                        // create next binary value
                        foreach (var item in transferTypesCollection)
                        {
                            if (item.Id > max_id)
                                max_id = item.Id;
                        }

                        // increment next ulong value
                        t.Id = max_id * 2;
                        // suggest new department Id
                        sbyte number;
                        if (sbyte.TryParse((transferTypesCollection.Select(p => p.Department).Max() + 1).ToString(), out number))
                            t.Department = number;

                        t.Location = 1;
                        t.Created = DateTime.Now;
                        transferTypesCollection.Add(t);
                        TransferTypesDataGrid.ScrollIntoView(t, null);
                        break;
                }

                SubTitleTextBlock = PanelTitleTextArea(CurrentVisiblePanel);

                SaveRowButton.Visibility = Visibility.Visible;
                CancelRowButton.Visibility = Visibility.Visible;
                AddRowButton.IsTapEnabled = false;
                AddRowButton.Foreground = paoDisabled;

                DeleteRowButton.IsTapEnabled = false;
                DeleteRowButton.Foreground = paoDisabled;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async void SaveRowButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                string dialogTitle = "";
                string dialogContent = "";
                bool showDialog = false;
                bool wrongRowError = false;
                bool rowNotSelected = false;

                switch (CurrentVisiblePanel)
                {
                    case DataContextItem.Users:

                        dialogTitle = "User was not saved.";

                        if (VsisUsersDataGrid.SelectedItems.Count == 1)
                        {
                            switch (DataTransType)
                            {
                                case DataTrans.Add:
                                    // make sure user did not select an existing row
                                    if (vsisUsersCollection.Count - 1 == VsisUsersDataGrid.SelectedIndex)
                                    {
                                        await SaveAddVsisUser();
                                    }
                                    else
                                    {
                                        showDialog = true;
                                        wrongRowError = true;
                                    }

                                    break;
                                case DataTrans.Update:
                                    await SaveUpdateVsisUser();
                                    break;
                            }
                        }
                        else
                        {
                            showDialog = true;
                            rowNotSelected = true;
                        }
                        break;

                    case DataContextItem.Counters:

                        dialogTitle = "Counter was not saved.";

                        if (CountersDataGrid.SelectedItems.Count == 1)
                        {
                            switch (DataTransType)
                            {
                                case DataTrans.Add:

                                    // make sure user did not select an existing row
                                    if (counterCollection.Count - 1 == CountersDataGrid.SelectedIndex)
                                    {
                                        await SaveAddCounter();
                                    }
                                    else
                                    {
                                        showDialog = true;
                                        wrongRowError = true;
                                    }

                                    break;
                                case DataTrans.Update:
                                    await SaveUpdateCounter();
                                    break;
                            }
                        }
                        else
                        {
                            showDialog = true;
                            rowNotSelected = true;
                        }
                        break;

                    case DataContextItem.Categories:

                        dialogTitle = "Category was not saved.";

                        if (CategoriesDataGrid.SelectedItems.Count == 1)
                        {
                            switch (DataTransType)
                            {
                                case DataTrans.Add:
                                    // make sure user did not select an existing row
                                    if (categoryCollection.Count - 1 == CategoriesDataGrid.SelectedIndex)
                                    {
                                        await SaveAddCategory();
                                    }
                                    else
                                    {
                                        showDialog = true;
                                        wrongRowError = true;
                                    }
                                    break;
                                case DataTrans.Update:
                                    await SaveUpdateCategory();
                                    break;
                            }
                        }
                        else
                        {
                            showDialog = true;
                            rowNotSelected = true;
                        }
                        break;

                    case DataContextItem.WaitTimes:

                        dialogTitle = "Wait Time was not saved.";

                        if (WaitNotifyDataGrid.SelectedItems.Count == 1)
                        {
                            switch (DataTransType)
                            {
                                case DataTrans.Add:
                                    // make sure user did not select an existing row
                                    if (waitTimesCollection.Count - 1 == WaitNotifyDataGrid.SelectedIndex)
                                    {
                                        await SaveAddWaitTime();
                                    }
                                    else
                                    {
                                        showDialog = true;
                                        wrongRowError = true;
                                    }
                                    break;
                                case DataTrans.Update:
                                    await SaveUpdateWaitTime();
                                    break;
                            }

                        }
                        else
                        {
                            showDialog = true;
                            rowNotSelected = true;
                        }
                        break;

                    case DataContextItem.GroupDevices:

                        dialogTitle = "Device was not saved.";

                        if (GroupDevicesDataGrid.SelectedItems.Count == 1)
                        {
                            switch (DataTransType)
                            {
                                case DataTrans.Add:
                                    // make sure user did not select an existing row
                                    if (groupDevicesCollection.Count - 1 == GroupDevicesDataGrid.SelectedIndex)
                                    {
                                        await SaveAddGroupDevice();
                                    }
                                    else
                                    {
                                        showDialog = true;
                                        wrongRowError = true;
                                    }
                                    break;
                                case DataTrans.Update:
                                    await SaveUpdateGroupDevice();
                                    break;
                            }
                        }
                        else
                        {
                            showDialog = true;
                            rowNotSelected = true;
                        }
                        break;

                    case DataContextItem.Departments:

                        dialogTitle = "Department was not saved.";

                        if (DepartmentsDataGrid.SelectedItems.Count == 1)
                        {
                            switch (DataTransType)
                            {
                                case DataTrans.Add:
                                    // make sure user did not select an existing row
                                    if (departmentCollection.Count - 1 == DepartmentsDataGrid.SelectedIndex)
                                    {
                                        await SaveAddDepartment();
                                    }
                                    else
                                    {
                                        showDialog = true;
                                        wrongRowError = true;
                                    }
                                    break;
                                case DataTrans.Update:
                                    await SaveUpdateDepartment();
                                    break;
                            }
                        }
                        else
                        {
                            showDialog = true;
                            rowNotSelected = true;
                        }
                        break;

                    case DataContextItem.TransferTypes:

                        dialogTitle = "Transfer Type was not saved.";

                        if (TransferTypesDataGrid.SelectedItems.Count == 1)
                        {
                            switch (DataTransType)
                            {
                                case DataTrans.Add:
                                    // make sure user did not select an existing row
                                    if (transferTypesCollection.Count - 1 == TransferTypesDataGrid.SelectedIndex)
                                    {
                                        await SaveAddTransferType();
                                    }
                                    else
                                    {
                                        showDialog = true;
                                        wrongRowError = true;
                                    }
                                    break;
                                case DataTrans.Update:
                                    await SaveUpdateTransferType();
                                    break;
                            }
                        }
                        else
                        {
                            showDialog = true;
                            rowNotSelected = true;
                        }
                        break;
                }
                if (showDialog)
                {
                    if (wrongRowError)
                        dialogContent = "Please select the last row in the table and click Save again.";

                    if (rowNotSelected)
                        dialogContent = "Please select a row to save.";

                    await DataChanges.GenericDialog(dialogTitle, dialogContent);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Hide datagrids
        /// Use when switching between datagrids
        /// </summary>
        private void HideAllDataGrids()
        {
            VsisUsersDataGrid.Visibility = Visibility.Collapsed;
            WaitNotifyDataGrid.Visibility = Visibility.Collapsed;
            CategoriesDataGrid.Visibility = Visibility.Collapsed;
            CountersDataGrid.Visibility = Visibility.Collapsed;
            GroupDevicesDataGrid.Visibility = Visibility.Collapsed;
            DepartmentsDataGrid.Visibility = Visibility.Collapsed;
            TransferTypesDataGrid.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Adds new category
        /// </summary>
        /// <returns></returns>
        private async Task SaveAddCategory()
        {
            Category c = new Category();

            if (CategoriesDataGrid.SelectedItems.Count == 1)
            {
                foreach (var obj in CategoriesDataGrid.SelectedItems)
                {
                    c = obj as Category;
                }
                await SendAddCategory(c);
            }
        }

        /// <summary>
        /// Update category
        /// </summary>
        /// <returns></returns>
        private async Task SaveUpdateCategory()
        {
            Category c = new Category();

            foreach (var obj in CategoriesDataGrid.SelectedItems)
            {
                c = obj as Category;
            }
            await SendUpdateCategory(c);
        }

        /// <summary>
        /// Update user
        /// </summary>
        /// <returns></returns>
        private async Task SaveUpdateVsisUser()
        {
            try
            {
                VsisUser user = new VsisUser();

                foreach (var obj in VsisUsersDataGrid.SelectedItems)
                {
                    user = obj as VsisUser;
                }

                if (user != null)
                {
                    UserChangesValidation validate = UserChangesValidation.FullName |
                        UserChangesValidation.LastName |
                        UserChangesValidation.Department |
                        UserChangesValidation.Categories |
                        UserChangesValidation.Role |
                        UserChangesValidation.Location;

                    if (user.FullName.Length > 0)
                        validate &= ~UserChangesValidation.FullName;
                    if (user.LastName.Length > 0)
                        validate &= ~UserChangesValidation.LastName;
                    if (user.Department > 0)
                        validate &= ~UserChangesValidation.Department;
                    if (user.Categories > 0)
                        validate &= ~UserChangesValidation.Categories;
                    if (user.Role.Length > 0)
                        validate &= ~UserChangesValidation.Role;
                    if (user.Location > 0)
                        validate &= ~UserChangesValidation.Location;

                    // determine the successful steps

                    if (validate == 0)
                    {
                        bool tf = await DataChanges.ConfirmDialog($"Update User", $"Apply changes to {user.FullName}");
                        if (tf)
                            await SendUpdateUser(user);
                    }
                    else
                    {
                        DataChangeAppliedAdminPage("Required fields", validate.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Local call and not a server message
        /// All dialog messages from server go to MainPage
        /// </summary>
        /// <param name="m"></param>
        /// <param name="d"></param>
        private async void DataChangeAppliedAdminPage(string reason_text, string descrption_text)
        {
            try
            {

                if (reason_text == "success")
                {
                    // do before clearing trans
                    await DataChanges.ChangeApplied((DataChanges.DataTrans)DataTransType, descrption_text);

                    DataTransType = DataTrans.None;
                    EnableEditControls(false);
                }
                else
                {
                    await DataChanges.ChangeApplied((DataChanges.DataTrans)DataTransType, reason_text);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        /// <summary>
        /// Add user
        /// </summary>
        /// <returns></returns>
        private async Task SaveAddVsisUser()
        {
            try
            {
                VsisUser user = new VsisUser();
                bool hasSelectedItem = false;

                foreach (var obj in VsisUsersDataGrid.SelectedItems)
                {
                    user = obj as VsisUser;
                    hasSelectedItem = true;
                }

                if (hasSelectedItem && user != null)
                {
                    // determine the successful steps
                    UserChangesValidation validate = UserChangesValidation.AuthName |
                        UserChangesValidation.FullName |
                        UserChangesValidation.LastName |
                        UserChangesValidation.Department |
                        UserChangesValidation.Categories |
                        UserChangesValidation.Role |
                        UserChangesValidation.Location;

                    if (user.AuthName.Length > 0)
                        validate &= ~UserChangesValidation.AuthName;
                    if (user.FullName.Length > 0)
                        validate &= ~UserChangesValidation.FullName;
                    if (user.LastName.Length > 0)
                        validate &= ~UserChangesValidation.LastName;
                    if (user.Department > 0)
                        validate &= ~UserChangesValidation.Department;
                    if (user.Categories > 0)
                        validate &= ~UserChangesValidation.Categories;
                    if (user.Role.Length > 0)
                        validate &= ~UserChangesValidation.Role;
                    if (user.Location > 0)
                        validate &= ~UserChangesValidation.Location;

                    if (validate == 0)
                    {
                        bool tf = await DataChanges.ConfirmDialog($"Add User?", $"Confirm to add {user.FullName}");
                        if (tf)
                            await SendAddUser(user);
                    }
                    else
                    {
                        DataChangeAppliedAdminPage("Required fields", validate.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Validate counter
        /// </summary>
        /// <param name="counter"></param>
        /// <returns></returns>
        private async Task<bool> ValidateCounter(Counter counter)
        {
            try
            {
                if (counter != null)
                {
                    // determine the successful steps
                    CounterChangesValidation validate = CounterChangesValidation.Host |
                        CounterChangesValidation.CounterNumber |
                        CounterChangesValidation.Description |
                        CounterChangesValidation.DisplayDescription |
                        CounterChangesValidation.Location |
                        CounterChangesValidation.Floor |
                        CounterChangesValidation.Category |
                        CounterChangesValidation.Icon;

                    if (counter.Host.Length > 0)
                        validate &= ~CounterChangesValidation.Host;
                    if (counter.CounterNumber.Length > 0)
                        validate &= ~CounterChangesValidation.CounterNumber;
                    if (counter.Description.Length > 0)
                        validate &= ~CounterChangesValidation.Description;
                    if (counter.DisplayDescription.Length > 0)
                        validate &= ~CounterChangesValidation.DisplayDescription;
                    if (counter.Location.HasValue)
                        validate &= ~CounterChangesValidation.Location;
                    if (counter.Floor.Length > 0)
                        validate &= ~CounterChangesValidation.Floor;
                    if (counter.Category > 0)
                        validate &= ~CounterChangesValidation.Category;
                    if (counter.Icon.Length > 0)
                        validate &= ~CounterChangesValidation.Icon;

                    if (validate == 0)
                    {
                        return true;
                    }
                    else
                    {
                        DataChangeAppliedAdminPage("Required fields", validate.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return false;
        }

        /// <summary>
        /// Add counter
        /// </summary>
        /// <returns></returns>
        private async Task SaveAddCounter()
        {
            try
            {
                Counter counter = new Counter();
                bool hasSelectedItem = false;

                foreach (var obj in CountersDataGrid.SelectedItems)
                {
                    counter = obj as Counter;
                    hasSelectedItem = true;
                }

                if (hasSelectedItem && counter != null)
                {
                    // determine the successful steps
                    CounterChangesValidation validate = CounterChangesValidation.Host |
                        CounterChangesValidation.CounterNumber |
                        CounterChangesValidation.Description |
                        CounterChangesValidation.DisplayDescription |
                        CounterChangesValidation.Location |
                        CounterChangesValidation.Floor |
                        CounterChangesValidation.Category |
                        CounterChangesValidation.Icon;

                    if (counter.Host.Length > 0)
                        validate &= ~CounterChangesValidation.Host;
                    if (counter.CounterNumber.Length > 0)
                        validate &= ~CounterChangesValidation.CounterNumber;
                    if (counter.Description.Length > 0)
                        validate &= ~CounterChangesValidation.Description;
                    if (counter.DisplayDescription.Length > 0)
                        validate &= ~CounterChangesValidation.DisplayDescription;
                    if (counter.Location.HasValue)
                        validate &= ~CounterChangesValidation.Location;
                    if (counter.Floor.Length > 0)
                        validate &= ~CounterChangesValidation.Floor;
                    if (counter.Category > 0)
                        validate &= ~CounterChangesValidation.Category;
                    if (counter.Icon.Length > 0)
                        validate &= ~CounterChangesValidation.Icon;

                    if (validate == 0)
                    {
                        bool tf = await DataChanges.ConfirmDialog($"Add Counter?", $"Confirm to add {counter.Host}");
                        if (tf)
                            await SendAddCounter(counter);
                    }
                    else
                    {
                        DataChangeAppliedAdminPage("Required fields", validate.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Save new Transfer Type
        /// </summary>
        /// <returns></returns>
        private async Task SaveAddTransferType()
        {
            try
            {
                Transfers t = new Transfers();

                bool hasSelectedItem = false;

                foreach (var obj in TransferTypesDataGrid.SelectedItems)
                {
                    t = obj as Transfers;
                    hasSelectedItem = true;
                }

                if (hasSelectedItem && t != null)
                {
                    // determine the successful steps
                    TransferTypesChangesValidation validate = TransferTypesChangesValidation.Department |
                        TransferTypesChangesValidation.Description |
                        TransferTypesChangesValidation.Icon |
                        TransferTypesChangesValidation.Location;

                    if (t.Department > 0)
                        validate &= ~TransferTypesChangesValidation.Department;
                    if (t.Description != null && t.Description.Length > 0)
                        validate &= ~TransferTypesChangesValidation.Description;
                    if (t.Icon != null && t.Icon.Length > 0)
                        validate &= ~TransferTypesChangesValidation.Icon;
                    if (t.Location > 0)
                        validate &= ~TransferTypesChangesValidation.Location;

                    if (validate == 0)
                    {
                        bool tf = await DataChanges.ConfirmDialog($"Add New Transfer Type", $"Confirm to add {t.Description}");
                        if (tf)
                            await SendAddTransferType(t);
                    }
                    else
                    {
                        DataChangeAppliedAdminPage("Required fields", validate.ToString());
                    }
                }
                else
                {
                    await DataChanges.GenericDialog("Save Transfer Type", "The Transfer Type was not saved because the new row was not selected." +
                         "\nPlease try selecting the newly added row and save again.\nIf this does not help, cancel your work and try again.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Add wait time notification
        /// </summary>
        /// <returns></returns>
        private async Task SaveAddWaitTime()
        {
            try
            {
                WaitTimeNotify w = new WaitTimeNotify();
                bool hasSelectedItem = false;
                string reason_text = "Required fields";

                foreach (var obj in WaitNotifyDataGrid.SelectedItems)
                {
                    w = obj as WaitTimeNotify;
                    hasSelectedItem = true;
                }

                if (hasSelectedItem && w != null)
                {
                    // determine the successful steps
                    WaitTimeChangesValidation validate = WaitTimeChangesValidation.Mail |
                    WaitTimeChangesValidation.Category |
                    WaitTimeChangesValidation.MaxWaitTime;

                    if (DataTransType == DataTrans.Add)
                    {
                        string email = ValidateEmail.IsValidEmail(w.Mail);
                        if (email == "")
                        {
                            validate &= ~WaitTimeChangesValidation.Mail;
                        }
                        else
                        {
                            reason_text = $"{email} is not a valid email.";
                        }
                    }
                    if (w.Category > 0)
                        validate &= ~WaitTimeChangesValidation.Category;
                    if (w.MaxWaitTimeMinutes != 0)
                        validate &= ~WaitTimeChangesValidation.MaxWaitTime;

                    if (validate == 0)
                    {
                        bool tf = await DataChanges.ConfirmDialog($"Add New Wait Time?", $"Confirm to add {w.Mail}");
                        if (tf)
                            await SendAddWaitTime(w);
                    }
                    else
                    {
                        DataChangeAppliedAdminPage(reason_text, validate.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Add department
        /// </summary>
        /// <returns></returns>
        private async Task SaveAddDepartment()
        {
            try
            {
                Department d = new Department();

                bool hasSelectedItem = false;

                foreach (var obj in DepartmentsDataGrid.SelectedItems)
                {
                    d = obj as Department;
                    hasSelectedItem = true;
                }

                if (hasSelectedItem && d != null)
                {
                    // determine the successful steps
                    DepartmentChangesValidation validate = DepartmentChangesValidation.DepartmentName |
                        DepartmentChangesValidation.Id |
                        DepartmentChangesValidation.OrderBy |
                        DepartmentChangesValidation.Symbol |
                        DepartmentChangesValidation.SymbolType;

                    if (d.DepartmentName != null && d.DepartmentName.Length > 0)
                        validate &= ~DepartmentChangesValidation.DepartmentName;
                    if (d.Id > 0)
                        validate &= ~DepartmentChangesValidation.Id;
                    if (d.OrderBy > 0)
                        validate &= ~DepartmentChangesValidation.OrderBy;
                    if (d.Symbol != null && d.Symbol.Length > 0)
                        validate &= ~DepartmentChangesValidation.Symbol;
                    if (d.SymbolType != null && d.SymbolType.Length > 0)
                    {
                        d.SymbolType = d.SymbolType.ToUpper();
                        validate &= ~DepartmentChangesValidation.SymbolType;
                    }

                    if (validate == 0)
                    {
                        bool tf = await DataChanges.ConfirmDialog($"Add New Department", $"Confirm to add {d.DepartmentName}");
                        if (tf)
                            await SendAddDepartment(d);
                    }
                    else
                    {
                        DataChangeAppliedAdminPage("Required fields", validate.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Add group device
        /// </summary>
        /// <returns></returns>
        private async Task SaveAddGroupDevice()
        {
            try
            {
                GroupDevices g = new GroupDevices();

                bool hasSelectedItem = false;

                foreach (var obj in GroupDevicesDataGrid.SelectedItems)
                {
                    g = obj as GroupDevices;
                    hasSelectedItem = true;
                }

                if (hasSelectedItem && g != null)
                {
                    GroupDeviceChangesValidation validate = GroupDeviceChangesValidation.Kind |
                        GroupDeviceChangesValidation.Description |
                        GroupDeviceChangesValidation.Location |
                        GroupDeviceChangesValidation.Name;

                    if (g.Kind != null && g.Kind.Length > 0)
                        validate &= ~GroupDeviceChangesValidation.Kind;
                    if (g.Description != null && g.Description.Length > 0)
                        validate &= ~GroupDeviceChangesValidation.Description;
                    if (g.Location > 0)
                        validate &= ~GroupDeviceChangesValidation.Location;
                    if (g.Name != null && g.Name.Length > 0)
                        validate &= ~GroupDeviceChangesValidation.Name;

                    if (validate == 0)
                    {
                        bool tf = await DataChanges.ConfirmDialog($"Add New Group Device", $"Confirm to add {g.Description}");
                        if (tf)
                            await SendAddGroupDevice(g);
                    }
                    else
                    {
                        DataChangeAppliedAdminPage("Required fields", validate.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Save updated Wait Time
        /// </summary>
        /// <returns></returns>
        private async Task SaveUpdateWaitTime()
        {
            try
            {
                WaitTimeNotify w = new WaitTimeNotify();

                bool hasSelectedItem = false;
                string reason_text = "Required fields";

                foreach (var obj in WaitNotifyDataGrid.SelectedItems)
                {
                    w = obj as WaitTimeNotify;
                    hasSelectedItem = true;
                }

                if (hasSelectedItem && w != null)
                {
                    // determine the successful steps
                    WaitTimeChangesValidation validate = WaitTimeChangesValidation.Mail |
                        WaitTimeChangesValidation.Category |
                        WaitTimeChangesValidation.MaxWaitTime;

                    if (w.Mail.Length > 0)
                    {
                        string email = ValidateEmail.IsValidEmail(w.Mail);
                        if (email == "")
                        {
                            validate &= ~WaitTimeChangesValidation.Mail;
                        }
                        else
                        {
                            reason_text = $"{email} is not a valid email.";
                        }
                    }
                    if (w.Category > 0)
                        validate &= ~WaitTimeChangesValidation.Category;
                    if (w.MaxWaitTimeMinutes != 0)
                        validate &= ~WaitTimeChangesValidation.MaxWaitTime;

                    if (validate == 0)
                    {
                        bool tf = await DataChanges.ConfirmDialog($"Update Category Wait Time", $"Apply changes to category wait time {w.Category}");
                        if (tf)
                        {
                            DataTransType = DataTrans.Update;
                            await SendUpdateWaitTime(w);
                        }
                    }
                    else
                    {
                        DataChangeAppliedAdminPage(reason_text, validate.ToString());
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Save updated counter
        /// </summary>
        /// <returns></returns>
        private async Task SaveUpdateCounter()
        {
            try
            {
                Counter counter = new Counter();

                foreach (var obj in CountersDataGrid.SelectedItems)
                {
                    counter = obj as Counter;
                }
                if (counter != null)
                {
                    bool isValid = await ValidateCounter(counter);
                    if (isValid)
                    {
                        bool tf = await DataChanges.ConfirmDialog($"Update Counter", $"Apply changes to counter");
                        if (tf)
                        {
                            await SendUpdateCounter(counter);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Save updated group device
        /// </summary>
        /// <returns></returns>
        private async Task SaveUpdateGroupDevice()
        {
            try
            {
                GroupDevices g = new GroupDevices();

                bool hasSelectedItem = false;

                foreach (var obj in GroupDevicesDataGrid.SelectedItems)
                {
                    g = obj as GroupDevices;
                    hasSelectedItem = true;
                }

                if (hasSelectedItem && g != null)
                {
                    GroupDeviceChangesValidation validate = GroupDeviceChangesValidation.Kind |
                        GroupDeviceChangesValidation.Description |
                        GroupDeviceChangesValidation.Location |
                        GroupDeviceChangesValidation.Name;

                    if (g.Kind != null && g.Kind.Length > 0)
                        validate &= ~GroupDeviceChangesValidation.Kind;
                    if (g.Description != null && g.Description.Length > 0)
                        validate &= ~GroupDeviceChangesValidation.Description;
                    if (g.Location > 0)
                        validate &= ~GroupDeviceChangesValidation.Location;
                    if (g.Name != null && g.Name.Length > 0)
                        validate &= ~GroupDeviceChangesValidation.Name;

                    if (validate == 0)
                    {
                        bool tf = await DataChanges.ConfirmDialog($"Update Group Device", $"Apply changes to {g.Description}");
                        if (tf)
                            await SendUpdateGroupDevice(g);
                    }
                    else
                    {
                        DataChangeAppliedAdminPage("Required fields", validate.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Update department
        /// </summary>
        /// <returns></returns>
        private async Task SaveUpdateDepartment()
        {
            try
            {
                Department d = new Department();

                bool hasSelectedItem = false;

                foreach (var obj in DepartmentsDataGrid.SelectedItems)
                {
                    d = obj as Department;
                    hasSelectedItem = true;
                }

                if (hasSelectedItem && d != null)
                {
                    // determine the successful steps
                    DepartmentChangesValidation validate = DepartmentChangesValidation.DepartmentName |
                        DepartmentChangesValidation.Id |
                        DepartmentChangesValidation.OrderBy |
                        DepartmentChangesValidation.Symbol |
                        DepartmentChangesValidation.SymbolType;

                    if (d.DepartmentName != null && d.DepartmentName.Length > 0)
                        validate &= ~DepartmentChangesValidation.DepartmentName;
                    if (d.Id > 0)
                        validate &= ~DepartmentChangesValidation.Id;
                    if (d.OrderBy > 0)
                        validate &= ~DepartmentChangesValidation.OrderBy;
                    if (d.Symbol != null && d.Symbol.Length > 0)
                        validate &= ~DepartmentChangesValidation.Symbol;
                    if (d.SymbolType != null && d.SymbolType.Length > 0)
                    {
                        d.SymbolType.ToUpper();
                        validate &= ~DepartmentChangesValidation.SymbolType;
                    }

                    if (validate == 0)
                    {
                        bool tf = await DataChanges.ConfirmDialog($"Update Department", $"Apply changes to department");
                        if (tf)
                            await SendUpdateDepartment(d);
                    }
                    else
                    {
                        DataChangeAppliedAdminPage("Required fields", validate.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Save updated Transfer Type
        /// </summary>
        /// <returns></returns>
        private async Task SaveUpdateTransferType()
        {
            try
            {
                Transfers t = new Transfers();

                bool hasSelectedItem = false;

                foreach (var obj in TransferTypesDataGrid.SelectedItems)
                {
                    t = obj as Transfers;
                    hasSelectedItem = true;
                }

                if (hasSelectedItem && t != null)
                {
                    // determine the successful steps
                    TransferTypesChangesValidation validate = TransferTypesChangesValidation.Department |
                        TransferTypesChangesValidation.Description |
                        TransferTypesChangesValidation.Icon |
                        TransferTypesChangesValidation.Location;

                    if (t.Department > 0)
                        validate &= ~TransferTypesChangesValidation.Department;
                    if (t.Description != null && t.Description.Length > 0)
                        validate &= ~TransferTypesChangesValidation.Description;
                    if (t.Icon != null && t.Icon.Length > 0)
                        validate &= ~TransferTypesChangesValidation.Icon;
                    if (t.Location > 0)
                        validate &= ~TransferTypesChangesValidation.Location;

                    if (validate == 0)
                    {
                        bool tf = await DataChanges.ConfirmDialog($"Update Transfer Type", $"Apply changes to transfer type");
                        if (tf)
                            await SendUpdateTransferType(t);
                    }
                    else
                    {
                        DataChangeAppliedAdminPage("Required fields", validate.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// SelectionChanged for all datagrids
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (DataTransType != DataTrans.Add &&
                    DataTransType != DataTrans.Update &&
                    DataTransType != DataTrans.Delete)
                {
                    DataTransType = DataTrans.None;

                    string row = "";

                    switch (CurrentVisiblePanel)
                    {
                        case DataContextItem.Users:

                            if (VsisUsersDataGrid.SelectedIndex > -1)
                            {
                                VsisUser user = vsisUsersCollection[VsisUsersDataGrid.SelectedIndex];
                                if (user != null)
                                    row = user.FullName;
                            }
                            break;

                        case DataContextItem.Counters:

                            if (CountersDataGrid.SelectedIndex > -1)
                            {
                                Counter counter = counterCollection[CountersDataGrid.SelectedIndex];
                                if (counter != null)
                                    row = counter.Description;
                            }
                            break;

                        case DataContextItem.Categories:

                            if (CategoriesDataGrid.SelectedIndex > -1)
                            {
                                Category category = categoryCollection[CategoriesDataGrid.SelectedIndex];
                                if (category != null)
                                    row = category.Description;
                            }
                            break;

                        case DataContextItem.WaitTimes:

                            if (WaitNotifyDataGrid.SelectedIndex > -1)
                            {
                                WaitTimeNotify wait = waitTimesCollection[WaitNotifyDataGrid.SelectedIndex];
                                if (wait != null)
                                    row = wait.Category.ToString();
                            }
                            break;

                        case DataContextItem.GroupDevices:

                            if (GroupDevicesDataGrid.SelectedIndex > -1)
                            {
                                GroupDevices devices = groupDevicesCollection[GroupDevicesDataGrid.SelectedIndex];
                                if (devices != null)
                                    row = devices.Description;
                            }
                            break;

                        case DataContextItem.Departments:

                            if (DepartmentsDataGrid.SelectedIndex > -1)
                            {
                                Department dept = departmentCollection[DepartmentsDataGrid.SelectedIndex];
                                if (dept != null)
                                    row = dept.DepartmentName;
                            }
                            break;

                        case DataContextItem.TransferTypes:

                            if (TransferTypesDataGrid.SelectedIndex > -1)
                            {
                                Transfers transfer = transferTypesCollection[TransferTypesDataGrid.SelectedIndex];
                                if (transfer != null)
                                    row = transfer.Description;
                            }
                            break;
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

                    int index = e.Row.GetIndex();

                    switch (CurrentVisiblePanel)
                    {
                        case DataContextItem.Users:

                            VsisUser v = vsisUsersCollection[index];
                            if (v.AuthName.Length > 0)
                            {
                                edit_row = true;
                                row = $"{v.AuthName}";
                            }
                            break;

                        case DataContextItem.Counters:

                            Counter counter = counterCollection[index];
                            if (counter.Description.Length > 0)
                            {
                                edit_row = true;
                                row = $"{counter.Description}";
                            }
                            break;

                        case DataContextItem.Categories:

                            Category c = categoryCollection[index];
                            if (c.Id > 0)
                            {
                                edit_row = true;
                                row = $"{c.Id.ToString()}";
                            }
                            break;

                        case DataContextItem.WaitTimes:

                            WaitTimeNotify w = waitTimesCollection[index];
                            if (w.Category > 0)
                            {
                                edit_row = true;
                                row = $"{w.Category.ToString()}";
                            }
                            break;

                        case DataContextItem.GroupDevices:

                            GroupDevices g = groupDevicesCollection[index];
                            if (g.Id > 0)
                            {
                                edit_row = true;
                                row = $"{g.Id.ToString()}";
                            }
                            break;

                        case DataContextItem.Departments:

                            Department d = departmentCollection[index];
                            if (d.Id > 0)
                            {
                                edit_row = true;
                                row = $"{d.Id.ToString()}";
                            }
                            break;

                        case DataContextItem.TransferTypes:

                            Transfers t = transferTypesCollection[index];
                            if (t.Id > 0)
                            {
                                edit_row = true;
                                row = $"{t.Id.ToString()}";
                            }
                            break;
                    }
                    if (edit_row)
                    {
                        SubTitleTextBlock = PanelTitleTextArea(CurrentVisiblePanel);

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

        /// <summary>
        /// Adjust title width based on visible panel
        /// </summary>
        /// <param name="panel"></param>
        /// <returns></returns>
        private double PanelTitleTextMaxWidth(DataContextItem dci)
        {
            double d = 0.0;

            switch (dci)
            {
                case DataContextItem.Users:
                    d = 1100;
                    break;
                case DataContextItem.Departments:
                    d = 750;
                    break;
                case DataContextItem.Counters:
                    d = 1300;
                    break;
                case DataContextItem.Categories:
                    d = 1000;
                    break;
                case DataContextItem.WaitTimes:
                    d = 900;
                    break;
                case DataContextItem.GroupDevices:
                    d = 1200;
                    break;
                case DataContextItem.TransferTypes:
                    d = 900;
                    break;
            }
            return d;
        }

        /// <summary>
        /// Title text verbiage by visible panel and data trans mode
        /// </summary>
        /// <param name="panel"></param>
        /// <returns></returns>
        private string PanelTitleTextArea(DataContextItem dci)
        {
            try
            {
                switch (dci)
                {
                    case DataContextItem.Users:

                        switch (DataTransType)
                        {
                            case DataTrans.None:

                                return $"This table permits you to Add, Update, or Delete user accounts. " +
                                    $"Use the data buttons at the bottom right of the table to make changes. " +
                                    $"Click the '+' button to add a new user account. To delete a user account, first click on the row you want to delete, " +
                                    $"then click the '-' button. A confirmation dialog will appear. To edit a user account, double-click the table cell " +
                                    $"for the user that you want to change. The cell will become editable. Tab to another cell on the same row before saving. Continue to make any other changes for the selected user. " +
                                    $"Only one row may be edited at a single time. Click the Save button to save your changes, or click the Cancel button to undo.";

                            case DataTrans.Add:

                                return $"A new row was inserted at the end of the table. Double-click cells to add values, or " +
                                    $"select from a list. Some values were added for you. Refer to the Categories table to calculate the sum of categories the new user supports.";

                            case DataTrans.Update:

                                return $"The Windows User Account name cannot be changed. You should create a new user account and then delete the obsolete account. " +
                                    $"Only one row may be edited at a time. Click Save to commit your changes, or click the Cancel button to undo.";

                            case DataTrans.Delete:
                                return "Note: Deleting a user account does not delete their user statistics.";
                        }
                        break;
                    case DataContextItem.Departments:

                        switch (DataTransType)
                        {
                            case DataTrans.None:

                                return $"This table permits you to Add, Update, or Delete departments. " +
                                    $"Use the buttons located at the bottom right of the table. " +
                                    $"Click the '+' button to add a new department. To delete a department, first click on the row you want to delete, " +
                                    $"then click the '-' button. A confirmation dialog will appear. To edit a department, double-click the table cell " +
                                    $"for the department that you want to change. The cell will become editable or may reveal a selection list. Tab to another cell on the same row before saving." +
                                    $"Only one row may be edited at a time. Click Save to commit your changes, or click the Cancel button to undo.";

                            case DataTrans.Add:

                                return $"A new row was inserted at the end of the table. Double-click cells to add values, or " +
                                    $"select from a list. Some values were added for you.";

                            case DataTrans.Update:

                                return $"Only one row may be edited at a time. Click Save to commit your changes, or click the Cancel button to undo.";

                            case DataTrans.Delete:
                                return "Note: Make sure to remove references to the deleted department in tables Users Transfers.";
                        }
                        break;

                    case DataContextItem.Counters:

                        switch (DataTransType)
                        {
                            case DataTrans.None:

                                return $"This table permits you to Add, Update, or Delete user accounts. " +
                                    $"Use the buttons located at the bottom right of the table. " +
                                    $"Click the '+' button to add a new user account. To delete a user account, first click on the row you want to delete, " +
                                    $"then click the '-' button. A confirmation dialog will appear. To edit a user account, double-click the table cell " +
                                    $"for the user account that you want to change. The cell will become editable or may reveal a selection list. Tab to another cell on the same row before saving." +
                                    $"Only one row may be edited at a time. Click Save to commit your changes, or click the Cancel button to undo.";

                            case DataTrans.Add:

                                return $"A new row was inserted at the end of the table. Double-click cells to add values, or " +
                                    $"select from a list. Some values were added for you. Refer to the Categories table to calculate the sum of categories the new user supports.";

                            case DataTrans.Update:

                                return $"Only one row may be edited at a time. Click Save to commit your changes, or click the Cancel button to undo.";

                            case DataTrans.Delete:
                                return "";
                        }
                        break;

                    case DataContextItem.Categories:

                        switch (DataTransType)
                        {
                            case DataTrans.None:

                                return $"This table permits you to Add, Update, or Delete user categories. " +
                                    $"Use the buttons located at the bottom right of the table. " +
                                    $"Click the '+' button to add a new category. To delete a category, first click on the row you want to delete, " +
                                    $"then click the '-' button. A confirmation dialog will appear. To edit a category, double-click the table cell " +
                                    $"for the category you want to change. The cell will become editable or may reveal a selection list. Tab to another cell on the same row before saving." +
                                    $"Only one row may be edited at a time. Click Save to commit your changes, or click the Cancel button to undo. " +
                                    $"Warning: Categories provide the logic to the Visitor Sign In System. Contact the IT department for guidance.";

                            case DataTrans.Add:

                                return $"A new row was inserted at the end of the table. Double-click cells to add values, or " +
                                    $"select from a list. Some values were added for you. Warning: Categories provide the logic to the Visitor Sign In System. " +
                                    $"Changing or adding data in this table can impact the system and cause the system to become inoperable. It is" +
                                    $"best to make changes outside of business hours to verify your changes. Contact the IT department for guidance.";

                            case DataTrans.Update:

                                return $"Warning: Categories provide the logic to the Visitor Sign In System. " +
                                    $"Changing or adding data in this table can impact the system and cause the system to become inoperable. It is" +
                                    $"best to make changes outside of business hours to verify that the category is working correctly. Contact the IT department for guidance.";

                            case DataTrans.Delete:
                                return $"Warning: Categories provide the logic to the Visitor Sign In System. " +
                                    $"Removing a category may corrupt users, counters, and wait time notifications. " +
                                    $"Please perform changes to this table outside of business hours. Contact the IT department for guidance.";
                        }
                        break;

                    case DataContextItem.WaitTimes:

                        switch (DataTransType)
                        {
                            case DataTrans.None:

                                return $"This table permits you to Add, Update, or Delete Wait Time Notifications. " +
                                    $"(Wait Time Notifications are Emails sent to managers whenever a visitor's wait time " +
                                    $"exceeds the value in the Max Wait Time Minutes column). Use the buttons located at the bottom right of the table. " +
                                    $"Click the '+' button to add a new Wait Time Notification. To delete a Wait Time Notification, " +
                                    $"first click on the row you want to delete, then click the '-' button. A confirmation dialog will appear. " +
                                    $"To edit a Wait Time Notification, double-click the table cell " +
                                    $"for the Wait Time Notification that you want to change. The cell will become editable or may reveal a selection list. Tab to another cell on the same row before saving." +
                                    $"Only one row may be edited at a time. Click Save to commit your changes, or click the Cancel button to undo.";

                            case DataTrans.Add:

                                return $"A new row was inserted at the end of the table. Double-click cells to add values, or " +
                                    $"select from a list. Some values were added for you.\nNote: Before creating a new Wait Time Notification, the Category must already exist.";

                            case DataTrans.Update:

                                return $"Only one row may be edited at a time.\nNote: Make sure to use only existing Categories. Refer to the Categories table." +
                                    $"Click Save to commit your changes, or click the Cancel button to undo.";

                            case DataTrans.Delete:
                                return "";
                        }
                        break;

                    case DataContextItem.GroupDevices:

                        switch (DataTransType)
                        {
                            case DataTrans.None:

                                return $"This table permits you to Add, Update, or Delete devices such as Displays and Kiosks. " +
                                    $"Use the buttons located at the bottom right of the table. " +
                                    $"Click the '+' button to add a new device. To delete a device, first click on the row you want to delete, " +
                                    $"then click the '-' button. A confirmation dialog will appear. To edit a device, double-click the table cell " +
                                    $"for the device that you want to change. The cell will become editable or may reveal a selection list. Tab to another cell on the same row before saving." +
                                    $"Only one row may be edited at a time. Click Save to commit your changes, or click the Cancel button to undo.";

                            case DataTrans.Add:

                                return $"A new row was inserted at the end of the table. Double-click cells to add values, or " +
                                    $"select from a list. Some values were added for you.";

                            case DataTrans.Update:

                                return $"Only one row may be edited at a time. Click Save to commit your changes, or click the Cancel button to undo.";

                            case DataTrans.Delete:
                                return "";
                        }
                        break;

                    case DataContextItem.TransferTypes:

                        switch (DataTransType)
                        {
                            case DataTrans.None:

                                return $"This table permits you to Add, Update, or Delete transfer types. " +
                                    $"Use the buttons located at the bottom right of the table. " +
                                    $"Click the '+' button to add a new transfer type. To delete a transfer type, first click on the row you want to delete, " +
                                    $"then click the '-' button. A confirmation dialog will appear. To edit a transfer type, double-click the table cell " +
                                    $"for the transfer type that you want to change. The cell will become editable or may reveal a selection list. Tab to another cell on the same row before saving." +
                                    $"Only one row may be edited at a time. Click Save to commit your changes, or click the Cancel button to undo.";

                            case DataTrans.Add:

                                return $"A new row was inserted at the end of the table. Double-click cells to add values, or " +
                                    $"select from a list. Some values were added for you. Refer to the Categories table to find the category your new transfer type supports.";

                            case DataTrans.Update:

                                return $"Only one row may be edited at a time. Click Save to commit your changes, or click the Cancel button to undo.";

                            case DataTrans.Delete:
                                return "";
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return "";
        }
        private void DataGridReadWrite()
        {
            VsisUsersDataGrid.IsReadOnly = !_isAuthenticated;
            WaitNotifyDataGrid.IsReadOnly = !_isAuthenticated;
            CategoriesDataGrid.IsReadOnly = !_isAuthenticated;
            CountersDataGrid.IsReadOnly = !_isAuthenticated;
            GroupDevicesDataGrid.IsReadOnly = !_isAuthenticated;
            DepartmentsDataGrid.IsReadOnly = !_isAuthenticated;
        }


        public DataTrans DataTransType {

            get
            {
                return _dataTransType;
            }
            set
            {
                //((App)Application.Current).DataTransType = (App.DataTrans)value;

                this.Set<DataTrans>(ref _dataTransType, value);
            }
        }


        public string HeaderTitleTextBlock
        {
            get
            {
                return _HeaderTitleTextBlock;
            }
            set { this.Set<string>(ref _HeaderTitleTextBlock, value); }
        }

        public string HeaderSubTitleTextBlock
        {
            get
            {
                return _HeaderSubTitleTextBlock;
            }
            set { this.Set<string>(ref _HeaderSubTitleTextBlock, value); }
        }

        public string TitleTextBlock
        {
            get
            {
                return _TitleTextBlock;
            }
            set { this.Set<string>(ref _TitleTextBlock, value); }
        }

        public string SubTitleTextBlock
        {
            get
            {
                return _SubTitleTextBlock;
            }
            set { this.Set<string>(ref _SubTitleTextBlock, value); }
        }

        public double SubTitleTextBlockMaxWidth
        {
            get
            {
                return _SubTitleTextBlockMaxWidth;
            }
            set { this.Set<double>(ref _SubTitleTextBlockMaxWidth, value); }
        }

        public string SubTextBlock
        {
            get
            {
                return _SubTextBlock;
            }
            set { this.Set<string>(ref _SubTextBlock, value); }
        }

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


        //public string DataChangeResultGlyph
        //{
        //    get
        //    {
        //        return _dataChangeResultGlyph;
        //    }
        //    set { this.Set<string>(ref _dataChangeResultGlyph, value); }
        //}
        //public Brush DataChangeResultGlyphForeground
        //{
        //    get
        //    {
        //        return _dataChangeResultGlyphForeground;
        //    }
        //    set
        //    {
        //        this.Set<Brush>(ref _dataChangeResultGlyphForeground, value);
        //    }
        //}
        //public string DataChangeResultTextBlock
        //{
        //    get
        //    {
        //        return _dataChangeResultTextBlock;
        //    }
        //    set { this.Set<string>(ref _dataChangeResultTextBlock, value); }
        //}


        //[Conditional("DEBUG")]
        private async Task AuthenticateUser()
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

                        EnableEditMode(true);

                        DeleteRowButton.Tapped += DeleteRowButton_Tapped;

                        CreateDataGridEvents();
                    }
                    DataGridReadWrite();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void CreateDataGridEvents()
        {
            VsisUsersDataGrid.SelectionChanged += DataGrid_SelectionChanged;
            VsisUsersDataGrid.BeginningEdit += DataGrid_BeginningEdit;

            WaitNotifyDataGrid.SelectionChanged += DataGrid_SelectionChanged;
            WaitNotifyDataGrid.BeginningEdit += DataGrid_BeginningEdit;

            CategoriesDataGrid.SelectionChanged += DataGrid_SelectionChanged;
            CategoriesDataGrid.BeginningEdit += DataGrid_BeginningEdit;

            CountersDataGrid.SelectionChanged += DataGrid_SelectionChanged;
            CountersDataGrid.BeginningEdit += DataGrid_BeginningEdit;

            GroupDevicesDataGrid.SelectionChanged += DataGrid_SelectionChanged;
            GroupDevicesDataGrid.BeginningEdit += DataGrid_BeginningEdit;

            DepartmentsDataGrid.SelectionChanged += DataGrid_SelectionChanged;
            DepartmentsDataGrid.BeginningEdit += DataGrid_BeginningEdit;

            TransferTypesDataGrid.SelectionChanged += DataGrid_SelectionChanged;
            TransferTypesDataGrid.BeginningEdit += DataGrid_BeginningEdit;
        }

        private void RemoveDataGridEvents()
        {
            VsisUsersDataGrid.SelectionChanged -= DataGrid_SelectionChanged;
            VsisUsersDataGrid.BeginningEdit -= DataGrid_BeginningEdit;

            WaitNotifyDataGrid.SelectionChanged -= DataGrid_SelectionChanged;
            WaitNotifyDataGrid.BeginningEdit -= DataGrid_BeginningEdit;

            CategoriesDataGrid.SelectionChanged -= DataGrid_SelectionChanged;
            CategoriesDataGrid.BeginningEdit -= DataGrid_BeginningEdit;

            CountersDataGrid.SelectionChanged -= DataGrid_SelectionChanged;
            CountersDataGrid.BeginningEdit -= DataGrid_BeginningEdit;

            GroupDevicesDataGrid.SelectionChanged -= DataGrid_SelectionChanged;
            GroupDevicesDataGrid.BeginningEdit -= DataGrid_BeginningEdit;

            DepartmentsDataGrid.SelectionChanged -= DataGrid_SelectionChanged;
            DepartmentsDataGrid.BeginningEdit -= DataGrid_BeginningEdit;

            TransferTypesDataGrid.SelectionChanged -= DataGrid_SelectionChanged;
            TransferTypesDataGrid.BeginningEdit -= DataGrid_BeginningEdit;
        }

        /// <summary>
        /// Show\Hide edit controls.
        /// Delete button is also set in
        /// DataGrid SelectionChanged
        /// </summary>
        /// <param name="tf"></param>
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

                    AddRowButton.Visibility = Visibility.Visible;

                    DeleteRowButton.Visibility = Visibility.Visible;
                    DeleteRowButton.Foreground = paoDisabled;

                    EditModeTextBlock = "[EDIT MODE]";
                }
                else
                {
                    ((App)Application.Current).IsAuthenticated = false;
                    _isAuthenticated = false;
                    Glyph = "\xE72E";

                    AddRowButton.Visibility = Visibility.Collapsed;

                    ReadOnlyLockIcon.Foreground = paoSteelBlue;

                    EditModeTextBlock = "";
                }
                DataGridReadWrite();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async void ReadOnlyLockIcon_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // leave edit mode
            if (_isAuthenticated)
            {
                DoCancel();

                EnableEditMode(false);

                RemoveDataGridEvents();
            }
            else
            {
                await AuthenticateUser();
            }
        }

        private void VsisUsersDataGrid_Sorting(object sender, DataGridColumnEventArgs e)
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
                            case "AuthName":
                                VsisUsersDataGrid.ItemsSource = new ObservableCollection<VsisUser>(from item in vsisUsersCollection
                                                                                                      orderby item.AuthName ascending
                                                                                                      select item);
                                break;
                            case "FullName":
                                VsisUsersDataGrid.ItemsSource = new ObservableCollection<VsisUser>(from item in vsisUsersCollection
                                                                                                      orderby item.FullName ascending
                                                                                                      select item);
                                break;
                            case "LastName":
                                VsisUsersDataGrid.ItemsSource = new ObservableCollection<VsisUser>(from item in vsisUsersCollection
                                                                                                      orderby item.LastName ascending
                                                                                                      select item);
                                break;
                            case "Department":
                                VsisUsersDataGrid.ItemsSource = new ObservableCollection<VsisUser>(from item in vsisUsersCollection
                                                                                                      orderby item.Department ascending
                                                                                                      select item);
                                break;
                            case "Categories":
                                VsisUsersDataGrid.ItemsSource = new ObservableCollection<VsisUser>(from item in vsisUsersCollection
                                                                                                      orderby item.Categories ascending
                                                                                                      select item);
                                break;
                            case "Active":
                                VsisUsersDataGrid.ItemsSource = new ObservableCollection<VsisUser>(from item in vsisUsersCollection
                                                                                                      orderby item.Active ascending
                                                                                                      select item);
                                break;
                            case "Roles":
                                VsisUsersDataGrid.ItemsSource = new ObservableCollection<VsisUser>(from item in vsisUsersCollection
                                                                                                      orderby item.Role ascending
                                                                                                      select item);
                                break;
                            case "Location":
                                VsisUsersDataGrid.ItemsSource = new ObservableCollection<VsisUser>(from item in vsisUsersCollection
                                                                                                      orderby item.Location ascending
                                                                                                      select item);
                                break;
                            case "Created":
                                VsisUsersDataGrid.ItemsSource = new ObservableCollection<VsisUser>(from item in vsisUsersCollection
                                                                                                      orderby item.Created ascending
                                                                                                      select item);
                                break;
                        }
                        e.Column.SortDirection = DataGridSortDirection.Ascending;
                    }
                    else
                    {
                        switch (e.Column.Tag)
                        {

                            case "AuthName":
                                VsisUsersDataGrid.ItemsSource = new ObservableCollection<VsisUser>(from item in vsisUsersCollection
                                                                                                      orderby item.AuthName descending
                                                                                                      select item);
                                break;
                            case "FullName":
                                VsisUsersDataGrid.ItemsSource = new ObservableCollection<VsisUser>(from item in vsisUsersCollection
                                                                                                      orderby item.FullName descending
                                                                                                      select item);
                                break;
                            case "LastName":
                                VsisUsersDataGrid.ItemsSource = new ObservableCollection<VsisUser>(from item in vsisUsersCollection
                                                                                                      orderby item.LastName descending
                                                                                                      select item);
                                break;
                            case "Department":
                                VsisUsersDataGrid.ItemsSource = new ObservableCollection<VsisUser>(from item in vsisUsersCollection
                                                                                                      orderby item.Department descending
                                                                                                      select item);
                                break;
                            case "Categories":
                                VsisUsersDataGrid.ItemsSource = new ObservableCollection<VsisUser>(from item in vsisUsersCollection
                                                                                                      orderby item.Categories descending
                                                                                                      select item);
                                break;
                            case "Active":
                                VsisUsersDataGrid.ItemsSource = new ObservableCollection<VsisUser>(from item in vsisUsersCollection
                                                                                                   orderby item.Active descending
                                                                                                   select item);
                                break;
                            case "Roles":
                                VsisUsersDataGrid.ItemsSource = new ObservableCollection<VsisUser>(from item in vsisUsersCollection
                                                                                                      orderby item.Role descending
                                                                                                      select item);
                                break;
                            case "Location":
                                VsisUsersDataGrid.ItemsSource = new ObservableCollection<VsisUser>(from item in vsisUsersCollection
                                                                                                      orderby item.Location descending
                                                                                                      select item);
                                break;
                            case "Created":
                                VsisUsersDataGrid.ItemsSource = new ObservableCollection<VsisUser>(from item in vsisUsersCollection
                                                                                                      orderby item.Created descending
                                                                                                      select item);
                                break;
                        }
                        e.Column.SortDirection = DataGridSortDirection.Descending;
                    }

                    // add code to handle sorting by other columns as required

                    // Remove sorting indicators from other columns
                    foreach (var dgColumn in VsisUsersDataGrid.Columns)
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
    }
}
