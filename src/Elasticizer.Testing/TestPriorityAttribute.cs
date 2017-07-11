using System;

namespace Elasticizer.Testing {
    public class TestPriorityAttribute : Attribute {
        public TestPriorityAttribute(int priority) => Priority = priority;

        public int Priority { get; }
    }
}
