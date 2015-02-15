using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;
using EPriceProviderServices.Helpers;
using EPriceProviderServices.MerlionDataService;
using EPriceProviderServices.Models;
using EPriceProviderServices.OcsDataService;
using EPriceProviderServices.OldiDataService;
using EPriceRequestServiceDBEngine;
using EPriceRequestServiceDBEngine.Enums;
using EPriceRequestServiceDBEngine.Models;
using CatalogResult = EPriceProviderServices.MerlionDataService.CatalogResult;
using MessageBox = System.Windows.MessageBox;
using TreeView = System.Windows.Controls.TreeView;

namespace EPriceProviderServices
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields

        private readonly DataLevelBase _merlionData;
        private MerlionServiceWrapper _merlionService;
        private readonly DataLevelBase _ocsData;
        private OcsServiceWrapper _ocsService;
        private readonly DataLevelBase _treolanData;
        private TreolanServiceWrapper _treolanService;
        private readonly DataLevelBase _oldiData;
        private OldiServiceWrapper _oldiService;
        private List<Un1tCategory> _un1tCategories;
        private const string LogSender = "Main App";
        private const string ErrorMessage =
            "Произошла ошибка, описание ошибки файле Log.txt.\nРекомендуется закрыть приложение во избежание нестабильной работы";
        private int _productLoaderCounter;
        private int _priceLoaderCounter;
        private int _propertyLoaderCounter;
        private int _directLoaderCounter;
        private int _catalogLoaderCounter;
        private bool _scheduleLoadRunning;
        private bool _loadingData;
        private readonly object _lockObj;
        private System.Threading.Timer _autoLoaderTimer;
        private readonly Dictionary<ProviderType, List<Un1tProductBase>> _providerProducts;
        private readonly Dictionary<ProviderType, List<Un1tProductBase>> _providerNewProducts;
        private NotifyIcon _trayIcon;
        private bool _isClose;
        private TreeView _activeProviderTreeView;
        private readonly Dictionary<ProviderType, List<ProviderCategoryViewModel>> _providerCategoriesViewModels;
        private bool _firstLoadCategory;
        private Un1tCategoryItemView _selectedUn1tCategoryView;
        private readonly List<Un1tCategoryViewModel> _un1tCategoryViewModels;
        private readonly int ThreadCount;
        private int _propertiesSaveCounter;
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            try
            {
                if (!IsSingleProcess())
                {
                    MessageBox.Show("В системе уже запущен экземпляр Provider Service");
                    _isClose = true;
                    Close();
                }
                ThreadCount = int.Parse(ConfigurationManager.AppSettings.Get("ThreadCount"));
                _firstLoadCategory = true;
                _isClose = false;
                _loadingData = false;
                _directLoaderCounter = 0;
                _priceLoaderCounter = 0;
                _productLoaderCounter = 0;
                _propertyLoaderCounter = 0;
                _scheduleLoadRunning = false;
                _lockObj = new object();
                _providerProducts = new Dictionary<ProviderType, List<Un1tProductBase>>();
                _providerNewProducts = new Dictionary<ProviderType, List<Un1tProductBase>>();
                _providerCategoriesViewModels = new Dictionary<ProviderType, List<ProviderCategoryViewModel>>();
                _un1tCategoryViewModels = new List<Un1tCategoryViewModel>();
                _merlionData = new MerlionData();
                _merlionData.CategoryLoadStateChanged += MerlionDataOnCategoryLoadStateChanged;
                _merlionData.CategoryMapDeleted += MerlionDataOnCategoryMapDeleted;
                _ocsData = new OcsData();
                _ocsData.CategoryLoadStateChanged += OcsDataOnCategoryLoadStateChanged;
                _ocsData.CategoryMapDeleted += OcsDataOnCategoryMapDeleted;
                _treolanData = new TreolanData();
                _treolanData.CategoryLoadStateChanged += TreolanDataOnCategoryLoadStateChanged;
                _treolanData.CategoryMapDeleted += TreolanDataOnCategoryMapDeleted;
                _oldiData = new OldiData();
                _oldiData.CategoryLoadStateChanged += OldiDataOnCategoryLoadStateChanged;
                _oldiData.CategoryMapDeleted += OldiDataOnCategoryMapDeleted;
                _merlionService = new MerlionServiceWrapper();
                _treolanService = new TreolanServiceWrapper();
                _ocsService = new OcsServiceWrapper();
                _oldiService = new OldiServiceWrapper();
                var treeViews = new[]
                {
                    MerlionCategoriesView, OldiCategoriesView, OcsCategoriesView, TreolanCategoriesView,
                    Un1tCategoriesView
                };
                foreach (var treeView in treeViews)
                {
                    ClearTreeViewItems(treeView);
                }
                btnNewUn1tCategory.IsEnabled = false;
                btnEditUn1tCategory.IsEnabled = false;
                btnDeleteUn1tCategory.IsEnabled = false;
                MerlionCategoriesView.Tag = ProviderType.Merlion;
                TreolanCategoriesView.Tag = ProviderType.Treolan;
                OcsCategoriesView.Tag = ProviderType.OCS;
                OldiCategoriesView.Tag = ProviderType.OLDI;
                _activeProviderTreeView = MerlionCategoriesView;
            }
            catch (Exception ex)
            {
                _merlionData = null;
                _oldiData = null;
                _ocsData = null;
                _treolanData = null;
                Log.WriteToLog(LogSender + " Конструктор() ", ex.Message);
                MessageBox.Show(ErrorMessage);
            }
        }

        private bool IsSingleProcess()
        {
            var processName = Process.GetCurrentProcess().ProcessName;
            var isNotSingle = Process.GetProcessesByName(processName).Count() > 1;
            return !isNotSingle;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            CreateTreyIcon();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (!_isClose)
            {
                e.Cancel = true;
                Hide();
            }
            else
            {
                if (_trayIcon == null) return;
                _trayIcon.Visible = false;
            }
        }    

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                txtStatus.Text = "Загрузка каталогов...";
                _catalogLoaderCounter = 4;
                await Task.Run(() =>
                {
                    try
                    {
                        GetMerlionData();
                        GetOcsData();
                        GetTreolanData();
                        GetOldiData();
                    }
                    catch (Exception ex)
                    {
                        Log.WriteToLog(LogSender + " Window_Loaded() ", "Ошибка: " + ex.Message);
                    }
                });
                btnNewUn1tCategory.IsEnabled = true;
                btnEditUn1tCategory.IsEnabled = true;
                btnDeleteUn1tCategory.IsEnabled = true;
            }
            catch (Exception ex)
            {
                Log.WriteToLog(LogSender + " Window_Loaded() ", ex.Message);
                MessageBox.Show(ErrorMessage);
            }

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_isClose) return;
            if (_merlionService != null)
            {
                _merlionService.Dispose();
            }
            if (_ocsService != null)
            {
                _ocsService.Dispose();
            }
            if (_treolanService != null)
            {
                _treolanService.Dispose();
            }
            if (_oldiService != null)
            {
                _oldiService.Dispose();
            }
        }

        #region Trey Icon

        private void MenuExitClick(object sender, RoutedEventArgs e)
        {
            _isClose = true;
            Close();
        }

        private bool CreateTreyIcon()
        {
            var result = false;
            if (_trayIcon == null)
            {
                _trayIcon = new NotifyIcon();
                _trayIcon.Icon = new System.Drawing.Icon("mainapp.ico");
                _trayIcon.Text = "Provider Service";
                var trayMeny = Resources["TrayMenu"] as System.Windows.Controls.ContextMenu;
                _trayIcon.Click += (sender, e) =>
                {
                    if (e != null)
                    {
                        if ((e as System.Windows.Forms.MouseEventArgs).Button == System.Windows.Forms.MouseButtons.Left)
                        {
                            ShowHideMainWindow(sender, null);
                        }
                        else
                        {
                            trayMeny.IsOpen = true;
                            Activate();
                        }
                    }
                };
                result = true;
            }
            else
            {
                result = true;
            }
            _trayIcon.Visible = true;
            return result;
        }

        private void ShowHideMainWindow(object sender, RoutedEventArgs e)
        {
            if (IsVisible)
            {
                Hide();
            }
            else
            {
                Show();
                WindowState = System.Windows.WindowState.Normal;
                Activate();
            }
        }

        #endregion

        #region Catalogs

        #region Load Catalogs

        private async Task GetOldiData()
        {
            try
            {
                await FillOldiCatalogs();
                Dispatcher.Invoke(() =>
                {
                    txtStatus.Text = "Каталог OLDI загружен";
                });
            }
            finally
            {
                var isComplete = Interlocked.Decrement(ref _catalogLoaderCounter) == 0;
                if (isComplete) CatalogsLoadComplete();
            }
        }

        private async Task GetMerlionData()
        {
            try
            {
                await FillMerlionCatalogs();
                Dispatcher.Invoke(() =>
                {
                    txtStatus.Text = "Каталог Merlion загружен";
                });
            }
            finally
            {
                var isComplete = Interlocked.Decrement(ref _catalogLoaderCounter) == 0;
                if (isComplete) CatalogsLoadComplete();
            }
        }

        private async Task GetOcsData()
        {
            try
            {
                await FillOcsCatalogs();
                Dispatcher.Invoke(() =>
                {
                    txtStatus.Text = "Каталог OCS загружен";
                });
            }
            finally
            {
                var isComplete = Interlocked.Decrement(ref _catalogLoaderCounter) == 0;
                if (isComplete) CatalogsLoadComplete();
            }
        }

        private async Task GetTreolanData()
        {
            try
            {
                await FillTreolanCatalogs();
                Dispatcher.Invoke(() =>
                {
                    txtStatus.Text = "Каталог Treolan загружен";
                });
            }
            finally
            {
                var isComplete = Interlocked.Decrement(ref _catalogLoaderCounter) == 0;
                if (isComplete) CatalogsLoadComplete();
            }
        }

        private async void GetUn1tData()
        {
            await FillUn1tCatalogs();
            Dispatcher.Invoke(() =>
            {
                txtStatus.Text = "Каталог UN1T загружен";
            });
        }

        private void CatalogsLoadComplete()
        {
            _firstLoadCategory = false;
            if (_activeProviderTreeView != null) AddProviderCategoryTreeViewModels(_activeProviderTreeView);
            GetUn1tData();
        }

        private async Task FillOldiCatalogs()
        {
            await FillTreeView(_oldiService, _oldiData, OldiCategoriesView, ProviderType.OLDI);
        }

        private async Task FillMerlionCatalogs()
        {
            await FillTreeView(_merlionService, _merlionData, MerlionCategoriesView, ProviderType.Merlion);
            _firstLoadCategory = false;
        }

        private async Task FillOcsCatalogs()
        {
            await FillTreeView(_ocsService, _ocsData, OcsCategoriesView, ProviderType.OCS);
        }

        private async Task FillTreolanCatalogs()
        {
            await FillTreeView(_treolanService, _treolanData, TreolanCategoriesView, ProviderType.Treolan);
        }

        private async Task FillUn1tCatalogs()
        {
            var views = new List<TreeViewItem>();
            var dbEngine = new DbEngine();
            var categories = await Task.Run(() =>
            {
                List<Un1tCategory> list = null;
                try
                {
                    list = dbEngine.LoadAllUn1tCategories();
                }
                catch (Exception ex)
                {
                    list = new List<Un1tCategory>();
                    Log.WriteToLog(LogSender + " FillUn1tCatalogs() ", "Ошибка: " + ex.Message);
                }
                return list;
            });
            _un1tCategories = categories;
            if (_un1tCategories == null)
            {
                _un1tCategories = new List<Un1tCategory>();
            }
            var rootCategories = categories.Where(x => x.IdParent == 0).ToList();
            Dispatcher.Invoke(() =>
            {
                Un1tCategoriesView.Items.Clear();
                if (rootCategories.Any())
                {
                    var list = new List<TreeViewItem>();
                    foreach (var rootCategory in rootCategories)
                    {
                        views.Add(GetTreeViewItemForUn1tCategory(rootCategory, categories));
                    }
                    foreach (var treeViewItem in views)
                    {
                        GetChildsForTreeView(treeViewItem, list);
                        SetIsMappedForTreeViewItem(treeViewItem);
                        Un1tCategoriesView.Items.Add(treeViewItem);
                    }
                    if (list.Any())
                    {
                        var viewModels = new List<Un1tCategoryViewModel>();
                        foreach (var treeViewItem in list)
                        {
                            var categoryView = treeViewItem.Header as Un1tCategoryItemView;
                            if (categoryView != null)
                            {
                                var viewModel = new Un1tCategoryViewModel()
                                {
                                    Item = categoryView,
                                    Model = categoryView.GetModel()
                                };
                                viewModels.Add(viewModel);
                            }
                        }
                        if (viewModels.Any())
                        {
                            _un1tCategoryViewModels.AddRange(viewModels);
                        }
                    }
                }
            });
        }

        private async Task FillTreeView(IDataService service, DataLevelBase dataLevel, TreeView treeView,
            ProviderType provider)
        {
            try
            {
                var categories = await Task.Run(() =>
                {
                    List<ProviderServiceCategoryBase> list = null;
                    try
                    {
                        list = dataLevel.GetCategories();
                    }
                    catch (Exception ex)
                    {
                        list = new List<ProviderServiceCategoryBase>();
                        Log.WriteToLog(LogSender + " FillTreeView() " + provider, ". Ошибка: " + ex.Message);
                    }
                    return list;
                });
                if (categories == null || !categories.Any())
                {
                    var result = await service.GetAllCategoriesAsync();
                    dataLevel.SetCategories(result);
                }
                var db = new DbEngine();
                var dbLoadCategories = db.LoadLoadCategoriesForProvider(provider);
                if (dbLoadCategories != null)
                {
                    dataLevel.UpdateLoadInfo(dbLoadCategories);
                }
                Dispatcher.Invoke(() =>
                {
                    var treeViewItems = GetTreeViewItems(dataLevel);
                    treeView.Items.Clear();
                    foreach (var treeViewItem in treeViewItems)
                    {
                        treeView.Items.Add(treeViewItem);
                    }
                });
            }
            catch (Exception ex)
            {
                Log.WriteToLog(LogSender + " FillTreeView() ", ex.Message);
                MessageBox.Show(ErrorMessage);
            }
        }

        private TreeViewItem GetTreeViewItemForUn1tCategory(Un1tCategory category, List<Un1tCategory> list)
        {
            var viewContent = new Un1tCategoryItemView(category);
            var treeViewItem = new TreeViewItem() { Header = viewContent };
            var childsCategory = list.Where(x => x.IdParent == category.Id).ToList();
            if (childsCategory.Any())
            {
                foreach (var childCategory in childsCategory)
                {
                    var childTreeViewItem = GetTreeViewItemForUn1tCategory(childCategory, list);
                    treeViewItem.Items.Add(childTreeViewItem);
                }
            }
            return treeViewItem;
        }

        #endregion

        #region Reload Catalogs

        private async void btnGetAllProviderCatalogs_Click(object sender, RoutedEventArgs e)
        {
            _catalogLoaderCounter = 4;
            ManualManagePanel.IsEnabled = false;
            AutoManagePanel.IsEnabled = false;
            _loadingData = true;
            WriteToLog("Получение каталогов всех поставщиков");
            await Task.Run(() =>
            {
                try
                {
                    var providers = new[] { ProviderType.Merlion, ProviderType.OCS, ProviderType.Treolan, ProviderType.OLDI };
                    foreach (var provider in providers)
                    {
                        ReloadProviderCatalog(provider);
                    }
                }
                catch (Exception ex)
                {
                    _loadingData = false;
                    Log.WriteToLog(LogSender + " btnGetAllProviderCatalogs_Click() ", "Ошибка: " + ex.Message);
                    Dispatcher.Invoke(() =>
                    {
                        ManualManagePanel.IsEnabled = true;
                        AutoManagePanel.IsEnabled = true;
                    });
                }
            });
        }

        private async void ReloadProviderCatalog(ProviderType provider)
        {
            try
            {
                IDataService service = null;
                DataLevelBase dataLevel = null;
                TreeView treeView = null;
                switch (provider)
                {
                    case ProviderType.Merlion:
                        service = _merlionService;
                        dataLevel = _merlionData;
                        treeView = MerlionCategoriesView;
                        break;
                    case ProviderType.Treolan:
                        service = _treolanService;
                        dataLevel = _treolanData;
                        treeView = TreolanCategoriesView;
                        break;
                    case ProviderType.OLDI:
                        service = _oldiService;
                        dataLevel = _oldiData;
                        treeView = OldiCategoriesView;
                        break;
                    case ProviderType.OCS:
                        service = _ocsService;
                        dataLevel = _ocsData;
                        treeView = OcsCategoriesView;
                        break;
                }
                if (service != null && dataLevel != null && treeView != null)
                {
                    WriteToLog("Стартовало получение каталога " + provider);
                    var result = await service.GetAllCategoriesAsync();
                    dataLevel.SetCategories(result);
                    var db = new DbEngine();
                    var dbLoadCategories = db.LoadLoadCategoriesForProvider(provider);
                    if (dbLoadCategories != null && dbLoadCategories.Any())
                    {
                        dataLevel.UpdateLoadInfo(dbLoadCategories);
                    }
                    Dispatcher.Invoke(() =>
                    {
                        var treeViewItems = GetTreeViewItems(dataLevel);
                        treeView.Items.Clear();
                        foreach (var treeViewItem in treeViewItems)
                        {
                            treeView.Items.Add(treeViewItem);
                        }
                    });
                    WriteToLog("Каталог " + provider + " перезагружен и сохранен");
                }
            }
            catch (Exception ex)
            {
                Log.WriteToLog(LogSender + " ReloadProviderCatalog(" + provider + ") ", ex.Message);
                WriteToLog("Ошибка при перезагрузке каталога " + provider);
            }
            finally
            {
                var isComplete = Interlocked.Decrement(ref _catalogLoaderCounter) == 0;
                if (isComplete) ProviderCatalogReloaded();
            }
        }

        private void ProviderCatalogReloaded()
        {
            _loadingData = false;
            WriteToLog("Каталоги всех поставщиков перезагружены");
            Dispatcher.Invoke(() =>
            {
                ManualManagePanel.IsEnabled = true;
                AutoManagePanel.IsEnabled = true;
            });
        }

        #endregion

        #region Work with catalogs

        private void OldiDataOnCategoryMapDeleted(object sender, DeleteMappedCategoryEventArgs args)
        {
            CategoryMapDelete(args.Item, _oldiData, ProviderType.OLDI);
        }

        private void TreolanDataOnCategoryMapDeleted(object sender, DeleteMappedCategoryEventArgs args)
        {
            CategoryMapDelete(args.Item, _treolanData, ProviderType.Treolan);
        }

        private void OcsDataOnCategoryMapDeleted(object sender, DeleteMappedCategoryEventArgs args)
        {
            CategoryMapDelete(args.Item, _ocsData, ProviderType.OCS);
        }

        private void MerlionDataOnCategoryMapDeleted(object sender, DeleteMappedCategoryEventArgs args)
        {
            CategoryMapDelete(args.Item, _merlionData, ProviderType.Merlion);
        }

        private void CategoryMapDelete(TreeViewItem item, DataLevelBase dataLevel, ProviderType provider)
        {
            if (item != null)
            {
                var categoryItem = item.Header as CategoryItemView;
                if (categoryItem != null)
                {
                    var model = categoryItem.GetModel();
                    if (model != null)
                    {
                        var message = "Удалить привязку для категории: " + model.Name + "?";
                        var dialogResult = MessageBox.Show(message, "Удалить привязку", MessageBoxButton.YesNo,
                            MessageBoxImage.Question);
                        if (dialogResult == MessageBoxResult.Yes)
                        {
                            var un1tCategoryId = model.IdUn1tCategory;
                            SetMappedForProviderCategory(model, dataLevel, -1);
                            SetProviderCategoryViewItemChildsIsLoaded(model, false);
                            if (_providerCategoriesViewModels.ContainsKey(provider))
                            {
                                var viewModels = _providerCategoriesViewModels[provider];
                                var providerCategories = viewModels.Select(x => x.Model).ToList();
                                var parent = providerCategories.FirstOrDefault(x => x.Id == model.IdParent);
                                while (parent != null)
                                {
                                    parent.IdUn1tCategory = -1;
                                    parent.IsLoad = false;
                                    var parentViewModel = viewModels.FirstOrDefault(x => x.Model.Id == parent.Id);
                                    if (parentViewModel != null)
                                    {
                                        parentViewModel.Item.SetChecked(false);
                                    }
                                    var childs = dataLevel.GetCategoriesForParent(parent.Id);
                                    var isAnyLoad = childs.Any(x => x.IdUn1tCategory != -1 && x.IsLoad);
                                    if (isAnyLoad && parentViewModel != null)
                                    {
                                        parentViewModel.Item.SetChecked(null);
                                    }
                                    parent = providerCategories.FirstOrDefault(x => x.Id == parent.IdParent);
                                }
                            }
                            UpdateUn1tCategoryView(un1tCategoryId);
                            Task.Run(() =>
                            {
                                var isLocked = false;
                                try
                                {
                                    Monitor.Enter(_lockObj, ref isLocked);
                                    dataLevel.SaveCategories();
                                    var db = new DbEngine();
                                    var categoriesToLoad =
                                        dataLevel.GetCategories()
                                            .Where(x => x.IsLoad)
                                            .Select(x => new ProviderCategoryBase()
                                            {
                                                Id = x.Id,
                                                IdParent = x.IdParent,
                                                IdParentType = x.IdParentType,
                                                IdType = x.IdType,
                                                IdUn1tCategory = x.IdUn1tCategory
                                            }).ToList();
                                    db.SaveLoadCategoriesForProvider(categoriesToLoad, provider);
                                }
                                finally
                                {
                                    if (isLocked) Monitor.Exit(_lockObj);
                                }
                            });
                            Un1tCategoriesView_SelectedItemChanged(null, null);
                        }
                    }
                }
            }
        }

        private void OldiDataOnCategoryLoadStateChanged(object sender, CategoryModelEventArgs args)
        {
            CategoryLoadStateChanged(_oldiData, args, ProviderType.OLDI);
        }

        private void TreolanDataOnCategoryLoadStateChanged(object sender, CategoryModelEventArgs args)
        {
            CategoryLoadStateChanged(_treolanData, args, ProviderType.Treolan);
        }

        private void OcsDataOnCategoryLoadStateChanged(object sender, CategoryModelEventArgs args)
        {
            CategoryLoadStateChanged(_ocsData, args, ProviderType.OCS);
        }

        private void MerlionDataOnCategoryLoadStateChanged(object sender, CategoryModelEventArgs args)
        {
            CategoryLoadStateChanged(_merlionData, args, ProviderType.Merlion);
        }

        private void CategoryLoadStateChanged(DataLevelBase dataLevel, CategoryModelEventArgs args,
            ProviderType provider)
        {
            try
            {
                if (args.Item == null || args.Model == null) return;
                var un1tSelectedItem = Un1tCategoriesView.SelectedItem as TreeViewItem;
                if (un1tSelectedItem == null) return;
                var categoryView = un1tSelectedItem.Header as Un1tCategoryItemView;
                if (categoryView == null) return;
                var model = categoryView.GetModel();
                if (model == null) return;
                var isLastLevel = _un1tCategories.Count(x => x.IdParent == model.Id) == 0;
                if (!isLastLevel)
                {
                    MessageBox.Show("Сопоставлять можно только с нижнем уровнем каталога UN1T!");
                    return;
                }
                var isLoaded = args.NewState == CategoryActionState.Load;
                dataLevel.SetCategoryIsLoaded(args.Model.Id, isLoaded);
                if (isLoaded)
                {
                    SetMappedForProviderCategory(args.Model, dataLevel, model.Id);
                    SetProviderCategoryViewItemChildsIsLoaded(args.Model, true);
                }
                else
                {
                    SetMappedForProviderCategory(args.Model, dataLevel, -1);
                    SetProviderCategoryViewItemChildsIsLoaded(args.Model, false);
                }
                if (_providerCategoriesViewModels.ContainsKey(provider))
                {
                    var viewModels = _providerCategoriesViewModels[provider];
                    var providerCategories = viewModels.Select(x => x.Model).ToList();
                    var parent = providerCategories.FirstOrDefault(x => x.Id == args.Model.IdParent);
                    while (parent != null)
                    {
                        var parentViewModel = viewModels.FirstOrDefault(x => x.Model.Id == parent.Id);
                        var childs = dataLevel.GetCategoriesForParent(parent.Id);
                        var isParentMapped = childs.Count() ==
                                                     childs.Count(x => x.IdUn1tCategory == model.Id && x.IsLoad);
                        parent.IsLoad = isParentMapped;
                        if (isParentMapped)
                        {
                            parent.IdUn1tCategory = model.Id;
                            if (parentViewModel != null)
                            {
                                parentViewModel.Item.SetChecked(true);
                            }
                        }
                        else
                        {
                            if (parentViewModel != null)
                            {
                                parentViewModel.Item.SetChecked(false);
                            }
                            parent.IdUn1tCategory = -1;
                            var isLoad = childs.Count() ==
                                                     childs.Count(x => x.IdUn1tCategory != -1 && x.IsLoad);
                            parent.IsLoad = isLoad;
                            if (isLoad)
                            {
                                if (parentViewModel != null)
                                {
                                    parentViewModel.Item.SetChecked(true);
                                }
                            }
                            else
                            {
                                var isAnyLoad = childs.Any(x => x.IdUn1tCategory != -1 && x.IsLoad);
                                if (isAnyLoad)
                                {
                                    if (parentViewModel != null)
                                    {
                                        parentViewModel.Item.SetChecked(null);
                                    }
                                }
                            }
                        }
                        parent = providerCategories.FirstOrDefault(x => x.Id == parent.IdParent);
                    }
                }
                UpdateUn1tCategoryView(model.Id);
                Task.Run(() =>
                {
                    var isLocked = false;
                    try
                    {
                        Monitor.Enter(_lockObj, ref isLocked);
                        dataLevel.SaveCategories();
                        var db = new DbEngine();
                        var categoriesToLoad =
                            dataLevel.GetCategories()
                                .Where(x => x.IsLoad)
                                .Select(x => new ProviderCategoryBase()
                                {
                                    Id = x.Id,
                                    IdParent = x.IdParent,
                                    IdParentType = x.IdParentType,
                                    IdType = x.IdType,
                                    IdUn1tCategory = x.IdUn1tCategory
                                }).ToList();
                        db.SaveLoadCategoriesForProvider(categoriesToLoad, provider);
                    }
                    finally
                    {
                        if (isLocked) Monitor.Exit(_lockObj);
                    }
                });
                Un1tCategoriesView_SelectedItemChanged(null, null);
            }
            catch (Exception ex)
            {
                Log.WriteToLog(LogSender + " CategoryLoadStateChanged() ", ex.Message);
                MessageBox.Show(ErrorMessage);
            }
        }

        private async void UpdateUn1tCategoryView(int id)
        {
            if (!_un1tCategoryViewModels.Any()) return;
            var idParentLast = await Task.Run(() =>
            {
                var idParent = -1;
                var category = _un1tCategories.FirstOrDefault(x => x.Id == id);
                if (category != null)
                {
                    var isLastParent = false;
                    while (!isLastParent)
                    {
                        if (category.IdParent == 0)
                        {
                            isLastParent = true;
                        }
                        else
                        {
                            idParent = category.IdParent;
                            category = _un1tCategories.FirstOrDefault(x => x.Id == idParent);
                            if (category == null)
                            {
                                isLastParent = true;
                            }
                        }
                    }
                }
                return idParent;
            });
            Dispatcher.Invoke(() =>
            {
                if (idParentLast != -1)
                {
                    var viewModel = _un1tCategoryViewModels.FirstOrDefault(x => x.Model.Id == idParentLast);
                    if (viewModel != null)
                    {
                        var treeViewItem = viewModel.Item.Parent as TreeViewItem;
                        if (treeViewItem != null)
                        {
                            SetNoneSelectedForUn1tCategotyViewItem(treeViewItem);
                            SetIsMappedForTreeViewItem(treeViewItem);
                        }
                    }
                }
                else
                {
                    var viewModel = _un1tCategoryViewModels.FirstOrDefault(x => x.Model.Id == id);
                    if (viewModel != null)
                    {
                        var treeViewItem = viewModel.Item.Parent as TreeViewItem;
                        if (treeViewItem != null)
                        {
                            SetNoneSelectedForUn1tCategotyViewItem(treeViewItem);
                            SetIsMappedForTreeViewItem(treeViewItem);
                        }
                    }
                }
                if (_selectedUn1tCategoryView != null)
                {
                    _selectedUn1tCategoryView.SetSelected(true);
                }
            });
        }

        private void SetNoneSelectedForUn1tCategotyViewItem(TreeViewItem item)
        {
            var categoryView = item.Header as Un1tCategoryItemView;
            if (categoryView != null)
            {
                categoryView.SetHighlight(false);
            }
            var childs = item.Items;
            if (childs != null)
            {
                foreach (TreeViewItem child in childs)
                {
                    SetNoneSelectedForUn1tCategotyViewItem(child);
                }
            }
        }

        private void SetProviderCategoryViewItemChildsIsLoaded(ProviderServiceCategoryBase category, bool isLoaded)
        {
            var treeView = _activeProviderTreeView;
            if (treeView == null) return;
            var provider = (ProviderType)treeView.Tag;
            if (!_providerCategoriesViewModels.ContainsKey(provider)) return;
            var viewModels = _providerCategoriesViewModels[provider];
            var providerCategories = viewModels.Select(x => x.Model).ToList();
            var allChilds = new List<ProviderServiceCategoryBase>();
            GetAllChildsForProviderCategory(category, allChilds, providerCategories);
            var childsViewModels = viewModels.Where(x => allChilds.Select(y => y.Id).Contains(x.Model.Id)).ToList();
            foreach (var viewModel in childsViewModels)
            {
                viewModel.Item.SetChecked(isLoaded);
            }
        }

        private void SetIsMappedForTreeViewItem(TreeViewItem item)
        {
            var isMapped = false;
            var un1tCategoryView = item.Header as Un1tCategoryItemView;
            if (un1tCategoryView != null)
            {
                var model = un1tCategoryView.GetModel();
                var dataLevels = new[] { _merlionData, _ocsData, _treolanData, _oldiData };
                foreach (var dataLevel in dataLevels)
                {
                    var categories = dataLevel.GetCategories();
                    if (categories.Any())
                    {
                        isMapped = categories.Count(x => x.IdUn1tCategory == model.Id) > 0;
                        if (isMapped) break;
                    }
                }
                var items = item.Items;
                if (items != null)
                {
                    foreach (TreeViewItem viewItem in items)
                    {
                        SetIsMappedForTreeViewItem(viewItem);
                    }
                }
                if (isMapped)
                {
                    un1tCategoryView.SetHighlight(true);
                    var parent = item.Parent as TreeViewItem;
                    while (parent != null)
                    {
                        var parentViewItem = parent.Header as Un1tCategoryItemView;
                        if (parentViewItem != null)
                        {
                            parentViewItem.SetHighlight(true);
                        }
                        parent = parent.Parent as TreeViewItem;
                    }
                }
            }
        }

        private TreeViewItem[] GetTreeViewItems(DataLevelBase dataLevel)
        {
            var views = new List<TreeViewItem>();
            try
            {
                var rootCategories = dataLevel.GetRootCategories();
                if (rootCategories != null)
                {
                    foreach (var rootCategory in rootCategories)
                    {
                        views.Add(GetTreeViewItemForCategory(rootCategory, dataLevel));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteToLog(LogSender + " GetTreeViewItems() ", ex.Message);
                MessageBox.Show(ErrorMessage);
            }
            return views.ToArray();
        }

        private TreeViewItem GetTreeViewItemForCategory(ProviderServiceCategoryBase category, DataLevelBase dataLevel)
        {
            var isAnyChildLoaded = false;
            if (!category.IsLoad)
            {
                isAnyChildLoaded = dataLevel.GetAllChildsForCategory(category.Id).Any(x => x.IsLoad);
            }
            var viewContent = new CategoryItemView(category, isAnyChildLoaded, dataLevel, false);
            var treeViewItem = new TreeViewItem() { Header = viewContent };
            var childsCategory = dataLevel.GetCategoriesForParent(category.Id);
            if (childsCategory != null && childsCategory.Any())
            {
                foreach (var childCategory in childsCategory)
                {
                    var childTreeViewItem = GetTreeViewItemForCategory(childCategory, dataLevel);
                    treeViewItem.Items.Add(childTreeViewItem);
                }
            }
            return treeViewItem;
        }

        private void ClearTreeViewItems(TreeView ctrl)
        {
            ctrl.Items.Clear();
            ctrl.Items.Add(new TreeViewItem() { Header = new TextBlock() { Text = "Загрузка данных" } });
        }

        private void TreeView_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var treeView = Un1tCategoriesView;
            try
            {
                if (treeView != null)
                {
                    var item = treeView.SelectedItem as TreeViewItem;
                    if (item != null)
                    {
                        item.IsSelected = false;
                    }
                    var treeViews = new[] { MerlionCategoriesView, OldiCategoriesView, OcsCategoriesView, TreolanCategoriesView };
                    var dataLevels = new[] { _merlionData, _oldiData, _ocsData, _treolanData };
                    for (var i = 0; i < treeViews.Length; i++)
                    {
                        var view = treeViews[i];
                        var dataLevel = dataLevels[i];
                        var selectedItem = view.SelectedItem as TreeViewItem;
                        if (selectedItem != null)
                        {
                            selectedItem.IsSelected = false;
                        }
                        var childs = view.Items;
                        if (childs != null)
                        {
                            foreach (TreeViewItem child in childs)
                            {
                                DeselectTreeViewItem(child, dataLevel);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteToLog(LogSender + " TreeView_MouseRightButtonUp() ", ex.Message);
                MessageBox.Show(ErrorMessage);
            }
        }

        private void DeselectTreeViewItem(TreeViewItem item, DataLevelBase dataLevel)
        {
            item.IsSelected = false;
            var categoryItemView = item.Header as CategoryItemView;
            if (categoryItemView != null)
            {
                var model = categoryItemView.GetModel();
                if (model != null)
                {
                    categoryItemView.SetHighlight(CategoryHighlight.None);
                    categoryItemView.SetChecked(model.IsLoad);
                    categoryItemView.SetEnabledIsLoad(false);
                    var isAnyChildLoaded = false;
                    if (!model.IsLoad)
                    {
                        isAnyChildLoaded = dataLevel.GetAllChildsForCategory(model.Id).Any(x => x.IsLoad);
                        if (isAnyChildLoaded)
                        {
                            categoryItemView.SetChecked(null);
                        }
                    }
                }
            }
            var childs = item.Items;
            if (childs != null)
            {
                foreach (TreeViewItem child in childs)
                {
                    DeselectTreeViewItem(child, dataLevel);
                }
            }
        }

        private void btnNewUn1tCategory_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = Un1tCategoriesView.SelectedItem as TreeViewItem;
            var parent = string.Empty;
            if (selectedItem != null)
            {
                var categoryView = selectedItem.Header as Un1tCategoryItemView;
                if (categoryView != null)
                {
                    var model = categoryView.GetModel();
                    if (model != null)
                    {
                        parent = model.Name;
                    }
                }
            }
            if (string.IsNullOrEmpty(parent))
            {
                parent = "Корень";
            }
            var createWindow = new Un1tCreateCategoryWindow(parent);
            createWindow.Owner = this;
            createWindow.ShowDialog();
        }

        private void btnEditUn1tCategory_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = Un1tCategoriesView.SelectedItem as TreeViewItem;
            if (selectedItem != null)
            {
                var categoryView = selectedItem.Header as Un1tCategoryItemView;
                if (categoryView != null)
                {
                    var model = categoryView.GetModel();
                    if (model != null)
                    {
                        var name = model.Name;
                        var editWindow = new Un1tEditCategoryWindow(name);
                        editWindow.Owner = this;
                        editWindow.ShowDialog();
                    }
                }
            }
        }

        public async void AddNewUn1tCategory(string name)
        {
            try
            {
                var selectedItem = Un1tCategoriesView.SelectedItem as TreeViewItem;
                Un1tCategory parent = null;
                if (selectedItem != null)
                {
                    var categoryView = selectedItem.Header as Un1tCategoryItemView;
                    if (categoryView != null)
                    {
                        var model = categoryView.GetModel();
                        if (model != null)
                        {
                            parent = model;
                        }
                    }
                }
                var parentId = 0;
                if (parent != null)
                {
                    parentId = parent.Id;
                }
                if (_un1tCategories == null || !_un1tCategories.Any())
                {
                    _un1tCategories = new List<Un1tCategory>();
                }
                var childs = _un1tCategories.Where(x => x.IdParent == parentId).ToList();
                if (childs.Any())
                {
                    var names = childs.Select(x => x.Name).ToList();
                    if (names.Any())
                    {
                        var isNameAvailable = !names.Contains(name);
                        if (!isNameAvailable)
                        {
                            var messageBoxResult =
                                MessageBox.Show("Такое имя уже существует в данной категории. Продолжить?",
                                    "Предупреждение", System.Windows.MessageBoxButton.YesNo, MessageBoxImage.Warning);
                            if (messageBoxResult == MessageBoxResult.No)
                            {
                                return;
                            }
                        }
                    }
                }
                var newId = GetNewId();
                var newCategory = new Un1tCategory() { Id = newId, IdParent = parentId, Name = name };
                _un1tCategories.Add(newCategory);
                var treeViewItems = Un1tCategoriesView.Items;
                if (selectedItem != null)
                {
                    treeViewItems = selectedItem.Items;
                }
                var newTreeViewItem = new TreeViewItem();
                newTreeViewItem.Header = new Un1tCategoryItemView(newCategory);
                treeViewItems.Add(newTreeViewItem);
                var viewModel = new Un1tCategoryViewModel()
                {
                    Item = newTreeViewItem.Header as Un1tCategoryItemView,
                    Model = newCategory
                };
                _un1tCategoryViewModels.Add(viewModel);
                var isCategoryCreated = await Task.Run(() =>
                {
                    var isComplete = false;
                    try
                    {
                        var db = new DbEngine();
                        db.CreateUn1tCategory(newCategory);
                        isComplete = true;
                    }
                    catch (Exception ex)
                    {
                        Log.WriteToLog(LogSender,
                            " DbEngine CreateUn1tCategory(" + newCategory.Name + ") Ошибка: " + ex.Message);
                        isComplete = false;
                    }
                    return isComplete;
                });
                Dispatcher.Invoke(() =>
                {
                    if (isCategoryCreated)
                    {
                        txtStatus.Text = "Новая категория создана и сохранена в БД";
                    }
                    else
                    {
                        MessageBox.Show("Ошибка при сохранении категории в Базе Данных!");
                    }
                });
            }
            catch (Exception ex)
            {
                Log.WriteToLog(LogSender + " AddNewUn1tCategory(" + name + ") ", ex.Message);
                MessageBox.Show(ErrorMessage);
            }
        }

        public async void EditUn1tCategory(string newName)
        {
            try
            {
                var selectedItem = Un1tCategoriesView.SelectedItem as TreeViewItem;
                if (selectedItem != null)
                {
                    var categoryView = selectedItem.Header as Un1tCategoryItemView;
                    if (categoryView != null)
                    {
                        var model = categoryView.GetModel();
                        if (model != null)
                        {
                            var childs = _un1tCategories.Where(x => x.IdParent == model.IdParent).ToList();
                            if (childs.Any())
                            {
                                var names = childs.Select(x => x.Name).ToList();
                                if (names.Any())
                                {
                                    var isNameAvailable = !names.Contains(newName);
                                    if (!isNameAvailable)
                                    {
                                        var messageBoxResult =
                                            MessageBox.Show("Такое имя уже существует в данной категории. Продолжить?",
                                                "Предупреждение", System.Windows.MessageBoxButton.YesNo,
                                                MessageBoxImage.Warning);
                                        if (messageBoxResult == MessageBoxResult.No)
                                        {
                                            return;
                                        }
                                    }
                                }
                            }
                            model.Name = newName;
                            categoryView.SetName(newName);
                            var isCategoryEdited = await Task.Run(() =>
                            {
                                var isComplete = false;
                                try
                                {
                                    var db = new DbEngine();
                                    db.UpdateUn1tCategory(model.Id, model.Name);
                                    isComplete = true;
                                }
                                catch (Exception ex)
                                {
                                    Log.WriteToLog(LogSender,
                                        " DbEngine UpdateUn1tCategory(" + model.Name + ") Ошибка: " + ex.Message);
                                    isComplete = false;
                                }
                                return isComplete;
                            });
                            Dispatcher.Invoke(() =>
                            {
                                if (isCategoryEdited)
                                {
                                    txtStatus.Text = "Категория изменена и сохранена в БД";
                                }
                                else
                                {
                                    MessageBox.Show("Ошибка при сохранении категории в Базе Данных!");
                                }
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteToLog(LogSender + " EditUn1tCategory(" + newName + ")", ex.Message);
                MessageBox.Show(ErrorMessage);
            }
        }

        private int GetNewId()
        {
            var ids = _un1tCategories.Select(x => x.Id).ToList();
            var id = 1;
            if (ids.Any())
            {
                id = ids.Max() + 1;
            }
            var isFound = false;
            while (!isFound)
            {
                isFound = !ids.Contains(id);
                if (!isFound)
                {
                    id++;
                }
            }
            return id;
        }

        private void btnDeleteUn1tCategory_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = Un1tCategoriesView.SelectedItem as TreeViewItem;
            if (selectedItem != null)
            {
                TreeView_MouseRightButtonUp(null, null);
                var categoryView = selectedItem.Header as Un1tCategoryItemView;
                if (categoryView != null)
                {
                    var model = categoryView.GetModel();
                    if (model != null)
                    {
                        var name = model.Name;

                        var messageBoxResult = MessageBox.Show("Удалить категорию: " + name + "?",
                            "Удалить", System.Windows.MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        if (messageBoxResult != MessageBoxResult.No)
                        {
                            DeleteUn1tCategoryTreeViewItem(selectedItem);
                        }
                    }
                }
            }
        }

        private void DeleteUn1tCategoryTreeViewItem(TreeViewItem viewItem)
        {
            try
            {
                var parent = viewItem.Parent as TreeViewItem;
                if (parent != null)
                {
                    parent.Items.Remove(viewItem);
                }
                else
                {
                    Un1tCategoriesView.Items.Remove(viewItem);
                }
                var categoryView = viewItem.Header as Un1tCategoryItemView;
                if (categoryView != null)
                {
                    var model = categoryView.GetModel();
                    if (model != null)
                    {
                        DeleteUn1tCategoryMappedLink(model);
                        var dataLevels = new DataLevelBase[] { _merlionData, _ocsData, _oldiData, _treolanData };
                        for (var i = 0; i < dataLevels.Length; i++)
                        {
                            var dataLevel = dataLevels[i];
                            dataLevel.SaveCategories();
                        }
                        DeleteUn1tCategory(model);
                        var viewModel = _un1tCategoryViewModels.FirstOrDefault(x => x.Model.Id == model.Id);
                        if (viewModel != null) _un1tCategoryViewModels.Remove(viewModel);
                        RefreshActiveCatalogView();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteToLog(LogSender + " DeleteUn1tCategoryTreeViewItem() ", ex.Message);
            }
        }

        private void DeleteUn1tCategoryMappedLink(Un1tCategory category)
        {

            var dataLevels = new DataLevelBase[] { _merlionData, _ocsData, _oldiData, _treolanData };
            foreach (var dataLevel in dataLevels)
            {
                var categories = dataLevel.GetCategories();
                var deleteMappedCategories = categories.Where(x => x.IdUn1tCategory == category.Id).ToList();
                deleteMappedCategories.ForEach((x) =>
                {
                    x.IdUn1tCategory = -1;
                    x.IsLoad = false;
                });
            }
            var childs = _un1tCategories.Where(x => x.IdParent == category.Id).ToList();
            foreach (var child in childs)
            {
                DeleteUn1tCategoryMappedLink(child);
            }
        }

        private void DeleteUn1tCategory(Un1tCategory category)
        {
            var isLocked = false;
            try
            {
                Monitor.Enter(_lockObj, ref isLocked);
                var deleteList = GetUn1TCategoriesForDelete(category);
                foreach (var un1TCategory in deleteList)
                {
                    _un1tCategories.Remove(un1TCategory);
                }
                var db = new DbEngine();
                var deleteComplete = db.DeleteUn1tCategory(category.Id);
                if (deleteComplete)
                {
                    txtStatus.Text = "Категория и все подкатегории удалены из БД";
                }
                else
                {
                    MessageBox.Show("Ошибка при удалении категории из Базы Данных!");
                }
            }
            catch (Exception ex)
            {
                Log.WriteToLog(LogSender, " DeleteUn1tCategory() Ошибка: " + ex.Message);
                MessageBox.Show("Ошибка при удалении категории из Базы Данных!");
                throw;
            }
            finally
            {
                if (isLocked) Monitor.Exit(_lockObj);
            }
        }

        private List<Un1tCategory> GetUn1TCategoriesForDelete(Un1tCategory deleteCategory)
        {
            var list = new List<Un1tCategory>();
            list.Add(deleteCategory);
            var childs = _un1tCategories.Where(x => x.IdParent == deleteCategory.Id).ToList();
            if (childs.Any())
            {
                foreach (var child in childs)
                {
                    list.AddRange(GetUn1TCategoriesForDelete(child));
                }
            }
            return list;
        }

        private void SetMappedForProviderCategory(ProviderServiceCategoryBase category, DataLevelBase dataLevel,
            int mappedId)
        {
            category.IdUn1tCategory = mappedId;
            var isLoad = true;
            if (mappedId == -1) isLoad = false;
            category.IsLoad = isLoad;
            var childs = dataLevel.GetCategoriesForParent(category.Id);
            foreach (var child in childs)
            {
                SetMappedForProviderCategory(child, dataLevel, mappedId);
            }
        }

        private void Un1tCategoriesView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                var type = GetUn1TCategoriesViewType();
                if (type == Un1tTreeViewType.Mapped) return;
                if (_selectedUn1tCategoryView != null)
                {
                    _selectedUn1tCategoryView.SetSelected(false);
                }
                btnNewUn1tCategory.IsEnabled = true;
                var un1tSelectedItem = Un1tCategoriesView.SelectedItem as TreeViewItem;
                if (un1tSelectedItem != null)
                {
                    var categoryView = un1tSelectedItem.Header as Un1tCategoryItemView;
                    if (categoryView != null)
                    {
                        var model = categoryView.GetModel();
                        if (model != null)
                        {
                            var id = model.Id;
                            var isReadyToAction = _un1tCategories.Count(x => x.IdParent == id) == 0;
                            if (isReadyToAction)
                            {
                                _selectedUn1tCategoryView = categoryView;
                                _selectedUn1tCategoryView.SetSelected(true);
                                var isBlock = false;
                                var dataLevels = new[] { _merlionData, _treolanData, _ocsData, _oldiData };
                                for (var i = 0; i < dataLevels.Length; i++)
                                {
                                    var dataLevel = dataLevels[i];
                                    var categories = dataLevel.GetCategories();
                                    var mappedCategoriesList = categories.Where(x => x.IdUn1tCategory == id).ToList();
                                    if (mappedCategoriesList.Any())
                                    {
                                        isBlock = true;
                                        break;
                                    }
                                }
                                btnNewUn1tCategory.IsEnabled = !isBlock;
                            }
                            RefreshActiveCatalogView();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteToLog(LogSender + " Un1tCategoriesView_SelectedItemChanged()", ex.Message);
                MessageBox.Show(ErrorMessage);
            }
        }

        private TextBlock GetToolTipForCategory(int categoryId)
        {
            var toolTip = string.Empty;
            if (categoryId != -1)
            {
                var parentsName = new List<string>();
                var category = _un1tCategories.FirstOrDefault(x => x.Id == categoryId);
                if (category != null)
                {
                    parentsName.Add(category.Name);
                    var parent = _un1tCategories.FirstOrDefault(x => x.Id == category.IdParent);
                    while (parent != null)
                    {
                        parentsName.Add(parent.Name);
                        parent = _un1tCategories.FirstOrDefault(x => x.Id == parent.IdParent);
                    }
                }
                toolTip = "Привязана к:";
                var spaceCounter = 1;
                for (var i = parentsName.Count() - 1; i > -1; i--)
                {
                    toolTip += "\n";
                    var name = parentsName[i];
                    var spaces = spaceCounter * 2;
                    spaceCounter++;
                    for (var j = 0; j < spaces; j++)
                    {
                        toolTip = toolTip + " ";
                    }
                    toolTip += name;
                }
            }
            if (string.IsNullOrEmpty(toolTip))
            {
                return null;
            }
            return new TextBlock() { Text = toolTip };
        }

        private void GetAllChildsForUn1tCategory(int id, List<int> childsId)
        {
            childsId.Add(id);
            var childs = _un1tCategories.Where(x => x.IdParent == id).ToList();
            foreach (var child in childs)
            {
                GetAllChildsForUn1tCategory(child.Id, childsId);
            }
        }

        private void GetAllChildsForProviderCategory(ProviderServiceCategoryBase category, List<ProviderServiceCategoryBase> childs,
            List<ProviderServiceCategoryBase> categories)
        {
            childs.Add(category);
            var categoryChilds = categories.Where(x => x.IdParent == category.Id).ToList();
            foreach (var child in categoryChilds)
            {
                GetAllChildsForProviderCategory(child, childs, categories);
            }
        }

        private void SetViewTypeForUni1CategoriesView(Un1tTreeViewType type)
        {
            Un1tCategoriesView.Tag = type;
            if (type == Un1tTreeViewType.Normal)
            {
                btnNewUn1tCategory.IsEnabled = true;
                btnEditUn1tCategory.IsEnabled = true;
                btnDeleteUn1tCategory.IsEnabled = true;
            }
            if (type == Un1tTreeViewType.Mapped)
            {
                btnNewUn1tCategory.IsEnabled = false;
                btnEditUn1tCategory.IsEnabled = false;
                btnDeleteUn1tCategory.IsEnabled = false;
            }
        }

        private void btnToggleCategoryView_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var type = GetUn1TCategoriesViewType();
                var newType = Un1tTreeViewType.Normal;
                if (type == Un1tTreeViewType.Mapped)
                {
                    newType = Un1tTreeViewType.Normal;
                }
                else if (type == Un1tTreeViewType.Normal)
                {
                    newType = Un1tTreeViewType.Mapped;
                }
                SetViewTypeForUni1CategoriesView(newType);
                ShowUn1tCategoriesForType(newType);
            }
            catch (Exception ex)
            {
                Log.WriteToLog(LogSender + " btnToggleCategoryView_Click()", ex.Message);
                MessageBox.Show(ErrorMessage);
            }
        }

        private void ShowUn1tCategoriesForType(Un1tTreeViewType type)
        {
            TreeView_MouseRightButtonUp(null, null);
            var views = new List<TreeViewItem>();
            Un1tCategoriesView.Items.Clear();
            if (type == Un1tTreeViewType.Normal)
            {
                var categories = _un1tCategories.ToList();
                var rootCategories = categories.Where(x => x.IdParent == 0).ToList();
                if (rootCategories.Any())
                {
                    foreach (var rootCategory in rootCategories)
                    {
                        views.Add(GetTreeViewItemForUn1tCategory(rootCategory, categories));
                    }
                    foreach (var treeViewItem in views)
                    {
                        SetIsMappedForTreeViewItem(treeViewItem);
                        Un1tCategoriesView.Items.Add(treeViewItem);
                    }
                }
            }
            if (type == Un1tTreeViewType.Mapped)
            {
                var categories = _un1tCategories.ToList();
                var rootCategories = categories.Where(x => x.IdParent == 0).ToList();
                if (rootCategories.Any())
                {
                    foreach (var rootCategory in rootCategories)
                    {
                        views.Add(GetTreeViewItemForUn1tCategoryForMappedView(rootCategory, categories));
                    }
                    foreach (var treeViewItem in views)
                    {
                        SetIsHighlightedForTreeViewItem(treeViewItem);
                        Un1tCategoriesView.Items.Add(treeViewItem);
                    }
                }
            }
        }

        private void SetIsHighlightedForTreeViewItem(TreeViewItem item)
        {
            var isHighlighted = TreeViewItemIsHighlighted(item);
            var un1tCategoryItem = item.Header as Un1tCategoryItemView;
            if (un1tCategoryItem != null)
            {
                un1tCategoryItem.SetHighlight(isHighlighted);
            }
        }

        private bool TreeViewItemIsHighlighted(TreeViewItem item)
        {
            var isHighlighted = false;
            var un1tCategoryItem = item.Header as Un1tCategoryItemView;
            if (un1tCategoryItem != null)
            {
                isHighlighted = un1tCategoryItem.IsHighlighted;
                if (!isHighlighted)
                {
                    var items = item.Items;
                    if (items != null)
                    {
                        foreach (TreeViewItem treeViewItem in items)
                        {
                            isHighlighted = TreeViewItemIsHighlighted(treeViewItem);
                            if (isHighlighted) break;
                        }
                    }
                }
                un1tCategoryItem.SetHighlight(isHighlighted);
            }
            return isHighlighted;
        }

        private TreeViewItem GetTreeViewItemForUn1tCategoryForMappedView(Un1tCategory category, List<Un1tCategory> list)
        {
            var dataLevels = new[] { _merlionData, _treolanData, _ocsData, _oldiData };
            var providerNames = new[] { ProviderType.Merlion, ProviderType.Treolan, ProviderType.OCS, ProviderType.OLDI };
            var viewContent = new Un1tCategoryItemView(category);
            var treeViewItem = new TreeViewItem() { Header = viewContent };
            var childsCategory = list.Where(x => x.IdParent == category.Id).ToList();
            if (childsCategory.Any())
            {
                foreach (var childCategory in childsCategory)
                {
                    var childTreeViewItem = GetTreeViewItemForUn1tCategoryForMappedView(childCategory, list);
                    treeViewItem.Items.Add(childTreeViewItem);
                }
            }
            else
            {
                for (var i = 0; i < dataLevels.Length; i++)
                {
                    var dataLevel = dataLevels[i];
                    var provider = providerNames[i];
                    var categories = dataLevel.GetCategories();
                    var mappedCategoriesList = categories.Where(x => x.IdUn1tCategory == category.Id).ToList();
                    if (mappedCategoriesList.Any())
                    {
                        var lowLevelCategories =
                            mappedCategoriesList.Where(x => mappedCategoriesList.Count(y => y.IdParent == x.Id) == 0)
                                .ToList();
                        if (lowLevelCategories.Any())
                        {
                            var viewItems =
                                lowLevelCategories.Select(x => GetItemForMappedUn1tCategory(x, provider)).ToList();
                            if (viewItems.Any())
                            {
                                foreach (var viewItem in viewItems)
                                {
                                    treeViewItem.Items.Add(viewItem);
                                }
                                viewContent.SetHighlight(true);
                            }
                        }
                    }
                }
            }
            return treeViewItem;
        }

        private TreeViewItem GetItemForMappedUn1tCategory(ProviderServiceCategoryBase providerCategory,
            ProviderType provider)
        {
            var treeViewItem = new TreeViewItem();
            var textName = new TextBlock()
            {
                Text = provider + ": " + providerCategory.Name,
                Background = new SolidColorBrush(Color.FromRgb(100, 200, 200))
            };
            treeViewItem.Tag = new DeleteItem()
            {
                Category = providerCategory,
                Provider = provider
            };
            treeViewItem.Header = textName;
            treeViewItem.MouseRightButtonUp += MappedCategoryOnDelete;
            return treeViewItem;
        }

        private void MappedCategoryOnDelete(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            var treeViewItem = sender as TreeViewItem;
            if (treeViewItem != null)
            {
                var deleteItem = treeViewItem.Tag as DeleteItem;
                if (deleteItem != null)
                {
                    var provider = deleteItem.Provider;
                    var category = deleteItem.Category;
                    var message = "Удалить привязку " + provider + " " + category.Name + " ?";
                    var dialogResult = MessageBox.Show(message, "Удалить привязку", MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    if (dialogResult == MessageBoxResult.Yes)
                    {
                        DataLevelBase dataLevel = null;
                        switch (provider)
                        {
                            case ProviderType.Merlion:
                                dataLevel = _merlionData;
                                break;
                            case ProviderType.Treolan:
                                dataLevel = _treolanData;
                                break;
                            case ProviderType.OCS:
                                dataLevel = _ocsData;
                                break;
                            case ProviderType.OLDI:
                                dataLevel = _oldiData;
                                break;
                        }
                        if (dataLevel != null)
                        {
                            SetMappedForProviderCategory(category, dataLevel, -1);
                            dataLevel.SaveCategories();
                            var db = new DbEngine();
                            var categoriesToLoad =
                                dataLevel.GetCategories()
                                    .Where(x => x.IsLoad)
                                    .Select(x => new ProviderCategoryBase()
                                    {
                                        Id = x.Id,
                                        IdParent = x.IdParent,
                                        IdParentType = x.IdParentType,
                                        IdType = x.IdType,
                                        IdUn1tCategory = x.IdUn1tCategory
                                    }).ToList();
                            db.SaveLoadCategoriesForProvider(categoriesToLoad, provider);
                            var parent = treeViewItem.Parent as TreeViewItem;
                            if (parent != null)
                            {
                                parent.Items.Remove(treeViewItem);
                            }
                        }
                    }
                }
            }
        }

        private Un1tTreeViewType GetUn1TCategoriesViewType()
        {
            var type = Un1tTreeViewType.Normal;
            var tag = Un1tCategoriesView.Tag;
            if (tag != null)
            {
                type = (Un1tTreeViewType)tag;
            }
            return type;
        }

        private void ProviderCategoryTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_firstLoadCategory) return;
            Dispatcher.Invoke(() =>
            {
                var activeTab = ProviderCategoryTabControl.SelectedItem as TabItem;
                if (activeTab != null)
                {
                    var name = activeTab.Header.ToString().Trim().ToLower();
                    var treeViewsDict = new Dictionary<string, TreeView>()
                    {
                        {"merlion", MerlionCategoriesView},
                        {"treolan", TreolanCategoriesView},
                        {"oldi", OldiCategoriesView},
                        {"ocs", OcsCategoriesView}
                    };
                    if (treeViewsDict.ContainsKey(name))
                    {
                        _activeProviderTreeView = treeViewsDict[name];
                        var provider = (ProviderType)_activeProviderTreeView.Tag;
                        if (!_providerCategoriesViewModels.ContainsKey(provider))
                        {
                            AddProviderCategoryTreeViewModels(_activeProviderTreeView);
                        }
                        RefreshActiveCatalogView();
                    }
                }
            });
        }

        private void RefreshActiveCatalogView()
        {
            Dispatcher.Invoke(() =>
            {
                var treeView = _activeProviderTreeView;
                if (treeView != null)
                {
                    var un1tSelectedItem = Un1tCategoriesView.SelectedItem as TreeViewItem;
                    if (un1tSelectedItem != null)
                    {
                        var categoryView = un1tSelectedItem.Header as Un1tCategoryItemView;
                        if (categoryView != null)
                        {
                            var model = categoryView.GetModel();
                            if (model != null)
                            {
                                var id = model.Id;
                                var isReadyToAction = _un1tCategories.Count(x => x.IdParent == id) == 0;
                                SetProviderCategoryTreeViewState(treeView, isReadyToAction, id);
                            }
                        }
                    }
                    else
                    {
                        SetUnselectedViewForTreeView(treeView);
                    }
                }
            });
        }

        private void SetUnselectedViewForTreeView(TreeView treeView)
        {
            var tag = treeView.Tag;
            if (tag == null) return;
            var provider = (ProviderType)tag;
            if (!_providerCategoriesViewModels.ContainsKey(provider)) return;
            var viewModels = _providerCategoriesViewModels[provider];
            foreach (var viewModel in viewModels)
            {
                var item = viewModel.Item;
                item.SetHighlight(CategoryHighlight.None);
                item.SetEnabledIsLoad(false);
                item.SetChecked(false);
                item.ToolTip = null;
            }
            var mappedCategories = viewModels.Where(x => x.Model.IdUn1tCategory != -1 && x.Model.IsLoad).ToList();
            foreach (var mappedCategory in mappedCategories)
            {
                mappedCategory.Item.SetChecked(true);
            }
            var parentsIds = mappedCategories.Select(x => x.Model.IdParent)
                    .Distinct()
                    .Where(x => !mappedCategories.Select(y => y.Model.Id).Contains(x))
                    .ToList();
            if (parentsIds.Any())
            {
                var parentsViews = viewModels.Where(x => parentsIds.Contains(x.Model.Id)).ToList();
                while (parentsViews.Any())
                {
                    foreach (var view in parentsViews)
                    {
                        var isFullLoad = viewModels.Count(x => x.Model.IdParent == view.Model.Id) ==
                                         viewModels.Count(
                                             x => x.Model.IdParent == view.Model.Id && x.Model.IdUn1tCategory != -1);
                        if (isFullLoad)
                        {
                            view.Item.SetChecked(true);
                        }
                        else
                        {
                            view.Item.SetChecked(null);
                        }
                    }
                    parentsIds = parentsViews.Select(x => x.Model.IdParent).Distinct().ToList();
                    parentsViews = viewModels.Where(x => parentsIds.Contains(x.Model.Id)).ToList();
                }
            }
        }

        private void SetProviderCategoryTreeViewState(TreeView treeView, bool isAction, int un1tCategoryId)
        {
            var tag = treeView.Tag;
            if (tag == null) return;
            var provider = (ProviderType)tag;
            if (!_providerCategoriesViewModels.ContainsKey(provider)) return;
            var viewModels = _providerCategoriesViewModels[provider];
            foreach (var viewModel in viewModels)
            {
                var item = viewModel.Item;
                item.SetHighlight(CategoryHighlight.None);
                item.SetEnabledIsLoad(isAction);
                item.ToolTip = null;
            }
            if (isAction)
            {
                var anotherMappedCategories =
                    viewModels.Where(
                        x =>
                            x.Model.IdUn1tCategory != -1 && x.Model.IdUn1tCategory != un1tCategoryId &&
                            x.Model.IsLoad).ToList();
                foreach (var mappedCategory in anotherMappedCategories)
                {
                    mappedCategory.Item.SetHighlight(CategoryHighlight.NoAction);
                    mappedCategory.Item.SetEnabledIsLoad(false);
                    mappedCategory.Item.ToolTip = GetToolTipForCategory(mappedCategory.Model.IdUn1tCategory);
                }
                var parentsIds = anotherMappedCategories.Select(x => x.Model.IdParent)
                    .Distinct()
                    .Where(x => !anotherMappedCategories.Select(y => y.Model.Id).Contains(x))
                    .ToList();
                if (parentsIds.Any())
                {
                    var providerCategories = viewModels.Select(x => x.Model).ToList();
                    var parentsViews = viewModels.Where(x => parentsIds.Contains(x.Model.Id)).ToList();
                    while (parentsViews.Any())
                    {
                        foreach (var view in parentsViews)
                        {
                            view.Item.SetEnabledIsLoad(false);
                            var highlight = CategoryHighlight.NoAction;
                            var allChilds = new List<ProviderServiceCategoryBase>();
                            var childs = viewModels.Where(x => x.Model.IdParent == view.Model.Id).ToList();
                            foreach (var providerCategory in childs)
                            {
                                GetAllChildsForProviderCategory(providerCategory.Model, allChilds, providerCategories);
                            }
                            if (allChilds.Any())
                            {
                                var isMappedForCategoryAny = allChilds.Any(x => x.IdUn1tCategory == un1tCategoryId);
                                if (isMappedForCategoryAny)
                                {
                                    highlight = CategoryHighlight.SemiLoadAction;
                                }
                                else
                                {
                                    var isNotAllChildsMappedForAnotherCategory =
                                        allChilds.Where(x => x.IdParent == view.Model.Id)
                                            .Any(x => x.IdUn1tCategory == -1);
                                    if (isNotAllChildsMappedForAnotherCategory)
                                    {
                                        highlight = CategoryHighlight.SemiAction;
                                    }
                                }
                            }
                            view.Item.SetHighlight(highlight);
                        }
                        parentsIds = parentsViews.Select(x => x.Model.IdParent).Distinct().ToList();
                        parentsViews = viewModels.Where(x => parentsIds.Contains(x.Model.Id)).ToList();
                    }
                }
                var mappedCategories =
                   viewModels.Where(x => x.Model.IdUn1tCategory == un1tCategoryId && x.Model.IsLoad)
                       .ToList();
                foreach (var mappedCategory in mappedCategories)
                {
                    mappedCategory.Item.SetHighlight(CategoryHighlight.FullLoad);
                }
                parentsIds = mappedCategories.Select(x => x.Model.IdParent)
                    .Distinct()
                    .Where(x => !mappedCategories.Select(y => y.Model.Id).Contains(x))
                    .ToList();
                if (parentsIds.Any())
                {
                    var parentsViews = viewModels.Where(x => parentsIds.Contains(x.Model.Id)).ToList();
                    while (parentsViews.Any())
                    {
                        foreach (var view in parentsViews)
                        {
                            view.Item.SetHighlight(CategoryHighlight.SemiLoad);
                        }
                        parentsIds = parentsViews.Select(x => x.Model.IdParent).Distinct().ToList();
                        parentsViews = viewModels.Where(x => parentsIds.Contains(x.Model.Id)).ToList();
                    }
                }
            }
            else
            {
                var allUn1tCategories = new List<int>();
                var child = _un1tCategories.Where(x => x.IdParent == un1tCategoryId).ToList();
                if (child.Any())
                {
                    foreach (var category in child)
                    {
                        GetAllChildsForUn1tCategory(category.Id, allUn1tCategories);
                    }
                }
                var lowLevelCategories =
                    allUn1tCategories.Where(x => _un1tCategories.Count(y => y.IdParent == x) == 0).ToList();
                if (!lowLevelCategories.Any()) return;
                var mappedCategories =
                    viewModels.Where(x => lowLevelCategories.Contains(x.Model.IdUn1tCategory) && x.Model.IsLoad)
                        .ToList();
                foreach (var mappedCategory in mappedCategories)
                {
                    mappedCategory.Item.SetHighlight(CategoryHighlight.SemiLoad);
                }
                var parentsIds = mappedCategories.Select(x => x.Model.IdParent)
                    .Distinct()
                    .Where(x => !mappedCategories.Select(y => y.Model.Id).Contains(x))
                    .ToList();
                if (parentsIds.Any())
                {
                    var parentsViews = viewModels.Where(x => parentsIds.Contains(x.Model.Id)).ToList();
                    while (parentsViews.Any())
                    {
                        foreach (var view in parentsViews)
                        {
                            view.Item.SetHighlight(CategoryHighlight.SemiLoad);
                        }
                        parentsIds = parentsViews.Select(x => x.Model.IdParent).Distinct().ToList();
                        parentsViews = viewModels.Where(x => parentsIds.Contains(x.Model.Id)).ToList();
                    }
                }
            }
        }

        private void AddProviderCategoryTreeViewModels(TreeView treeView)
        {
            Dispatcher.Invoke(() =>
            {
                var provider = (ProviderType)treeView.Tag;
                if (_providerCategoriesViewModels.ContainsKey(provider))
                {
                    _providerCategoriesViewModels.Remove(provider);
                }
                var list = new List<TreeViewItem>();
                var items = treeView.Items;
                if (items != null)
                {
                    foreach (TreeViewItem item in items)
                    {
                        GetChildsForTreeView(item, list);
                    }
                }
                if (list.Any())
                {
                    var viewModels = new List<ProviderCategoryViewModel>();
                    foreach (var treeViewItem in list)
                    {
                        var categoryView = treeViewItem.Header as CategoryItemView;
                        if (categoryView != null)
                        {
                            var viewModel = new ProviderCategoryViewModel()
                            {
                                Item = categoryView,
                                Model = categoryView.GetModel()
                            };
                            viewModels.Add(viewModel);
                        }
                    }
                    if (viewModels.Any())
                    {
                        _providerCategoriesViewModels.Add(provider, viewModels);
                    }
                }
            });
        }

        private void GetChildsForTreeView(TreeViewItem item, List<TreeViewItem> list)
        {
            list.Add(item);
            var childs = item.Items;
            if (childs != null)
            {
                foreach (TreeViewItem child in childs)
                {
                    GetChildsForTreeView(child, list);
                }
            }
        }

        #endregion

        #endregion

        #region Service

        private async void btnGetAllProviderProductsAndPricies_Click(object sender, RoutedEventArgs e)
        {
            _loadingData = true;
            _productLoaderCounter = 4;
            _directLoaderCounter = 4;
            _providerProducts.Clear();
            _providerNewProducts.Clear();
            ManualManagePanel.IsEnabled = false;
            AutoManagePanel.IsEnabled = false;
            WriteToLog("Обновление курсов валют и прочих справочников");
            txtMerlionProductsCount.Text = "Merlion: 0";
            txtTreolanProductsCount.Text = "Treolan: 0";
            txtOcsProductsCount.Text = "OCS: 0";
            txtOldiProductsCount.Text = "OLDI: 0";
            txtTotalProductsCount.Text = "Всего: 0";
            await Task.Run(() =>
            {
                try
                {
                    var services = new IDataService[] { _merlionService, _ocsService, _oldiService, _treolanService };
                    foreach (var dataService in services)
                    {
                        RefreshServiceVariable(dataService);
                    }
                }
                catch (Exception ex)
                {
                    _loadingData = false;
                    Log.WriteToLog(LogSender + " GetAllProviderProductsAndPricies() ", "Ошибка: " + ex.Message);
                    Dispatcher.Invoke(() =>
                    {
                        ManualManagePanel.IsEnabled = true;
                        AutoManagePanel.IsEnabled = true;
                    });
                }
            });
        }

        private async void btnStartAutoLoader_Click(object sender, RoutedEventArgs e)
        {
            if (_loadingData)
            {
                MessageBox.Show("Идет загрузка данных, повторите попытку позже");
                return;
            }
            try
            {
                ManualManagePanel.IsEnabled = false;
                IsLoadPropertiesAuto.IsEnabled = false;
                AutoUpdatePeriod.IsEnabled = false;
                btnStartAutoLoader.Visibility = Visibility.Collapsed;
                btnStopAutoLoader.Visibility = Visibility.Visible;
                _scheduleLoadRunning = true;
                WriteToLog("Запущена служба загрузки по расписанию");
                AutoLoad();
            }
            catch (Exception ex)
            {
                WriteToLog("Ошибка при запуске службы загрузки по расписанию");
                Log.WriteToLog(LogSender, " btnStartAutoLoader_Click() Ошибка: " + ex.Message);
                ManualManagePanel.IsEnabled = true;
                IsLoadPropertiesAuto.IsEnabled = true;
                AutoUpdatePeriod.IsEnabled = true;
                btnStartAutoLoader.Visibility = Visibility.Visible;
                btnStopAutoLoader.Visibility = Visibility.Collapsed;
            }

        }

        private void btnStopAutoLoader_Click(object sender, RoutedEventArgs e)
        {
            if (_autoLoaderTimer != null)
            {
                _autoLoaderTimer.Dispose();
                _autoLoaderTimer = null;
            }
            _scheduleLoadRunning = false;
            btnStartAutoLoader.Visibility = Visibility.Visible;
            btnStopAutoLoader.Visibility = Visibility.Collapsed;
            IsLoadPropertiesAuto.IsEnabled = true;
            AutoUpdatePeriod.IsEnabled = true;
            if (!_loadingData)
            {
                ManualManagePanel.IsEnabled = true;
            }
            else
            {
                AutoManagePanel.IsEnabled = false;
            }
            WriteToLog("Таймер загрузки снят");
        }

        private void SetAutoUpdateTimer()
        {
            Dispatcher.Invoke(() =>
            {
                var period = 60;
                var selectedPeriodItem = AutoUpdatePeriod.SelectedItem as ComboBoxItem;
                if (selectedPeriodItem != null && selectedPeriodItem.Tag != null)
                {
                    period = int.Parse((selectedPeriodItem.Tag).ToString());
                }
                _autoLoaderTimer = new System.Threading.Timer((state) => AutoLoad(), null, period * 60 * 1000,
                    Timeout.Infinite);
                var now = DateTime.Now;
                now = now.AddMilliseconds(period * 60 * 1000);
                WriteToLog("Таймер загрузки установлен. Следующая загрузка: " + now.ToString("dd.MM.yyyy HH:mm:ss"));
            });
        }

        private async void AutoLoad()
        {
            if (_autoLoaderTimer != null)
            {
                _autoLoaderTimer.Dispose();
                _autoLoaderTimer = null;
            }
            WriteToLog("Активация загрузки по расписанию");
            Dispatcher.Invoke(() =>
            {
                txtMerlionProductsCount.Text = "Merlion: 0";
                txtTreolanProductsCount.Text = "Treolan: 0";
                txtOcsProductsCount.Text = "OCS: 0";
                txtOldiProductsCount.Text = "OLDI: 0";
                txtTotalProductsCount.Text = "Всего: 0";
            });
            _loadingData = true;
            _productLoaderCounter = 4;
            _directLoaderCounter = 4;
            _providerProducts.Clear();
            _providerNewProducts.Clear();
            WriteToLog("Обновление курсов валют и прочих справочников");
            await Task.Run(() =>
            {
                try
                {
                    var services = new IDataService[] { _merlionService, _ocsService, _oldiService, _treolanService };
                    foreach (var dataService in services)
                    {
                        RefreshServiceVariable(dataService);
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteToLog(LogSender + " AutoLoad() ", "Ошибка: " + ex.Message);
                }
            });
        }

        private async void RefreshServiceVariable(IDataService service)
        {
            try
            {
                await Task.Run(() => service.RefreshServiceDirect());
                WriteToLog("Справочники " + service.Provider + " обновлены");
            }
            catch
            {
                WriteToLog("Ошибка при обновлении справочников " + service.Provider);
            }
            finally
            {
                var isComplete = Interlocked.Decrement(ref _directLoaderCounter) == 0;
                if (isComplete) ServiceDirectRefreshed();
            }
        }

        private void ServiceDirectRefreshed()
        {
            WriteToLog("Все справочники обновлены");
            Parallel.Invoke(
                () => { if (!GetProductsMultiThread(_merlionData, _merlionService)) GetMerlionProducts(); },
                () => { if (!GetProductsMultiThread(_treolanData, _treolanService)) GetTreolanProducts(); },
                () => { if (!GetProductsMultiThread(_oldiData, _oldiService)) GetOldiProducts(); },
                () => GetOcsProducts());
        }

        private bool GetProductsMultiThread(DataLevelBase dataLevel, IDataService service)
        {
            var isStarted = false;
            try
            {
                var categoriesToLoad = dataLevel.GetCategoriesToLoad();
                if (categoriesToLoad.Count() > 1)
                {
                    var threadCount = categoriesToLoad.Count() > ThreadCount - 1 ? ThreadCount : categoriesToLoad.Count();
                    service.GetProductsForCategoriesMultiThread(categoriesToLoad, ProductsLoadMultiThreadComplete,
                        threadCount);
                    WriteToLog("Стартовала загрузка товаров (многопоточно) " + service.Provider);
                    isStarted = true;
                }
            }
            catch (Exception ex)
            {
                Log.WriteToLog(LogSender, " GetProductsMultiThread(" + service.Provider + ") Ошибка: " + ex.Message);
                WriteToLog("Ошибка при загрузке товаров (многопоточно) " + service.Provider);
            }
            return isStarted;
        }

        private async void GetMerlionProducts()
        {
            try
            {
                WriteToLog("Стартовала загрузка товаров Merlion");
                var products = await GetProducts(_merlionData, _merlionService);
                Dispatcher.Invoke(() =>
                {
                    txtMerlionProductsCount.Text = "Merlion: " + products.Count();
                });
                PutProductsInDictionary(products, ProviderType.Merlion);
                WriteToLog("Товары Merlion загружены. Количество: " + products.Count());
            }
            catch
            {
                WriteToLog("Ошибка при загрузке товаров Merlion");
            }
            finally
            {
                var isComplete = Interlocked.Decrement(ref _productLoaderCounter) == 0;
                if (isComplete) LoadProductComplete();
            }
        }

        private async void GetTreolanProducts()
        {
            try
            {
                WriteToLog("Стартовала загрузка товаров Treolan");
                var products = await GetProducts(_treolanData, _treolanService);
                Dispatcher.Invoke(() =>
                {
                    txtTreolanProductsCount.Text = "Treolan: " + products.Count();
                });
                PutProductsInDictionary(products, ProviderType.Treolan);
                WriteToLog("Товары Treolan загружены. Количество: " + products.Count());
            }
            catch
            {
                WriteToLog("Ошибка при загрузке товаров Treolan");
            }
            finally
            {
                var isComplete = Interlocked.Decrement(ref _productLoaderCounter) == 0;
                if (isComplete) LoadProductComplete();
            }
        }

        private async void GetOcsProducts()
        {
            try
            {
                WriteToLog("Стартовала загрузка товаров OCS");
                var products = await GetProducts(_ocsData, _ocsService);
                Dispatcher.Invoke(() =>
                {
                    txtOcsProductsCount.Text = "OCS: " + products.Count();
                });
                PutProductsInDictionary(products, ProviderType.OCS);
                WriteToLog("Товары OCS загружены. Количество: " + products.Count());
            }
            catch
            {
                WriteToLog("Ошибка при загрузке товаров OCS");
            }
            finally
            {
                var isComplete = Interlocked.Decrement(ref _productLoaderCounter) == 0;
                if (isComplete) LoadProductComplete();
            }
        }

        private async void GetOldiProducts()
        {
            try
            {
                WriteToLog("Стартовала загрузка товаров OLDI");
                var products = await GetProducts(_oldiData, _oldiService);
                Dispatcher.Invoke(() =>
                {
                    txtOldiProductsCount.Text = "OLDI: " + products.Count();
                });
                PutProductsInDictionary(products, ProviderType.OLDI);
                WriteToLog("Товары OLDI загружены. Количество: " + products.Count());
            }
            catch
            {
                WriteToLog("Ошибка при загрузке товаров OLDI");
            }
            finally
            {
                var isComplete = Interlocked.Decrement(ref _productLoaderCounter) == 0;
                if (isComplete) LoadProductComplete();
            }
        }

        private async Task<List<Un1tProductBase>> GetProducts(DataLevelBase dataLevel, IDataService service)
        {
            var products = new List<Un1tProductBase>();
            try
            {
                var categoriesToLoad = dataLevel.GetCategoriesToLoad();
                products = await service.GetProductsForCategories(categoriesToLoad);
                if (products != null && products.Any())
                {
                    var now = DateTime.Now;
                    dataLevel.SetUn1tCategoryForProducts(products);
                    products.ForEach(x =>
                    {
                        x.Date = now;
                        if (x.Stocks != null)
                        {
                            x.Stocks.ForEach(y => y.Date = now);
                        }
                    });
                    products = products.Where(x => x.IdCategory != -1).ToList();
                }
            }
            catch (Exception ex)
            {
                Log.WriteToLog(LogSender, " GetProducts() Ошибка: " + ex.Message);
                throw;
            }
            return products;
        }

        private void ProductsLoadMultiThreadComplete(ProviderType provider, List<Un1tProductBase> products)
        {
            try
            {
                DataLevelBase dataLevel = null;
                switch (provider)
                {
                    case ProviderType.Merlion:
                        dataLevel = _merlionData;
                        break;
                    case ProviderType.Treolan:
                        dataLevel = _treolanData;
                        break;
                    case ProviderType.OCS:
                        dataLevel = _ocsData;
                        break;
                    case ProviderType.OLDI:
                        dataLevel = _oldiData;
                        break;
                }
                if (products != null && products.Any())
                {
                    var now = DateTime.Now;
                    if (dataLevel != null) dataLevel.SetUn1tCategoryForProducts(products);
                    products.ForEach(x =>
                    {
                        x.Date = now;
                        if (x.Stocks != null)
                        {
                            x.Stocks.ForEach(y => y.Date = now);
                        }
                    });
                    products = products.Where(x => x.IdCategory != -1).ToList();
                    Dispatcher.Invoke(() =>
                    {
                        TextBlock txt = null;
                        switch (provider)
                        {
                            case ProviderType.Merlion:
                                txt = txtMerlionProductsCount;
                                break;
                            case ProviderType.Treolan:
                                txt = txtTreolanProductsCount;
                                break;
                            case ProviderType.OCS:
                                txt = txtOcsProductsCount;
                                break;
                            case ProviderType.OLDI:
                                txt = txtOldiProductsCount;
                                break;
                        }
                        if (txt != null) txt.Text = provider + ": " + products.Count();
                    });
                    PutProductsInDictionary(products, provider);
                }
                WriteToLog("Товары " + provider + " загружены. Количество: " + (products != null ? products.Count() : 0));
            }
            catch (Exception ex)
            {
                Log.WriteToLog(LogSender + " ProductsLoadMultiThreadComplete(" + provider + ") ",
                    "Ошибка: " + ex.Message);
                WriteToLog("Ошибка при многопоточной загрузке товаров: " + provider);
            }
            finally
            {
                var isComplete = Interlocked.Decrement(ref _productLoaderCounter) == 0;
                if (isComplete) LoadProductComplete();
            }
        }

        private void PutProductsInDictionary(List<Un1tProductBase> products, ProviderType provider)
        {
            var isLocked = false;
            try
            {
                Monitor.Enter(_lockObj, ref isLocked);
                _providerProducts.Remove(provider);
                _providerProducts.Add(provider, products);
                var total = 0;
                foreach (var providerKey in _providerProducts.Keys)
                {
                    total += _providerProducts[providerKey].Count();
                }
                Dispatcher.Invoke(() =>
                {
                    txtTotalProductsCount.Text = "Всего: " + total;
                });
            }
            catch (Exception ex)
            {
                Log.WriteToLog(LogSender, " PutProductsInDictionary(" + provider + ") Ошибка" + ex.Message);
                throw;
            }
            finally
            {
                if (isLocked) Monitor.Exit(_lockObj);
            }
        }

        private void LoadProductComplete()
        {
            WriteToLog("Все товары загружены");
            GetAllProviderStocks();
        }

        private async void GetAllProviderStocks()
        {
            _priceLoaderCounter = 2;
            await Task.Run(() =>
            {
                try
                {
                    WriteToLog("Загрузка цен Merlion и OLDI...");
                    Parallel.Invoke(
                        () =>
                        {
                            if (!GetStocksMultiThread(_merlionData, _merlionService, ProviderType.Merlion))
                                GetMerlionStocks();
                        },
                        () =>
                        {
                            if (!GetStocksMultiThread(_oldiData, _oldiService, ProviderType.OLDI)) GetOldiStocks();
                        });
                }
                catch (Exception ex)
                {
                    Log.WriteToLog(LogSender + " GetAllProviderStocks() ", "Ошибка: " + ex.Message);
                }
            });
        }

        private bool GetStocksMultiThread(DataLevelBase dataLevel, IDataService service, ProviderType provider)
        {
            var isStarted = false;
            try
            {
                var categoriesToLoad = dataLevel.GetCategoriesToLoad();
                if (categoriesToLoad.Count() > 1)
                {
                    var threadCount = categoriesToLoad.Count() > ThreadCount - 1 ? ThreadCount : categoriesToLoad.Count();
                    service.GetStocksForCategoriesMultiThread(categoriesToLoad, StocksLoadMultiThreadComplete,
                        threadCount);
                    WriteToLog("Стартовала загрузка цен (многопоточно) " + provider);
                    isStarted = true;
                }
            }
            catch (Exception ex)
            {
                Log.WriteToLog(LogSender, " GetStocksMultiThread(" + provider + ") Ошибка: " + ex.Message);
                WriteToLog("Ошибка при загрузке цен (многопоточно) " + provider);
            }
            return isStarted;
        }

        private void StocksLoadMultiThreadComplete(ProviderType provider, List<StockServiceBase> stocks)
        {
            try
            {
                if (stocks != null && stocks.Any())
                {
                    var now = DateTime.Now;
                    foreach (var stockServiceBase in stocks)
                    {
                        stockServiceBase.Stock.Date = now;
                    }
                    PutStocksInDictionary(stocks, ProviderType.Merlion);
                }
                WriteToLog("Цены " + provider + " загружены");
            }
            catch (Exception ex)
            {
                Log.WriteToLog(LogSender + " StocksLoadMultiThreadComplete("+provider+") ", "Ошибка: " + ex.Message);
                WriteToLog("Ошибка при загрузке цен " + provider);
            }
            finally
            {
                var isComplete = Interlocked.Decrement(ref _priceLoaderCounter) == 0;
                if (isComplete) LoadStocksComplete();
            }
        }

        private async void GetMerlionStocks()
        {
            try
            {
                WriteToLog("Стартовала загрузка цен Merlion");
                var list = await GetStocks(_merlionData, _merlionService);
                if (list.Any())
                {
                    PutStocksInDictionary(list, ProviderType.Merlion);
                }
                WriteToLog("Цены Merlion загружены");
            }
            catch
            {
                WriteToLog("Ошибка при загрузке цен Merlion");
            }
            finally
            {
                var isComplete = Interlocked.Decrement(ref _priceLoaderCounter) == 0;
                if (isComplete) LoadStocksComplete();
            }
        }

        private async void GetOldiStocks()
        {
            try
            {
                WriteToLog("Стартовала загрузка цен OLDI");
                var list = await GetStocks(_oldiData, _oldiService);
                if (list.Any())
                {
                    PutStocksInDictionary(list, ProviderType.OLDI);
                }
                WriteToLog("Цены OLDI загружены");
            }
            catch
            {
                WriteToLog("Ошибка при загрузке цен OLDI");
            }
            finally
            {
                var isComplete = Interlocked.Decrement(ref _priceLoaderCounter) == 0;
                if (isComplete) LoadStocksComplete();
            }
        }

        private async Task<List<StockServiceBase>> GetStocks(DataLevelBase dataLevel, IDataService service)
        {
            var list = new List<StockServiceBase>();
            try
            {
                var categoriesToLoad = dataLevel.GetCategoriesToLoad();
                foreach (var category in categoriesToLoad)
                {
                    var stocks = await service.GetStocksAsync(category.Id);
                    if (stocks.Any())
                    {
                        list.AddRange(stocks);
                    }
                }
                var now = DateTime.Now;
                foreach (var stockServiceBase in list)
                {
                    stockServiceBase.Stock.Date = now;
                }
            }
            catch (Exception ex)
            {
                Log.WriteToLog(LogSender, " GetStocks(" + service.Provider + ") Ошибка: " + ex.Message);
                throw;
            }
            return list;
        }

        private void PutStocksInDictionary(List<StockServiceBase> stocks, ProviderType provider)
        {
            var isLocked = false;
            try
            {
                Monitor.Enter(_lockObj, ref isLocked);
                if (_providerProducts.ContainsKey(provider))
                {
                    var products = _providerProducts[provider];
                    if (products != null && products.Any())
                    {
                        products.ForEach((x) =>
                        {
                            var productStocks =
                                stocks.Where(y => y.IdProductProvider == x.IdProvider).Select(y => y.Stock);
                            x.Stocks.AddRange(productStocks);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteToLog(LogSender, " PutStocksInDictionary(" + provider + ") Ошибка" + ex.Message);
                throw;
            }
            finally
            {
                if (isLocked) Monitor.Exit(_lockObj);
            }
        }

        private async void LoadStocksComplete()
        {
            WriteToLog("Все цены загружены");
            await SaveProductsInStorage();
            GetNewProductsProperties();
        }

        private async Task SaveProductsInStorage()
        {
            WriteToLog("Сохранение полученных товаров в хранилище...");
            await Task.Run(() =>
            {
                var currentProvider = ProviderType.Merlion;
                try
                {
                    var maximum = _providerProducts.Values.Sum(x => x.Count());
                    if (maximum > 0)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            StoragePanel.Visibility = Visibility.Visible;
                            DataSaveCount.Tag = maximum;
                            DataSaveCount.Content = "Осталось: " + maximum;
                            DataSaveProgress.Value = 0;
                            DataSaveProgress.Maximum = maximum;
                        });
                    }
                    var providers = new[]
                    {ProviderType.Merlion, ProviderType.OCS, ProviderType.OLDI, ProviderType.Treolan};
                    var db = new DbEngine();
                    foreach (var provider in providers)
                    {
                        if (_providerProducts.ContainsKey(provider))
                        {
                            var products = _providerProducts[provider];
                            if (products.Any())
                            {
                                var newProducts = db.SaveProducts(products, provider,() => UpdateDataSaveProgress(1));
                                WriteToLog("Сохранено " + products.Count() + " товаров " + provider +
                                           ", новых товаров: " +
                                           newProducts.Count());
                                if (newProducts.Any())
                                {
                                    _providerNewProducts.Add(provider, newProducts);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteToLog(LogSender + " SaveProductsInStorage(" + currentProvider + ") ",
                        "Ошибка: " + ex.Message);
                    WriteToLog("Ошибка при сохранении товаров " + currentProvider);
                }
                finally
                {
                    Dispatcher.Invoke(() =>
                    {
                        StoragePanel.Visibility = Visibility.Collapsed;
                    });
                }
            });
            WriteToLog("Сохранение завершено");
        }

        private async void GetNewProductsProperties()
        {
            var isPropertiesLoad = false;
            Dispatcher.Invoke(() =>
            {
                if (_scheduleLoadRunning)
                {
                    isPropertiesLoad = IsLoadPropertiesAuto.IsChecked.HasValue && IsLoadPropertiesAuto.IsChecked.Value;
                }
                else
                {
                    isPropertiesLoad = IsLoadProperties.IsChecked.HasValue && IsLoadProperties.IsChecked.Value;
                }
            });
            if (isPropertiesLoad)
            {
                Dispatcher.Invoke(() =>
                {
                    PropertyPanel.Visibility = Visibility.Visible;
                    PropertyLoadProgress.Value = 0;
                    var maximum = 0;
                    if (_providerNewProducts.Keys.Any())
                    {
                        foreach (var provider in _providerNewProducts.Keys)
                        {
                            if (_providerNewProducts[provider] != null && _providerNewProducts[provider].Any())
                            {
                                maximum += _providerNewProducts[provider].Count();
                            }
                        }
                    }
                    PropertyLoadProgress.Maximum = maximum;
                    PropertiesLoadCount.Tag = maximum;
                    PropertiesLoadCount.Content = "Осталось обработать: " + maximum;
                    PropertiesLoadedCount.Tag = 0;
                    PropertiesLoadedCount.Content = "Получено характеристик: " + 0;
                });
                _propertyLoaderCounter = _providerNewProducts.Keys.Count;
                await Task.Run(() =>
                {
                    try
                    {
                        if (_providerNewProducts.Keys.Any())
                        {
                            WriteToLog("Загрузка характеристик для новых товаров...");
                            foreach (var provider in _providerNewProducts.Keys)
                            {
                                if (_providerNewProducts[provider] != null && _providerNewProducts[provider].Any())
                                {
                                    if ((provider == ProviderType.Merlion || provider == ProviderType.Treolan) && _providerNewProducts[provider].Count() > 20)
                                    {
                                        Task.Run(() =>
                                        {
                                            GetPropertiesForProductsMultiThread(provider);
                                        });
                                    }
                                    else
                                    {
                                        Task.Run(() =>
                                        {
                                            GetPropertiesForProducts(provider);
                                        });
                                    }
                                }
                            }
                        }
                        else
                        {
                            _loadingData = false;
                            WriteToLog("Новых товаров нет. Загрузка характеристик отменена");
                            Dispatcher.Invoke(() =>
                            {
                                PropertyPanel.Visibility = Visibility.Collapsed;
                                _providerProducts.Clear();
                                _providerNewProducts.Clear();
                                if (!_scheduleLoadRunning)
                                {
                                    ManualManagePanel.IsEnabled = true;
                                    AutoManagePanel.IsEnabled = true;
                                }
                                else
                                {
                                    SetAutoUpdateTimer();
                                }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.WriteToLog(LogSender + " GetNewProductsProperties() ", "Ошибка: " + ex.Message);
                    }
                });
            }
            else
            {
                _loadingData = false;
                _providerProducts.Clear();
                _providerNewProducts.Clear();
                if (!_scheduleLoadRunning)
                {
                    Dispatcher.Invoke(() =>
                    {
                        ManualManagePanel.IsEnabled = true;
                        AutoManagePanel.IsEnabled = true;
                    });
                }
                else
                {
                    SetAutoUpdateTimer();
                }
            }
        }

        private void UpdatePropertyProgress(int loaded = 0)
        {
            Dispatcher.Invoke(() =>
            {
                PropertyLoadProgress.Value++;
                var tag = PropertiesLoadCount.Tag;
                if (tag != null)
                {
                    var count = (int)tag;
                    count--;
                    PropertiesLoadCount.Tag = count;
                    PropertiesLoadCount.Content = "Осталось обработать: " + count;
                    if (loaded != 0)
                    {
                        tag = PropertiesLoadedCount.Tag;
                        if (tag != null)
                        {
                            count = (int) tag;
                            count += loaded;
                            PropertiesLoadedCount.Tag = count;
                            PropertiesLoadedCount.Content = "Получено характеристик: " + count;
                        }
                    }
                }
            });
        }

        private void GetPropertiesForProductsMultiThread(ProviderType provider)
        {
            try
            {
                var newProducts = _providerNewProducts[provider];
                if (newProducts.Any())
                {
                    IDataService service = null;
                    switch (provider)
                    {
                        case ProviderType.Merlion:
                            service = _merlionService;
                            break;
                        case ProviderType.Treolan:
                            service = _treolanService;
                            break;
                    }
                    if (service != null)
                    {
                        WriteToLog("Стартовала загрузка характеристик (многопоточно) " + provider);
                        var productsId = new List<string>();
                        if (provider == ProviderType.Treolan)
                        {
                            productsId = newProducts.Select(x => x.PartNumber).ToList();
                        }
                        if (provider == ProviderType.Merlion)
                        {
                            productsId = newProducts.Select(x => x.IdProvider).ToList();
                        }
                        service.GetPropertiesForProductsMultiThread(productsId, PropertiesLoadMultiThreadComplete,
                            UpdatePropertyProgress,
                            (productsId.Count() > ThreadCount - 1)
                                ? ThreadCount
                                : productsId.Count());
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteToLog(LogSender + " GetPropertiesForProductsMultiThread(" + provider + ") ", "Ошибка: " + ex.Message);
                WriteToLog("Ошибка при загрузке характеристик (многопоточно) " + provider);
            }
        }

        private void PropertiesLoadMultiThreadComplete(ProviderType provider, List<PropertyProductBase> properties)
        {
            try
            {
                var newProducts = _providerNewProducts[provider];
                if (properties != null && properties.Any())
                {
                    var validProperies =
                        properties.Where(
                            x =>
                                !string.IsNullOrEmpty(x.IdPropertyProvider) &&
                                !string.IsNullOrEmpty(x.Name) &&
                                !string.IsNullOrEmpty(x.Value)).ToList();
                    if (validProperies.Any())
                    {
                        foreach (var product in newProducts)
                        {
                            if (provider == ProviderType.Treolan)
                            {
                                product.Properties.AddRange(validProperies.Where(x => x.IdProductProvider == product.PartNumber));
                            }
                            else
                            {
                                product.Properties.AddRange(validProperies.Where(x => x.IdProductProvider == product.IdProvider));
                            }
                        }
                    }
                }
                WriteToLog("Характеристики " + provider + " загружены. Количество: " + (properties != null
                    ? properties.Count()
                    : 0));
            }
            finally
            {
                var isComplete = Interlocked.Decrement(ref _propertyLoaderCounter) == 0;
                if (isComplete) LoadPropertiesComplete();
            }
        }

        private async void GetPropertiesForProducts(ProviderType provider)
        {
            try
            {
                var newProducts = _providerNewProducts[provider];
                if (newProducts.Any())
                {
                    IDataService service = null;
                    switch (provider)
                    {
                        case ProviderType.Merlion:
                            service = _merlionService;
                            break;
                        case ProviderType.Treolan:
                            service = _treolanService;
                            break;
                        case ProviderType.OLDI:
                            service = _oldiService;
                            break;
                        case ProviderType.OCS:
                            service = _ocsService;
                            break;
                    }
                    if (service != null)
                    {
                        WriteToLog("Стартовала загрузка характеристик " + provider);
                        var propertiesCount = 0;
                        if (provider != ProviderType.OLDI)
                        {
                            foreach (var product in newProducts)
                            {
                                var productId = product.IdProvider;
                                if (provider == ProviderType.Treolan)
                                {
                                    productId = product.PartNumber;
                                }
                                var properties = await service.GetPropertiesForProductAsync(productId);
                                UpdatePropertyProgress();
                                if (properties != null && properties.Any())
                                {
                                    propertiesCount += properties.Count();
                                    var validProperies =
                                        properties.Where(
                                            x =>
                                                !string.IsNullOrEmpty(x.IdPropertyProvider) &&
                                                !string.IsNullOrEmpty(x.Name) &&
                                                !string.IsNullOrEmpty(x.Value)).ToList();
                                    if (validProperies.Any())
                                    {
                                        product.Properties.AddRange(validProperies);
                                    }
                                }
                            }
                        }
                        else
                        {
                            var properties =
                                await
                                    _oldiService.GetProductsPropertiesAsync(null,
                                        newProducts.Select(x => x.IdProvider).ToList());
                            if (properties != null && properties.Any())
                            {
                                propertiesCount = properties.Count();
                                var validProperies =
                                    properties.Where(
                                        x =>
                                            !string.IsNullOrEmpty(x.IdPropertyProvider) &&
                                            !string.IsNullOrEmpty(x.Name) &&
                                            !string.IsNullOrEmpty(x.Value)).ToList();
                                if (validProperies.Any())
                                {
                                    foreach (var product in newProducts)
                                    {
                                        var productProperties =
                                            validProperies.Where(x => x.IdProductProvider == product.IdProvider)
                                                .ToList();
                                        product.Properties.AddRange(productProperties);
                                        UpdatePropertyProgress(productProperties.Count());
                                    }
                                }
                            }
                        }
                        WriteToLog("Характеристики " + provider + " загружены. Количество: " + propertiesCount);
                    }
                }

            }
            catch (Exception ex)
            {
                Log.WriteToLog(LogSender + " GetPropertiesForProducts(" + provider + ") ", "Ошибка: " + ex.Message);
                WriteToLog("Ошибка при загрузке характеристик " + provider);
            }
            finally
            {
                var isComplete = Interlocked.Decrement(ref _propertyLoaderCounter) == 0;
                if (isComplete) LoadPropertiesComplete();
            }
        }

        private async void LoadPropertiesComplete()
        {
            var isMultiThreads = false;
            try
            {
                WriteToLog("Все характеристики загружены");
                Dispatcher.Invoke(() =>
                {
                    PropertyPanel.Visibility = Visibility.Collapsed;
                });
                isMultiThreads = await SaveProperiesProductsInStorage();
            }
            finally
            {
                _loadingData = false;
                if (!isMultiThreads)
                {
                    if (!_scheduleLoadRunning)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            ManualManagePanel.IsEnabled = true;
                            AutoManagePanel.IsEnabled = true;
                        });
                    }
                    else
                    {
                        SetAutoUpdateTimer();
                    }
                }
            }
        }

        private async Task<bool> SaveProperiesProductsInStorage()
        {
            WriteToLog("Сохранение полученных характеристик в хранилище...");
            var isMultiThreads = false;
            await Task.Run(() =>
            {
                try
                {
                    var maximum = _providerNewProducts.Values.SelectMany(x => x.SelectMany(y => y.Properties)).Count();
                    if (maximum > 0)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            StoragePanel.Visibility = Visibility.Visible;
                            DataSaveCount.Tag = maximum;
                            DataSaveCount.Content = "Осталось: " + maximum;
                            DataSaveProgress.Value = 0;
                            DataSaveProgress.Maximum = maximum;
                        });
                    }
                    _propertiesSaveCounter = ThreadCount;
                    var db = new DbEngine();
                    var products = _providerNewProducts.Values.SelectMany(x => x.Select(y => y)).ToList();
                    if (products.Any())
                    {
                        var productWithProperties = products.Where(x => x.Properties.Any()).ToList();
                        if (productWithProperties.Any())
                        {
                            if (productWithProperties.Count() < ThreadCount)
                            {
                                db.SaveProductsProperties(productWithProperties, UpdateDataSaveProgress);
                                WriteToLog("Сохранено " +
                                           productWithProperties.SelectMany(x => x.Properties).Count() +
                                           " характеристик");
                            }
                            else
                            {
                                if (!isMultiThreads) isMultiThreads = true;
                                WriteToLog("Сохранение характеристик многопоточно");
                                SaveProperiesProductsForProvideInStorageMultiThread(productWithProperties);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteToLog(LogSender, " SaveProperiesProductsInStorage() Ошибка: " + ex.Message);
                    WriteToLog("Ошибка при сохранении характеристик");
                }
                finally
                {
                    if (!isMultiThreads) Dispatcher.Invoke(() => StoragePanel.Visibility = Visibility.Collapsed);
                }
            });
            if (!isMultiThreads)
            {
                _providerProducts.Clear();
                _providerNewProducts.Clear();
                WriteToLog("Сохранение характеристик завершено");
            }
            return isMultiThreads;
        }

        private void SaveProperiesProductsForProvideInStorageMultiThread(List<Un1tProductBase> products)
        {
            try
            {
                var partSize = (products.Count() - (products.Count() % ThreadCount)) / ThreadCount;
                var mod = products.Count() % ThreadCount;
                var skip = 0;
                for (var i = 0; i < ThreadCount; i++)
                {
                    var size = partSize;
                    if (i < mod)
                    {
                        size += 1;
                    }
                    var partData = products.Skip(skip).Take(size).ToList();
                    SavePropertiesPardData(partData);
                    skip += size;
                }
            }
            catch (Exception ex)
            {
                Log.WriteToLog(LogSender, " SaveProperiesProductsForProvideInStorageMultiThread() Ошибка: " + ex.Message);
                WriteToLog("Ошибка при сохранении характеристик ");
            }
        }

        private async void SavePropertiesPardData(List<Un1tProductBase> products)
        {
            try
            {
                await Task.Run(() =>
                {
                    var db = new DbEngine();
                    db.SaveProductsProperties(products, UpdateDataSaveProgress);
                });

            }
            catch (Exception ex)
            {
                Log.WriteToLog(LogSender, " SavePropertiesPardData() Ошибка: " + ex.Message);
                throw;
            }
            finally
            {
                var isComplete = Interlocked.Decrement(ref _propertiesSaveCounter) == 0;
                if (isComplete) SavePropertiesMultiThreadComplete();
            }
        }

        private void SavePropertiesMultiThreadComplete()
        {
            WriteToLog("Сохранение характеристик завершено. Сохраненo: "
                           + _providerNewProducts.Values.SelectMany(x => x.SelectMany(y => y.Properties)).Count());
            _providerProducts.Clear();
            _providerNewProducts.Clear();
            Dispatcher.Invoke(() => StoragePanel.Visibility = Visibility.Collapsed);
            if (!_scheduleLoadRunning)
            {
                Dispatcher.Invoke(() =>
                {
                    ManualManagePanel.IsEnabled = true;
                    AutoManagePanel.IsEnabled = true;
                });
            }
            else
            {
                SetAutoUpdateTimer();
            }
        }

        public void UpdateDataSaveProgress(int loaded)
        {
            if (loaded == 0) return;
            Dispatcher.Invoke(() =>
            {
                var tag = DataSaveCount.Tag;
                if (tag != null)
                {
                    var count = (int)tag;
                    count -= loaded;
                    DataSaveCount.Tag = count;
                    DataSaveCount.Content = "Осталось: " + count;
                    DataSaveProgress.Value += loaded;
                }
            });
        }

        #endregion

        private void WriteToLog(string message)
        {
            var messageCountLimit = 100;
            Dispatcher.Invoke(() =>
            {
                lstProductsLog.Items.Insert(0, DateTime.Now.ToString("HH:mm:ss") + " " + message);
                if (lstProductsLog.Items.Count > messageCountLimit)
                {
                    while (lstProductsLog.Items.Count > messageCountLimit)
                    {
                        lstProductsLog.Items.RemoveAt(lstProductsLog.Items.Count - 1);
                    }
                }
                lstProductsLog.ScrollIntoView(lstProductsLog.Items[0]);
            });
        }
    }

    public class DeleteItem
    {
        public ProviderServiceCategoryBase Category { get; set; }

        public ProviderType Provider { get; set; }
    }

    public enum Un1tTreeViewType
    {
        Normal = 1,
        Mapped = 2
    }
}
