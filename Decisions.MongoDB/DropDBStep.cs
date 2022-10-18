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
    public class DropDBStep : BaseMongoDBAdvancedStep, ISyncStep, IDataConsumer
    {
        public override string StepName => "Drop Database";

        public DataDescription[] InputData
        {
            get
            {
                List<DataDescription> inputs = new List<DataDescription>();

                inputs.Add(new DataDescription(typeof(string), CONN_STRING_INPUT));
                inputs.Add(new DataDescription(typeof(string), DB_NAME_INPUT));

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
            if (string.IsNullOrEmpty(connString))
                throw new Exception("Connection string is missing");
            if (string.IsNullOrEmpty(dbName))
                throw new Exception("Database name is missing");

            MongoClient client = new MongoClient(connString);
            client.DropDatabase(dbName);
            return new ResultData(PATH_SUCCESS);
        }

    }
}
