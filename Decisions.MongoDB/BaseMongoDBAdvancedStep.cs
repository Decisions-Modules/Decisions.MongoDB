using DecisionsFramework;
using DecisionsFramework.Design.Flow.CoreSteps;
using DecisionsFramework.Design.Flow.Interface;
using DecisionsFramework.Design.Flow.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Decisions.MongoDB
{
    /// <summary>
    /// MongoDB steps under the 'Advanced' category are not associated with a MongoDBServer entity.
    /// </summary>
    public abstract class BaseMongoDBAdvancedStep : BaseFlowAwareStep, IAddedToFlow
    {
        protected Log log = new Log(nameof(BaseMongoDBAdvancedStep));
        protected const string CONN_STRING_INPUT = "Connection String";
        protected const string DB_NAME_INPUT = "Database Name";
        protected const string COLLECTION_NAME_INPUT = "Collection Name";
        protected const string PATH_SUCCESS = "Success";
        protected const string PATH_ERROR = "Error";
        protected const string SETTINGS_CATEGORY = "Settings";

        public abstract string StepName { get; }

        public void AddedToFlow()
        {
            if (this.Flow != null && this.FlowStep != null)
                FlowEditService.SetDefaultStepName(this.Flow, this.FlowStep, StringUtils.SplitCamelCaseString(StepName) + " {0}");
        }
        public void RemovedFromFlow() { }

    }
}
