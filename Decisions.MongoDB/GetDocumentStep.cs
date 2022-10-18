using DecisionsFramework;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.ServiceLayer.Services.ContextData;
using MongoDB.Bson;
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
    public class GetDocumentStep : BaseMongoDBStep, ISyncStep, IDataConsumer
    {
        const string DOCUMENT_ID_INPUT = "Document ID";
        const string RESULT = "Document";

        public GetDocumentStep() { }
        public GetDocumentStep(string serverId)
        {
            ServerId = serverId;
        }

        public override string StepName => "Get Document By ID";

        public DataDescription[] InputData
        {
            get
            {
                List<DataDescription> inputs = new List<DataDescription>();

                AddInputsFromServerConfig(inputs);

                inputs.Add(new DataDescription(GetIdPropertyType(), DOCUMENT_ID_INPUT));

                return inputs.ToArray();
            }
        }

        public override OutcomeScenarioData[] OutcomeScenarios
        {
            get
            {
                return new OutcomeScenarioData[]
                {
                    new OutcomeScenarioData(PATH_SUCCESS, new DataDescription(GetDocumentType(), RESULT))
                };
            }
        }

        public ResultData Run(StepStartData data)
        {
            MethodInfo getDocument = typeof(GetDocumentStep)
                .GetMethod(nameof(GetDocument), BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGenericMethod(GetDocumentType());
            object doc = getDocument.Invoke(this, new object[] { data });
            return new ResultData(PATH_SUCCESS, new DataPair[] { new DataPair(RESULT, doc) });
        }

        private TDocument GetDocument<TDocument>(StepStartData data)
        {
            IMongoCollection<TDocument> collection = GetMongoCollection<TDocument>(data);
            object docId = data[DOCUMENT_ID_INPUT];
            if (docId == null || docId as string == string.Empty)
                throw new Exception("Document ID is missing");
            FilterDefinition<TDocument> filter = FetchStepUtility.GetIdMatchFilter<TDocument>(docId, GetIdPropertyTypeEnum());
            TDocument doc = collection.Find(filter).FirstOrDefault();
            return doc;
        }

    }
}
