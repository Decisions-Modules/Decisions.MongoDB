using DecisionsFramework;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Properties;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Decisions.MongoDB
{
    public enum MongoDBSortOrder { Ascending, Descending };

    [Writable]
    [DataContract]
    public class MongoDBSort : IValidationSource
    {
        [WritableValue]
        string fieldName;

        [DataMember]
        [PropertyClassification("Field Name", 10)]
        [SelectStringEditor(nameof(AllFieldNames))]
        public string FieldName
        {
            get { return fieldName; }
            set { fieldName = value; }
        }

        [WritableValue]
        MongoDBSortOrder sortOrder;

        [DataMember]
        [PropertyClassification("Sort Order", 20)]
        public MongoDBSortOrder SortOrder
        {
            get { return sortOrder; }
            set { sortOrder = value; }
        }

        string[] allFieldNames;

        [PropertyHidden]
        public string[] AllFieldNames
        {
            get { return allFieldNames; }
            set { allFieldNames = value; }
        }

        public override string ToString()
        {
            switch (SortOrder)
            {
                case MongoDBSortOrder.Ascending:
                    return $"{FieldName} (ASC)";
                case MongoDBSortOrder.Descending:
                    return $"{FieldName} (DESC)";
                default:
                    return $"{FieldName}";
            }
        }

        public ValidationIssue[] GetValidationIssues()
        {
            List<ValidationIssue> issues = new List<ValidationIssue>();

            if (string.IsNullOrEmpty(FieldName))
                issues.Add(new ValidationIssue(this, "Field name is required", "", BreakLevel.Fatal, nameof(FieldName)));

            return issues.ToArray();
        }
    }

}
