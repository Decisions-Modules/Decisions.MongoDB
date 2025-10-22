using System;
using DecisionsFramework;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.Design.Properties;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Decisions.MongoDB
{
    [Writable]
    public class UpsertDocumentStep : BaseInsertStep
    {
        private const string DOCUMENT_INPUT_NAME = "Document";
        private const string FILTER_INPUT_NAME = "Filter";
        
        public UpsertDocumentStep() : base() { }
        
        public UpsertDocumentStep(string serverId) : base(serverId) { }

        public override string StepName => "Upsert Document";

        [WritableValue]
        private MongoDBFilter filter;

        [PropertyClassification(1, "Filter Criteria", SETTINGS_CATEGORY)]
        public MongoDBFilter Filter
        {
            get
            {
                UpdateFilterInProperty(filter);
                return filter;
            }
            set
            {
                filter = value;
                UpdateFilterInProperty(filter);
                OnPropertyChanged();
                OnPropertyChanged(nameof(InputData));
            }
        }

        [PropertyHidden]
        public string[] FieldNames
        {
            get
            {
                Type type = GetDocumentType();
                if (type == typeof(string))
                    return new string[0];

                return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Select(x => x.Name).ToArray();
            }
        }

        private void UpdateFilterInProperty(MongoDBFilter filter)
        {
            if (filter == null) return;

            string[] fieldNames = FieldNames;
            filter.AllFieldNames = fieldNames;
        }

        protected override void OnTypeChanged()
        {
            UpdateFilterInProperty(filter);
            base.OnTypeChanged();
        }

        public override DataDescription[] InputData
        {
            get
            {
                List<DataDescription> inputs = new List<DataDescription>();

                AddInputsFromServerConfig(inputs);

                inputs.Add(new DataDescription(GetDocumentType(), DOCUMENT_INPUT_NAME));

                // Add filter-specific inputs if filter is configured
                if (filter != null)
                {
                    Dictionary<string, DataDescription> inputNames = new Dictionary<string, DataDescription>();
                    foreach (DataDescription dd in inputs)
                        inputNames.Add(dd.Name, dd);

                    foreach (DataDescription dd in filter.GetDataDescriptions(GetDocumentType()))
                    {
                        if (!inputNames.ContainsKey(dd.Name))
                        {
                            inputNames.Add(dd.Name, dd);
                            inputs.Add(dd);
                        }
                    }
                }

                return inputs.ToArray();
            }
        }

        public override ValidationIssue[] GetAdditionalValidationIssues()
        {
            List<ValidationIssue> issues = new List<ValidationIssue>();

            issues.AddRange(GetDuplicateInputValidationIssues());

            return issues.ToArray();
        }

        private ValidationIssue[] GetDuplicateInputValidationIssues()
        {
            if (filter == null)
                return new ValidationIssue[0];

            // Check all inputs for duplicates. If the type is the same, warn; otherwise, error.
            HashSet<string> warnings = new HashSet<string>();
            HashSet<string> errors = new HashSet<string>();
            List<DataDescription> inputs = new List<DataDescription>();

            AddInputsFromServerConfig(inputs);

            Dictionary<string, DataDescription> inputNames = new Dictionary<string, DataDescription>();
            foreach (DataDescription dd in inputs)
                inputNames.Add(dd.Name, dd);

            foreach (DataDescription dd in filter.GetDataDescriptions(GetDocumentType()))
            {
                if (!inputNames.ContainsKey(dd.Name))
                {
                    inputNames.Add(dd.Name, dd);
                }
                else
                {
                    if (dd.FullTypeName == inputNames[dd.Name].FullTypeName)
                    {
                        warnings.Add(dd.Name);
                    }
                    else
                    {
                        errors.Add(dd.Name);
                    }
                }
            }

            List<ValidationIssue> issues = new List<ValidationIssue>();
            foreach (string warningName in warnings)
            {
                issues.Add(new ValidationIssue(this, $"Multiple filter criteria use the name '{warningName}'. The same input value will be used for each.", "", BreakLevel.Warning, nameof(Filter)));
            }
            foreach (string errorName in errors)
            {
                issues.Add(new ValidationIssue(this, $"Multiple filter criteria use the name '{errorName}' and the types do not match.", "", BreakLevel.Fatal, nameof(Filter)));
            }
            return issues.ToArray();
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