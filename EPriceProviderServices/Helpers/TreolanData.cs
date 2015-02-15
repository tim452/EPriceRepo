using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EPriceProviderServices.Models;
using EPriceRequestServiceDBEngine.Enums;

namespace EPriceProviderServices.Helpers
{
    public class TreolanData : DataLevelBase
    {
        public TreolanData()
            : base(ProviderType.Treolan, string.Empty)
        {
        }

        public override List<ProviderServiceCategoryBase> GetCategoriesToLoad()
        {
            var list = new List<ProviderServiceCategoryBase>();
            var isLocked = false;
            try
            {
                Monitor.Enter(_lockObj, ref isLocked);
                var rootCategories = new List<ProviderServiceCategoryBase>();
                foreach (var category in _categories)
                {
                    var isParentExists = _categories.FirstOrDefault(x => x.Id == category.IdParent) != null;
                    if (!isParentExists)
                    {
                        rootCategories.Add(category);
                    }
                }
                foreach (var category in rootCategories)
                {
                    list.AddRange(GetCategoriesToLoadForCategory(category.Id));
                }
            }
            catch (Exception ex)
            {
                Log.WriteToLog(_logSender + " GetCategoriesToLoad() ", "Ошибка: " + ex.Message);
                throw;
            }
            finally
            {
                if (isLocked) Monitor.Exit(_lockObj);
            }
            return list;
        }

        public override List<ProviderServiceCategoryBase> GetRootCategories()
        {
            var rootCategories = new List<ProviderServiceCategoryBase>();
            var isLocked = false;
            try
            {
                Monitor.Enter(_lockObj, ref isLocked);
                foreach (var category in _categories)
                {
                    var isParentExists = _categories.FirstOrDefault(x => x.Id == category.IdParent) != null;
                    if (!isParentExists)
                    {
                        rootCategories.Add(category);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteToLog(_logSender + " GetRootCategories() ", "Ошибка: " + ex.Message);
                throw;
            }
            finally
            {
                if (isLocked) Monitor.Exit(_lockObj);
            }
            return rootCategories;
        }
    }
}
