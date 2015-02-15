using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using EPriceProviderServices.MerlionDataService;
using EPriceProviderServices.Models;
using EPriceRequestServiceDBEngine.Enums;
using EPriceRequestServiceDBEngine.Models;

namespace EPriceProviderServices.Helpers
{
    public class MerlionServiceWrapper : IDisposable, IDataService
    {
        #region Fields

        private MLPortClient _proxy;
        private readonly int _itemsPageSize;
        private readonly string _logSender;
        private readonly ProviderType _provider;
        private bool _isRunning;
        private readonly List<ShipmentMethodsResult> _shipmentMethods;
        private readonly List<CurrencyRateResult> _currencyRates;
        private readonly object _lockObj;
        private readonly string _login;
        private readonly string _password;
        private int _properiesLoadCounter;
        private Action<ProviderType, List<PropertyProductBase>> _propertiesLoadCompleteAction;
        private List<PropertyProductBase> _propertiesList;
        private int _productsLoadCounter;
        private Action<ProviderType, List<Un1tProductBase>> _productsLoadCompleteAction;
        private List<Un1tProductBase> _productsList;
        private int _stocksLoadCounter;
        private Action<ProviderType, List<StockServiceBase>> _stocksLoadCompleteAction;
        private List<StockServiceBase> _stocksList;

        #endregion

        public MerlionServiceWrapper()
        {
            _provider = ProviderType.Merlion;
            _lockObj = new object();
            _isRunning = false;
            _logSender = "Merlion служба";
            _shipmentMethods = new List<ShipmentMethodsResult>();
            _currencyRates = new List<CurrencyRateResult>();
            _login = ConfigurationManager.AppSettings.Get("MerlionServiceLogin");
            _password = ConfigurationManager.AppSettings.Get("MerlionServicePassword");
            _itemsPageSize = int.Parse(ConfigurationManager.AppSettings.Get("MerlionServiceItemsPageSize"));
            try
            {
                _proxy = new MLPortClient();
                if (_proxy.ClientCredentials != null)
                {
                    _proxy.ClientCredentials.UserName.UserName = _login;
                    _proxy.ClientCredentials.UserName.Password = _password;
                }
                _proxy.Open();
            }
            catch (Exception ex)
            {
                _proxy = null;
                Log.WriteToLog(_logSender + " Конструктор() ", "Ошибка: " + ex.Message);
                throw;
            }
        }

        public bool IsRunning
        {
            get { return _isRunning; }
        }

        public ProviderType Provider
        {
            get { return _provider; }
        }

        #region Server Direct

        public void RefreshServiceDirect()
        {
            RefreshShipmentMethods();
            RefreshCurrencyRates();
        }

        private void RefreshShipmentMethods()
        {
            var isLocked = false;
            try
            {
                Monitor.Enter(_lockObj, ref isLocked);
                _isRunning = true;
                var methods = _proxy.getShipmentMethods(string.Empty);
                if (methods != null && methods.Any())
                {
                    _shipmentMethods.Clear();
                    _shipmentMethods.AddRange(methods);
                }
            }
            catch (Exception ex)
            {
                _proxy = new MLPortClient();
                if (_proxy.ClientCredentials != null)
                {
                    _proxy.ClientCredentials.UserName.UserName = _login;
                    _proxy.ClientCredentials.UserName.Password = _password;
                }
                _proxy.Open();
                Log.WriteToLog(_logSender + " GetShipmentMethods() ", "Ошибка: " + ex.Message);
                throw;
            }
            finally
            {
                _isRunning = false;
                if (isLocked) Monitor.Exit(_lockObj);
            }
        }

        private void RefreshCurrencyRates()
        {
            var isLocked = false;
            try
            {
                Monitor.Enter(_lockObj, ref isLocked);
                _isRunning = true;
                var rates = _proxy.getCurrencyRate(null);
                if (rates != null && rates.Any())
                {
                    _currencyRates.Clear();
                    _currencyRates.AddRange(rates);
                }
            }
            catch (Exception ex)
            {
                _proxy = new MLPortClient();
                if (_proxy.ClientCredentials != null)
                {
                    _proxy.ClientCredentials.UserName.UserName = _login;
                    _proxy.ClientCredentials.UserName.Password = _password;
                }
                _proxy.Open();
                Log.WriteToLog(_logSender + " GetCurrencyRate() ", "Ошибка: " + ex.Message);
                throw;
            }
            finally
            {
                _isRunning = false;
                if (isLocked) Monitor.Exit(_lockObj);
            }
        }

