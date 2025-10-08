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
    /// <summary>
    /// Upserts (insert or update) a document in MongoDB based on a filter criteria.
    /// 
    /// Required Inputs:
    /// - Document: The document to insert/update
    /// - Filter: MongoDBFilter configuration specifying what to match
    /// 
    /// Filter Value Resolution:
    /// The step automatically extracts filter values from the document when possible.
    /// For example, if your filter has FieldName="name", it will use the document's "name" field value for filtering.
    /// You can also provide explicit filter value inputs to override the document values.
    /// </summary>
    [Writable]
    public class UpsertDocumentStep : BaseInsertStep
    {
        private const string DOCUMENT_INPUT_NAME = "Document";
        private const string FILTER_INPUT_NAME = "Filter";
        
        public UpsertDocumentStep() : base() { }
        
        public UpsertDocumentStep(string serverId) : base(serverId) { }

        public override string StepName => "Upsert Document";

        public override DataDescription[] InputData
        {
            get
            {
                List<DataDescription> inputs = new List<DataDescription>();

                AddInputsFromServerConfig(inputs);

                inputs.Add(new DataDescription(GetDocumentType(), DOCUMENT_INPUT_NAME));
                inputs.Add(new DataDescription(typeof(MongoDBFilter), FILTER_INPUT_NAME));

                return inputs.ToArray();
            }
        }

        public override ResultData Run(StepStartData data)
        {
            MethodInfo upsertDocument = typeof(UpsertDocumentStep)
                .GetMethod(nameof(UpsertDocument), BindingFlags.NonPublic | BindingFlags.Instance)
                ?.MakeGenericMethod(GetDocumentType());
            upsertDocument?.Invoke(this, new object[] { data });
            return new ResultData(PATH_SUCCESS);
        }

        private void UpsertDocument<TDocument>(StepStartData data)
        {
            IMongoCollection<TDocument> collection = GetMongoCollection<TDocument>(data);
            TDocument doc;
            MongoDBFilter filter;
            
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

            try
            {
                filter = (MongoDBFilter)data[FILTER_INPUT_NAME];
            }
            catch(Exception ex)
            {
                throw new LoggedException("Filter is missing", ex);
            }
            if (filter == null)
                throw new LoggedException("Filter is missing");

            // Create a modified StepStartData that includes filter values from the document
            var enhancedData = CreateEnhancedStepData(data, filter, doc);

            FilterDefinition<TDocument> mongoFilter;
            try
            {
                mongoFilter = FetchStepUtility.GetFilter<TDocument>(filter, enhancedData);
            }
            catch(Exception ex)
            {
                string filterInputName = filter.GetInputName();
                throw new LoggedException($"Filter configuration error: Unable to resolve filter value for '{filterInputName}' (field '{filter.FieldName}'). Make sure the document contains this field.", ex);
            }

            var options = new ReplaceOptions { IsUpsert = true };
            collection.ReplaceOne(mongoFilter, doc, options);
        }

        private StepStartData CreateEnhancedStepData<TDocument>(StepStartData originalData, MongoDBFilter filter, TDocument doc)
        {
            // Create a copy of the original data
            var enhancedData = new StepStartData();
            foreach (var kvp in originalData)
            {
                enhancedData[kvp.Key] = kvp.Value;
            }

            // Try to extract filter values from the document
            if (filter.FilterType == MongoFilterType.FieldName && !string.IsNullOrEmpty(filter.FieldName))
            {
                string filterInputName = filter.GetInputName();
                
                // Only add if not already provided by user
                if (!enhancedData.ContainsKey(filterInputName))
                {
                    // Try to get the value from the document
                    var docType = typeof(TDocument);
                    var property = docType.GetProperty(filter.FieldName, BindingFlags.Public | BindingFlags.Instance);
                    if (property != null)
                    {
                        var value = property.GetValue(doc);
                        if (value != null)
                        {
                            enhancedData[filterInputName] = value;
                        }
                    }
                }
            }
            else if (filter.FilterType == MongoFilterType.CombineAnd || filter.FilterType == MongoFilterType.CombineOr)
            {
                // Handle combined filters recursively
                if (filter.SubFilters != null)
                {
                    foreach (var subFilter in filter.SubFilters)
                    {
                        var subEnhancedData = CreateEnhancedStepData(enhancedData, subFilter, doc);
                        foreach (var kvp in subEnhancedData)
                        {
                            if (!enhancedData.ContainsKey(kvp.Key))
                            {
                                enhancedData[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                }
            }

            return enhancedData;
        }
    }
}