using DecisionsFramework;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Flow.Mapping;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Decisions.MongoDB
{
    [Writable]
    public class InsertDocumentStep : BaseMongoDBStep, ISyncStep, IDataConsumer
    {
        const string DOCUMENT_INPUT = "Document";

        public InsertDocumentStep() { }
        public InsertDocumentStep(string serverId)
        {
            ServerId = serverId;
        }

        public override string StepName => "Insert Document";

        public override bool ShowIdTypeOverride => false;

        public DataDescription[] InputData
        {
            get
            {
                List<DataDescription> inputs = new List<DataDescription>();

                AddInputsFromServerConfig(inputs);

                inputs.Add(new DataDescription(GetDocumentType(), DOCUMENT_INPUT));

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
            MethodInfo insertDocument = typeof(InsertDocumentStep)
                .GetMethod(nameof(InsertDocument), BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGenericMethod(GetDocumentType());
            insertDocument.Invoke(this, new object[] { data });
            return new ResultData(PATH_SUCCESS);
        }

        private void InsertDocument<TDocument>(StepStartData data)
        {
            IMongoCollection<TDocument> collection = GetMongoCollection<TDocument>(data);
            TDocument doc;
            try
            {
                doc = (TDocument)data[DOCUMENT_INPUT];
            }
            catch
            {
                throw new Exception("Document is missing");
            }
            if (doc == null)
                throw new Exception("Document is missing");

            collection.InsertOne(doc);
        }

    }
}
