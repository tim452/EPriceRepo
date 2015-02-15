using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using System.Xml;
using System.Xml.Linq;
using EPriceProviderServices.Models;
using EPriceProviderServices.TreolanDataService;
using EPriceProviderServices.TreolanProductService;
using EPriceRequestServiceDBEngine.Enums;
using EPriceRequestServiceDBEngine.Models;

namespace EPriceProviderServices.Helpers
{
    public class TreolanServiceWrapper : IDisposable, IDataService
    {
        #region Fields

        private WebServiceSoapPortClient _proxy;
        private B2BWebService _productProxy;
        private readonly string _login;
        private readonly string _password;
        private readonly string _logSender;
        private readonly ProviderType _provider;
        private bool _isRunning;
        private int _properiesLoadCounter;
        private Action<ProviderType, List<PropertyProductBase>> _propertiesLoadCompleteAction;
        private List<PropertyProductBase> _propertiesList;
        private int _productsLoadCounter;
        private Action<ProviderType, List<Un1tProductBase>> _productsLoadCompleteAction;
        private List<Un1tProductBase> _productsList;
        private readonly object _lockObj;

        #endregion

        public TreolanServiceWrapper()
        {
            _lockObj = new object();
            _provider = ProviderType.Treolan;
            _isRunning = false;
            _logSender = "Treolan служба";
            _login = ConfigurationManager.AppSettings.Get("TreolanServiceLogin");
            _password = ConfigurationManager.AppSettings.Get("TreolanServicePassword");
            try
            {
                _proxy = new WebServiceSoapPortClient();
                _proxy.Open();
                _productProxy = new B2BWebService();
            }
            catch (Exception ex)
            {
                _proxy = null;
                _productProxy = null;
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

        }

        #endregion

        #region Catalogs

        public async Task<ProviderServiceCategoryBase[]> GetAllCategoriesAsync()
        {
            var list = new List<ProviderServiceCategoryBase>();
            var request = new GenCatalogRequest(_login, _password, string.Empty, string.Empty, 0,
                false, false, false, 0);
            try
            {
                _isRunning = true;
                var result = await _proxy.GenCatalogAsync(request);
                if (!string.IsNullOrEmpty(result.Result))
                {
                    var xml = XDocument.Parse(result.Result);
                    var root = xml.Root;
                    if (root != null)
                    {
                        var categories = root.Descendants("category");
                        foreach (var category in categories)
                        {
                            var id = category.Attribute("id").Value;
                            var idParent = category.Attribute("parent").Value;
                            var name = category.Attribute("name").Value;
                            var treolanCategory = new ProviderServiceCategoryBase()
                            {
                                Id = id,
                                IdType = IdValueType.String,
                                IdParent = idParent,
                                IdParentType = IdValueType.String,
                                Name = name,
                                IdUn1tCategory = -1,
                                IsLoad = false
                            };
                            list.Add(treolanCategory);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _proxy = new WebServiceSoapPortClient();
                _proxy.Open();
                Log.WriteToLog(_logSender + " GetAllCategoriesAsync() ", "Ошибка: " + ex.Message);
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
            var proxy = new WebServiceSoapPortClient();
            try
            {
                proxy.Open();
                foreach (var category in categories)
                {
                    try
                    {
                        var request = new GenCatalogRequest(_login, _password, category.Id, string.Empty, 0,
                    false, false, false, 0);
                        var result = await proxy.GenCatalogAsync(request);
                        if (!string.IsNullOrEmpty(result.Result))
                        {
                            var xml = XDocument.Parse(result.Result);
                            var root = xml.Root;
                            if (root != null)
                            {
                                var positions = root.Descendants("position");
                                foreach (var position in positions)
                                {
                                    var idParent = string.Empty;
                                    var parent = position.Parent;
                                    if (parent != null)
                                    {
                                        idParent = parent.Attribute("id").Value;
                                    }
                                    var id = position.Attribute("id").Value;
                                    var name = position.Attribute("name").Value;
                                    var articul = position.Attribute("articul").Value.Trim();
                                    var vendor = position.Attribute("vendor").Value;
                                    var currency = (Currency)Enum.Parse(typeof(Currency), position.Attribute("currency").Value);
                                    var freenom = position.Attribute("freenom").Value.Trim().ToLower();
                                    var productCount = 0;
                                    if (freenom == "много")
                                    {
                                        productCount = -2;
                                    }
                                    else if (freenom == "мало")
                                    {
                                        productCount = -3;
                                    }
                                    else
                                    {
                                        int.TryParse(freenom, out productCount);
                                    }
                                    var price = float.Parse(position.Attribute("price").Value.Replace(".", ","));
                                    var stock = new StockBase()
                                    {
                                        Location = Location.Moscow,
                                        Currency = currency,
                                        Price = price,
                                        Value = productCount
                                    };
                                    var model = new Un1tProductBase()
                                    {
                                        Id = -1,
                                        IdProvider = id,
                                        IdProviderType = IdValueType.String,
                                        IdCategory = -1,
                                        IdProviderCategory = idParent,
                                        IdProviderCategoryType = IdValueType.String,
                                        Brand = string.Empty,
                                        Name = name,
                                        PartNumber = articul,
                                        Provider = ProviderType.Treolan,
                                        Vendor = vendor
                                    };
                                    model.Stocks.Add(stock);
                                    list.Add(model);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.WriteToLog(_logSender, " LoadPartProductsData() Ошибка: " + ex.Message);
                        proxy = new WebServiceSoapPortClient();
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
                var request = new GenCatalogRequest(_login, _password, categoryId, string.Empty, 0,
                    false, false, false, 0);
                var result = await _proxy.GenCatalogAsync(request);
                if (!string.IsNullOrEmpty(result.Result))
                {
                    var xml = XDocument.Parse(result.Result);
                    var root = xml.Root;
                    if (root != null)
                    {
                        var positions = root.Descendants("position");
                        foreach (var position in positions)
                        {
                            var idParent = string.Empty;
                            var parent = position.Parent;
                            if (parent != null)
                            {
                                idParent = parent.Attribute("id").Value;
                            }
                            var id = position.Attribute("id").Value;
                            var name = position.Attribute("name").Value;
                            var articul = position.Attribute("articul").Value.Trim();
                            var vendor = position.Attribute("vendor").Value;
                            var currency = (Currency)Enum.Parse(typeof(Currency), position.Attribute("currency").Value);
                            var freenom = position.Attribute("freenom").Value.Trim().ToLower();
                            var productCount = 0;
                            if (freenom == "много")
                            {
                                productCount = -2;
                            }
                            else if (freenom == "мало")
                            {
                                productCount = -3;
                            }
                            else
                            {
                                int.TryParse(freenom, out productCount);
                            }
                            var price = float.Parse(position.Attribute("price").Value.Replace(".", ","));
                            var stock = new StockBase()
                            {
                                Location = Location.Moscow,
                                Currency = currency,
                                Price = price,
                                Value = productCount
                            };
                            var model = new Un1tProductBase()
                            {
                                Id = -1,
                                IdProvider = id,
                                IdProviderType = IdValueType.String,
                                IdCategory = -1,
                                IdProviderCategory = idParent,
                                IdProviderCategoryType = IdValueType.String,
                                Brand = string.Empty,
                                Name = name,
                                PartNumber = articul,
                                Provider = ProviderType.Treolan,
                                Vendor = vendor
                            };
                            model.Stocks.Add(stock);
                            list.Add(model);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _proxy = new WebServiceSoapPortClient();
                _proxy.Open();
                Log.WriteToLog(_logSender + " GetProductsForCategory() ", "Ошибка: " + ex.Message);
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

        public Task<List<StockServiceBase>> GetStocksAsync(string categoryId)
        {
            throw new NotImplementedException();
        }

        public void GetStocksForCategoriesMultiThread(List<ProviderServiceCategoryBase> categories,
            Action<ProviderType, List<StockServiceBase>> completeAction, int threadCount)
        {
            
        }

        #endregion

        #region Properties

        public async Task<List<PropertyProductBase>> GetPropertiesForProductAsync(string articul)
        {
            var list = new List<PropertyProductBase>();
            try
            {
                _isRunning = true;
                var node = await Task.Run(() =>
                {
                    _productProxy = new B2BWebService();
                    _productProxy.Timeout = 30000;
                    XmlNode result = _productProxy.ProductInfoV2(_login, _password, articul);
                    return result;
                });
                if (node != null)
                {
                    var firstChild = node.FirstChild;
                    if (firstChild != null)
                    {
                        var properties = firstChild.SelectNodes("//Property");
                        if (properties != null)
                        {
                            foreach (XmlNode propertyXml in properties)
                            {
                                var attributes = propertyXml.Attributes;
                                if (attributes != null)
                                {
                                    var propId = string.Empty;
                                    var propName = string.Empty;
                                    var propValue = string.Empty;
                                    if (attributes["ID"] != null) propId = attributes["ID"].Value;
                                    if (attributes["Name"] != null) propName = attributes["Name"].Value;
                                    if (attributes["Value"] != null) propValue = attributes["Value"].Value;
                                    if (!string.IsNullOrEmpty(propId) && !string.IsNullOrEmpty(propName) &&
                                        !string.IsNullOrEmpty(propValue))
                                    {
                                        var property = new PropertyProductBase()
                                        {
                                            IdProductProvider = articul,
                                            IdProduct = -1,
                                            IdProductProviderType = IdValueType.String,
                                            IdProvertyPropertyType = IdValueType.String,
                                            Provider = ProviderType.Treolan,
                                            IdPropertyProvider = propId,
                                            Name = propName,
                                            Value = propValue
                                        };
                                        list.Add(property);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _productProxy = new B2BWebService();
                Log.WriteToLog(_logSender + " GetPropertiesForProductAsync(" + articul + ") ", "Ошибка: " + ex.Message);
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
            var proxy = new B2BWebService();
            proxy.Timeout = 30000;
            try
            {
                foreach (var productId in productsId)
                {
                    var loadedCount = 0;
                    var node = await Task.Run(() =>
                    {
                        XmlNode result = null;
                        try
                        {
                            result = proxy.ProductInfoV2(_login, _password, productId);
                        }
                        catch (Exception ex)
                        {
                            proxy = new B2BWebService();
                            Log.WriteToLog(_logSender + " LoadPartPropertiesData(" + productId + ") ",
                                "Ошибка: " + ex.Message);
                        }
                        return result;
                    });
                    if (node != null)
                    {
                        var firstChild = node.FirstChild;
                        if (firstChild != null)
                        {
                            var properties = firstChild.SelectNodes("//Property");
                            if (properties != null)
                            {
                                foreach (XmlNode propertyXml in properties)
                                {
                                    var attributes = propertyXml.Attributes;
                                    if (attributes != null)
                                    {
                                        var propId = string.Empty;
                                        var propName = string.Empty;
                                        var propValue = string.Empty;
                                        if (attributes["ID"] != null) propId = attributes["ID"].Value;
                                        if (attributes["Name"] != null) propName = attributes["Name"].Value;
                                        if (attributes["Value"] != null) propValue = attributes["Value"].Value;
                                        if (!string.IsNullOrEmpty(propId) && !string.IsNullOrEmpty(propName) &&
                                            !string.IsNullOrEmpty(propValue))
                                        {
                                            var property = new PropertyProductBase()
                                            {
                                                IdProductProvider = productId,
                                                IdProduct = -1,
                                                IdProductProviderType = IdValueType.String,
                                                IdProvertyPropertyType = IdValueType.String,
                                                Provider = ProviderType.Treolan,
                                                IdPropertyProvider = propId,
                                                Name = propName,
                                                Value = propValue
                                            };
                                            list.Add(property);
                                        }
                                        loadedCount++;
                                    }
                                }
                            }
                        }
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
                proxy.Dispose();
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
