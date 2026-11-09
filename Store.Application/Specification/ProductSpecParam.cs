using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Store.Application.Specification
{
    public class ProductSpecParam
    {
       public int PageIndex = 1;
        int _pageSize = 6;
        int _maxPageSize = 50;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > _maxPageSize) ? _maxPageSize : value;
        }
        public int? CategoryId { get; set; }
        public string? Sort { get; set; }
        private string? _search;
        public string? Search
        {
            get => _search ;
            set => _search = value?.ToLower();
        }
    }
}
