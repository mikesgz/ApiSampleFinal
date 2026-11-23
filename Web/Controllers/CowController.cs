using ApiSampleFinal.Models.CowModels;
using ApiSampleFinal.Models.MilkModels;
using AutoMapper;
using Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace ApiSampleFinal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CowController : ControllerBase
    {
        public ICowService CowService { get; set; }
     
        public IMapper Mapper { get; set; }
        public CowController(ICowService service, IMapper mapper)
        {
            CowService = service;
            Mapper = mapper;
        }

        [HttpPost("AddCow")]
        public IActionResult AddMilkExistences([FromBody] CowDto cow)
        {
            CowService.AddCow(Mapper.Map<Cow>(cow));
            return Ok();
        }

        [HttpGet("GetAllCows")]
        public IActionResult GetAllCows()
        {
             IList<Cow> cows = CowService.GetAllCows();
            return Ok(Mapper.Map<IList<CowDto>>(cows));
        }
    }


}
