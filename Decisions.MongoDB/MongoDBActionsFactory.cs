using System;
using DecisionsFramework.Data.ORMapper;
using DecisionsFramework.Design;
using DecisionsFramework.ServiceLayer;
using DecisionsFramework.ServiceLayer.Actions;
using DecisionsFramework.ServiceLayer.Actions.Common;
using DecisionsFramework.ServiceLayer.Services.Folder;
using DecisionsFramework.ServiceLayer.Services.Projects;
using DecisionsFramework.ServiceLayer.Utilities;

namespace Decisions.MongoDB
{
    public class MongoDBActionsFactory : IEntityActionFactory
    {
        public BaseActionType[] GetActions(AbstractUserContext userContext, EntityActionType[] actionTypes, IPlatformEntity target, string entityId)
        {
            if (target is not Folder folder || (folder.GetFolderBehavior() is not DesignerProjectFolder and not MongoDBFolderBehavior))
                return Array.Empty<BaseActionType>();

            var project = ProjectUtility.GetProjectOfEntity(entityId, false, false);

            // Show actions only if project depends on the module
            if (project != null && !ProjectUtility.IsDependentModule(project.FolderID, "Decisions.MongoDB"))
                return Array.Empty<BaseActionType>();

            return new BaseActionType[]
            {
                new EditObjectAction(typeof(MongoDBServer), "Add MongoDB Server", "", "", null,
                    new MongoDBServer() { EntityFolderID = folder.FolderID },
                    new SetValueDelegate(AddServer))
                {
                    Filtered = true,
                    DisplayType = ActionDisplayType.Primary,
                    ActionAddsType = typeof(MongoDBServer),
                    RefreshScope = ActionRefreshScope.OwningFolder,
                    Description = "The MongoDB Module allows a user to configure steps to read and write from a MongoDB database, automatically deserializing to and serializing from a chosen data type.",
                    CreateGalleryImage = "https://github.com/Decisions-Modules/Decisions.MongoDB/blob/9.x/image.png?raw=true"
                },
            };
        }

        private void AddServer(AbstractUserContext usercontext, object obj)
        {
            new DynamicORM().Store(obj as MongoDBServer);
        }
    }
}