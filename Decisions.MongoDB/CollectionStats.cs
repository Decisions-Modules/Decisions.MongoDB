using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Form;
using DecisionsFramework.Design.Properties;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Decisions.MongoDB
{
    [Writable]
    public class CollectionStats
    {
        [WritableValue]
        string collectionNamespace;

        [PropertyClassification("Collection Namespace", 10)]
        [BsonElement("ns")]
        public string CollectionNamespace
        {
            get { return collectionNamespace; }
            set { collectionNamespace = value; }
        }

        [WritableValue]
        long numDocuments;

        [PropertyClassification("Number of Documents", 13)]
        [BsonElement("count")]
        public long NumDocuments
        {
            get { return numDocuments; }
            set { numDocuments = value; }
        }

        [WritableValue]
        decimal averageDocumentSize;

        [PropertyClassification("Average Document Size", 14)]
        [BsonElement("avgObjSize")]
        public decimal AverageDocumentSize
        {
            get { return averageDocumentSize; }
            set { averageDocumentSize = value; }
        }

        [WritableValue]
        long dataSize;

        [PropertyClassification("Data Size", 15)]
        [BsonElement("size")]
        public long DataSize
        {
            get { return dataSize; }
            set { dataSize = value; }
        }

        [WritableValue]
        long storageSize;

        [PropertyClassification("Storage Size", 16)]
        [BsonElement("storageSize")]
        public long StorageSize
        {
            get { return storageSize; }
            set { storageSize = value; }
        }

        [WritableValue]
        long numIndexes;

        [PropertyClassification("Number of Indexes", 17)]
        [BsonElement("nindexes")]
        public long NumIndexes
        {
            get { return numIndexes; }
            set { numIndexes = value; }
        }

        [WritableValue]
        long indexSize;

        [PropertyClassification("Index Size", 18)]
        [BsonElement("totalIndexSize")]
        public long IndexSize
        {
            get { return indexSize; }
            set { indexSize = value; }
        }

        [WritableValue]
        decimal averageIndexSize; // Not deserialized, so set this value manually

        [PropertyClassification("Average Index Size", 19)]
        public decimal AverageIndexSize
        {
            get { return averageIndexSize; }
            set { averageIndexSize = value; }
        }
    }
}
