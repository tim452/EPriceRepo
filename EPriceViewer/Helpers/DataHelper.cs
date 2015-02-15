using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using EPriceRequestServiceDBEngine;
using EPriceRequestServiceDBEngine.Enums;
using EPriceRequestServiceDBEngine.Models;

namespace EPriceViewer.Helpers
{
    public class DataHelper
    {
        private const int RootCatalogId = 0;
        private readonly DbEngine _storage;
        private readonly List<Un1tCategory> _categories;

        public List<Un1tCategory> Categories
        {
            get { return _categories.ToList(); }
        } 

        public DataHelper(DbEngine storage)
        {
            _storage = storage;
            _categories = LoadUn1tCategories();
        }

        public bool IsCategoryMapped(int categoryId)
        {
            var isMapped = false;
            var category = _categories.FirstOrDefault(x => x.Id == categoryId);
            if (category != null)
            {
                isMapped = category.LoadedProviderCategoryCount != 0;
                if (!isMapped)
                {
                    var childs = _categories.Where(x => x.IdParent == categoryId);
                    foreach (var child in childs)
                    {
                        isMapped = IsCategoryMapped(child.Id);
                        if (isMapped) break;
                    }
                }
            }
            return isMapped;
        }

        public List<Product> GetProductsForCategory(int categoryId)
        {
            return _storage.GetProductsForCategory(categoryId);
        }

        public List<Un1tCategory> LoadUn1tCategories()
        {
            return _storage.LoadAllUn1tCategories();
        }

        public List<Un1tCategory> GetRootCategories()
        {
            return _categories.Where(x => x.IdParent == RootCatalogId).ToList();
        }
    }
}
