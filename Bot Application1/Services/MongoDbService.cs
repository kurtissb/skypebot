using Bot_Application1.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Bot_Application1.Services
{
    public class MongoDbService
    {
        private MongoClient client;
        private MongoClientSettings settings;
        const string connectionString = "mongodb://localhost:27017";

        public MongoDbService()
        {
            this.client = new MongoClient(connectionString);

            settings = new MongoClientSettings
            {
                Server = new MongoServerAddress("localhost", 27017),
                UseSsl = false
            };
        }


        public async Task<WBSCode> getWbsCode(string name)
        {
            var db = client.GetDatabase("innovation");
            var collection = db.GetCollection<BsonDocument>("wbscodes");
            var results = new List<BsonDocument>();

            var textInfo = new CultureInfo("en-UK", false).TextInfo;

            await collection.Find(new BsonDocument("fullName", textInfo.ToTitleCase(name)))
                     .ForEachAsync(document => results.Add(document));

            if (results.Count > 0)
            {
                return new WBSCode
                {
                    code = results[0].GetElement("wbs").Value.AsBsonDocument.GetElement("code").ToString(),
                    description = results[0].GetElement("wbs").Value.AsBsonDocument.GetElement("description").ToString(),
                    chargeable = results[0].GetElement("wbs").Value.AsBsonDocument.GetElement("chargable").ToString().ToLower() == "true" ? true : false
                };

            }

            return null;
        }
    }
}