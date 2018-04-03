using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Bot_Application1.Models
{
    public class Person
    {
        [BsonId]
        public ObjectId Id;
        [BsonElement("resourceName")]
        public string resourceName;
        [BsonElement("enterpriseId")]
        public string enterpiseId;
        [BsonElement("personalNumber")]
        public int personalNumber;
        [BsonElement("project_team")]
        public string project_team;
        [BsonElement("startDate")]
        public System.DateTime startDate;
        [BsonElement("rollOffDate")]
        public System.DateTime rollOffDate;
        [BsonElement("wbsCodes")]
        public System.Collections.Generic.IEnumerable<WBSCode> wbsCodes;

        public string getWbsCode()
        {
            //TODO: write method to get most up to date wbsCodes (top 3 maybe?)
            return "abc";
        }

        public override string ToString()
        {
            return System.String.Format("{0} current wbs code is {1}", enterpiseId, getWbsCode());
        }
    }
}