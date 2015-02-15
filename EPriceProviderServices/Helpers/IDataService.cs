using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EPriceProviderServices.Models;
using EPriceRequestServiceDBEngine.Enums;
using EPriceRequestServiceDBEngine.Models;

namespace EPriceProviderServices.Helpers
{
    public interface IDataService
    {
        Task<ProviderServiceCategoryBase[]> GetAllCategoriesAsync();

        Task<List<Un1tProductBase>> GetProductsForCategories(List<ProviderServiceCategoryBase> categories);

        Task<List<StockServiceBase>> GetStocksAsync(string categoryId);

        Task<List<PropertyProductBase>> GetPropertiesForProductAsync(string productId);

        void GetPropertiesForProductsMultiThread(List<string> productsId,
            Action<ProviderType, List<PropertyProductBase>> completeAction, Action<int> updateUI, int threadCount);

        void GetProductsForCategoriesMultiThread(List<ProviderServiceCategoryBase> categories,
            Action<ProviderType, List<Un1tProductBase>> completeAction, int threadCount);

        void GetStocksForCategoriesMultiThread(List<ProviderServiceCategoryBase> categories,
            Action<ProviderType, List<StockServiceBase>> completeAction, int threadCount);

        void RefreshServiceDirect();

        bool IsRunning { get; }

        ProviderType Provider { get; }
    }
}
