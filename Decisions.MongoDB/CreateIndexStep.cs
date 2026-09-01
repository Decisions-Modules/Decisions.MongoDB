using System;
using System.Collections.Generic;
using System.Linq;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.ServiceLayer.Services.ContextData;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Decisions.MongoDB;

[Writable]
public class CreateIndexStep : BaseMongoDBAdvancedStep, ISyncStep, IDataConsumer
{
    public override string StepName => "Create Index";

    private const string INDEX_KEYS = "Index Keys";
    private const string RESULT = "Created Index Name";
    private const string ERROR = "Error Creating Index";

    [WritableValue]
    private IndexOptionsDefinition options;

    [PropertyClassification(1, "Options (Optional)", SETTINGS_CATEGORY)]
    public IndexOptionsDefinition Options
    {
        get => options;
        set => options = value;
    }

    public DataDescription[] InputData
    {
        get
        {
            List<DataDescription> inputs = new List<DataDescription>();
            
            inputs.Add(new DataDescription(typeof(string), CONN_STRING_INPUT));
            inputs.Add(new DataDescription(typeof(string), DB_NAME_INPUT));
            inputs.Add(new DataDescription(typeof(string), COLLECTION_NAME_INPUT));
            inputs.Add(new DataDescription(new DecisionsNativeType(typeof(IndexKeyDefinition)), INDEX_KEYS, true, false, false));
            
            return inputs.ToArray();
        }
    }
    
    public override OutcomeScenarioData[] OutcomeScenarios
    {
        get
        {
            return new OutcomeScenarioData[]
            {
                new OutcomeScenarioData(PATH_SUCCESS, new DataDescription(typeof(string), RESULT)),
                new OutcomeScenarioData(PATH_ERROR, new DataDescription(typeof(string), ERROR)),
            };
        }
    }

    public ResultData Run(StepStartData data)
    {
        string connString = data.Data[CONN_STRING_INPUT] as string;
        string dbName = data.Data[DB_NAME_INPUT] as string;
        string collName = data.Data[COLLECTION_NAME_INPUT] as string;
        IndexKeyDefinition[] indexKeys = data.Data[INDEX_KEYS] as IndexKeyDefinition[];
        
        if (string.IsNullOrEmpty(connString))
            throw new Exception("Connection string is missing");
        if (string.IsNullOrEmpty(dbName))
            throw new Exception("Database name is missing");
        if (string.IsNullOrEmpty(collName))
            throw new Exception("Collection name is missing");
        if (indexKeys == null || indexKeys.Length == 0)
            throw new Exception("At least one index key is required");

        try
        {
            MongoClient client = new MongoClient(connString);
            IMongoDatabase db = client.GetDatabase(dbName);
            IMongoCollection<BsonDocument> collection = db.GetCollection<BsonDocument>(collName);

            IndexKeysDefinitionBuilder<BsonDocument> keysBuilder = Builders<BsonDocument>.IndexKeys;
            
            // Build one key definition per field (with its sort direction)
            List<IndexKeysDefinition<BsonDocument>> keyDefs = indexKeys.Select(k =>
                k.Direction == IndexSortDirection.Ascending
                    ? keysBuilder.Ascending(k.FieldName)
                    : keysBuilder.Descending(k.FieldName)).ToList();
            
            // Combine all field keys into a single index definition
            IndexKeysDefinition<BsonDocument> keys = keysBuilder.Combine(keyDefs);

            CreateIndexOptions createIndexOptions = new CreateIndexOptions();
            if (Options != null)
            {
                // Leaving Name unset lets MongoDB auto-generate one from the field names
                if (!string.IsNullOrEmpty(Options.IndexName))
                    createIndexOptions.Name = Options.IndexName;
                createIndexOptions.Unique = Options.Unique;
                createIndexOptions.Sparse = Options.Sparse;
                if(Options.EnableTTL)
                    createIndexOptions.ExpireAfter = TimeSpan.FromSeconds(Options.TTLSeconds);
            }

            // Creates a single index
            string createdName = collection.Indexes.CreateOne(new CreateIndexModel<BsonDocument>(keys, createIndexOptions));
            return new ResultData(PATH_SUCCESS, new DataPair[] { new DataPair(RESULT, createdName) });
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed to create index");
            return new ResultData(PATH_ERROR, new DataPair[] { new DataPair(ERROR, ex.Message) });
        }
    }
}