using AutoMapper;
using GiftCertificateService.Models;
using System.Data;
using System.Data.Common;

namespace GiftCertificateService.Mapping
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<DbDataReader, CertGetResponseDTO>()
                .ForMember(dest => dest.Barcode, opt => opt.MapFrom(src => src.GetString("Barcode")))
                .ForMember(dest => dest.Sum, opt => opt.MapFrom(src => src.GetDecimal("SumLeft")));
        }
    }
}
