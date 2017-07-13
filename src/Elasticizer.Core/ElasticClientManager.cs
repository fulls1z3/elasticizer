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
                                               ElasticSettings settings,
                                               ConnectionPool connectionPool) {
            if (!(endpoints != null && endpoints.GetEnumerator().MoveNext()))
                throw new ArgumentException("The argument `endpoints` cannot be null and must have at least one item.",
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
                .ConnectionLimit(settings?.ConnectionLimit > 0 ? settings.ConnectionLimit : 80)
                .DefaultIndex(defaultIndex)
                .EnableHttpCompression()
                .EnableHttpPipelining()
                .EnableTcpKeepAlive(settings?.KeepAliveTime.Ticks > 0
                        ? settings.KeepAliveTime
                        : TimeSpan.FromMilliseconds(2000),
                    settings?.KeepAliveInterval.Ticks > 0
                        ? settings.KeepAliveInterval
                        : TimeSpan.FromMilliseconds(2000))
                .MaximumRetries(settings?.MaxRetries > 0 ? settings.MaxRetries : 10)
                .MaxRetryTimeout(
                    settings?.MaxRetryTimeout.Ticks > 0 ? settings.MaxRetryTimeout : TimeSpan.FromMinutes(2))
                .RequestTimeout(settings?.RequestTimeout.Ticks > 0 ? settings.RequestTimeout : TimeSpan.FromMinutes(2));

            return new ElasticClient(connectionSettings);
        }

        public static void Initialize(IList<string> endpoints,
                                      string index,
                                      ElasticSettings settings = null,
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
