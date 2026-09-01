using System;
using System.Collections.Generic;
using System.Linq;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.ServiceLayer.Services.ContextData;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Decisions.MongoDB;

[Writable]
public class ListIndexesStep : BaseMongoDBAdvancedStep, ISyncStep, IDataConsumer
{
    private const string RESULT = "Indexes";
    private const string ERROR = "Error Getting Indexes";
    
    public override string StepName => "List Indexes";
    
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
                new OutcomeScenarioData(PATH_SUCCESS, new DataDescription(typeof(string[]), RESULT)),
                new OutcomeScenarioData(PATH_ERROR, new DataDescription(typeof(string), ERROR)),
            };
        }
    }
    
     public ResultData Run(StepStartData data)
    {
        string connString = data.Data[CONN_STRING_INPUT] as string;
        string dbName = data.Data[DB_NAME_INPUT] as string;
        string collName = data.Data[COLLECTION_NAME_INPUT] as string;
        
        if (string.IsNullOrEmpty(connString))
            throw new Exception("Connection string is missing");
        if (string.IsNullOrEmpty(dbName))
            throw new Exception("Database name is missing");
        if (string.IsNullOrEmpty(collName))
            throw new Exception("Collection name is missing");

        try
        {
            MongoClient client = new MongoClient(connString);
            IMongoDatabase db = client.GetDatabase(dbName);
            IMongoCollection<BsonDocument> collection = db.GetCollection<BsonDocument>(collName);
            
            // Get indexes from Collection
            List<BsonDocument> indexes = collection.Indexes.List().ToList();

            string[] indexNames = indexes.Select(index => index["name"].AsString).ToArray();

            return new ResultData(PATH_SUCCESS, new DataPair[] { new DataPair(RESULT, indexNames) });
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed to list indexes");
            return new ResultData(PATH_ERROR, new DataPair[] { new DataPair(ERROR, ex.Message) });
        }
    }
}