using System;
using System.Collections.Generic;
using System.Reflection;
using DecisionsFramework;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Flow.Mapping;
using MongoDB.Driver;

namespace Decisions.MongoDB
{
    [Writable]
    public class InsertBulkDocumentStep : BaseInsertStep
    {
        private const string DOCUMENT_INPUT_NAME = "Documents";
        
        public InsertBulkDocumentStep() : base() { }
        
        public InsertBulkDocumentStep(string serverId) : base(serverId) { }

        public override string StepName => "Insert Documents";
        
        public override DataDescription[] InputData
        {
            get
            {
                List<DataDescription> inputs = new List<DataDescription>();

                AddInputsFromServerConfig(inputs);

                inputs.Add(new DataDescription(GetDocumentType(), DOCUMENT_INPUT_NAME, true));

                return inputs.ToArray();
            }
        }
        
        public override ResultData Run(StepStartData data)
        {
            MethodInfo insertDocument = typeof(InsertBulkDocumentStep)
                .GetMethod(nameof(InsertDocuments), BindingFlags.NonPublic | BindingFlags.Instance)
                ?.MakeGenericMethod(GetDocumentType());
            insertDocument?.Invoke(this, new object[] { data });
            return new ResultData(PATH_SUCCESS);
        }

        private void InsertDocuments<TDocument>(StepStartData data)
        {
            IMongoCollection<TDocument> collection = GetMongoCollection<TDocument>(data);
            TDocument[] docs;
            try
            {
                docs = (TDocument[])data[DOCUMENT_INPUT_NAME];
            }
            catch(Exception ex)
            {
                throw new LoggedException("Documents are missing", ex);
            }
            if (docs == null)
                throw new LoggedException("Documents are missing");

            collection.InsertMany(docs);
        }
    }
}