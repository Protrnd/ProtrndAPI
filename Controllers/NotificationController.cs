using Microsoft.AspNetCore.Mvc;
using ProtrndWebAPI.Services.Network;

namespace ProtrndWebAPI.Controllers
{
    [Route("api/n")]
    [ApiController]
    [ProTrndAuthorizationFilter]
    public class NotificationController : BaseController
    {
        public NotificationController(IServiceProvider serviceProvider) : base(serviceProvider) { }

        [HttpGet("get/{id}")]
        public async Task<ActionResult<ActionResponse>> GetNotifications(Guid id)
        {
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = await _notificationService.GetNotificationsAsync(id) });
        }

        [HttpPut("set/viewed/{id}")]
        public async Task<ActionResult<ActionResponse>> SetNotificationViewed(Guid id)
        {
            var resultOk = await _notificationService.SetNotificationViewedAsync(id);
            if (!resultOk)
                return BadRequest(new ActionResponse { Message = "Notification set viewed failed" });
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = "Notification set viewed ok" });
        }
    }
}
