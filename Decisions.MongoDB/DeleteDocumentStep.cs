using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow;
using MongoDB.Bson;
using MongoDB.Driver; 
using System.Collections.Generic;
using DecisionsFramework;
using DecisionsFramework.Design.Flow.Mapping;

namespace Decisions.MongoDB
{
    [Writable]
    public class DeleteDocumentStep : BaseDeleteStep
    {
        private const string DOCUMENT_ID_INPUT_NAME = "Document ID";
        public DeleteDocumentStep() : base() { }
        
        public DeleteDocumentStep(string serverId) : base(serverId) { }

        public override string StepName => "Delete Document";

        public override bool ShowTypePicker => false;

        public override DataDescription[] InputData
        {
            get
            {
                List<DataDescription> inputs = new List<DataDescription>();
                
                AddInputsFromServerConfig(inputs);
                inputs.Add(new DataDescription(GetIdPropertyType(), DOCUMENT_ID_INPUT_NAME, false));

                return inputs.ToArray();
            }
        } 

        public override OutcomeScenarioData[] OutcomeScenarios => new[]
        {
            new OutcomeScenarioData(PATH_SUCCESS)
        };

        public override ResultData Run(StepStartData data)
        { 
            object docId = data[DOCUMENT_ID_INPUT_NAME];
            if (docId == null || docId as string == string.Empty)
                throw new LoggedException($"{DOCUMENT_ID_INPUT_NAME} is missing");
            FilterDefinition<BsonDocument> filter = FetchStepUtility.GetIdMatchFilter<BsonDocument>(docId, GetIdPropertyTypeEnum());
            GetMongoRawDocumentCollection(data).DeleteOne(filter);
            return new ResultData(PATH_SUCCESS);
        }
    }
}
