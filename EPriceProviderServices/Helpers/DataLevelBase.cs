using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using EPriceProviderServices.Models;
using EPriceRequestServiceDBEngine.Enums;
using EPriceRequestServiceDBEngine.Models;

namespace EPriceProviderServices.Helpers
{
    public class DataLevelBase
    {
        #region Fileds

        protected List<ProviderServiceCategoryBase> _categories = null;
        public event EventHandler<CategoryModelEventArgs> CategoryLoadStateChanged;
        public event EventHandler<DeleteMappedCategoryEventArgs> CategoryMapDeleted;
        protected readonly object _lockObj;
        protected readonly string _logSender;
        private readonly ProviderType _provider;
        private readonly string _rootCatalogId;

        #endregion

        protected DataLevelBase(ProviderType provider, string rootCatalogId)
        {
            _rootCatalogId = rootCatalogId;
            _provider = provider;
            _lockObj = new object();
            _logSender = _provider + " уровень данных";
            _categories = LoadCategories();
        }

        #region Set differnt data 

        public void SetUn1tCategoryForProducts(List<Un1tProductBase> products)
        {
            products.ForEach((x) =>
            {
                ProviderServiceCategoryBase category = null;
                if (!string.IsNullOrEmpty(x.IdProviderCategory))
                {
                    category = GetCategory(x.IdProviderCategory);
                }
                if (category != null)
                {
                    x.IdCategory = category.IdUn1tCategory;
                }
            });
        }

        public void SetCategories(IEnumerable<ProviderServiceCategoryBase> newCategories)
        {
            var isLocked = false;
            try
            {
                Monitor.Enter(_lockObj, ref isLocked);
                if (_categories == null || !_categories.Any())
                {
                    _categories = newCategories.ToList();
                }
                else
                {
                    var oldCategories = _categories;
                    _categories = newCategories.ToList();
                    _categories.ForEach(x =>
                    {
                        var oldCategory = oldCategories.FirstOrDefault(y => y.Id == x.Id);
                        if (oldCategory != null)
                        {
                            x.IsLoad = oldCategory.IsLoad;
                            x.IdUn1tCategory = oldCategory.IdUn1tCategory;
                        }
                    });
                }
                SaveCategories();
            }
            catch (Exception ex)
            {
                Log.WriteToLog(_logSender + " SetCategories() ", "Ошибка: " + ex.Message);
                throw;
            }
            finally
            {
                if (isLocked) Monitor.Exit(_lockObj);
            }
        }

        public void SetCategoryIsLoaded(string id, bool isLoaded)
        {
            var isLocked = false;
            try
            {
                Monitor.Enter(_lockObj, ref isLocked);
                var category = _categories.FirstOrDefault(x => x.Id == id);
                if (category != null)
                {
                    category.IsLoad = isLoaded;
                    var childs = GetCategoriesForParent(category.Id);
                    foreach (var child in childs)
                    {
                        SetCategoryIsLoaded(child.Id, isLoaded);
                    }
                    var parent = _categories.FirstOrDefault(x => x.Id == category.IdParent);
                    if (parent != null)
                    {
                        var childsParent = GetCategoriesForParent(parent.Id);
                        parent.IsLoad = childsParent.Count() == childsParent.Count(x => x.IsLoad);
                    }
                }
                SaveCategories();
            }
            catch (Exception ex)
            {
                Log.WriteToLog(_logSender + " SetCategoryIsLoaded(" + id + "," + isLoaded + ") ",
                    "Ошибка: " + ex.Message);
                throw;
            }
            finally
            {
                if (isLocked) Monitor.Exit(_lockObj);
            }
        }

        public void UpdateLoadInfo(List<ProviderCategoryBase> categoriesToLoad)
        {
            if (categoriesToLoad == null) return;
            if (_categories == null || !_categories.Any()) return;
            _categories.ForEach((x) =>
            {
                x.IdUn1tCategory = -1;
                x.IsLoad = false;
            });
            foreach (var providerCategoryBase in categoriesToLoad)
            {
                var category = _categories.FirstOrDefault(x => x.Id == providerCategoryBase.Id);
                if (category != null)
                {
                    category.IsLoad = true;
                    category.IdUn1tCategory = providerCategoryBase.IdUn1tCategory;
                }
            }
            SaveCategories();
        }

        #endregion

        #region Get categories

        public List<ProviderServiceCategoryBase> GetCategories()
        {
            List<ProviderServiceCategoryBase> list = null;
            var isLocked = false;
            try
            {
                Monitor.Enter(_lockObj, ref isLocked);
                list = _categories.ToList();
            }
            catch (Exception ex)
            {
                Log.WriteToLog(_logSender + " GetCategories() ", "Ошибка: " + ex.Message);
                throw;
            }
            finally
            {
                if (isLocked) Monitor.Exit(_lockObj);
            }
            return list;
        }

