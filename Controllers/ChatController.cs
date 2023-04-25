using Microsoft.AspNetCore.Mvc;
using ProtrndWebAPI.Models.Payments;
using ProtrndWebAPI.Services.Network;

namespace ProtrndWebAPI.Controllers
{
    [Route("api/chat")]
    [ApiController]
    [ProTrndAuthorizationFilter]
    public class ChatController : BaseController
    {
        public ChatController(IServiceProvider serviceProvider) : base(serviceProvider) { }

        [HttpPost("send")]
        public async Task<ActionResult<ActionResponse>> SendChat(ChatDTO chat)
        {
            return Ok(new ActionResponse
            {
                Successful = true,
                Message = "OK",
                Data = await _chatService.SendChat(new Chat
                {
                    ReceiverId = chat.ReceiverId,
                    SenderId = _profileClaims.ID,
                    Message = chat.Message,
                    ItemId = chat.ItemId,
                    Type = chat.Type
                }),
                StatusCode = 200
            });
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            return Ok(new ActionResponse
            {
                Successful = true,
                Message = "OK",
                Data = await _chatService.GetConversationAsync(_profileClaims.ID),
                StatusCode = 200
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetChats(Guid id)
        {
            return Ok(new ActionResponse
            {
                Successful = true,
                Message = "OK",
                Data = await _chatService.GetChatsFromUser(_profileClaims.ID, id),
                StatusCode = 200
            });
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteChatMessage(Guid id)
        {
            await _chatService.DeleteChatMessage(id);
            return Ok();
        }
    }
}