        #endregion

        #region Catalogs

        public async Task<ProviderServiceCategoryBase[]> GetAllCategoriesAsync()
        {
            return await GetCategoryAsync("All");
        }

        private async Task<ProviderServiceCategoryBase[]> GetCategoryAsync(string id)
        {
            var list = new List<ProviderServiceCategoryBase>();
            try
            {
                _isRunning = true;
                var result = await _proxy.getCatalogAsync(id);
                var catalogs = result.getCatalogResult;
                if (catalogs != null && catalogs.Any())
                {
                    list.AddRange(catalogs.Select(x => x.ConvertToDataModel()));
                }
            }
            catch (Exception ex)
            {
                _proxy = new MLPortClient();
                if (_proxy.ClientCredentials != null)
                {
                    _proxy.ClientCredentials.UserName.UserName = _login;
                    _proxy.ClientCredentials.UserName.Password = _password;
                }
                _proxy.Open();
                Log.WriteToLog(_logSender + " GetCategoryAsync(" + id + ") ", "Ошибка: " + ex.Message);
                throw;
            }
            finally
            {
                _isRunning = false;
            }
            return list.ToArray();
        }

        #endregion

        #region Products

        public async Task<List<Un1tProductBase>> GetProductsForCategories(List<ProviderServiceCategoryBase> categories)
        {
            var list = new List<Un1tProductBase>();
            if (!categories.Any())
            {
                return list;
            }
            foreach (var category in categories)
            {
                var result = await GetProductsForCategory(category.Id);
                if (result.Any())
                {
                    list.AddRange(result);
                }
            }
            return list;
        }

        public void GetProductsForCategoriesMultiThread(List<ProviderServiceCategoryBase> categories,
            Action<ProviderType, List<Un1tProductBase>> completeAction, int threadCount)
        {
            if (categories.Count() < 2 || threadCount < 2) return;
            _productsLoadCompleteAction = completeAction;
            _productsList = new List<Un1tProductBase>();
            _productsLoadCounter = threadCount;
            var partSize = (categories.Count() - (categories.Count() % threadCount)) / threadCount;
            var mod = categories.Count() % threadCount;
            var skip = 0;
            for (var i = 0; i < threadCount; i++)
            {
                var size = partSize;
                if (i < mod)
                {
                    size += 1;
                }
                var partData = categories.Skip(skip).Take(size).ToList();
                Task.Run(() => LoadPartProductsData(partData));
                skip += size;
            }
        }

