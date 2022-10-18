using DecisionsFramework;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Flow.Mapping;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Decisions.MongoDB
{
    [Writable]
    public class RenameCollectionStep : BaseMongoDBAdvancedStep, ISyncStep, IDataConsumer
    {
        const string OLD_COLLECTION_NAME_INPUT = "Old Collection Name";
        const string NEW_COLLECTION_NAME_INPUT = "New Collection Name";

        public override string StepName => "Rename Collection";

        public DataDescription[] InputData
        {
            get
            {
                List<DataDescription> inputs = new List<DataDescription>();

                inputs.Add(new DataDescription(typeof(string), CONN_STRING_INPUT));
                inputs.Add(new DataDescription(typeof(string), DB_NAME_INPUT));
                inputs.Add(new DataDescription(typeof(string), OLD_COLLECTION_NAME_INPUT));
                inputs.Add(new DataDescription(typeof(string), NEW_COLLECTION_NAME_INPUT));

                return inputs.ToArray();
            }
        }

        public override OutcomeScenarioData[] OutcomeScenarios
        {
            get
            {
                return new OutcomeScenarioData[]
                {
                    new OutcomeScenarioData(PATH_SUCCESS)
                };
            }
        }

        public ResultData Run(StepStartData data)
        {
            string connString = data[CONN_STRING_INPUT] as string;
            string dbName = data[DB_NAME_INPUT] as string;
            string oldName = data[OLD_COLLECTION_NAME_INPUT] as string;
            string newName = data[NEW_COLLECTION_NAME_INPUT] as string;
            if (string.IsNullOrEmpty(connString))
                throw new Exception("Connection string is missing");
            if (string.IsNullOrEmpty(dbName))
                throw new Exception("Database name is missing");
            if (string.IsNullOrEmpty(oldName))
                throw new Exception("Old collection name is missing");
            if (string.IsNullOrEmpty(newName))
                throw new Exception("New collection name is missing");

            MongoClient client = new MongoClient(connString);
            IMongoDatabase db = client.GetDatabase(dbName);
            db.RenameCollection(oldName, newName);
            return new ResultData(PATH_SUCCESS);
        }

    }
}
