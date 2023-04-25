using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ProtrndWebAPI.Settings;

namespace ProtrndWebAPI.Services
{
    public class ChatService: BaseService
    {
        public ChatService(IOptions<DBSettings> options) : base(options) { }

        public async Task<List<Conversations>> GetConversationAsync(Guid profileId)
        {
            return await _conversationsCollection.Find(c => c.Senderid == profileId || c.ReceiverId == profileId)
                .SortByDescending(c => c.Time)
                .ToListAsync();
        }

        public async Task<bool> SendChat(Chat chat)
        {
            await _chatCollection.InsertOneAsync(chat);
            var conversation = await GetConversationAsync(chat.ReceiverId);
            if (conversation.Count > 0)
            {
                conversation[0].Time = DateTime.Now;
                var find = Builders<Conversations>.Filter.Eq(c => c.Senderid, chat.SenderId);
                var update = Builders<Conversations>.Update.Set(c => c.Time, DateTime.Now);
                var updateResult = await _conversationsCollection.UpdateOneAsync(find, update);
                return updateResult.ModifiedCount > 0;
            } else
            {
                await _conversationsCollection.InsertOneAsync(new Conversations { ReceiverId= chat.ReceiverId, Senderid = chat.SenderId });
                return true;
            }
        }

        public async Task<List<Chat>> GetChatsFromUser(Guid senderId, Guid receiverId)
        {
            return await _chatCollection.Find(c => c.SenderId == senderId && c.ReceiverId == receiverId)
                .SortByDescending(c => c.Time)
                .ToListAsync();
        }

        public async Task DeleteChatMessage(Guid chatId)
        {
            var delete = Builders<Chat>.Filter.Eq(c => c.Id, chatId);
            await _chatCollection.DeleteOneAsync(delete);
            return;
        }
    }
}
