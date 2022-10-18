﻿using DecisionsFramework;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Flow.Mapping;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Decisions.MongoDB
{
    [Writable]
    public class DeleteDocumentStep : BaseMongoDBStep, ISyncStep, IDataConsumer
    {
        const string DOCUMENT_ID_INPUT = "Document ID";

        public DeleteDocumentStep() { }
        public DeleteDocumentStep(string serverId)
        {
            ServerId = serverId;
        }

        public override string StepName => "Delete Document";

        public override bool ShowTypePicker => false;

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
                    new OutcomeScenarioData(PATH_SUCCESS)
                };
            }
        }

        public ResultData Run(StepStartData data)
        {
            IMongoCollection<BsonDocument> collection = GetMongoRawDocumentCollection(data);
            object docId = data[DOCUMENT_ID_INPUT];
            if (docId == null || docId as string == string.Empty)
                throw new Exception("Document ID is missing");
            FilterDefinition<BsonDocument> filter = FetchStepUtility.GetIdMatchFilter<BsonDocument>(docId, GetIdPropertyTypeEnum());
            collection.DeleteOne(filter);
            return new ResultData(PATH_SUCCESS);
        }

    }
}
