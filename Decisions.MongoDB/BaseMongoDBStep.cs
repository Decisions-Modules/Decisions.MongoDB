using DecisionsFramework;
using DecisionsFramework.Data.ORMapper;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Flow.CoreSteps;
using DecisionsFramework.Design.Flow.Interface;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.Design.Flow.Service;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.Design.Properties.Attributes;
using DecisionsFramework.ServiceLayer.Utilities;
using DecisionsFramework.Utilities;
using MongoDB.Bson;
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
    public enum IdType
    {
        [Description("String or ObjectID")] StringOrObjectId,
        [Description("Integer (32 bits)")] Int32,
        [Description("Integer (64 bits)")] Int64,
        [Description("Float (Single)")] Float,
        [Description("Double")] Double
    };

    public abstract class BaseMongoDBStep : BaseFlowAwareStep, IAddedToFlow, IExportable, IValidationSource
    {
        protected const string SETTINGS_CATEGORY = "Settings";
        protected const string CONN_STRING_INPUT = "Connection String";
        protected const string DB_NAME_INPUT = "Database Name";
        protected const string COLLECTION_NAME_INPUT = "Collection Name";
        protected const string PATH_SUCCESS = "Success";
        protected const string PATH_ERROR = "Error";

        [WritableValue]
        private string serverId;

        [PropertyHidden]
        public string ServerId
        {
            get { return serverId; }
            set { serverId = value; }
        }

        private MongoDBServer server;

        [PropertyHidden]
        public MongoDBServer Server
        {
            get
            {
                if (server == null)
                {
                    server = new ORM<MongoDBServer>().Fetch(serverId);
                }
                return server;
            }
        }

        [PropertyHidden]
        public virtual bool ShowTypePicker => Server?.ChooseDocTypeOnStep == true;

        [WritableValue]
        private string documentType;

        [PropertyClassification(0, "Document Type", SETTINGS_CATEGORY)]
        [BooleanPropertyHidden(nameof(ShowTypePicker), false)]
        [TypePickerEditor]
        public string DocumentType
        {
            get { return documentType; }
            set
            {
                documentType = value;
                OnTypeChanged();
            }
        }

        [PropertyHidden]
        public virtual bool ShowIdTypeOverride => true;

        [WritableValue]
        private bool overrideIdType;

        [PropertyClassification(10, "Override Document ID Type", SETTINGS_CATEGORY)]
        [BooleanPropertyHidden(nameof(ShowIdTypeOverride), false)]
        public bool OverrideIdType
        {
            get { return overrideIdType; }
            set
            {
                overrideIdType = value;
                OnPropertyChanged();
            }
        }

        [WritableValue]
        private IdType idType;

        [PropertyClassification(11, "Document ID Type", SETTINGS_CATEGORY)]
        [BooleanPropertyHidden(nameof(OverrideIdType), false)]
        public IdType IdType
        {
            get { return idType; }
            set
            {
                idType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IDataConsumer.InputData));
            }
        }

        protected virtual void OnTypeChanged()
        {
            OnPropertyChanged(nameof(IDataConsumer.InputData));
            OnPropertyChanged(nameof(ISyncStep.OutcomeScenarios));
        }

        public abstract string StepName { get; }

        public void AddedToFlow()
        {
            if (this.Flow != null && this.FlowStep != null)
                FlowEditService.SetDefaultStepName(this.Flow, this.FlowStep, StringUtils.SplitCamelCaseString(StepName) + " {0}");
        }
        public void RemovedFromFlow() { }

        public IORMEntity[] GetExportEntities(IORMEntity[] entities)
        {
            if (!string.IsNullOrEmpty(serverId))
            {
                MongoDBServer server = new ORM<MongoDBServer>().Fetch(serverId);
                if (server != null)
                    return new IORMEntity[] { server };
            }
            return new IORMEntity[0];
        }

        public ValidationIssue[] GetValidationIssues()
        {
            List<ValidationIssue> issues = new List<ValidationIssue>();

            if (string.IsNullOrEmpty(serverId))
                issues.Add(new ValidationIssue(this, "No MongoDBServer ID found", "", BreakLevel.Fatal));
            else if (Server == null)
                issues.Add(new ValidationIssue(this, "No MongoDBServer found", "", BreakLevel.Fatal));

            if (ShowTypePicker)
            {
                if (string.IsNullOrEmpty(DocumentType))
                    issues.Add(new ValidationIssue(this, "No document type chosen", "", BreakLevel.Fatal));
                else
                {
                    Type docType = TypeUtilities.FindTypeByFullName(DocumentType);
                    if (docType == null)
                        issues.Add(new ValidationIssue(this, "Document type not found", "", BreakLevel.Fatal));
                    else
                    {
                        if (docType.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance) == null
                            && docType.GetProperty("id", BindingFlags.Public | BindingFlags.Instance) == null
                            && docType.GetProperty("_id", BindingFlags.Public | BindingFlags.Instance) == null)
                        {
                            issues.Add(new ValidationIssue(this, "This type has no property named Id, id, or _id, and will not have its ID property loaded", "", BreakLevel.Warning));
                        }
                    }
                }
            }

            ValidationIssue[] extras = GetAdditionalValidationIssues();
            if (extras != null)
                issues.AddRange(extras);

            return issues.ToArray();
        }

        public virtual ValidationIssue[] GetAdditionalValidationIssues() => null;

        /// <summary>
        /// Add inputs that aren't already defined on the MongoDBServer entity.
        /// </summary>
        protected void AddInputsFromServerConfig(List<DataDescription> inputs)
        {
            if (Server?.SpecifyConnStringInFlow == true)
                inputs.Add(new DataDescription(typeof(string), CONN_STRING_INPUT));

            if (Server?.SpecifyDbInFlow == true)
                inputs.Add(new DataDescription(typeof(string), DB_NAME_INPUT));

            if (Server?.SpecifyCollectionInFlow == true)
                inputs.Add(new DataDescription(typeof(string), COLLECTION_NAME_INPUT));
        }

        /// <summary>
        /// Get the connection string from input data or from the MongoDBServer entity, depending on configuration.
        /// </summary>
        protected string GetConnString(StepStartData data)
        {
            if (Server?.SpecifyConnStringInFlow == true)
                return data[CONN_STRING_INPUT] as string;
            else
                return Server?.ConnectionString;
        }

        /// <summary>
        /// Get the database name from input data or from the MongoDBServer entity, depending on configuration.
        /// </summary>
        protected string GetDbName(StepStartData data)
        {
            if (Server?.SpecifyDbInFlow == true)
                return data[DB_NAME_INPUT] as string;
            else
                return Server?.DatabaseName;
        }

        /// <summary>
        /// Get the collection name from input data or from the MongoDBServer entity, depending on configuration.
        /// </summary>
        protected string GetCollectionName(StepStartData data)
        {
            if (Server?.SpecifyCollectionInFlow == true)
                return data[COLLECTION_NAME_INPUT] as string;
            else
                return Server?.CollectionName;
        }

        protected MongoClient GetMongoClient(StepStartData data)
        {
            return new MongoClient(GetConnString(data));
        }

        protected IMongoDatabase GetMongoDatabase(StepStartData data)
        {
            return new MongoClient(GetConnString(data)).GetDatabase(GetDbName(data));
        }

        protected IMongoCollection<BsonDocument> GetMongoRawDocumentCollection(StepStartData data)
        {
            return new MongoClient(GetConnString(data)).GetDatabase(GetDbName(data)).GetCollection<BsonDocument>(GetCollectionName(data));
        }

        protected IMongoCollection<T> GetMongoCollection<T>(StepStartData data)
        {
            return new MongoClient(GetConnString(data)).GetDatabase(GetDbName(data)).GetCollection<T>(GetCollectionName(data));
        }

        protected string GetDocumentTypeName()
        {
            if (Server?.ChooseDocTypeOnStep == true)
                return this.DocumentType;
            else
                return Server?.DocumentType;
        }

        protected Type GetDocumentType()
        {
            return TypeUtilities.FindTypeByFullName(GetDocumentTypeName(), false) ?? typeof(string);
        }

        protected IdType GetIdPropertyTypeEnum()
        {
            if (!ShowIdTypeOverride)
                return IdType.StringOrObjectId;

            if (OverrideIdType)
                return this.IdType;

            Type docType = GetDocumentType();
            if (docType == typeof(string)) // if docType is string then the real doc type wasn't found
                return IdType.StringOrObjectId;

            PropertyInfo idProperty = docType.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance)
                ?? docType.GetProperty("id", BindingFlags.Public | BindingFlags.Instance)
                ?? docType.GetProperty("_id", BindingFlags.Public | BindingFlags.Instance);
            if (idProperty == null)
                return IdType.StringOrObjectId;

            if (idProperty.PropertyType == typeof(int))
                return IdType.Int32;
            else if (idProperty.PropertyType == typeof(long))
                return IdType.Int64;
            else if (idProperty.PropertyType == typeof(float))
                return IdType.Float;
            else if (idProperty.PropertyType == typeof(double))
                return IdType.Double;
            else
                return IdType.StringOrObjectId;
        }

        protected Type GetIdPropertyType() => GetIdPropertyType(GetIdPropertyTypeEnum());

        internal static Type GetIdPropertyType(IdType idType)
        {
            switch (idType)
            {
                case IdType.StringOrObjectId: return typeof(string);
                case IdType.Int32: return typeof(int);
                case IdType.Int64: return typeof(long);
                case IdType.Float: return typeof(float);
                case IdType.Double: return typeof(double);
                default: return null;
            }
        }

    }
}
