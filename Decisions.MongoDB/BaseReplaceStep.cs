using System.Collections.Generic;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.Design.Flow.Mapping.InputImpl;
using DecisionsFramework.Design.Properties;

namespace Decisions.MongoDB
{
    [Writable]
    public abstract class BaseReplaceStep : BaseMongoDBStep, ISyncStep, IDataConsumer, IDefaultInputMappingStep
    {
        protected const string DocumentInput = "Document";
        
        protected BaseReplaceStep() { }

        protected BaseReplaceStep(string serverId)
        {
            ServerId = serverId;
        }

        protected DataDescription[] GetInputData(bool isList)
        {
            List<DataDescription> inputs = new List<DataDescription>();

            AddInputsFromServerConfig(inputs);

            inputs.Add(new DataDescription(GetDocumentType(), DocumentInput));
            inputs.Add(new DataDescription(GetIdPropertyType(), DocumentIdInputName, isList));

            return inputs.ToArray();
        }

        [WritableValue]
        private bool upsert = true;

        [PropertyClassification(0, "Insert Document If ID Not Found", SETTINGS_CATEGORY)]
        public bool Upsert
        {
            get => upsert;
            set { upsert = value; OnPropertyChanged(); }
        }

        public override OutcomeScenarioData[] OutcomeScenarios => new[]
        {
            new OutcomeScenarioData(PATH_SUCCESS)
        };

        public abstract ResultData Run(StepStartData data);
        
        public abstract DataDescription[] InputData { get; }

        public IInputMapping[] DefaultInputs => new IInputMapping []
                { new IgnoreInputMapping { InputDataName = DocumentIdInputName } };

        protected abstract string DocumentIdInputName { get; }
    }
}