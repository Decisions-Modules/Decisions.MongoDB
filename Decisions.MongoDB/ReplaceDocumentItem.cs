using System.Runtime.Serialization;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;

namespace Decisions.MongoDB
{
    [DataContract, Writable]
    public class ReplaceDocumentItem
    { 
        [WritableValue, DataMember] 
        public string Id { get; set; }
        
        [WritableValue, DataMember] 
        public object Document { get; set; }
    }
}