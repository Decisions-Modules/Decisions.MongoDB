using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        public DeleteBulkDocumentStep() : base() { }
        
        public DeleteBulkDocumentStep(string serverId) : base(serverId) { }
        public override string StepName => "Bulk Delete Documents";
        protected override string DocumentIdInputName => "Document IDs";

        public override DataDescription[] InputData => GetInputData(true);

        public override OutcomeScenarioData[] OutcomeScenarios => new[]
        {
            new OutcomeScenarioData(PATH_SUCCESS),
            new OutcomeScenarioData(PATH_ERROR)
        }; 

        public override ResultData Run(StepStartData data)
        {
            List < ObjectId > objectIds = (
                from object docIdObject in (IList)data[DocumentIdInputName] 
                select new ObjectId(docIdObject.ToString()))
                .ToList();
            
            DeleteResult result = GetMongoRawDocumentCollection(data)
                .DeleteMany(Builders<BsonDocument>.Filter.In("_id", objectIds));
            
            return result.DeletedCount == 0 ? new ResultData(PATH_ERROR) : new ResultData(PATH_SUCCESS);
        }
    }
}