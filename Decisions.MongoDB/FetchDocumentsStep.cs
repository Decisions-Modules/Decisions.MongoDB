using DecisionsFramework;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.ServiceLayer.Services.ContextData;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Decisions.MongoDB
{
    [Writable]
    public class FetchDocumentsStep : BaseMongoDBStep, ISyncStep, IDataConsumer
    {
        const string RESULTS = "Documents";

        public FetchDocumentsStep() { }
        public FetchDocumentsStep(string serverId)
        {
            ServerId = serverId;
        }

        public override string StepName => "Fetch Documents";

        public override bool ShowIdTypeOverride => false;

        [WritableValue]
        private bool combineFiltersUsingAnd = true;

        [PropertyClassification(0, "Combine Filters Using AND", SETTINGS_CATEGORY)]
        public bool CombineFiltersUsingAnd
        {
            get { return combineFiltersUsingAnd; }
            set { combineFiltersUsingAnd = value; OnPropertyChanged(); }
        }

        [WritableValue]
        private MongoDBFilter[] filters;

        [PropertyClassification(1, "Fetch Criteria", SETTINGS_CATEGORY)]
        public MongoDBFilter[] Filters
        {
            get
            {
                UpdateFiltersInArray(filters);
                return filters;
            }
            set
            {
                filters = value;
                UpdateFiltersInArray(filters);
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

        private void UpdateFiltersInArray(MongoDBFilter[] filters)
        {
            if (filters == null) return;

            string[] fieldNames = FieldNames;
            foreach (MongoDBFilter filter in filters)
            {
                filter.AllFieldNames = fieldNames;
            }
        }

        protected override void OnTypeChanged()
        {
            UpdateFiltersInArray(filters);
            base.OnTypeChanged();
        }

        public DataDescription[] InputData
        {
            get
            {
                List<DataDescription> inputs = new List<DataDescription>();

                AddInputsFromServerConfig(inputs);

                Dictionary<string, DataDescription> inputNames = new Dictionary<string, DataDescription>();
                foreach (DataDescription dd in inputs)
                    inputNames.Add(dd.Name, dd);

                if (filters != null)
                {
                    foreach (DataDescription dd in filters.SelectMany(filter => filter.GetDataDescriptions(GetDocumentType())))
                    {
                        if (!inputNames.ContainsKey(dd.Name))
                        { // Just use the first one with this name, and let the validation error handle any type conflicts for the same input name
                            inputNames.Add(dd.Name, dd);
                            inputs.Add(dd);
                        }
                    }
                }

                return inputs.ToArray();
            }
        }

        public override OutcomeScenarioData[] OutcomeScenarios
        {
            get
            {
                return new OutcomeScenarioData[]
                {
                    new OutcomeScenarioData(PATH_SUCCESS, new DataDescription(GetDocumentType(), RESULTS, true))
                };
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
            if (filters == null)
                return new ValidationIssue[0];

            // Check all inputs for duplicates. If the type is the same, warn; otherwise, error.
            HashSet<string> warnings = new HashSet<string>();
            HashSet<string> errors = new HashSet<string>();
            List<DataDescription> inputs = new List<DataDescription>();

            AddInputsFromServerConfig(inputs);

            Dictionary<string, DataDescription> inputNames = new Dictionary<string, DataDescription>();
            foreach (DataDescription dd in inputs)
                inputNames.Add(dd.Name, dd);

            foreach (DataDescription dd in filters.SelectMany(filter => filter.GetDataDescriptions(GetDocumentType())))
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
                issues.Add(new ValidationIssue(this, $"Multiple filters use the name '{warningName}'. The same input value will be used for each.", "", BreakLevel.Warning, nameof(Filters)));
            }
            foreach (string errorName in errors)
            {
                issues.Add(new ValidationIssue(this, $"Multiple filters use the name '{errorName}' and the types do not match.", "", BreakLevel.Fatal, nameof(Filters)));
            }
            return issues.ToArray();
        }

        public ResultData Run(StepStartData data)
        {
            MethodInfo fetchDocuments = typeof(FetchDocumentsStep)
                .GetMethod(nameof(FetchDocuments), BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGenericMethod(GetDocumentType());
            object docs = fetchDocuments.Invoke(this, new object[] { data });
            return new ResultData(PATH_SUCCESS, new DataPair[] { new DataPair(RESULTS, docs) });
        }

        private TDocument[] FetchDocuments<TDocument>(StepStartData data)
        {
            IMongoCollection<TDocument> collection = GetMongoCollection<TDocument>(data);
            FilterDefinition<TDocument> filter = FetchStepUtility.GetCombinedFilter<TDocument>(filters, data, combineFiltersUsingAnd) ?? Builders<TDocument>.Filter.Empty;
            TDocument[] docs = collection.Find(filter).ToEnumerable().ToArray();
            if (docs.Length == 0)
            {
                return null;
            }
            return docs;
        }
    }

    public enum MongoFilterType
    {
        [Description("Field Name")] FieldName,
        [Description("Nested Document Field Name")] NestedDocumentFieldName,
        [Description("Combine Filters (AND)")] CombineAnd,
        [Description("Combine Filters (OR)")] CombineOr
    }

    public enum MongoQueryMatchType
    {
        Equals, DoesNotEqual, GreaterThanOrEqualTo, LessThanOrEqualTo, GreaterThan, LessThan,
        Exists, DoesNotExist
    }

    public enum MongoFieldType
    {
        String, Integer, DateTime, Boolean, Decimal
    }

}
