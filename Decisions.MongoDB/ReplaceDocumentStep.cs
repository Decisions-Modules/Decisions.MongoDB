using DecisionsFramework;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.Design.Flow.Mapping.InputImpl;
using DecisionsFramework.Design.Properties;
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
    public class ReplaceDocumentStep : BaseMongoDBStep, ISyncStep, IDataConsumer, IDefaultInputMappingStep
    {
        const string DOCUMENT_INPUT = "Document";
        const string DOCUMENT_ID_INPUT = "Document ID";

        public ReplaceDocumentStep() { }
        public ReplaceDocumentStep(string serverId)
        {
            ServerId = serverId;
        }

        public override string StepName => "Replace Document";

        [WritableValue]
        private bool upsert = true;

        [PropertyClassification(0, "Insert Document If ID Not Found", SETTINGS_CATEGORY)]
        public bool Upsert
        {
            get { return upsert; }
            set { upsert = value; OnPropertyChanged(); }
        }

        public DataDescription[] InputData
        {
            get
            {
                List<DataDescription> inputs = new List<DataDescription>();

                AddInputsFromServerConfig(inputs);

                inputs.Add(new DataDescription(GetDocumentType(), DOCUMENT_INPUT));
                inputs.Add(new DataDescription(GetIdPropertyType(), DOCUMENT_ID_INPUT));

                return inputs.ToArray();
            }
        }

        public IInputMapping[] DefaultInputs
        {
            get
            {
                return new IInputMapping[]
                {
                    new IgnoreInputMapping { InputDataName = DOCUMENT_ID_INPUT }
                };
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
            MethodInfo replaceDocument = typeof(ReplaceDocumentStep)
                .GetMethod(nameof(ReplaceDocument), BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGenericMethod(GetDocumentType());
            replaceDocument.Invoke(this, new object[] { data });
            return new ResultData(PATH_SUCCESS);
        }

        private void ReplaceDocument<TDocument>(StepStartData data)
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

            object id = data[DOCUMENT_ID_INPUT];
            if (id == null || id as string == string.Empty)
                throw new Exception("Document ID input is missing");

            FilterDefinition<TDocument> filter = FetchStepUtility.GetIdMatchFilter<TDocument>(id, GetIdPropertyTypeEnum());
            ReplaceOneResult result = collection.ReplaceOne(filter, doc, new ReplaceOptions { IsUpsert = upsert });
            if (!upsert && result.MatchedCount == 0)
            {
                throw new InvalidOperationException($"Document with ID '{id}' doesn't exist in collection");
            }
        }

    }
}
