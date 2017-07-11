using System;

namespace Elasticizer.Core {
    public class ElasticSettings {
        public int ConnectionLimit { get; set; }
        public TimeSpan KeepAliveTime { get; set; }
        public TimeSpan KeepAliveInterval { get; set; }
        public int MaxRetries { get; set; }
        public TimeSpan MaxRetryTimeout { get; set; }
        public TimeSpan RequestTimeout { get; set; }
    }
}
