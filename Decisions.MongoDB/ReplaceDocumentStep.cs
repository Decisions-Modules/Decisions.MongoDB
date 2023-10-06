using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Flow.Mapping;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Reflection;
using DecisionsFramework;

namespace Decisions.MongoDB
{
    [Writable]
    public class ReplaceDocumentStep_02 : BaseReplaceStep
    { 

        public ReplaceDocumentStep_02() : base() { }
        public ReplaceDocumentStep_02(string serverId) : base(serverId) { }

        public override string StepName => "Replace Document_02";
        
        public override DataDescription[] InputData
        {
            get
            {
                List<DataDescription> inputs = new List<DataDescription>();
        
                AddInputsFromServerConfig(inputs);
                
                inputs.Add(new DataDescription(GetDocumentType(), DocumentInput));
                inputs.Add(new DataDescription(GetIdPropertyType(), DocumentIdInputName, isList: false));
                
                return inputs.ToArray();
            }
        }

        protected override string DocumentIdInputName => "Document ID"; 

        public override ResultData Run(StepStartData data)
        {
            MethodInfo replaceDocument = typeof(ReplaceDocumentStep_02)
                .GetMethod(nameof(ReplaceDocument), BindingFlags.NonPublic | BindingFlags.Instance)
                ?.MakeGenericMethod(GetDocumentType());
            replaceDocument?.Invoke(this, new object[] { data });
            return new ResultData(PATH_SUCCESS);
        }

        private void ReplaceDocument<TDocument>(StepStartData data)
        {
            TDocument doc = GetDocumentInput<TDocument>(data);
            IMongoCollection<TDocument> collection = GetMongoCollection<TDocument>(data);

            
            
            object id = data[DocumentIdInputName];
            if (id == null || id as string == string.Empty)
                throw new LoggedException("Document ID input is missing");

            FilterDefinition<TDocument> filter = FetchStepUtility.GetIdMatchFilter<TDocument>(id, GetIdPropertyTypeEnum());
            ReplaceOneResult result = collection.ReplaceOne(filter, doc, new ReplaceOptions { IsUpsert = Upsert });
            if (!Upsert && result.MatchedCount == 0)
            {
                throw new InvalidOperationException($"Document with ID '{id}' doesn't exist in collection");
            }
        }
    }
}
