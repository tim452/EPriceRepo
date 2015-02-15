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
    public class OldiData : DataLevelBase
    {
        public OldiData()
            : base(ProviderType.OLDI, string.Empty)
        {
        
        }

        public override List<ProviderServiceCategoryBase> GetCategoriesToLoad()
        {
            var list = new List<ProviderServiceCategoryBase>();
            var isLocked = false;
            try
            {
                Monitor.Enter(_lockObj, ref isLocked);
                var loadList = _categories.Where(x => x.IsLoad && x.IdUn1tCategory != -1).ToList();
                if (loadList.Any())
                {
                    var lowLevelLoadList = loadList.Where(x => !GetCategoriesForParent(x.Id).Any()).ToList();
                    if (lowLevelLoadList.Any())
                    {
                        list.AddRange(lowLevelLoadList);
                    }
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
    }
}
