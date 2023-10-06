using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using DecisionsFramework;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.Utilities;
using MongoDB.Driver;

namespace Decisions.MongoDB
{
    public class ReplaceBulkDocumentStep_02 : BaseReplaceStep
    {
        public ReplaceBulkDocumentStep_02() : base() { }
        
        public ReplaceBulkDocumentStep_02(string serverId) : base(serverId) { }

        public override string StepName => "Replace Documents_02";

        public override DataDescription[] InputData
        {
            get
            {
                List<DataDescription> inputs = new List<DataDescription>();
                AddInputsFromServerConfig(inputs);
                inputs.Add(new DataDescription(GetDocumentType(), DocumentInput));
                inputs.Add(new DataDescription(GetIdPropertyType(), DocumentIdInputName, isList: true));
                return inputs.ToArray();
            }
        }

        protected override string DocumentIdInputName => "Document IDs"; 
        
        public override ResultData Run(StepStartData data)
        {
            MethodInfo replaceDocument = typeof(ReplaceBulkDocumentStep_02)
                .GetMethod(nameof(ReplaceDocument), BindingFlags.NonPublic | BindingFlags.Instance)
                ?.MakeGenericMethod(GetDocumentType());
            replaceDocument?.Invoke(this, new object[] { data });
            return new ResultData(PATH_SUCCESS);
        }

        private void ReplaceDocument<TDocument>(StepStartData data)
        {

            if (data[DocumentIdInputName] == null)
                throw new LoggedException("Document IDs inputs are missing");
            
            object[] ids = data[DocumentIdInputName].GetType().IsArray
                ? (object[])data[DocumentIdInputName]
                : new[] { data[DocumentIdInputName] };
            
            if (ids.Length == 0 || ids.Any(id => id is null or ""))
                { throw new LoggedException("One or more Document IDs inputs are missing"); }

            
            var idType = GetIdPropertyTypeEnum();
            var documentInput = GetDocumentInput<TDocument>(data);

            var idPropertyInfo = GetIdPropertyName<TDocument>();
            
            
            
            var updates = ids
                .Select(id => new ReplaceOneModel<TDocument>(
                    FetchStepUtility.GetIdMatchFilter<TDocument>(id, idType),
                    SetId<TDocument>(idPropertyInfo, documentInput, id)){ IsUpsert = Upsert })
                .Cast<WriteModel<TDocument>>()
                .ToList();

            
            var result = GetMongoCollection<TDocument>(data).BulkWriteAsync(
                updates, new BulkWriteOptions() { IsOrdered = false }).Result;
            
            
            // result.ProcessedRequests
        }

        private PropertyInfo GetIdPropertyName<TDocument>(string[] searchCandidates = null)
        {
            var candidates = searchCandidates ?? new[] { "Id", "id", "_id" };
            foreach (var name in candidates)
            {
                var candidate = typeof(TDocument).GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (candidate != null)
                    return candidate;
            }
            throw new DataException($"Unexpected document type {typeof(TDocument)} does not have a field in " +
                                    $"{candidates.Select(s => "'" + s + "' ")}and therefore cannot be processed");
        }

        private static TDocument SetId<TDocument>(PropertyInfo info, TDocument document, object idValue)
        {
            info.SetValue(document, idValue);
            return document;
        }
    }
}