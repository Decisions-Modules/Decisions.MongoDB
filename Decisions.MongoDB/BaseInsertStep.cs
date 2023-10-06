using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Flow.Mapping;

namespace Decisions.MongoDB
{
    [Writable]
    public abstract class BaseInsertStep : BaseMongoDBStep, ISyncStep, IDataConsumer
    {
        protected BaseInsertStep() { }

        protected BaseInsertStep(string serverId)
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
    }
}