        private async void LoadPartProductsData(List<ProviderServiceCategoryBase> categories)
        {
            var list = new List<Un1tProductBase>();
            var proxy = new MLPortClient();
            try
            {
                if (proxy.ClientCredentials != null)
                {
                    proxy.ClientCredentials.UserName.UserName = _login;
                    proxy.ClientCredentials.UserName.Password = _password;
                }
                proxy.Open();
                foreach (var category in categories)
                {
                    var page = 1;
                    var isComplete = false;
                    try
                    {
                        while (!isComplete)
                        {
                            var result = await proxy.getItemsAsync(category.Id, string.Empty, string.Empty, page, _itemsPageSize);
                            var items = result.getItemsResult;
                            if (items == null || !items.Any() || items.Count() < _itemsPageSize)
                            {
                                isComplete = true;
                            }
                            if (items != null && items.Any())
                            {
                                list.AddRange(items.Select(x => x.ConvertToDataModel()));
                            }
                            page++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.WriteToLog(_logSender, " LoadPartProductsData() Ошибка: " + ex.Message);
                        proxy = new MLPortClient();
                        if (proxy.ClientCredentials != null)
                        {
                            proxy.ClientCredentials.UserName.UserName = _login;
                            proxy.ClientCredentials.UserName.Password = _password;
                        }
                        proxy.Open();
                    }
                    
                }
            }
            catch (Exception ex)
            {
                Log.WriteToLog(_logSender, " LoadPartProductsData() Ошибка: " + ex.Message);
            }
            finally
            {
                proxy.Close();
                var isComplete = Interlocked.Decrement(ref _productsLoadCounter) == 0;
                LoadProductsMultiThreadComplete(list, isComplete);
            }
        }

        private void LoadProductsMultiThreadComplete(List<Un1tProductBase> part, bool isComplete)
        {
            var isLocked = false;
            try
            {
                Monitor.Enter(_lockObj, ref isLocked);
                _productsList.AddRange(part);
                if (isComplete)
                {
                    _productsLoadCompleteAction(_provider, _productsList);
                }
            }
            finally
            {
                if (isLocked) Monitor.Exit(_lockObj);
            }
        }

        private async Task<Un1tProductBase[]> GetProductsForCategory(string categoryId)
        {
            var list = new List<Un1tProductBase>();
            try
            {
                _isRunning = true;
                var page = 1;
                var isComplete = false;
                while (!isComplete)
                {
                    var result = await _proxy.getItemsAsync(categoryId, string.Empty, string.Empty, page, _itemsPageSize);
                    var items = result.getItemsResult;
                    if (items == null || !items.Any() || items.Count() < _itemsPageSize)
                    {
                        isComplete = true;
                    }
                    if (items != null && items.Any())
                    {
                        list.AddRange(items.Select(x => x.ConvertToDataModel()));
                    }
                    page++;
                }
            }
            catch (Exception ex)
            {
                _proxy = new MLPortClient();
                if (_proxy.ClientCredentials != null)
                {
                    _proxy.ClientCredentials.UserName.UserName = _login;
                    _proxy.ClientCredentials.UserName.Password = _password;
                }
                _proxy.Open();
                Log.WriteToLog(_logSender + " GetProductsForCategory(" + categoryId + ") ", "Ошибка: " + ex.Message);
                throw;
            }
            finally
            {
                _isRunning = false;
            }
            return list.ToArray();
        }    

        #endregion

        #region Price & Stock

        public async Task<List<StockServiceBase>> GetStocksAsync(string categoryId)
        {
            List<StockServiceBase> list = null;
            try
            {
                _isRunning = true;
                list = new List<StockServiceBase>();
                var shipmentMethod = _shipmentMethods.ToList().FirstOrDefault(x => x.IsDefault == 1);
                var rateRub = -1f;
                var currencyRateRub = _currencyRates.ToList().FirstOrDefault(x => x.Code.Trim().ToLower() == "руб");
                if (currencyRateRub != null && currencyRateRub.ExchangeRate.HasValue)
                {
                    rateRub = currencyRateRub.ExchangeRate.Value;
                }
                if (shipmentMethod != null)
                {
                    var result = await _proxy.getShipmentDatesAsync(shipmentMethod.Code);
                    if (result.getShipmentDatesResult != null && result.getShipmentDatesResult.Any())
                    {
                        var dates = result.getShipmentDatesResult.Select(x => x.Date).Take(1);
                        foreach (var date in dates)
                        {
                            var availableResult =
                                await
                                    _proxy.getItemsAvailAsync(categoryId, shipmentMethod.Code, date, "0", string.Empty);
                            if (availableResult.getItemsAvailResult != null &&
                                availableResult.getItemsAvailResult.Any())
                            {
                                var itemsResult = availableResult.getItemsAvailResult;
                                if (itemsResult != null && itemsResult.Any())
                                {
                                    list.AddRange(itemsResult.SelectMany(x => x.ConvertToDataModel(rateRub)));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _proxy = new MLPortClient();
                if (_proxy.ClientCredentials != null)
                {
                    _proxy.ClientCredentials.UserName.UserName = _login;
                    _proxy.ClientCredentials.UserName.Password = _password;
                }
                _proxy.Open();
                Log.WriteToLog(_logSender + " GetStocksAsync() ", "Ошибка: " + ex.Message);
                throw;
            }
            finally
            {
                _isRunning = false;
            }
            return list;
        }

        public void GetStocksForCategoriesMultiThread(List<ProviderServiceCategoryBase> categories,
            Action<ProviderType, List<StockServiceBase>> completeAction, int threadCount)
        {
            if (categories.Count() < 2 || threadCount < 2) return;
            _stocksLoadCompleteAction = completeAction;
            _stocksList = new List<StockServiceBase>();
            _stocksLoadCounter = threadCount;
            var partSize = (categories.Count() - (categories.Count() % threadCount)) / threadCount;
            var mod = categories.Count() % threadCount;
            var skip = 0;
            for (var i = 0; i < threadCount; i++)
            {
                var size = partSize;
                if (i < mod)
                {
                    size += 1;
                }
                var partData = categories.Skip(skip).Take(size).ToList();
                Task.Run(() => LoadPartStocksData(partData));
                skip += size;
            }
        }

        private async void LoadPartStocksData(List<ProviderServiceCategoryBase> categories)
        {
            var list = new List<StockServiceBase>();
            var proxy = new MLPortClient();
            try
            {
                if (proxy.ClientCredentials != null)
                {
                    proxy.ClientCredentials.UserName.UserName = _login;
                    proxy.ClientCredentials.UserName.Password = _password;
                }
                proxy.Open();
                var shipmentMethod = _shipmentMethods.ToList().FirstOrDefault(x => x.IsDefault == 1);
                var dates = new List<string>();
                if (shipmentMethod != null)
                {
                    var result = await proxy.getShipmentDatesAsync(shipmentMethod.Code);
                    if (result.getShipmentDatesResult != null && result.getShipmentDatesResult.Any())
                    {
                        dates = result.getShipmentDatesResult.Select(x => x.Date).Take(1).ToList();
                    }
                    foreach (var category in categories)
                    {
                        try
                        {
                            var rateRub = -1f;
                            var currencyRateRub =
                                _currencyRates.ToList().FirstOrDefault(x => x.Code.Trim().ToLower() == "руб");
                            if (currencyRateRub != null && currencyRateRub.ExchangeRate.HasValue)
                            {
                                rateRub = currencyRateRub.ExchangeRate.Value;
                            }
                            foreach (var date in dates)
                            {
                                var availableResult =
                                    await
                                        proxy.getItemsAvailAsync(category.Id, shipmentMethod.Code, date, "0", string.Empty);
                                if (availableResult.getItemsAvailResult != null &&
                                    availableResult.getItemsAvailResult.Any())
                                {
                                    var itemsResult = availableResult.getItemsAvailResult;
                                    if (itemsResult != null && itemsResult.Any())
                                    {
                                        list.AddRange(itemsResult.SelectMany(x => x.ConvertToDataModel(rateRub)));
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.WriteToLog(_logSender, " LoadPartStocksData(" + category.Id + ") Ошибка: " + ex.Message);
                            proxy = new MLPortClient();
                            if (proxy.ClientCredentials != null)
                            {
                                proxy.ClientCredentials.UserName.UserName = _login;
                                proxy.ClientCredentials.UserName.Password = _password;
                            }
                            proxy.Open();
                        }
                        
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteToLog(_logSender, " LoadPartStocksData() Ошибка: " + ex.Message);
            }
            finally
            {
                proxy.Close();
                var isComplete = Interlocked.Decrement(ref _stocksLoadCounter) == 0;
                LoadStocksMultiThreadComplete(list, isComplete);
            }
        }

        private void LoadStocksMultiThreadComplete(List<StockServiceBase> part, bool isComplete)
        {
            var isLocked = false;
            try
            {
                Monitor.Enter(_lockObj, ref isLocked);
                _stocksList.AddRange(part);
                if (isComplete)
                {
                    _stocksLoadCompleteAction(_provider, _stocksList);
                }
            }
            finally
            {
                if (isLocked) Monitor.Exit(_lockObj);
            }
        }

        #endregion

        #region Properties

        public async Task<List<PropertyProductBase>> GetPropertiesForProductAsync(string productId)
        {
            return await GetProductsPropertiesAsync(null, productId);
        }

        private async Task<List<PropertyProductBase>> GetProductsPropertiesAsync(string categoryId, string itemId, int lastDayChanged = 0)
        {
            var list = new List<PropertyProductBase>();
            try
            {
                _isRunning = true;
                var page = 1;
                var isComplete = false;
                while (!isComplete)
                {
                    var result =
                        await _proxy.getItemsPropertiesAsync(categoryId, itemId, page, _itemsPageSize, lastDayChanged);
                    var items = result.getItemsPropertiesResult;
                    if (items == null || !items.Any() || items.Count() < _itemsPageSize)
                    {
                        isComplete = true;
                    }
                    if (items != null && items.Any())
                    {
                        list.AddRange(items.Select(x => x.ConvertToDataModel()));
                    }
                    page++;
                }
            }
            catch (Exception ex)
            {
                _proxy = new MLPortClient();
                if (_proxy.ClientCredentials != null)
                {
                    _proxy.ClientCredentials.UserName.UserName = _login;
                    _proxy.ClientCredentials.UserName.Password = _password;
                }
                _proxy.Open();
                Log.WriteToLog(_logSender + " GetProductsPropertiesAsync() ", "Ошибка: " + ex.Message);
                throw;
            }
            finally
            {
                _isRunning = false;
            }
            return list;
        }

        public void GetPropertiesForProductsMultiThread(List<string> productsId,
            Action<ProviderType, List<PropertyProductBase>> completeAction, Action<int> updateUI, int threadCount)
        {
            if (productsId.Count() < 2 || threadCount < 2) return;
            _propertiesLoadCompleteAction = completeAction;
            _propertiesList = new List<PropertyProductBase>();
            _properiesLoadCounter = threadCount;
            var partSize = (productsId.Count() - (productsId.Count() % threadCount)) / threadCount;
            var mod = productsId.Count() % threadCount;
            var skip = 0;
            for (var i = 0; i < threadCount; i++)
            {
                var size = partSize;
                if (i < mod)
                {
                    size += 1;
                }
                var partData = productsId.Skip(skip).Take(size).ToList();
                Task.Run(() => LoadPartPropertiesData(partData, updateUI));
                skip += size;
            }
        }

        private async void LoadPartPropertiesData(List<string> productsId, Action<int> updateAction)
        {
            var list = new List<PropertyProductBase>();
            var proxy = new MLPortClient();
            try
            {
                if (proxy.ClientCredentials != null)
                {
                    proxy.ClientCredentials.UserName.UserName = _login;
                    proxy.ClientCredentials.UserName.Password = _password;
                }
                proxy.Open();
                foreach (var productId in productsId)
                {
                    var loadedCount = 0;
                    var page = 1;
                    var isComplete = false;
                    try
                    {
                        while (!isComplete)
                        {
                            var result = await proxy.getItemsPropertiesAsync(null, productId, page, _itemsPageSize, 0);
                            var items = result.getItemsPropertiesResult;
                            if (items == null || !items.Any() || items.Count() < _itemsPageSize)
                            {
                                isComplete = true;
                            }
                            if (items != null && items.Any())
                            {
                                list.AddRange(items.Select(x => x.ConvertToDataModel()));
                                loadedCount += items.Count();
                            }
                            page++;
                        }
                    }
                    catch (Exception ex)
                    {
                        proxy = new MLPortClient();
                        if (proxy.ClientCredentials != null)
                        {
                            proxy.ClientCredentials.UserName.UserName = _login;
                            proxy.ClientCredentials.UserName.Password = _password;
                        }
                        proxy.Open();
                        Log.WriteToLog(_logSender, " LoadPartPropertiesData() Ошибка: " + ex.Message);
                    }
                    updateAction(loadedCount);
                }
            }
            catch (Exception ex)
            {
                Log.WriteToLog(_logSender, " LoadPartPropertiesData() Ошибка: " + ex.Message);
            }
            finally
            {
                proxy.Close();
                var isComplete = Interlocked.Decrement(ref _properiesLoadCounter) == 0;
                LoadPropertiesMultiThreadComplete(list, isComplete);
            }
        }

        private void LoadPropertiesMultiThreadComplete(List<PropertyProductBase> part, bool isComplete)
        {
            var isLocked = false;
            try
            {
                Monitor.Enter(_lockObj, ref isLocked);
                _propertiesList.AddRange(part);
                if (isComplete)
                {
                    _propertiesLoadCompleteAction(_provider, _propertiesList);
                }
            }
            finally
            {
                if (isLocked) Monitor.Exit(_lockObj);
            }
        }

        #endregion

        public void Dispose()
        {
            if (_proxy != null)
            {
                _proxy.Close();
            }
        }
    }
}
