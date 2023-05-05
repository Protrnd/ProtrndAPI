using Microsoft.AspNetCore.Mvc;
using ProtrndWebAPI.Services.Network;

namespace ProtrndWebAPI.Controllers
{
    [Route("api/tag")]
    [ApiController]
    [ProTrndAuthorizationFilter]
    public class TagController : BaseController
    {
        public TagController(IServiceProvider serviceProvider) : base(serviceProvider) { }

        [HttpGet("get/{name}")]
        public async Task<ActionResult<ActionResponse>> GetTags(string name)
        {
            var tags = await _tagsService.GetTagsWithNameAsync(name);
            if (tags == null)
                return NotFound(new ActionResponse { StatusCode = 404, Message = ActionResponseMessage.NotFound });
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = tags });
        }

        [HttpPost("add/{name}")]
        public async Task<ActionResult<ActionResponse>> AddTag(string name)
        {
            var added = await _tagsService.AddTagAsync(name);
            if (!added)
                return BadRequest(new ActionResponse { StatusCode = 400, Message = "Error Adding Tag!" });
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = "Tag added!", Data = name });
        }


    }
}
