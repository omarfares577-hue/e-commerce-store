using AutoMapper;
using Store.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Store.Application.Dtos.ReturnDtos;
namespace Store.API.Helpers
{
    public class ProductUrlResolver : IValueResolver<Product,ProductDto,string?>
    {
        private readonly IConfiguration _config;

        public ProductUrlResolver(IConfiguration config)
        {
            _config = config;
        }

        public string? Resolve(Product source, ProductDto destination, string? destMember, ResolutionContext context)
        {
            if (string.IsNullOrEmpty(source?.PictureUrl))
                return null;

            var baseUrl = _config["ApiUrl"];

            if (string.IsNullOrEmpty(baseUrl))
                return source.PictureUrl;

            return $"{baseUrl.TrimEnd('/')}/{source.PictureUrl.TrimStart('/')}";
        }
    }
}
