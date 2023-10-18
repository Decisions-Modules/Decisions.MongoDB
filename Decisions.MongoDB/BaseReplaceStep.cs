using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.Design.Properties;

namespace Decisions.MongoDB
{
    [Writable]
    public abstract class BaseReplaceStep : BaseMongoDBStep, ISyncStep, IDataConsumer
    {
        protected BaseReplaceStep() { }

        protected BaseReplaceStep(string serverId)
        {
            ServerId = serverId;
        }

        public override bool ShowIdTypeOverride => false;

        public override OutcomeScenarioData[] OutcomeScenarios => new[]
        {
            new OutcomeScenarioData(PATH_SUCCESS)
        }; 
        
        public abstract ResultData Run(StepStartData data);
        
        public abstract DataDescription[] InputData { get; }

        public IInputMapping[] DefaultInputs { get; }

        [WritableValue]
        protected bool upsert = true;
        
        [PropertyClassification(0, "Insert Document If ID Not Found", SETTINGS_CATEGORY)]
        public bool Upsert
        {
            get { return upsert; }
            set { upsert = value; OnPropertyChanged(); }
        }
    }
}