using System;
using DecisionsFramework;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Flow.Mapping;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Reflection;

namespace Decisions.MongoDB
{
    [Writable]
    public class InsertDocumentStep_02 : BaseInsertStep
    {
        private const string DOCUMENT_INPUT_NAME =  "Document";
        
        public InsertDocumentStep_02() : base() { }
        
        public InsertDocumentStep_02(string serverId) : base(serverId) { }

        public override string StepName => "Insert Document_02";

        public override DataDescription[] InputData
        {
            get
            {
                List<DataDescription> inputs = new List<DataDescription>();

                AddInputsFromServerConfig(inputs);

                inputs.Add(new DataDescription(GetDocumentType(), DOCUMENT_INPUT_NAME));

                return inputs.ToArray();
            }
        }

        public override ResultData Run(StepStartData data)
        {
            MethodInfo insertDocument = typeof(InsertDocumentStep_02)
                .GetMethod(nameof(InsertDocument), BindingFlags.NonPublic | BindingFlags.Instance)
                ?.MakeGenericMethod(GetDocumentType());
            insertDocument?.Invoke(this, new object[] { data });
            return new ResultData(PATH_SUCCESS);
        }

        private void InsertDocument<TDocument>(StepStartData data)
        {
            IMongoCollection<TDocument> collection = GetMongoCollection<TDocument>(data);
            TDocument doc;
            try
            {
                doc = (TDocument)data[DOCUMENT_INPUT_NAME];
            }
            catch(Exception ex)
            {
                throw new LoggedException("Document is missing", ex);
            }
            if (doc == null)
                throw new LoggedException("Document is missing");

            collection.InsertOne(doc);
        }
    }
}
