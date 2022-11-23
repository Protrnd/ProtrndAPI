using Microsoft.AspNetCore.Mvc;
using ProtrndWebAPI.Services.Network;

namespace ProtrndWebAPI.Controllers
{
    [Route("api/location")]
    [ApiController]
    [ProTrndAuthorizationFilter]
    public class LocationController : BaseController
    {
        public LocationController(IServiceProvider serviceProvider) : base(serviceProvider) { }

        [HttpPost("add")]
        public async Task<ActionResult<ActionResponse>> AddLocation(LocationDTO location)
        {
            return Ok(new ActionResponse { StatusCode = 200, Successful = true, Message = "Location inserted", Data = await _locationService.AddLocationAsync(location) });
        }

        [HttpGet("get")]
        public async Task<ActionResult<ActionResponse>> GetLocations()
        {
            return Ok(new ActionResponse { Successful = true, Data = await _locationService.GetLocations(), Message = "Locations retrieved", StatusCode = 200 });
        }
    }
}
