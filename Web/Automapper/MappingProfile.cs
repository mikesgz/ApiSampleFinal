using ApiSampleFinal.Models.CowModels;
using ApiSampleFinal.Models.MilkModels;
using AutoMapper;
using Domain;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ApiSampleFinal.Automapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            MilkMapper();
            CowMapper();
        }

        private void MilkMapper()
        {
            CreateMap<Milk, MilkDTO>()
    
            .ReverseMap();

        }

        private void CowMapper()
        {
            CreateMap<Cow, CowDto>()
             .ForMember(x => x.Liters, y => y.MapFrom(z => z.GetMilkQuantity()));


            CreateMap<CowDto, Cow>()
              ;

        }

    }

    
}
