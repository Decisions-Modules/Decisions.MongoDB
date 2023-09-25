using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using DecisionsFramework.Design.Flow.Mapping;

namespace Decisions.MongoDB
{
    [Writable]
    public class DeleteDocumentStep : BaseDeleteStep
    {
        public DeleteDocumentStep() : base() { }
        
        public DeleteDocumentStep(string serverId) : base(serverId) { }

        public override string StepName => "Delete Document";

        public override bool ShowTypePicker => false;

        public override DataDescription[] InputData => GetInputData(false);
        
        protected override string DocumentIdInputName => "Document ID";

        public override OutcomeScenarioData[] OutcomeScenarios => new[]
        {
            new OutcomeScenarioData(PATH_SUCCESS)
        };

        public override ResultData Run(StepStartData data)
        {
            IMongoCollection<BsonDocument> collection = GetMongoRawDocumentCollection(data);
            object docId = data[DocumentIdInputName];
            if (docId == null || docId as string == string.Empty)
                throw new Exception("Document ID is missing");
            FilterDefinition<BsonDocument> filter = FetchStepUtility.GetIdMatchFilter<BsonDocument>(docId, GetIdPropertyTypeEnum());
            collection.DeleteOne(filter);
            return new ResultData(PATH_SUCCESS);
        }
    }
}
