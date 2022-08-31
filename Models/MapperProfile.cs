using AutoMapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace GiftCertificateService.Models
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<SqlDataReader, ResponseCertGetDTO>()
                .ForMember(dest => dest.Barcode, opt => opt.MapFrom(src => src.GetString("Barcode")))
                .ForMember(dest => dest.Sum, opt => opt.MapFrom(src => src.GetDecimal("SumLeft")));
        }
    }
}