        public ProviderServiceCategoryBase GetCategory(string id)
        {
            ProviderServiceCategoryBase model = null;
            var isLocked = false;
            try
            {
                Monitor.Enter(_lockObj, ref isLocked);
                model = _categories.FirstOrDefault(x => x.Id == id);
            }
            catch (Exception ex)
            {
                Log.WriteToLog(_logSender + " GetCategory(" + id + ") ", "Ошибка: " + ex.Message);
                throw;
            }
            finally
            {
                if (isLocked) Monitor.Exit(_lockObj);
            }
            return model;
        }

        public List<ProviderServiceCategoryBase> GetAllChildsForCategory(string idCategory)
        {
            var list = new List<ProviderServiceCategoryBase>();
            var childs = GetCategoriesForParent(idCategory);
            if (childs.Any())
            {
                list.AddRange(childs);
                foreach (var child in childs)
                {
                    list.AddRange(GetAllChildsForCategory(child.Id));
                }
            }
            return list;
        }

        public List<ProviderServiceCategoryBase> GetCategoriesForParent(string parentId)
        {
            List<ProviderServiceCategoryBase> list = null;
            var isLocked = false;
            try
            {
                Monitor.Enter(_lockObj, ref isLocked);
                list = _categories.Where(x => x.IdParent == parentId).ToList();
            }
            catch (Exception ex)
            {
                Log.WriteToLog(_logSender + " GetCategoriesForParent(" + parentId + ") ", "Ошибка: " + ex.Message);
                throw;
            }
            finally
            {
                if (isLocked) Monitor.Exit(_lockObj);
            }
            return list;
        }

        public virtual List<ProviderServiceCategoryBase> GetRootCategories()
        {
            return GetCategoriesForParent(_rootCatalogId);
        }

        public virtual List<ProviderServiceCategoryBase> GetCategoriesToLoad()
        {
            var list = new List<ProviderServiceCategoryBase>();
            var isLocked = false;
            try
            {
                Monitor.Enter(_lockObj, ref isLocked);
                var rootCategories = GetCategoriesForParent(_rootCatalogId);
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

        protected IEnumerable<ProviderServiceCategoryBase> GetCategoriesToLoadForCategory(string id)
        {
            var list = new List<ProviderServiceCategoryBase>();
            var category = _categories.FirstOrDefault(x => x.Id == id);
            if (category != null)
            {
                if (category.IsLoad)
                {
                    list.Add(category);
                }
                else
                {
                    var childs = GetCategoriesForParent(category.Id);
                    foreach (var child in childs)
                    {
                        list.AddRange(GetCategoriesToLoadForCategory(child.Id));
                    }
                }
            }
            return list.ToArray();
        }

        #endregion

        #region Raise event

        public void RaiseCategoryLoadStateChangedEvent(ProviderServiceCategoryBase model, CategoryActionState newState, TreeViewItem item)
        {
            var ev = CategoryLoadStateChanged;
            if (ev != null)
            {
                var args = new CategoryModelEventArgs() { Model = model, NewState = newState, Item = item };
                ev(this, args);
            }
        }

        public void RaiseCategoryMapDeleted(TreeViewItem item)
        {
            var ev = CategoryMapDeleted;
            if (ev != null)
            {
                var args = new DeleteMappedCategoryEventArgs() { Item = item };
                ev(this, args);
            }
        }

        #endregion

        #region Storage category

        private List<ProviderServiceCategoryBase> LoadCategories()
        {
            var isLocked = false;
            var assemblyLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var fileName = _provider + "Categories.dt";
            var list = new List<ProviderServiceCategoryBase>();
            if (!string.IsNullOrEmpty(assemblyLocation))
            {
                try
                {
                    Monitor.Enter(_lockObj, ref isLocked);
                    var path = Path.Combine(assemblyLocation, fileName);
                    if (File.Exists(path))
                    {
                        var formatter = new BinaryFormatter();
                        using (var stream = File.OpenRead(path))
                        {
                            list = (List<ProviderServiceCategoryBase>)formatter.Deserialize(stream);
                        }
                    }
                }
                catch(Exception ex) 
                {
                    Log.WriteToLog(_logSender + " LoadCategories() ", "Ошибка: " + ex.Message);
                    throw;
                }
                finally
                {
                    if (isLocked) Monitor.Exit(_lockObj);
                }
            }
            return list;
        }

        public void SaveCategories()
        {
            var isLocked = false;
            var assemblyLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var fileName = _provider + "Categories.dt";
            if (!string.IsNullOrEmpty(assemblyLocation))
            {
                try
                {
                    Monitor.Enter(_lockObj, ref isLocked);
                    var path = Path.Combine(assemblyLocation, fileName);
                    var fileMode = FileMode.Truncate;
                    if (!File.Exists(path))
                    {
                        fileMode = FileMode.CreateNew;
                    }
                    var formatter = new BinaryFormatter();
                    using (var stream = File.Open(path, fileMode, FileAccess.Write))
                    {
                        formatter.Serialize(stream, _categories);
                        stream.Flush();
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteToLog(_logSender + " SaveCategories() ", "Ошибка: " + ex.Message);
                    throw;
                }
                finally
                {
                    if (isLocked) Monitor.Exit(_lockObj);
                }
            }
        }

        #endregion
    }
}
