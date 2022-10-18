using DecisionsFramework;
using DecisionsFramework.Data.ORMapper;
using DecisionsFramework.Design.Flow.Service;
using DecisionsFramework.ServiceLayer;
using DecisionsFramework.ServiceLayer.Actions;
using DecisionsFramework.ServiceLayer.Actions.Common;
using DecisionsFramework.ServiceLayer.Services.Folder;
using DecisionsFramework.ServiceLayer.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Decisions.MongoDB
{
    public class MongoDBFolderBehavior : SystemFolderBehavior
    {
        public override BaseActionType[] GetFolderActions(Folder folder, BaseActionType[] proposedActions, EntityActionType[] types)
        {
            List<BaseActionType> list = new List<BaseActionType>(base.GetFolderActions(folder, proposedActions, types) ?? new BaseActionType[0]);
            list.Add(new EditObjectAction(typeof(MongoDBServer), "Add MongoDB Server", "", "", null,
                                                  new MongoDBServer() { EntityFolderID = folder.FolderID },
                                                  new SetValueDelegate(AddServer))
            {
                ActionAddsType = typeof(MongoDBServer),
                RefreshScope = ActionRefreshScope.OwningFolder
            });
            return list.ToArray();
        }

        private void AddServer(AbstractUserContext usercontext, object obj)
        {
            new DynamicORM().Store(obj as MongoDBServer);
        }

    }

    public class MongoDBInitializer : IInitializable
    {
        const string MONGO_DB_FOLDER_ID = "MONGODB_SERVER_FOLDER";
        private static readonly Log log = new Log("MongoDB - MongoDBInitializer");
        public void Initialize()
        {
            ORM<Folder> orm = new ORM<Folder>();
            Folder folder = orm.Fetch(MONGO_DB_FOLDER_ID);
            if (folder == null)
            {
                log.Debug($"Creating System Folder '{MONGO_DB_FOLDER_ID}'");
                folder = new Folder(MONGO_DB_FOLDER_ID, "MongoDB", Constants.INTEGRATIONS_FOLDER_ID);
                folder.FolderBehaviorType = typeof(MongoDBFolderBehavior).FullName;

                orm.Store(folder);
            }

            FlowEditService.RegisterModuleBasedFlowStepFactory(new MongoDBStepFactory());

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
