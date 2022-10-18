using DecisionsFramework;
using DecisionsFramework.Data.ORMapper;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.Design.Properties.Attributes;
using DecisionsFramework.ServiceLayer;
using DecisionsFramework.ServiceLayer.Actions;
using DecisionsFramework.ServiceLayer.Actions.Common;
using DecisionsFramework.ServiceLayer.Utilities;
using DecisionsFramework.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Decisions.MongoDB
{
    [Writable]
    [ORMEntity]
    public class MongoDBServer : AbstractFolderEntity, IValidationSource
    {
        const string SETTINGS = "Settings";

        [WritableValue]
        [ORMPrimaryKeyField]
        [PropertyHidden]
        public string ServerId { get; set; }

        [WritableValue]
        [ORMField]
        [PropertyClassification(3, "Get Connection String from Flow", SETTINGS)]
        public bool SpecifyConnStringInFlow { get; set; }

        [WritableValue]
        [ORMField(4000, typeof(FixedLengthStringFieldConverter))]
        [PropertyClassification(10, "Connection String", SETTINGS)]
        [BooleanPropertyHidden(nameof(SpecifyConnStringInFlow), true)]
        public string ConnectionString { get; set; }

        [WritableValue]
        [ORMField]
        [PropertyClassification(5, "Get Database Name from Flow", SETTINGS)]
        public bool SpecifyDbInFlow { get; set; }

        [WritableValue]
        [ORMField]
        [PropertyClassification(12, "Database Name", SETTINGS)]
        [BooleanPropertyHidden(nameof(SpecifyDbInFlow), true)]
        public string DatabaseName { get; set; }

        [WritableValue]
        [ORMField]
        [PropertyClassification(7, "Get Collection Name from Flow", SETTINGS)]
        public bool SpecifyCollectionInFlow { get; set; }

        [WritableValue]
        [ORMField]
        [PropertyClassification(14, "Collection Name", SETTINGS)]
        [BooleanPropertyHidden(nameof(SpecifyCollectionInFlow), true)]
        public string CollectionName { get; set; }

        [WritableValue]
        [ORMField]
        [PropertyClassification(17, "Choose Document Type on Step", SETTINGS)]
        public bool ChooseDocTypeOnStep { get; set; }

        [WritableValue]
        [ORMField]
        [PropertyClassification(18, "Document Type", SETTINGS)]
        [BooleanPropertyHidden(nameof(ChooseDocTypeOnStep), true)]
        [TypePickerEditor]
        public string DocumentType { get; set; }

        public override BaseActionType[] GetActions(AbstractUserContext userContext, EntityActionType[] types)
        {
            List<BaseActionType> actions = new List<BaseActionType>(base.GetActions(userContext, types) ?? new BaseActionType[0]);
            actions.Add(new EditObjectAction(typeof(MongoDBServer), "Edit", "", "", () => this, (usercontext, obj) => { ((MongoDBServer)obj).Store(); }));
            return actions.ToArray();
        }

        public ValidationIssue[] GetValidationIssues()
        {
            List<ValidationIssue> issues = new List<ValidationIssue>();

            if (!string.IsNullOrEmpty(EntityName))
            {
                if (EntityName == MongoDBStepFactory.ADVANCED_NODE)
                    issues.Add(new ValidationIssue(this, "'Advanced' isn't a valid name for MongoDB server integrations", "", BreakLevel.Fatal, nameof(EntityName)));
                else
                {
                    MongoDBServer[] otherServers = new ORM<MongoDBServer>().Fetch(new WhereCondition[]
                    {
                        new FieldWhereCondition("entity_name", QueryMatchType.Equals, EntityName),
                        new FieldWhereCondition("server_id", QueryMatchType.DoesNotEqual, ServerId)
                    });
                    if (otherServers.Length > 0)
                        issues.Add(new ValidationIssue(this, "Another MongoDB server integration already exists with this name", "", BreakLevel.Fatal, nameof(EntityName)));
                }
            }
            if (!SpecifyConnStringInFlow && string.IsNullOrEmpty(ConnectionString))
                issues.Add(new ValidationIssue(this, "Connection string is required", "", BreakLevel.Fatal, nameof(ConnectionString)));
            if (!SpecifyDbInFlow && string.IsNullOrEmpty(DatabaseName))
                issues.Add(new ValidationIssue(this, "Database name is required", "", BreakLevel.Fatal, nameof(DatabaseName)));
            if (!SpecifyCollectionInFlow && string.IsNullOrEmpty(CollectionName))
                issues.Add(new ValidationIssue(this, "Collection name is required", "", BreakLevel.Fatal, nameof(CollectionName)));
            if (!ChooseDocTypeOnStep)
            {
                if (string.IsNullOrEmpty(DocumentType))
                    issues.Add(new ValidationIssue(this, "Document type is required", "", BreakLevel.Fatal, nameof(DocumentType)));
                else
                {
                    Type docType = TypeUtilities.FindTypeByFullName(DocumentType);
                    if (docType == null)
                        issues.Add(new ValidationIssue(this, "Document type not found", "", BreakLevel.Fatal, nameof(DocumentType)));
                    else
                    {
                        if (docType.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance) == null
                            && docType.GetProperty("id", BindingFlags.Public | BindingFlags.Instance) == null
                            && docType.GetProperty("_id", BindingFlags.Public | BindingFlags.Instance) == null)
                        {
                            issues.Add(new ValidationIssue(this, "This type has no property named Id, id, or _id, and will not have its ObjectId loaded", "", BreakLevel.Warning, nameof(DocumentType)));
                        }
                    }
                }
            }

            return issues.ToArray();
        }
    }
}
