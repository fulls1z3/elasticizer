namespace Elasticizer.Domain {
    public abstract class BaseDocument : IDocument {
        public virtual string Id { get; set; }
    }
}
