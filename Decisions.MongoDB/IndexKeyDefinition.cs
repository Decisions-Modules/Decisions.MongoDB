using System.Collections.Generic;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Properties;

namespace Decisions.MongoDB
{
    public enum IndexSortDirection
    {
        Ascending,
        Descending
    }

    [Writable]
    public class IndexKeyDefinition
    {
        [WritableValue]
        private string fieldName;

        [PropertyClassification("Field Name", 0)]
        public string FieldName
        {
            get => fieldName;
            set => fieldName = value;
        }

        [WritableValue]
        private IndexSortDirection direction;

        [PropertyClassification("Direction", 1)]
        public IndexSortDirection Direction
        {
            get => direction;
            set => direction = value;
        }
        
        public override string ToString()
        {
            List<string> parts = GetDisplayParts();
            
            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }

        private List<string> GetDisplayParts()
        {
            List<string> parts = new List<string>();
            
            if(!string.IsNullOrEmpty(FieldName))
                parts.Add($"FieldName: {FieldName}");
            if(!string.IsNullOrEmpty(Direction.ToString()))
                parts.Add($"Direction: {Direction.ToString()}");
            
            return parts;
        }
    }
}