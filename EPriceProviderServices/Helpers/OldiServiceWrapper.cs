using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using EPriceProviderServices.Models;
using EPriceProviderServices.OldiDataService;
using System.Windows;
using EPriceRequestServiceDBEngine.Enums;
using EPriceRequestServiceDBEngine.Models;
using Currency = EPriceProviderServices.OldiDataService.Currency;

namespace EPriceProviderServices.Helpers
{
    public class OldiServiceWrapper : IDisposable, IDataService
    {
        #region Fields

        private B2bPublic _proxy;
        private readonly string _token;
        private readonly string _login;
        private readonly string _password;
        private readonly string _logSender;
        private readonly ProviderType _provider;
        private bool _isRunning;
        private readonly object _lockObj;
        private readonly List<EPriceProviderServices.OldiDataService.Currency> _currencyList;
        private int _productsLoadCounter;
        private Action<ProviderType, List<Un1tProductBase>> _productsLoadCompleteAction;
        private List<Un1tProductBase> _productsList;
        private int _stocksLoadCounter;
        private Action<ProviderType, List<StockServiceBase>> _stocksLoadCompleteAction;
        private List<StockServiceBase> _stocksList;

        #endregion

        public OldiServiceWrapper()
        {
            _provider = ProviderType.OLDI;
            _lockObj = new object();
            _currencyList = new List<Currency>();
            _isRunning = false;
            _logSender = "OLDI служба";
            _token = ConfigurationManager.AppSettings.Get("OldiServiceToken");
            _login = ConfigurationManager.AppSettings.Get("OldiServiceLogin");
            _password = ConfigurationManager.AppSettings.Get("OldiServicePassword");
            try
            {
                _proxy = new B2bPublic();
                _proxy.Credentials = new NetworkCredential(_login, _password);
                _proxy.Timeout = 120000;
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
            RefreshCurrencyList();
        }

        private void RefreshCurrencyList()
        {
            var isLocked = false;
            try
            {
                Monitor.Enter(_lockObj, ref isLocked);
                _isRunning = true;
                Error error = null;
                var clientParams = _proxy.GetClientParam(null, _token, out error);
                if (error != null && error.ErrorCode == "0")
                {
                    if (clientParams != null && clientParams.Currencies != null && clientParams.Currencies.Any())
                    {
                        _currencyList.Clear();
                        _currencyList.AddRange(clientParams.Currencies);
                    }
                }
            }
            catch (Exception ex)
            {
                _proxy = new B2bPublic();
                _proxy.Credentials = new NetworkCredential(_login, _password);
                _proxy.Timeout = 120000;
                Log.WriteToLog(_logSender + " RefreshCurrencyList() ", "Ошибка: " + ex.Message);
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
            var list = new List<ProviderServiceCategoryBase>();
            var groups = await GetCatalogsForParentAsync(string.Empty);
            if (groups.Any())
            {
                list.AddRange(groups);
            }
            return list.ToArray();
        }

        private async Task<ProviderServiceCategoryBase[]> GetCatalogsForParentAsync(string parentId)
        {
            var list = new List<ProviderServiceCategoryBase>();
            try
            {
                _isRunning = true;
                Error error = null;
                var items = await Task.Run(() =>
                {
                    Item[] returnItems = null;
                    try
                    {
                        returnItems = _proxy.GetItems(parentId, null, _token, out error);
                    }
                    catch (Exception ex)
                    {
                        Log.WriteToLog(_logSender + " GetCatalogsForParentAsync(" + parentId + ") ", "Ошибка: " + ex.Message);
                        throw;
                    }
                    return returnItems.ToArray();
                });
                if (error != null && error.ErrorCode == "0")
                {
                    if (items != null && items.Length > 0)
                    {
                        var groups = items.Where(x => x.IsGroup.HasValue && x.IsGroup.Value).ToList();
                        if (groups.Any())
                        {
                            var categories = groups.Select(x => new ProviderServiceCategoryBase()
                            {
                                Id = x.Code,
                                IdParent = parentId,
                                Name = x.Name,
                                IdType = IdValueType.String,
                                IdParentType = IdValueType.String,
                                IdUn1tCategory = -1,
                                IsLoad = false
                            });
                            list.AddRange(categories);
                            foreach (var item in groups)
                            {
                                var childGroups = await GetCatalogsForParentAsync(item.Code);
                                list.AddRange(childGroups);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _proxy = new B2bPublic();
                _proxy.Credentials = new NetworkCredential(_login, _password);
                _proxy.Timeout = 120000;
                Log.WriteToLog(_logSender + " GetCatalogsForParentAsync(" + parentId + ") ", "Ошибка: " + ex.Message);
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
            foreach (var oldiCategory in categories)
            {
                var items = await GetItemsForCategoryAsync(oldiCategory.Id);
                if (items.Any())
                {
                    list.AddRange(items);
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
            var proxy = new B2bPublic();
            try
            {
                proxy.Credentials = new NetworkCredential(_login, _password);
                proxy.Timeout = 120000;
                foreach (var category in categories)
                {
                    Error error = null;
                    var items = await Task.Run(() =>
                    {
                        Item[] returnItems = null;
                        try
                        {
                            returnItems = proxy.GetItems(category.Id, null, _token, out error);
                        }
                        catch (Exception ex)
                        {
                            returnItems = new Item[0];
                            Log.WriteToLog(_logSender + " LoadPartProductsData(" + category.Id + ") ", "Ошибка: " + ex.Message);
                            proxy = new B2bPublic();
                            proxy.Credentials = new NetworkCredential(_login, _password);
                            proxy.Timeout = 120000;
                        }
                        return returnItems.ToArray();
                    });
                    if (error != null && error.ErrorCode == "0")
                    {
                        if (items != null && items.Length > 0)
                        {
                            var products = items.Where(x => !x.IsGroup.HasValue || !x.IsGroup.Value).ToList();
                            if (products.Any())
                            {
                                list.AddRange(products.Select(x => x.ConvertToProductDataModel()));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteToLog(_logSender, " LoadPartProductsData() Ошибка: " + ex.Message);
            }
            finally
            {
                proxy.Dispose();
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

        private async Task<Un1tProductBase[]> GetItemsForCategoryAsync(string categoryId)
        {
            var list = new List<Un1tProductBase>();
            try
            {
                _isRunning = true;
                Error error = null;
                var items = await Task.Run(() =>
                {
                    var returnItems = _proxy.GetItems(categoryId, null, _token, out error);
                    return returnItems.ToArray();
                });
                if (error != null && error.ErrorCode == "0")
                {
                    if (items != null && items.Length > 0)
                    {
                        var products = items.Where(x => !x.IsGroup.HasValue || !x.IsGroup.Value).ToList();
                        if (products.Any())
                        {
                            list.AddRange(products.Select(x => x.ConvertToProductDataModel()));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _proxy = new B2bPublic();
                _proxy.Credentials = new NetworkCredential(_login, _password);
                _proxy.Timeout = 120000;
                Log.WriteToLog(_logSender + " GetItemsForCategoryAsync(" + categoryId + ") ", "Ошибка: " + ex.Message);
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
            var list = new List<StockServiceBase>();
            try
            {
                _isRunning = true;
                Error error = null;
                var items = await Task.Run(() =>
                {
                    var returnItems = _proxy.GetItemsStock(categoryId, null, "1", _token, out error);
                    return returnItems.ToList();
                });
                if (error != null && error.ErrorCode == "0")
                {
                    if (items != null && items.Any())
                    {
                        list.AddRange(items.SelectMany(x => x.ConvertToStockDataModel(_currencyList.ToList())));
                        SaveStockInfo(items);
                    }
                }
            }
            catch (Exception ex)
            {
                _proxy = new B2bPublic();
                _proxy.Credentials = new NetworkCredential(_login, _password);
                _proxy.Timeout = 120000;
                Log.WriteToLog(_logSender + " GetStocksAsync(" + categoryId + ") ", "Ошибка: " + ex.Message);
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
            try
            {
                var proxy = new B2bPublic();
                proxy.Credentials = new NetworkCredential(_login, _password);
                proxy.Timeout = 120000;
                foreach (var category in categories)
                {
                    Error error = null;
                    var items = await Task.Run(() =>
                    {
                        Item[] returnItems = null;
                        try
                        {
                            returnItems = proxy.GetItemsStock(category.Id, null, "1", _token, out error);
                        }
                        catch (Exception ex)
                        {
                            returnItems = new Item[0];
                            Log.WriteToLog(_logSender + " LoadPartStocksData(" + category.Id + ") ", "Ошибка: " + ex.Message);
                            proxy = new B2bPublic();
                            proxy.Credentials = new NetworkCredential(_login, _password);
                            proxy.Timeout = 120000;
                        }
                        return returnItems.ToList();
                    });
                    if (error != null && error.ErrorCode == "0")
                    {
                        if (items != null && items.Any())
                        {
                            list.AddRange(items.SelectMany(x => x.ConvertToStockDataModel(_currencyList.ToList())));
                            SaveStockInfo(items);
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
            return await GetProductsPropertiesAsync(null, new List<string>(new[] { productId }));
        }

        public async Task<List<PropertyProductBase>> GetProductsPropertiesAsync(string categoryId,
            List<string> productsId)
        {
            var list = new List<PropertyProductBase>();
            try
            {
                _isRunning = true;
                Error error = null;
                var items = await Task.Run(() =>
                {
                    Item[] returnItems = _proxy.GetItemsProp(categoryId,
                            ((productsId != null && productsId.Any())
                                ? productsId.Select(x => new Item() { Code = x }).ToArray()
                                : null), _token, out error);
                    return returnItems;
                });
                if (error != null && error.ErrorCode == "0")
                {
                    if (items != null && items.Any())
                    {
                        foreach (var item in items)
                        {
                            var itemProperties = item.Properties;
                            if (itemProperties != null && itemProperties.Any())
                            {
                                foreach (var itemProperty in itemProperties)
                                {
                                    var property = new PropertyProductBase()
                                    {
                                        IdProduct = -1,
                                        IdProductProvider = item.Code,
                                        IdProductProviderType = IdValueType.String,
                                        IdPropertyProvider = itemProperty.Id,
                                        IdProvertyPropertyType = IdValueType.String,
                                        Name = itemProperty.Name,
                                        Provider = ProviderType.OLDI,
                                        Value = itemProperty.Value
                                    };
                                    list.Add(property);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _proxy = new B2bPublic();
                _proxy.Credentials = new NetworkCredential(_login, _password);
                _proxy.Timeout = 120000;
                Log.WriteToLog(
                    _logSender + " GetProductsPropertiesAsync(" +
                    (!string.IsNullOrEmpty(categoryId) ? categoryId : string.Empty) + ") ", "Ошибка: " + ex.Message);
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

        }

        #endregion

        #region Manage stock info

        private void SaveStockInfo(List<Item> items)
        {
            return;
            var isLocked = false;
            var assemblyLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var stocksFileName = "OLDIStockInfo.xml";
            var pricesFileName = "OLDIPriceInfo.xml";
            var stocksList = new List<Stock>();
            var pricesList = new List<PriceType>();
            if (!string.IsNullOrEmpty(assemblyLocation))
            {
                try
                {
                    Monitor.Enter(_lockObj, ref isLocked);
                    var stockPath = Path.Combine(assemblyLocation, stocksFileName);
                    var stockFormatter = new XmlSerializer(typeof(List<Stock>));
                    if (File.Exists(stockPath))
                    {
                        using (var stream = File.OpenRead(stockPath))
                        {
                            stocksList = (List<Stock>)stockFormatter.Deserialize(stream);
                        }
                    }
                    var newStocks = items.SelectMany(x => x.StockItem).Select(x=>x.Stock);
                    stocksList.AddRange(newStocks);
                    stocksList = stocksList.Distinct(new StockComparer()).ToList();
                    using (var stream = File.Create(stockPath))
                    {
                        stockFormatter.Serialize(stream, stocksList);
                    }
                    var pricePath = Path.Combine(assemblyLocation, pricesFileName);
                    var priceFormatter = new XmlSerializer(typeof(List<PriceType>));
                    if (File.Exists(pricePath))
                    {
                        using (var stream = File.OpenRead(pricePath))
                        {
                            pricesList = (List<PriceType>)priceFormatter.Deserialize(stream);
                        }
                    }
                    var newPrices = items.SelectMany(x => x.PriceItem).Select(x => x.PriceType);
                    pricesList.AddRange(newPrices);
                    pricesList = pricesList.Distinct(new PriceTypeComparer()).ToList();
                    using (var stream = File.Create(pricePath))
                    {
                        priceFormatter.Serialize(stream, pricesList);
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteToLog(_logSender + " SaveStockInfo() ", "Ошибка: " + ex.Message);
                }
                finally
                {
                    if (isLocked) Monitor.Exit(_lockObj);
                }
            }
        }

        #endregion

        public void Dispose()
        {
            if (_proxy != null)
            {
                _proxy.Dispose();
            }
        }
    }

    public class StockComparer : IEqualityComparer<Stock>
    {
        public bool Equals(Stock x, Stock y)
        {
            return x.Name.Trim() == y.Name.Trim();
        }

        public int GetHashCode(Stock obj)
        {
            return obj.Name.GetHashCode();
        }
    }

    public class PriceTypeComparer : IEqualityComparer<PriceType>
    {
        public bool Equals(PriceType x, PriceType y)
        {
            return x.Name.Trim() == y.Name.Trim();
        }

        public int GetHashCode(PriceType obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}
