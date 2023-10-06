using DecisionsFramework.Data.ORMapper;
using DecisionsFramework.Design.Flow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Decisions.MongoDB
{
    public class MongoDBStepFactory : BaseFlowEntityFactory
    {
        const string ROOT_CATEGORY_NODE = "Integration";
        const string PARENT_NODE = "MongoDB";
        public const string ADVANCED_NODE = "Advanced";

        public override string[] GetRootCategories(string flowId, string folderId) => new string[] { ROOT_CATEGORY_NODE };

        public override string[] GetSubCategories(string[] nodes, string flowId, string folderId)
        {
            if (nodes == null || nodes.Length == 0 || nodes[0] != ROOT_CATEGORY_NODE)
                return new string[0];

            if (nodes.Length == 1)
                return new string[] { PARENT_NODE };

            if (nodes[1] != PARENT_NODE)
                return new string[0];

            ORM<MongoDBServer> orm = new ORM<MongoDBServer>();
            if (nodes.Length == 2)
                return orm.Fetch().Select(x => x.EntityName).Concat(new string[] { ADVANCED_NODE }).ToArray();

            return new string[0];
        }

        public override FlowStepToolboxInformation[] GetStepsInformation(string[] nodes, string flowId, string folderId)
        {
            if (nodes == null || nodes.Length != 3 || nodes[0] != ROOT_CATEGORY_NODE || nodes[1] != PARENT_NODE)
                return new FlowStepToolboxInformation[0];

            List<FlowStepToolboxInformation> list = new List<FlowStepToolboxInformation>();
            if (nodes[2] == ADVANCED_NODE)
            {
                list.Add(new FlowStepToolboxInformation($"List Database Names", nodes, $"MongoDB.ListDBs"));
                list.Add(new FlowStepToolboxInformation($"Drop Database", nodes, $"MongoDB.DropDB"));
                list.Add(new FlowStepToolboxInformation($"List Collection Names", nodes, $"MongoDB.ListCollections"));
                list.Add(new FlowStepToolboxInformation($"Drop Collection", nodes, $"MongoDB.DropCollection"));
                list.Add(new FlowStepToolboxInformation($"Rename Collection", nodes, $"MongoDB.RenameCollection"));
            }
            else
            {
                ORM<MongoDBServer> orm = new ORM<MongoDBServer>();
                MongoDBServer server = orm.Fetch(new WhereCondition[] { new FieldWhereCondition("entity_name", QueryMatchType.Equals, nodes[2]) }).FirstOrDefault();
                if (server == null)
                    return new FlowStepToolboxInformation[0];

                list.Add(new FlowStepToolboxInformation($"Get Document By ID", nodes, $"MongoDB.GetDoc${server.ServerId}"));
                list.Add(new FlowStepToolboxInformation($"Fetch Documents", nodes, $"MongoDB.FetchDocs${server.ServerId}"));
                list.Add(new FlowStepToolboxInformation($"Delete Document", nodes, $"MongoDB.DeleteDoc${server.ServerId}"));
                list.Add(new FlowStepToolboxInformation($"Replace Document", nodes, $"MongoDB.ReplaceDoc${server.ServerId}"));
                list.Add(new FlowStepToolboxInformation($"Insert Document", nodes, $"MongoDB.InsertDoc${server.ServerId}"));
                list.Add(new FlowStepToolboxInformation($"Get Raw Document By ID", nodes, $"MongoDB.GetRawDoc${server.ServerId}"));
            }

            return list.ToArray();
        }

        public override IFlowEntity CreateStep(string[] nodes, string stepId, StepCreationInfo additionalInfo)
        {
            if (stepId == null)
                return null;
            if (stepId.StartsWith("MongoDB.ListDBs"))
                return new ListDBsStep();
            if (stepId.StartsWith("MongoDB.DropDB"))
                return new DropDBStep();
            if (stepId.StartsWith("MongoDB.ListCollections"))
                return new ListCollectionsStep();
            if (stepId.StartsWith("MongoDB.DropCollection"))
                return new DropCollectionStep();
            if (stepId.StartsWith("MongoDB.RenameCollection"))
                return new RenameCollectionStep();

            string[] parts = (stepId ?? string.Empty).Split('$');
            if (parts.Length < 2)
                return null;

            string id = parts[1];

            if (stepId.StartsWith("MongoDB.GetDoc"))
                return new GetDocumentStep(id);
            if (stepId.StartsWith("MongoDB.FetchDocs"))
                return new FetchDocumentsStep(id);
            if (stepId.StartsWith("MongoDB.DeleteDoc"))
                return new DeleteDocumentStep_02(id);
            if (stepId.StartsWith("MongoDB.DeleteBulkDoc"))
                return new DeleteBulkDocumentStep_02(id);
            if (stepId.StartsWith("MongoDB.ReplaceDoc"))
                return new ReplaceDocumentStep_02(id);
            if (stepId.StartsWith("MongoDB.ReplaceBulkDoc"))
                return new ReplaceBulkDocumentStep_02(id);
            if (stepId.StartsWith("MongoDB.InsertDoc"))
                return new InsertDocumentStep_02(id);
            if (stepId.StartsWith("MongoDB.InsertBulkDoc"))
                return new InsertBulkDocumentStep_02(id);
            if (stepId.StartsWith("MongoDB.GetRawDoc"))
                return new GetRawDocumentStep(id);

            return null;
        }

        public override FlowStepToolboxInformation[] GetFavoriteSteps(string flowId, string folderId)
            => new FlowStepToolboxInformation[0];

        public override FlowStepToolboxInformation[] SearchSteps(string flowId, string folderId, string searchString, int maxRecords)
            => new FlowStepToolboxInformation[0];
    }
}
