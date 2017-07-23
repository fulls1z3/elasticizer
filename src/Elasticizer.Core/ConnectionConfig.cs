using System;
using System.Collections.Specialized;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Elasticsearch.Net;

namespace Elasticizer.Core {
    public class ConnectionConfig {
        public string BasicAuthenticationUsername { get; set; }
        public string BasicAuthenticationPassword { get; set; }
        public X509CertificateCollection ClientCerfificates { get; set; }
        public X509Certificate ClientCerfificate { get; set; }
        public int ConnectionLimit { get; set; }
        public TimeSpan DeadTimeout { get; set; }
        public bool DisableAutomaticProxyDetection { get; set; }
        public bool DisablePings { get; set; }
        public bool EnableDebugMode { get; set; }
        public bool EnableHttpCompression { get; set; }
        public bool EnableHttpPipelining { get; set; }
        public TimeSpan KeepAliveTime { get; set; }
        public TimeSpan KeepAliveInterval { get; set; }
        public NameValueCollection Headers { get; set; }
        public NameValueCollection QueryStringParameters { get; set; }
        public TimeSpan MaxDeadTimeout { get; set; }
        public int MaxRetries { get; set; }
        public TimeSpan MaxRetryTimeout { get; set; }
        public Func<Node, bool> NodePredicate { get; set; }
        public Action<IApiCallDetails> RequestCompletedHandler { get; set; }
        public Action<RequestData> RequestDataCreatedHandler { get; set; }
        public TimeSpan PingTimeout { get; set; }
        public string ProxyAddress { get; set; }
        public string ProxyUsername { get; set; }
        public string ProxyPassword { get; set; }
        public TimeSpan RequestTimeout { get; set; }
        public Func<object, X509Certificate, X509Chain, SslPolicyErrors, bool> ServerCertificateValidationCallback { get; set; }
        public TimeSpan SniffLifeSpan { get; set; }
        public bool SniffsOnConnectionFault { get; set; }
        public bool SniffsOnStartup { get; set; }
        public bool ThrowExceptions { get; set; }
    }
}
