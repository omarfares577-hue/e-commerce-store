using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Store.Domain.Entities;
namespace Store.Application.Specification
{
    public class ProductsWithCategoriesSpecification : BaseSpecification<Product>
    {
        public ProductsWithCategoriesSpecification()
        {
            AddInclude(p=>p.Category);
        }
        public ProductsWithCategoriesSpecification(int id) : base(p=>p.Id ==id )
        {
            AddInclude(p => p.Category);
        }
        public ProductsWithCategoriesSpecification(ProductSpecParam specParam)
        : base(p => (string.IsNullOrEmpty(specParam.Search) || p.Name.ToLower().Contains(specParam.Search)) &&
        (!specParam.CategoryId.HasValue || p.CategoryId == specParam.CategoryId))

        {
            AddInclude(p => p.Category);
            AddOrderBy(p => p.Id);
            if(!string.IsNullOrEmpty(specParam.Sort))
            {
                switch (specParam.Sort.ToLower())
                {
                    case "priceasc":
                        AddOrderBy(p => p.Price);
                        break;
                    case "pricedesc":
                        AddOrderByDescending(p => p.Price);
                        break;
                    default:
                        AddOrderBy(p => p.Id);
                        break;
                }
            }
            ApplyPaging(specParam.PageSize * (specParam.PageIndex - 1), specParam.PageSize);
        }
    }
}

