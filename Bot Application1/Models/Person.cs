using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Bot_Application1.Models
{
    public class Person
    {
        [BsonId]
        public ObjectId Id;
        [BsonElement("fullname")]
        public string fullname;
        [BsonElement("wbs")]
        public WBSCode chargeCode;
    }
}