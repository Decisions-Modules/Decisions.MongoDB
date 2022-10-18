using DecisionsFramework;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.Design.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Decisions.MongoDB
{
    [Writable]
    [DataContract]
    public class MongoDBFilter : IValidationSource
    {
        [WritableValue]
        MongoFilterType filterType;

        [DataMember]
        [PropertyClassification("Filter Type", 0)]
        public MongoFilterType FilterType
        {
            get { return filterType; }
            set { filterType = value; }
        }

        [WritableValue]
        MongoDBFilter[] subFilters;

        [DataMember]
        [PropertyClassification("Filters", 1)]
        [PropertyHiddenByValue(nameof(FilterType), MongoFilterType.FieldName, true)]
        [PropertyHiddenByValue(nameof(FilterType), MongoFilterType.NestedDocumentFieldName, true)]
        public MongoDBFilter[] SubFilters
        {
            get
            {
                UpdateFiltersInArray(subFilters);
                return subFilters;
            }
            set
            {
                subFilters = value;
                UpdateFiltersInArray(subFilters);
            }
        }

        [WritableValue]
        string fieldName;

        [DataMember]
        [PropertyClassification("Field Name", 1)]
        [SelectStringEditor(nameof(AllFieldNames))]
        [PropertyHiddenByValue(nameof(FilterType), MongoFilterType.FieldName, false)]
        public string FieldName
        {
            get { return fieldName; }
            set { fieldName = value; }
        }

        [WritableValue]
        string fieldPath;

        [DataMember]
        [PropertyClassification("Path to Nested Document Field (e.g. address.streetname)", 1)]
        [PropertyHiddenByValue(nameof(FilterType), MongoFilterType.NestedDocumentFieldName, false)]
        public string FieldPath
        {
            get { return fieldPath; }
            set { fieldPath = value; }
        }

        [WritableValue]
        string inputAlias;

        [DataMember]
        [PropertyClassification("Input Data Alias (Optional)", 2)]
        [PropertyHiddenByValue(nameof(FilterType), MongoFilterType.CombineAnd, true)]
        [PropertyHiddenByValue(nameof(FilterType), MongoFilterType.CombineOr, true)]
        [PropertyHiddenByValue(nameof(MatchType), MongoQueryMatchType.Exists, true)]
        [PropertyHiddenByValue(nameof(MatchType), MongoQueryMatchType.DoesNotExist, true)]
        public string InputAlias
        {
            get { return inputAlias; }
            set { inputAlias = value; }
        }

        [WritableValue]
        MongoFieldType fieldType;

        [DataMember]
        [PropertyClassification("Data Type of Field", 3)]
        [PropertyHiddenByValue(nameof(FilterType), MongoFilterType.NestedDocumentFieldName, false)]
        public MongoFieldType FieldType
        {
            get { return fieldType; }
            set { fieldType = value; }
        }

        [WritableValue]
        MongoQueryMatchType matchType;

        [DataMember]
        [PropertyClassification("Query Match Type", 4)]
        [PropertyHiddenByValue(nameof(FilterType), MongoFilterType.CombineAnd, true)]
        [PropertyHiddenByValue(nameof(FilterType), MongoFilterType.CombineOr, true)]
        public MongoQueryMatchType MatchType
        {
            get { return matchType; }
            set { matchType = value; }
        }

        string[] allFieldNames;

        [PropertyHidden]
        public string[] AllFieldNames
        {
            get { return allFieldNames; }
            set { allFieldNames = value; }
        }

        private void UpdateFiltersInArray(MongoDBFilter[] filters)
        {
            if (filters == null) return;

            foreach (MongoDBFilter subFilter in filters)
            {
                subFilter.AllFieldNames = AllFieldNames;
            }
        }

        public override string ToString() => GetToString(false);

        private string GetToString(bool nested)
        {
            switch (FilterType)
            {
                case MongoFilterType.FieldName:
                case MongoFilterType.NestedDocumentFieldName:
                    return $"{GetInputName()} {MatchType}"; // "field1 Equals"
                case MongoFilterType.CombineAnd:
                case MongoFilterType.CombineOr:
                    if (subFilters == null || subFilters.Length == 0)
                        return "(No filters)";
                    if (subFilters.Length == 1)
                        return subFilters[0].ToString();
                    string separator = FilterType == MongoFilterType.CombineAnd ? " AND " : " OR ";
                    string result = string.Join(separator, subFilters.Select(subFilter => subFilter.GetToString(true)));
                    if (nested)
                        return "(" + result + ")"; // "Field2 Exists AND (Desc Equals OR Name Equals)"
                    else
                        return result;
                default:
                    throw new Exception("Invalid MongoFilterType");
            }

        }

        public string GetInputName()
        {
            if (!string.IsNullOrEmpty(InputAlias))
                return InputAlias;
            if (FilterType == MongoFilterType.NestedDocumentFieldName)
                return FieldPath;
            else
                return FieldName;
        }

        internal DataDescription[] GetDataDescriptions(Type documentType)
        {
            List<DataDescription> dds = new List<DataDescription>();

            if (FilterType == MongoFilterType.FieldName)
            {
                if (!string.IsNullOrEmpty(FieldName) && MatchType != MongoQueryMatchType.Exists && MatchType != MongoQueryMatchType.DoesNotExist)
                {
                    PropertyInfo prop = documentType.GetProperty(FieldName, BindingFlags.Public | BindingFlags.Instance);
                    if (prop != null)
                    {
                        dds.Add(new DataDescription(new DecisionsNativeType(prop.PropertyType), GetInputName(), false, true, false));
                    }
                }
            }
            else if (FilterType == MongoFilterType.NestedDocumentFieldName)
            {
                if (!string.IsNullOrEmpty(FieldPath) && MatchType != MongoQueryMatchType.Exists && MatchType != MongoQueryMatchType.DoesNotExist)
                {
                    dds.Add(new DataDescription(new DecisionsNativeType(FetchStepUtility.GetTypeFromMongoFieldType(FieldType)), GetInputName(), false, true, false));
                }
            }
            else if (subFilters != null) // AND/OR
            {
                foreach (MongoDBFilter subFilter in subFilters)
                {
                    dds.AddRange(subFilter.GetDataDescriptions(documentType));
                }
            }

            return dds.ToArray();
        }

        public ValidationIssue[] GetValidationIssues()
        {
            List<ValidationIssue> issues = new List<ValidationIssue>();

            if (FilterType == MongoFilterType.FieldName && string.IsNullOrEmpty(FieldName))
                issues.Add(new ValidationIssue(this, "Field name is required", "", BreakLevel.Fatal, nameof(FieldName)));

            if (FilterType == MongoFilterType.NestedDocumentFieldName && string.IsNullOrEmpty(FieldPath))
                issues.Add(new ValidationIssue(this, "Field path is required", "", BreakLevel.Fatal, nameof(FieldPath)));

            if (FilterType == MongoFilterType.CombineAnd || FilterType == MongoFilterType.CombineOr)
            {
                if (SubFilters == null || SubFilters.Length == 0)
                    issues.Add(new ValidationIssue(this, "At least one filter is required", "", BreakLevel.Fatal, nameof(SubFilters)));
                else if (SubFilters.Length == 1)
                    issues.Add(new ValidationIssue(this, "Combine Filters is selected, but only one filter is defined", "", BreakLevel.Warning, nameof(SubFilters)));
            }

            return issues.ToArray();
        }
    }

}
