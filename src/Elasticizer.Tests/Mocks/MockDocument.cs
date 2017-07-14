using System;
using Elasticizer.Domain;

namespace Elasticizer.Tests.Mocks {
    public class MockDocument: BaseDocument {
        public string Name { get; set; }
        public string Value { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public bool IsActive { get; set; }
    }
}
