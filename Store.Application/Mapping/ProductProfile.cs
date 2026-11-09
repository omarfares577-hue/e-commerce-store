using AutoMapper;
using Store.API.Helpers;
using Store.Application.Dtos.ReturnDtos;
using Store.Domain.Entities;
using Store.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Store.Application.Mapping
{
    public class ProductProfile : Profile
    {
        public ProductProfile() {
            CreateMap<Category, CategoryDto>();
            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
            .ForMember(dest => dest.PictureUrl, opt => opt.MapFrom<ProductUrlResolver>());
            CreateMap<Order, OrderDto>()
                .ForMember(d => d.DeliveryMethod, o => o.MapFrom(s => s.DeliveryMethod.ShortName)) 
                .ForMember(d => d.Total, o => o.MapFrom(s => s.GetTotal()));

            CreateMap<OrderItems, OrderItemsDto>();
            CreateMap<ShippingAddress, AddressDto>().ReverseMap();
            CreateMap<AddressDto, Address>().ReverseMap();
        }
    }
}
