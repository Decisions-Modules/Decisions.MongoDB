using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using DecisionsFramework;
using DecisionsFramework.Design.Flow.Mapping;

namespace Decisions.MongoDB
{
    [Writable]
    public class DeleteDocumentStep_02 : BaseDeleteStep
    {
        public DeleteDocumentStep_02() : base() { }
        
        public DeleteDocumentStep_02(string serverId) : base(serverId) { }

        public override string StepName => "Delete Document_02";

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
                throw new LoggedException("Document ID is missing");
            FilterDefinition<BsonDocument> filter = FetchStepUtility.GetIdMatchFilter<BsonDocument>(docId, GetIdPropertyTypeEnum());
            collection.DeleteOne(filter);
            return new ResultData(PATH_SUCCESS);
        }
    }
}
