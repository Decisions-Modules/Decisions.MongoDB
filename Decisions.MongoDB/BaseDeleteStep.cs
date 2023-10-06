using System.Collections.Generic;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Flow.Mapping;

namespace Decisions.MongoDB
{
    [Writable]
    public abstract class BaseDeleteStep : BaseMongoDBStep, ISyncStep, IDataConsumer
    { 

        protected BaseDeleteStep() { }

        protected BaseDeleteStep(string serverId)
        {
            ServerId = serverId;
        }

        public override bool ShowTypePicker => false;

        protected DataDescription[] GetInputData(bool isList)
        {
            List<DataDescription> inputs = new List<DataDescription>();
            
            AddInputsFromServerConfig(inputs);
            inputs.Add(new DataDescription(GetIdPropertyType(), DocumentIdInputName, isList));

            return inputs.ToArray();
        } 
        
        public abstract ResultData Run(StepStartData data);
        
        public abstract DataDescription[] InputData { get; }

        protected abstract string DocumentIdInputName { get; }
    }
}