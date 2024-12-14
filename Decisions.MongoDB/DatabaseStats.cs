using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Properties;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Decisions.MongoDB
{
    [Writable]
    public class DatabaseStats
    {
        [WritableValue]
        string databaseName;

        [PropertyClassification("Database Name", 10)]
        [BsonElement("db")]
        public string DatabaseName
        {
            get { return databaseName; }
            set { databaseName = value; }
        }

        [WritableValue]
        long numCollections;

        [PropertyClassification("Number of Collections", 11)]
        [BsonElement("collections")]
        public long NumCollections
        {
            get { return numCollections; }
            set { numCollections = value; }
        }

        [WritableValue]
        long numViews;

        [PropertyClassification("Number of Views", 12)]
        [BsonElement("views")]
        public long NumViews
        {
            get { return numViews; }
            set { numViews = value; }
        }

        [WritableValue]
        long numDocuments;

        [PropertyClassification("Number of Documents", 13)]
        [BsonElement("objects")]
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
        [BsonElement("dataSize")]
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
        [BsonElement("indexes")]
        public long NumIndexes
        {
            get { return numIndexes; }
            set { numIndexes = value; }
        }

        [WritableValue]
        long indexSize;

        [PropertyClassification("Index Size", 18)]
        [BsonElement("indexSize")]
        public long IndexSize
        {
            get { return indexSize; }
            set { indexSize = value; }
        }

        [WritableValue]
        long fsUsedSize;

        [PropertyClassification("Filesystem Used Size", 19)]
        [BsonElement("fsUsedSize")]
        public long FilesystemUsedSize
        {
            get { return fsUsedSize; }
            set { fsUsedSize = value; }
        }

        [WritableValue]
        long fsTotalSize;

        [PropertyClassification("Filesystem Total Size", 20)]
        [BsonElement("fsTotalSize")]
        public long FilesystemTotalSize
        {
            get { return fsTotalSize; }
            set { fsTotalSize = value; }
        }
    }
}
