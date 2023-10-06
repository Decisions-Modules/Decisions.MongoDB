using System.Collections.Generic;
using DecisionsFramework;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Flow.Mapping;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Decisions.MongoDB
{
    [Writable]
    public class DeleteBulkDocumentStep : BaseDeleteStep
    {
        private const string DOCUMENT_ID_INPUT_NAME = "Document IDs";
        private const string PATH_ERROR = "Error";

        public DeleteBulkDocumentStep() : base() { }
        
        public DeleteBulkDocumentStep(string serverId) : base(serverId) { }
        public override string StepName => "Bulk Delete Documents"; 

        public override DataDescription[] InputData
        {
            get
            {
                List<DataDescription> inputs = new List<DataDescription>();
                
                AddInputsFromServerConfig(inputs);
                inputs.Add(new DataDescription(GetIdPropertyType(), DOCUMENT_ID_INPUT_NAME, true));

                return inputs.ToArray();
            }
        }

        public override OutcomeScenarioData[] OutcomeScenarios => new[]
        {
            new OutcomeScenarioData(PATH_SUCCESS),
            new OutcomeScenarioData(PATH_ERROR)
        }; 

        public override ResultData Run(StepStartData data)
        {
            object inputIds = data[DOCUMENT_ID_INPUT_NAME];
            if (inputIds == null || ((string[])inputIds).Length == 0)
                throw new LoggedException($"{DOCUMENT_ID_INPUT_NAME} is missing");
            List<string> inputs = new List<string>((string[])data[DOCUMENT_ID_INPUT_NAME]);
            DeleteResult result = GetMongoRawDocumentCollection(data)
                .DeleteMany(FetchStepUtility.GetIdsInFilter<BsonDocument>(inputs, GetIdPropertyTypeEnum()));
            return result.DeletedCount == 0 ? new ResultData(PATH_ERROR) : new ResultData(PATH_SUCCESS);
        }
        
        
    }
}