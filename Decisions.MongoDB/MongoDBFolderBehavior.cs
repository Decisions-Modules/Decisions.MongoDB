using System;
using System.Collections.Generic;
using System.Linq;
using DecisionsFramework;
using DecisionsFramework.Design.Flow.Service;
using DecisionsFramework.ServiceLayer;
using DecisionsFramework.ServiceLayer.Actions;
using DecisionsFramework.ServiceLayer.Services.Folder;
using DecisionsFramework.ServiceLayer.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using DecisionsFramework.ServiceLayer.Services.ConfigurationStorage;
using DecisionsFramework.ServiceLayer.Services.ConfigurationStorage.CreateRegistration;
using DecisionsFramework.Utilities.CodeGeneration.FlowSteps;

namespace Decisions.MongoDB
{
    [Obsolete("Previously this folder was necessary only to provide action to Create MongoDB Database Server integration, but now we can create it from gallery")]
    public class MongoDBFolderBehavior : SystemFolderBehavior
    {
        public override bool CanDeleteFolder(string folderId) => true;
        
        public override BaseActionType[] GetFolderActions(Folder folder, BaseActionType[] proposedActions, EntityActionType[] types)
        {
            List<BaseActionType> actions = new List<BaseActionType>(base.GetFolderActions(folder, proposedActions, types) ?? Array.Empty<BaseActionType>());

            //'Delete Folder' action
            BaseActionType deleteFolderAction = proposedActions.FirstOrDefault(pa => pa.Name == Folder.DELETE_FOLDER_ACTION_NAME);
            if (deleteFolderAction != null)
            {
                actions.Add(deleteFolderAction);
            }

            return actions.ToArray();
        }
    }

    public class MongoDBInitializer : IInitializable
    {
        public void Initialize()
        {
            EntityActionFactoriesHolder.GetInstance().RegisterObjectForType(typeof(Folder), new MongoDBActionsFactory());
            NewRegistrationFactories.RegisterCategory(
                new FilteredActionCategory("DataTypes/Integration",
                    action => action.Name.Contains("MongoDB", StringComparison.InvariantCultureIgnoreCase) ? GalleryConstants.DATABASE_INTEGRATION_CATEGORY : null)
            );
            FlowEditService.RegisterModuleBasedFlowStepFactory(new MongoDBStepFactory());
            FlowEditService.RegisterModuleBasedProjectStepFactory(new MongoDBStepFactory() { ProjectScope = ToolboxProjectScope.ProjectOnly});

            // Register new conventions to be used for all MongoDB collections:
            ConventionRegistry.Register("CustomConventions", new ConventionPack
            {
                new IgnoreExtraElementsConvention(true),
                new ObjectIdToStringConvention()
            }, type => true);
        }
    }

    // This lets string properties be used as object IDs:
    public class ObjectIdToStringConvention : ConventionBase, IPostProcessingConvention
    {
        public void PostProcess(BsonClassMap classMap)
        {
            BsonMemberMap idMap = classMap.IdMemberMap;
            if (idMap == null || idMap.IdGenerator != null)
                return;
            if (idMap.MemberType == typeof(string))
                idMap.SetIdGenerator(StringObjectIdGenerator.Instance).SetSerializer(new ObjectIdOrStringSerializer());
        }
    }

    public class ObjectIdOrStringSerializer : SerializerBase<string>
    {
        private StringSerializer objectIdSerializer;
        private StringSerializer stringSerializer;
        private static readonly Log log = new Log("MongoDB ObjectIdOrStringSerializer");

        public ObjectIdOrStringSerializer()
        {
            objectIdSerializer = new StringSerializer(BsonType.ObjectId);
            stringSerializer = new StringSerializer(BsonType.String);
        }

        
        public override string Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            /*
             * DT-029197 - Added try catch blocks that allow for the decoding of BSON objectID type to a Decisions string
             */
            try
            {
                return stringSerializer.Deserialize(context, args);
            }
            catch(Exception ex)
            {
                log.Debug(ex, "Failed to deserialize document field using the stringSerializer.");
            }

            try
            {
                return objectIdSerializer.Deserialize(context, args);
            }
            catch(Exception ex)
            {
                /* We throw this exception because now we know that all attempts to deserialize failed */
                throw new BsonSerializationException("Failed to deserialize received mongodb document.", ex);
            }
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, string value)
        {
            ObjectId objectId;
            if (ObjectId.TryParse(value, out objectId))
            {
                objectIdSerializer.Serialize(context, value);
            }
            else
            {
                stringSerializer.Serialize(context, value);
            }
        }
    }

}
