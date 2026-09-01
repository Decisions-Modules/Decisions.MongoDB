using System.Collections.Generic;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.Design.Properties.Attributes;

namespace Decisions.MongoDB
{
    [Writable]
    public class IndexOptionsDefinition
    {
        private const string OPTIONS_CATEGORY = "Options";
        private const string UNIQUE_OPTIONS_CATEGORY = "Unique Options";
        private const string SPARSE_OPTIONS_CATEGORY = "Sparse Options";
        private const string TTL_OPTIONS_CATEGORY = "TTL Options";
        
        [WritableValue] private string indexName;

        [PropertyClassification(0, "Index Name (MongoDB auto-generates if left empty)", OPTIONS_CATEGORY)]
        public string IndexName
        {
            get => indexName;
            set => indexName = value;
        }
        
        [InfoOrWarningEditor(false, "https://www.mongodb.com/docs/manual/core/index-unique/", true)]
        [PropertyClassification(0, "UniqueDetails",  OPTIONS_CATEGORY, UNIQUE_OPTIONS_CATEGORY)]
        public string UniqueDetails
        {
            get => "";
            set { }
        }
        
        [WritableValue] private bool unique;

        [PropertyClassification(1, "Create Unique Index", OPTIONS_CATEGORY, UNIQUE_OPTIONS_CATEGORY)]
        public bool Unique
        {
            get => unique;
            set => unique = value;
        }
        
        [InfoOrWarningEditor(false, "https://www.mongodb.com/docs/manual/core/index-sparse/", true)]
        [PropertyClassification(0, "SparseDetails",  OPTIONS_CATEGORY, SPARSE_OPTIONS_CATEGORY)]
        public string SparseDetails
        {
            get => "";
            set { }
        }

        [WritableValue] private bool sparse;

        [PropertyClassification(1, "Create Sparse Index", OPTIONS_CATEGORY, SPARSE_OPTIONS_CATEGORY)]
        public bool Sparse
        {
            get => sparse;
            set => sparse = value;
        }
        
        [InfoOrWarningEditor(false, "https://www.mongodb.com/docs/manual/core/index-ttl/", true)]
        [PropertyClassification(0, "TTLDetails",  OPTIONS_CATEGORY, TTL_OPTIONS_CATEGORY)]
        public string TTLDetails
        {
            get => "";
            set { }
        }

        [WritableValue] private bool enableTTL;

        [PropertyClassification(1, "Create TTL", OPTIONS_CATEGORY, TTL_OPTIONS_CATEGORY)]
        public bool EnableTTL
        {
            get => enableTTL;
            set => enableTTL = value;
        }

        [WritableValue] private int ttlSeconds;

        [PropertyClassification(2, "Seconds", OPTIONS_CATEGORY, TTL_OPTIONS_CATEGORY)]
        [PropertyHiddenByValue(nameof(EnableTTL), true, false)]
        public int TTLSeconds
        {
            get => ttlSeconds;
            set => ttlSeconds = value;
        }
        
        public override string ToString()
        {
            List<string> parts = GetDisplayParts();
            
            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }

        private List<string> GetDisplayParts()
        {
            List<string> parts = new List<string>();
            
            if(!string.IsNullOrEmpty(IndexName))
                parts.Add($"IndexName: {IndexName}");
            if(Unique)
                parts.Add($"Unique: {Unique.ToString()}");
            if(Sparse)
                parts.Add($"Sparse: {Sparse.ToString()}");
            if (EnableTTL)
            {
                parts.Add($"TTLSeconds: {TTLSeconds.ToString()}");
            }
            
            return parts;
        }
    }
}
