using Microsoft.AspNetCore.Mvc;

namespace ProtrndWebAPI.Controllers
{
    [Route("api/chat")]
    [ApiController]
    public class ChatController : BaseController
    {
        public ChatController(IServiceProvider serviceProvider) : base(serviceProvider) { }

        [HttpPost("send")]
        public async Task<IActionResult> SendChat(ChatDTO chat)
        {
            return Ok(await _chatService.SendChat(new Chat
            {
                ReceiverId = chat.ReceiverId,
                SenderId = _profileClaims.ID,
                Message = chat.Message,
                Type = chat.Type
            }));
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            return Ok(await _chatService.GetConversationAsync(_profileClaims.ID));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetChats(Guid id)
        {
            return Ok(await _chatService.GetChatsFromUser(_profileClaims.ID, id));
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteChatMessage(Guid id)
        {
            await _chatService.DeleteChatMessage(id);
            return Ok();
        }
    }
}
