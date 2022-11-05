using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ProtrndWebAPI.Models.Payments
{
    [BsonIgnoreExtraElements]
    public class Gift
    {
        [BsonIgnore]
        public ObjectId? Id { get; set; } = null;
        public Guid ProfileId { get; set; }
        public Guid PostId { get; set; } = Guid.Empty;
        public bool Disabled = false;
    }
}
