using Microsoft.AspNetCore.Mvc;
using ProtrndWebAPI.Services.Network;

namespace ProtrndWebAPI.Controllers
{
    [Route("api/profile")]
    [ApiController]
    public class ProfileController : BaseController
    {
        public ProfileController(IServiceProvider serviceProvider) : base(serviceProvider) { }

        [HttpGet]
        [ProTrndAuthorizationFilter]
        public async Task<ActionResult<ActionResponse>> GetCurrentProfile()
        {
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = await _profileService.GetProfileByIdAsync(_profileClaims.ID) });
        }

        [HttpGet("all")]
        [ProTrndAuthorizationFilter(role: Constants.Admin)]
        public async Task<ActionResult<ActionResponse>> GetAllProfiles()
        {
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = await _profileService.GetAllProfiles() });
        }

        [HttpGet("{id}")]
        [ProTrndAuthorizationFilter]
        public async Task<ActionResult<ActionResponse>> GetProfileById(Guid id)
        {
            var profile = await _profileService.GetProfileByIdAsync(id);
            if (profile == null)
                return NotFound(new ActionResponse { StatusCode = 404, Message = ActionResponseMessage.NotFound });
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = profile });
        }

        [HttpGet("fetch/{name}")]
        [ProTrndAuthorizationFilter]
        public async Task<ActionResult<ActionResponse>> GetProfileByUsername(string name)
        {
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = await _profileService.GetProfileByNameAsync(name) });
        }

        [HttpGet("name/{name}")]
        [ProTrndAuthorizationFilter]
        public async Task<ActionResult<ActionResponse>> GetProfilesByUsername(string name)
        {
            var profile = await _profileService.GetProfilesByNameAsync(name);
            if (profile == null)
                return NotFound(new ActionResponse { StatusCode = 404, Message = ActionResponseMessage.NotFound });
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = profile });
        }

        [HttpPut("update")]
        [ProTrndAuthorizationFilter]
        public async Task<ActionResult<ActionResponse>> UpdateProfile([FromBody] ProfileDTO updateProfile)
        {
            var profile = new Profile
            {
                FullName = updateProfile.FullName,
                UserName = updateProfile.UserName,
                BackgroundImageUrl = updateProfile.BackgroundImageUrl,
                ProfileImage = updateProfile.ProfileImage,
                About = updateProfile.About,
                Location = updateProfile.Location
            };
            var currentProfile = await _profileService.GetProfileByIdAsync(_profileClaims.ID);
            var result = await _profileService.UpdateProfile(currentProfile, profile);
            if (result == null)
                return BadRequest(new ActionResponse { StatusCode = 400, Message = "Update failed" });
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = result });
        }

        [HttpPost("follow/{id}")]
        [ProTrndAuthorizationFilter]
        public async Task<ActionResult<ActionResponse>> Follow(Guid id)
        {
            if (id == _profileClaims.ID)
                return Forbid();
            var followOk = await _profileService.Follow(_profileClaims, id);
            if (!followOk)
                return BadRequest(new ActionResponse { StatusCode = 400, Message = "Follow failed" });
            await _notificationService.FollowNotification(_profileClaims, id);
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = "Follow successful" });
        }

        [HttpDelete("unfollow/{id}")]
        [ProTrndAuthorizationFilter]
        public async Task<ActionResult<ActionResponse>> UnFollow(Guid id)
        {
            var resultOk = await _profileService.UnFollow(_profileClaims, id);
            if (!resultOk)
                return BadRequest(new ActionResponse { StatusCode = 400, Message = "Unfollow failed" });
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = "Unfollow successful" });
        }

        [HttpGet("followers/{id}")]
        [ProTrndAuthorizationFilter]
        public async Task<ActionResult<ActionResponse>> GetFollowers(Guid id)
        {
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = await _profileService.GetFollowersAsync(id) });
        }

        [HttpGet("is-following/{id}")]
        [ProTrndAuthorizationFilter]
        public async Task<ActionResult<ActionResponse>> IsFollowing(Guid id)
        {
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = await _profileService.IsFollowing(_profileClaims.ID, id) });
        }

        [HttpGet("followings/{id}")]
        [ProTrndAuthorizationFilter]
        public async Task<ActionResult<ActionResponse>> GetFollowings(Guid id)
        {
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = await _profileService.GetFollowings(id) });
        }

        [HttpGet("followers/{id}/count")]
        [ProTrndAuthorizationFilter]
        public async Task<ActionResult<ActionResponse>> GetFollowerCount(Guid id)
        {
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = await _profileService.GetFollowerCount(id) });
        }

        [HttpGet("followings/{id}/count")]
        [ProTrndAuthorizationFilter]
        public async Task<ActionResult<ActionResponse>> GetFollowingCount(Guid id)
        {
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = await _profileService.GetFollowingCount(id) });
        }

        [HttpPut("disable/{id}")]
        [ProTrndAuthorizationFilter(role: Constants.Admin)]
        public async Task<ActionResult<ActionResponse>> DisableAccount(Guid id)
        {
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = await _profileService.UpdateDisableProfile(id, true) });
        }

        [HttpPut("enable/{id}")]
        [ProTrndAuthorizationFilter(role: Constants.Admin)]
        public async Task<ActionResult<ActionResponse>> EnableAccount(Guid id)
        {
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = await _profileService.UpdateDisableProfile(id, false) });
        }
    }
}
