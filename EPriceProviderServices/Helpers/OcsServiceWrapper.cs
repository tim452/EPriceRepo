using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using EPriceProviderServices.MerlionDataService;
using EPriceProviderServices.Models;
using EPriceProviderServices.OcsDataService;
using EPriceRequestServiceDBEngine.Enums;
using EPriceRequestServiceDBEngine.Models;

namespace EPriceProviderServices.Helpers
{
    public class OcsServiceWrapper : IDisposable, IDataService
    {
        #region Fields

        private readonly string _login;
        private readonly string _token;
        private B2BWebServiceSoapClient _proxy;
        private readonly string _logSender;
        private readonly ProviderType _provider;
        private bool _isRunning;
        private decimal _currencyExchangeValue;
        private readonly object _lockObj;
        private int _productsLoadCounter;
        private Action<ProviderType, List<Un1tProductBase>> _productsLoadCompleteAction;
        private List<Un1tProductBase> _productsList;

        #endregion

        public OcsServiceWrapper()
        {
            _provider = ProviderType.OCS;
            _lockObj = new object();
            _isRunning = false;
            _logSender = "OCS служба";
            _login = ConfigurationManager.AppSettings.Get("OcsServiceLogin");
            _token = ConfigurationManager.AppSettings.Get("OcsServiceToken");
            try
            {
                _proxy = new B2BWebServiceSoapClient();
                _proxy.Open();
                _currencyExchangeValue = 1;
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
            RefreshCurrencyRate();
        }

        private void RefreshCurrencyRate()
        {
            var isLocked = false;
            try
            {
                Monitor.Enter(_lockObj, ref isLocked);
                _isRunning = true;
                _currencyExchangeValue = _proxy.GetCurrentCurrencyRate(_login, _token).Rate;
            }
            catch (Exception ex)
            {
                _proxy = new B2BWebServiceSoapClient();
                _proxy.Open();
                Log.WriteToLog(_logSender + " RefreshCurrencyRate() ", "Ошибка: " + ex.Message);
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
            try
            {
                _isRunning = true;
                var result = await _proxy.GetCatalogAsync(_login, _token);
                if (result.Body.GetCatalogResult.OperationStatus == 0)
                {
                    if (result.Body.GetCatalogResult.Categories != null && result.Body.GetCatalogResult.Categories.Any())
                    {
                        var catalogs =
                            result.Body.GetCatalogResult.Categories.Where(x => !string.IsNullOrEmpty(x.CategoryID))
                                .ToArray();
                        if (catalogs.Any())
                        {
                            list.AddRange(catalogs.Select(x => x.ConvertToDataModel()));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _proxy = new B2BWebServiceSoapClient();
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
            try
            {
                _isRunning = true;
                if (!categories.Any())
                {
                    return list;
                }
                var categoryStringArray = new ArrayOfString();
                categoryStringArray.AddRange(categories.Select(x => x.Id).ToArray());
                var result =
                    await
                        _proxy.GetProductAvailabilityAsync(_login, _token, 0, string.Empty, categoryStringArray, null,
                            null,
                            1);
                if (result.Body.GetProductAvailabilityResult.OperationStatus == 0)
                {
                    var products = result.Body.GetProductAvailabilityResult.Products;
                    list.AddRange(products.Select(x => x.ConvertToDataModel(_currencyExchangeValue)));
                    SaveStockInfo(products);
                }
            }
            catch (Exception ex)
            {
                _proxy = new B2BWebServiceSoapClient();
                _proxy.Open();
                Log.WriteToLog(_logSender + " GetProductsForCategories() ", "Ошибка: " + ex.Message);
                throw;
            }
            finally
            {
                _isRunning = false;
            }
            return list;
        }

        public void GetProductsForCategoriesMultiThread(List<ProviderServiceCategoryBase> categories,
            Action<ProviderType, List<Un1tProductBase>> completeAction, int threadCount)
        {
            
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

        public async Task<List<PropertyProductBase>> GetPropertiesForProductAsync(string productId)
        {
            return new List<PropertyProductBase>();
        }

        public void GetPropertiesForProductsMultiThread(List<string> productsId,
            Action<ProviderType, List<PropertyProductBase>> completeAction, Action<int> updateUI, int threadCount)
        {

        }

        #endregion

        #region Manage stock info

        private void SaveStockInfo(OcsDataService.Product[] products)
        {
            return;
            var isLocked = false;
            var assemblyLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var fileName = "OCSStockInfo.xml";
            var list = new List<QuantityLocation>();
            if (!string.IsNullOrEmpty(assemblyLocation))
            {
                try
                {
                    Monitor.Enter(_lockObj, ref isLocked);
                    var path = Path.Combine(assemblyLocation, fileName);
                    var formatter = new XmlSerializer(typeof (List<QuantityLocation>));
                    if (File.Exists(path))
                    {
                        using (var stream = File.OpenRead(path))
                        {
                            list = (List<QuantityLocation>) formatter.Deserialize(stream);
                        }
                    }
                    var newQuantity = products.SelectMany(x => x.Locations);
                    list.AddRange(newQuantity);
                    list = list.Distinct(new QuantityLocationComparer()).ToList();
                    using (var stream = File.Create(path))
                    {
                        formatter.Serialize(stream, list);
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
                _proxy.Close();
            }
        }
    }

    public class QuantityLocationComparer : IEqualityComparer<QuantityLocation>
    {
        public bool Equals(QuantityLocation x, QuantityLocation y)
        {
            return x.Location == y.Location;
        }

        public int GetHashCode(QuantityLocation obj)
        {
            return obj.Location.GetHashCode();
        }
    }
}
