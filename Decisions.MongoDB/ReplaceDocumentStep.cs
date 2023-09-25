﻿using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Flow.Mapping;
using MongoDB.Driver;
using System;
using System.Reflection;

namespace Decisions.MongoDB
{
    [Writable]
    public class ReplaceDocumentStep : BaseReplaceStep
    { 

        public ReplaceDocumentStep() : base() { }
        public ReplaceDocumentStep(string serverId) : base(serverId) { }

        public override string StepName => "Replace Document";
        
        public override DataDescription[] InputData => GetInputData(false);

        protected override string DocumentIdInputName => "Document ID"; 

        public override ResultData Run(StepStartData data)
        {
            MethodInfo replaceDocument = typeof(ReplaceDocumentStep)
                .GetMethod(nameof(ReplaceDocument), BindingFlags.NonPublic | BindingFlags.Instance)
                ?.MakeGenericMethod(GetDocumentType());
            replaceDocument.Invoke(this, new object[] { data });
            return new ResultData(PATH_SUCCESS);
        }

        private void ReplaceDocument<TDocument>(StepStartData data)
        {
            IMongoCollection<TDocument> collection = GetMongoCollection<TDocument>(data);
            TDocument doc;
            try
            {
                doc = (TDocument)data[DocumentInput];
            }
            catch
            {
                throw new Exception("Document is missing");
            }
            if (doc == null)
                throw new Exception("Document is missing");

            object id = data[DocumentIdInputName];
            if (id == null || id as string == string.Empty)
                throw new Exception("Document ID input is missing");

            FilterDefinition<TDocument> filter = FetchStepUtility.GetIdMatchFilter<TDocument>(id, GetIdPropertyTypeEnum());
            ReplaceOneResult result = collection.ReplaceOne(filter, doc, new ReplaceOptions { IsUpsert = Upsert });
            if (!Upsert && result.MatchedCount == 0)
            {
                throw new InvalidOperationException($"Document with ID '{id}' doesn't exist in collection");
            }
        }

    }
}
