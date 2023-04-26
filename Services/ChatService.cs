﻿using Microsoft.Extensions.Options;
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
                var newConversation = conversation[0];
                newConversation.Time = chat.Time;
                var find = Builders<Conversations>.Filter.Where(c => c.Senderid == chat.SenderId && c.ReceiverId == chat.ReceiverId || c.Senderid == chat.ReceiverId && c.ReceiverId == chat.SenderId);
                newConversation.RecentMessage = chat.Message;
                newConversation.Senderid = chat.SenderId;
                newConversation.ReceiverId = chat.ReceiverId;
                var updateResult = await _conversationsCollection.ReplaceOneAsync(find, newConversation);
                return updateResult.ModifiedCount > 0;
            } else
            {
                await _conversationsCollection.InsertOneAsync(new Conversations { ReceiverId= chat.ReceiverId, Senderid = chat.SenderId, RecentMessage = chat.Message });
                return true;
            }
        }

        public async Task<List<Chat>> GetChatsFromUser(Guid senderId, Guid receiverId)
        {
            return await _chatCollection.Find(c => c.SenderId == senderId && c.ReceiverId == receiverId || c.ReceiverId == senderId && c.SenderId == receiverId)
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
