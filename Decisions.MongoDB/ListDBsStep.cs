using DecisionsFramework;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.ServiceLayer.Services.ContextData;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Decisions.MongoDB
{
    [Writable]
    public class ListDBsStep : BaseMongoDBAdvancedStep, ISyncStep, IDataConsumer
    {
        const string RESULTS = "Database Names";

        public override string StepName => "List Database Names";

        public DataDescription[] InputData
        {
            get
            {
                List<DataDescription> inputs = new List<DataDescription>();

                inputs.Add(new DataDescription(typeof(string), CONN_STRING_INPUT));

                return inputs.ToArray();
            }
        }

        public override OutcomeScenarioData[] OutcomeScenarios
        {
            get
            {
                return new OutcomeScenarioData[]
                {
                    new OutcomeScenarioData(PATH_SUCCESS, new DataDescription(typeof(string), RESULTS, true))
                };
            }
        }

        public ResultData Run(StepStartData data)
        {
            string connString = data[CONN_STRING_INPUT] as string;
            if (string.IsNullOrEmpty(connString))
                throw new Exception("Connection string is missing");

            MongoClient client = new MongoClient(connString);
            List<string> results = new List<string>();
            using (IAsyncCursor<string> names = client.ListDatabaseNames())
            {
                while (names.MoveNext())
                {
                    foreach (var name in names.Current)
                        results.Add(name);
                }
            }
            return new ResultData(PATH_SUCCESS, new DataPair[] { new DataPair(RESULTS, results.ToArray()) });
        }

    }
}
