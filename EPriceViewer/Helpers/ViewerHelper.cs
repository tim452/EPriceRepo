using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EPriceRequestServiceDBEngine.Enums;

namespace EPriceViewer.Helpers
{
    public static class ViewerHelper
    {
        public static string GetDisplayNameForLocation(Location location)
        {
            var name = string.Empty;
            switch (location)
            {
                case Location.Moscow:
                    name = "Москва";
                    break;
                case Location.SanktPeterburg:
                    name = "СПБ";
                    break;
                case Location.Region:
                    name = "Регионы";
                    break;
            }
            return name;
        }
    }
}
