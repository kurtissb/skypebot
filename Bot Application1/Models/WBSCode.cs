using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Bot_Application1.Models
{
    public class WBSCode
    {
        [BsonId]
        public ObjectId Id;
        [BsonElement("code")]
        public string code { get; set; }
        [BsonElement("description")]
        public string description { get; set; }
        [BsonElement("chargeable")]
        public bool chargeable { get; set; }
        [BsonElement("addedDate")]
        public DateTime addedDate { get; set; }

        public override string ToString()
        {
            return "code: " + code +
                "\ndescription: " + description +
                "\nchargeable: " + chargeable;
        }
    }
}