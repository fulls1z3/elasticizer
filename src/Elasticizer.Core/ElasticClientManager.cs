using System;
using System.Collections.Generic;
using System.Linq;
using Elasticsearch.Net;
using Nest;

namespace Elasticizer.Core {
    public static class ElasticClientManager {
        private static Dictionary<string, ElasticClient> _clients;

        private static ElasticClient GetClient(IList<string> endpoints,
                                               string defaultIndex,
                                               ConnectionConfig config,
                                               ConnectionPool connectionPool) {
            if (!endpoints.HasItems())
                throw new ArgumentException(string.Format(Utils.ARGUMENT_EMPTY_LIST_MESSAGE, nameof(endpoints)),
                    nameof(endpoints));

            var uris = endpoints.Select(x => new Uri(x));

            IConnectionPool pool;

            switch (connectionPool) {
                case ConnectionPool.SingleNode:
                    pool = new SingleNodeConnectionPool(new Uri(endpoints.First()));
                    break;
                case ConnectionPool.Static:
                    pool = new StaticConnectionPool(uris);
                    break;
                case ConnectionPool.Sniffing:
                    pool = new SniffingConnectionPool(uris);
                    break;
                case ConnectionPool.Sticky:
                    pool = new StickyConnectionPool(uris);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(connectionPool), connectionPool, null);
            }

            var connectionSettings = new ConnectionSettings(pool)
                .ConnectionLimit(config?.ConnectionLimit > 0 ? config.ConnectionLimit : 80)
                .DisableAutomaticProxyDetection(config?.DisableAutomaticProxyDetection ?? false)
                .DisableDirectStreaming(config?.EnableDebugMode ?? false)
                .DisablePing(config?.DisablePings ?? false)
                .DefaultIndex(defaultIndex)
                .EnableHttpCompression(config?.EnableHttpCompression ?? true)
                .EnableHttpPipelining(config?.EnableHttpPipelining ?? true)
                .EnableTcpKeepAlive(config?.KeepAliveTime.Ticks > 0
                        ? config.KeepAliveTime
                        : TimeSpan.FromMilliseconds(2000),
                    config?.KeepAliveInterval.Ticks > 0
                        ? config.KeepAliveInterval
                        : TimeSpan.FromMilliseconds(2000))
                .MaximumRetries(config?.MaxRetries > 0 ? config.MaxRetries : 10)
                .MaxRetryTimeout(
                    config?.MaxRetryTimeout.Ticks > 0 ? config.MaxRetryTimeout : TimeSpan.FromSeconds(60))
                .PrettyJson(config?.EnableDebugMode ?? false)
                .RequestTimeout(config?.RequestTimeout.Ticks > 0
                    ? config.RequestTimeout
                    : TimeSpan.FromSeconds(60))
                .SniffLifeSpan(config?.SniffLifeSpan.Ticks > 0
                    ? config.SniffLifeSpan
                    : TimeSpan.FromHours(1))
                .SniffOnConnectionFault(config?.SniffsOnConnectionFault ?? true)
                .SniffOnStartup(config?.SniffsOnStartup ?? true)
                .ThrowExceptions(config?.ThrowExceptions ?? false);

            if (!string.IsNullOrWhiteSpace(config?.BasicAuthenticationUsername)
                && !string.IsNullOrWhiteSpace(config.BasicAuthenticationPassword))
                connectionSettings.BasicAuthentication(config.BasicAuthenticationUsername, config.BasicAuthenticationPassword);

            if (config?.ClientCerfificates != null && config.ClientCerfificates.HasItems())
                connectionSettings.ClientCertificates(config.ClientCerfificates);
            else if (config?.ClientCerfificate != null)
                connectionSettings.ClientCertificate(config.ClientCerfificate);

            if (config?.DeadTimeout.Ticks > 0)
                connectionSettings.DeadTimeout(config.DeadTimeout);

            if (config?.Headers != null && config.Headers.HasItems())
                connectionSettings.GlobalHeaders(config.Headers);

            if (config?.QueryStringParameters != null && config.QueryStringParameters.HasItems())
                connectionSettings.GlobalQueryStringParameters(config.QueryStringParameters);

            if (config?.MaxDeadTimeout.Ticks > 0)
                connectionSettings.MaxDeadTimeout(config.DeadTimeout);

            if (config?.NodePredicate != null)
                connectionSettings.NodePredicate(config.NodePredicate);

            if (config?.RequestCompletedHandler != null)
                connectionSettings.OnRequestCompleted(config.RequestCompletedHandler);

            if (config?.RequestDataCreatedHandler != null)
                connectionSettings.OnRequestDataCreated(config.RequestDataCreatedHandler);

            if (config?.PingTimeout.Ticks > 0)
                connectionSettings.PingTimeout(config.PingTimeout);

            if (!string.IsNullOrWhiteSpace(config?.ProxyAddress))
                connectionSettings.Proxy(new Uri(config.ProxyAddress), config?.ProxyUsername, config?.ProxyPassword);

            if (config?.ServerCertificateValidationCallback != null)
                connectionSettings.ServerCertificateValidationCallback(config.ServerCertificateValidationCallback);

            return new ElasticClient(connectionSettings);
        }

        public static void Initialize(IList<string> endpoints,
                                      string index,
                                      ConnectionConfig settings = null,
                                      ConnectionPool connectionPool = ConnectionPool.SingleNode) {
            if (_clients == null)
                _clients = new Dictionary<string, ElasticClient>();

            if (_clients.ContainsKey(index))
                _clients[index] =
                    GetClient(endpoints, index, settings, connectionPool);
            else
                _clients.Add(index,
                    GetClient(endpoints, index, settings, connectionPool));
        }

        public static ElasticClient GetInstance(string index) => _clients[index];
    }
}
