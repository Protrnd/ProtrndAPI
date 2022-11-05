using Microsoft.AspNetCore.Mvc;
using ProtrndWebAPI.Services.Network;

namespace ProtrndWebAPI.Controllers
{
    [Route("api/profile")]
    [ApiController]
    [ProTrndAuthorizationFilter]
    public class ProfileController : BaseController
    {
        public ProfileController(IServiceProvider serviceProvider) : base(serviceProvider) { }

        [HttpGet("get/id/{id}")]
        public async Task<ActionResult<ActionResponse>> GetProfileById(Guid id)
        {
            var profile = await _profileService.GetProfileByIdAsync(id);
            if (profile == null)
                return NotFound(new ActionResponse { StatusCode = 404, Message = ActionResponseMessage.NotFound });
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = profile });
        }

        [HttpPut("update")]
        public async Task<ActionResult<ActionResponse>> UpdateProfile([FromBody] ProfileDTO updateProfile)
        {
            var profile = new Profile { FullName = updateProfile.FullName, UserName = updateProfile.UserName };
            var result = await _profileService.UpdateProfile(_profile, profile);
            if (result == null)
                return BadRequest(new ActionResponse { StatusCode = 400, Message = "Update failed" });
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = result });
        }

        [HttpPost("follow/{id}")]
        public async Task<ActionResult<ActionResponse>> Follow(Guid id)
        {
            if (id == _profile.Identifier)
                return Forbid();
            var followOk = await _profileService.Follow(_profile, id);
            if (!followOk)
                return BadRequest(new ActionResponse { StatusCode = 400, Message = "Follow failed" });
            await _notificationService.FollowNotification(_profile, id);
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = "Follow successful" });
        }

        [HttpDelete("unfollow/{id}")]
        public async Task<ActionResult<ActionResponse>> UnFollow(Guid id)
        {
            var resultOk = await _profileService.UnFollow(_profile, id);
            if (!resultOk)
                return BadRequest(new ActionResponse { StatusCode = 400, Message = "Unfollow failed" });
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = "Unfollow successful" });
        }       

        [HttpGet("get/followers/{id}")]
        public async Task<ActionResult<ActionResponse>> GetFollowers(Guid id)
        {
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = await _profileService.GetFollowersAsync(id) });
        }

        [HttpGet("get/followings/{id}")]
        public async Task<ActionResult<ActionResponse>> GetFollowings(Guid id)
        {
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = await _profileService.GetFollowings(id) });
        }        

        [HttpGet("get/followers/{id}/count")]
        public async Task<ActionResult<ActionResponse>> GetFollowerCount(Guid id)
        {
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = await _profileService.GetFollowersAsync(id) });
        }        

        [HttpGet("get/followings/{id}/count")]
        public async Task<ActionResult<ActionResponse>> GetFollowingCount(Guid id)
        {
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = await _profileService.GetFollowersAsync(id) });
        }        

        [HttpGet("get/gifts/total")]
        public async Task<IActionResult> GetGiftTotal()
        {
            return NotFound();
            if (_profile == null)
            {
                return BadRequest(new ActionResponse { StatusCode = 401, Message = "Unauthorized" });
            }
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = await _profileService.GetFollowersAsync(_profile.Id) });
        }
      
    }
}
