using System;
using System.Collections.Generic;
using System.Reflection;
using DecisionsFramework;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Flow.Mapping;
using MongoDB.Driver;

namespace Decisions.MongoDB
{
    public class BulkReplaceDocumentStep : BaseReplaceStep
    {
        private const string DOCUMENT_INPUT_NAME = "Documents";
        public override string StepName => "Replace Documents";
        
        public BulkReplaceDocumentStep() : base() { }

        public BulkReplaceDocumentStep(string serverId) : base(serverId) { }

        public override DataDescription[] InputData
        {
            get
            {
                List<DataDescription> inputs = new List<DataDescription>();

                AddInputsFromServerConfig(inputs);
                
                inputs.Add(new DataDescription(typeof(ReplaceDocumentItem), DOCUMENT_INPUT_NAME, true)); 
                
                return inputs.ToArray();
            }
        }
        
        public override ResultData Run(StepStartData data)
        {
            MethodInfo replaceDocuments = typeof(BulkReplaceDocumentStep)
                .GetMethod(nameof(ReplaceDocuments), BindingFlags.NonPublic | BindingFlags.Instance)
                ?.MakeGenericMethod(GetDocumentType());
            replaceDocuments?.Invoke(this, new object[] { data });
            return new ResultData(PATH_SUCCESS);
        }

        private void ReplaceDocuments<TDocument>(StepStartData data)
        {
            ReplaceDocumentItem[] docs;
            try
            {
                docs = (ReplaceDocumentItem[])data[DOCUMENT_INPUT_NAME];
            }
            catch(Exception ex)
            {
                throw new LoggedException("Documents are missing", ex);
            }
            if (docs == null)
                throw new LoggedException("Documents are missing");
            IdType idType = GetIdPropertyTypeEnum();
            List<ReplaceOneModel<TDocument>> writeModels = new List<ReplaceOneModel<TDocument>>();
            foreach (ReplaceDocumentItem doc in docs)
            {
                if (string.IsNullOrEmpty(doc.Id))
                    throw new LoggedException("One or more replace document items is missing corresponding 'Id' value");
                if (doc.Document == null)
                    throw new LoggedException("One or more replace document items 'Document' is null/missing");
                if (doc is not TDocument)
                    throw new LoggedException("Input document does not match target type");
                writeModels.Add(new ReplaceOneModel<TDocument>(
                    FetchStepUtility.GetIdMatchFilter<TDocument>(doc.Id, idType), 
                    (TDocument)doc.Document) { IsUpsert = upsert });
            }
            GetMongoCollection<TDocument>(data).BulkWrite(writeModels);
        }
    }
}