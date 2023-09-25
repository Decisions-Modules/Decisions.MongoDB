using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Flow.Mapping;

namespace Decisions.MongoDB
{
    public class ReplaceBulkDocumentStep : BaseReplaceStep
    {
        public ReplaceBulkDocumentStep() : base() { }
        
        public ReplaceBulkDocumentStep(string serverId) : base(serverId) { }

        public override string StepName => "Replace Documents";
        
        public override DataDescription[] InputData => GetInputData(true);

        protected override string DocumentIdInputName => "Document IDs"; 
        
        public override ResultData Run(StepStartData data)
        {
            throw new System.NotImplementedException();
        }   
    }
}