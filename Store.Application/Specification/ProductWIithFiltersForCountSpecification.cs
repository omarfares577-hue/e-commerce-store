using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Store.Application.Contracts; 
using Store.Domain.Entities;

namespace Store.Application.Specification
{
    public class ProductWIithFiltersForCountSpecification : BaseSpecification<Product>
    {
        public ProductWIithFiltersForCountSpecification(ProductSpecParam productParams)
            : base(p => (string.IsNullOrEmpty(productParams.Search) || p.Name.ToLower().Contains(productParams.Search)) &&
        (!productParams.CategoryId.HasValue || p.CategoryId == productParams.CategoryId))
        { }
    }
}
