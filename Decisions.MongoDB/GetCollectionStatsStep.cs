using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Flow.Mapping;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;

namespace Decisions.MongoDB
{
    [Writable]
    public class GetCollectionStatsStep : BaseMongoDBAdvancedStep, ISyncStep, IDataConsumer
    {
        const string STATS_OUTPUT = "Collection Stats";
        const string RAW_STATS_OUTPUT = "Collection Stats Raw String";

        public override string StepName => "Get Collection Stats";

        public DataDescription[] InputData
        {
            get
            {
                List<DataDescription> inputs = new List<DataDescription>();

                inputs.Add(new DataDescription(typeof(string), CONN_STRING_INPUT));
                inputs.Add(new DataDescription(typeof(string), DB_NAME_INPUT));
                inputs.Add(new DataDescription(typeof(string), COLLECTION_NAME_INPUT));

                return inputs.ToArray();
            }
        }

        public override OutcomeScenarioData[] OutcomeScenarios
        {
            get
            {
                return new OutcomeScenarioData[]
                {
                    new OutcomeScenarioData(PATH_SUCCESS,
                        new DataDescription(typeof(CollectionStats), STATS_OUTPUT),
                        new DataDescription(typeof(string), RAW_STATS_OUTPUT))
                };
            }
        }

        public ResultData Run(StepStartData data)
        {
            string connString = data[CONN_STRING_INPUT] as string;
            string dbName = data[DB_NAME_INPUT] as string;
            string collName = data[COLLECTION_NAME_INPUT] as string;
            if (string.IsNullOrEmpty(connString))
                throw new Exception("Connection string is missing");
            if (string.IsNullOrEmpty(dbName))
                throw new Exception("Database name is missing");
            if (string.IsNullOrEmpty(collName))
                throw new Exception("Collection name is missing");

            MongoClient client = new MongoClient(connString);
            IMongoDatabase db = client.GetDatabase(dbName);
            BsonDocument bsonCommand = new BsonDocument("collStats", new BsonString(collName));
            Command<BsonDocument> command = new BsonDocumentCommand<BsonDocument>(bsonCommand);
            BsonDocument bsonResult = db.RunCommand(command);
            CollectionStats stats = BsonSerializer.Deserialize<CollectionStats>(bsonResult);
            if (stats.NumIndexes > 0)
                stats.AverageIndexSize = stats.IndexSize / stats.NumIndexes;

            return new ResultData(PATH_SUCCESS,
                new KeyValuePair<string, object>(STATS_OUTPUT, stats),
                new KeyValuePair<string, object>(RAW_STATS_OUTPUT, bsonResult.ToString()));
        }
    }
}
