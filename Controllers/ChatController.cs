using Microsoft.AspNetCore.Mvc;
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
        public async Task<ActionResult<ActionResponse>> SendChat([FromBody] ChatDTO chat)
        {
            return Ok(new ActionResponse
            {
                Successful = true,
                Message = "OK",
                Data = await _chatService.SendChat(new Chat
                {
                    ReceiverId = chat.ReceiverId,
                    SenderId = _profileClaims.ID,
                    Convoid = chat.Convoid,
                    Message = chat.Message,
                    ItemId = chat.ItemId,
                    Type = chat.Type
                }),
                StatusCode = 200
            });
        }

        [HttpGet("conversation/{id}")]
        public async Task<ActionResult<ActionResponse>> GetConversationId(Guid id)
        {
            return Ok(new ActionResponse
            {
                Successful = true,
                Message = "OK",
                Data = await _chatService.GetConversationIdAsync(_profileClaims.ID, id),
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
                Data = await _chatService.GetChatsFromUser(id),
                StatusCode = 200
            });
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteChatMessage(Guid id)
        {
            return Ok(new ActionResponse
            {
                Successful = true,
                Message = "OK",
                Data = await _chatService.DeleteChatMessage(id),
                StatusCode = 200
            });
        }
    }
}
