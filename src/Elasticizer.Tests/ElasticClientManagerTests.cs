using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Elasticizer.Core;
using Elasticsearch.Net;
using Xunit;
using XunitOrderer;

namespace Elasticizer.Tests {
    [TestPriority(10)]
    public class ElasticClientManagerTests : TestClassBase {
        // NOTE: You should provide the connection settings at the `app.config` before running the tests
        private readonly string _testEndpoint = ConfigurationManager.AppSettings["TEST_ENDPOINT"];

        private readonly string _testUsername = ConfigurationManager.AppSettings["TEST_USERNAME"];
        private readonly string _testPassword = ConfigurationManager.AppSettings["TEST_PASSWORD"];
        private readonly string _testIndex = ConfigurationManager.AppSettings["TEST_INDEX"];

        [Fact]
        [TestPriority(10)]
        public void GetInstanceWithoutInitializationShouldThrow() => Assert.Throws<NullReferenceException>(delegate {
            ElasticClientManager.GetInstance(_testIndex);
        });

        [Fact]
        [TestPriority(20)]
        public void InitializationWithNoConnectionPoolShouldSucceed() {
            ElasticClientManager.Initialize(new List<string> {
                    string.Format(_testEndpoint, _testUsername, _testPassword)
                },
                _testIndex);

            var testClient = ElasticClientManager.GetInstance(_testIndex);

            Assert.IsType<SingleNodeConnectionPool>(testClient.ConnectionSettings.ConnectionPool);
        }

        [Fact]
        [TestPriority(21)]
        public void InitializationWithStaticConnectionPoolShouldSucceed() {
            ElasticClientManager.Initialize(new List<string> {
                    string.Format(_testEndpoint, _testUsername, _testPassword)
                },
                _testIndex,
                null,
                ConnectionPool.Static);

            var testClient = ElasticClientManager.GetInstance(_testIndex);

            Assert.IsType<StaticConnectionPool>(testClient.ConnectionSettings.ConnectionPool);
        }

        [Fact]
        [TestPriority(22)]
        public void InitializationWithSniffingConnectionPoolShouldSucceed() {
            ElasticClientManager.Initialize(new List<string> {
                    string.Format(_testEndpoint, _testUsername, _testPassword)
                },
                _testIndex,
                null,
                ConnectionPool.Sniffing);

            var testClient = ElasticClientManager.GetInstance(_testIndex);

            Assert.IsType<SniffingConnectionPool>(testClient.ConnectionSettings.ConnectionPool);
        }

        [Fact]
        [TestPriority(23)]
        public void InitializationWithStickyConnectionPoolShouldSucceed() {
            ElasticClientManager.Initialize(new List<string> {
                    string.Format(_testEndpoint, _testUsername, _testPassword)
                },
                _testIndex,
                null,
                ConnectionPool.Sticky);

            var testClient = ElasticClientManager.GetInstance(_testIndex);

            Assert.IsType<StickyConnectionPool>(testClient.ConnectionSettings.ConnectionPool);
        }

        [Fact]
        [TestPriority(24)]
        public void InitializationWithSettingsSucceed() {
            var config = new ConnectionConfig {
                BasicAuthenticationUsername = "xyz",
                BasicAuthenticationPassword = "xyz",
                ConnectionLimit = 80,
                DeadTimeout = TimeSpan.FromMilliseconds(1000),
                DisableAutomaticProxyDetection = true,
                DisablePings = true,
                EnableDebugMode = true,
                EnableHttpCompression = true,
                EnableHttpPipelining = false,
                KeepAliveTime = TimeSpan.FromMilliseconds(2000),
                KeepAliveInterval = TimeSpan.FromMilliseconds(2000),
                Headers = new NameValueCollection {{"test", ""}},
                QueryStringParameters = new NameValueCollection {{"test", ""}},
                MaxDeadTimeout = TimeSpan.FromMilliseconds(1000),
                MaxRetries = 10,
                MaxRetryTimeout = TimeSpan.FromMinutes(2),
                NodePredicate = x => x.MasterOnlyNode,
                RequestCompletedHandler = delegate { },
                RequestDataCreatedHandler = delegate { },
                PingTimeout = TimeSpan.FromMilliseconds(1000),
                ProxyAddress = "http://localhost:8080/",
                ProxyUsername = "xyz",
                ProxyPassword = "xyz",
                RequestTimeout = TimeSpan.FromMinutes(2),
                ServerCertificateValidationCallback = delegate { return false; },
                SniffLifeSpan = TimeSpan.FromHours(1),
                SniffsOnConnectionFault = false,
                SniffsOnStartup = false,
                ThrowExceptions = true
            };

            ElasticClientManager.Initialize(new List<string> {
                    string.Format(_testEndpoint, _testUsername, _testPassword)
                },
                _testIndex,
                config);

            var testClient = ElasticClientManager.GetInstance(_testIndex);

            Assert.IsType<SingleNodeConnectionPool>(testClient.ConnectionSettings.ConnectionPool);
            Assert.Equal(config.ConnectionLimit, testClient.ConnectionSettings.ConnectionLimit);
            Assert.Equal(config.DeadTimeout, testClient.ConnectionSettings.DeadTimeout);
            Assert.True(testClient.ConnectionSettings.DisableAutomaticProxyDetection);
            Assert.True(testClient.ConnectionSettings.DisableDirectStreaming);
            Assert.True(testClient.ConnectionSettings.DisablePings);
            Assert.True(testClient.ConnectionSettings.EnableHttpCompression);
            Assert.False(testClient.ConnectionSettings.HttpPipeliningEnabled);
            Assert.Equal(config.KeepAliveTime, testClient.ConnectionSettings.KeepAliveTime);
            Assert.Equal(config.KeepAliveInterval, testClient.ConnectionSettings.KeepAliveInterval);
            Assert.Equal(new NameValueCollection {{"test", ""}}, testClient.ConnectionSettings.Headers);
            Assert.Equal(new NameValueCollection {{"pretty", ""}, {"test", ""}}, testClient.ConnectionSettings.QueryStringParameters);
            Assert.Equal(config.MaxDeadTimeout, testClient.ConnectionSettings.MaxDeadTimeout);
            Assert.Equal(config.MaxRetries, testClient.ConnectionSettings.MaxRetries);
            Assert.Equal(config.MaxRetryTimeout, testClient.ConnectionSettings.MaxRetryTimeout);
            Assert.True(testClient.ConnectionSettings.PrettyJson);
            Assert.Equal(config.NodePredicate, testClient.ConnectionSettings.NodePredicate);
            Assert.NotNull(testClient.ConnectionSettings.OnRequestCompleted);
            Assert.NotNull(testClient.ConnectionSettings.OnRequestDataCreated);
            Assert.Equal(config.PingTimeout, testClient.ConnectionSettings.PingTimeout);
            Assert.Equal(config.ProxyAddress, testClient.ConnectionSettings.ProxyAddress);
            Assert.Equal(config.ProxyUsername, testClient.ConnectionSettings.ProxyUsername);
            Assert.Equal(config.ProxyPassword, testClient.ConnectionSettings.ProxyPassword);
            Assert.Equal(config.RequestTimeout, testClient.ConnectionSettings.RequestTimeout);
            Assert.Equal(config.ServerCertificateValidationCallback, testClient.ConnectionSettings.ServerCertificateValidationCallback);
            Assert.Equal(config.SniffLifeSpan, testClient.ConnectionSettings.SniffInformationLifeSpan);
            Assert.False(testClient.ConnectionSettings.SniffsOnConnectionFault);
            Assert.False(testClient.ConnectionSettings.SniffsOnStartup);
            Assert.True(testClient.ConnectionSettings.ThrowExceptions);
        }

        [Fact]
        [TestPriority(25)]
        public void InitializationWithCertificatesSucceed() {
            var config = new ConnectionConfig {
                ClientCerfificates = new X509CertificateCollection {new X509Certificate()}
            };

            ElasticClientManager.Initialize(new List<string> {
                    string.Format(_testEndpoint, _testUsername, _testPassword)
                },
                _testIndex,
                config);

            var testClient = ElasticClientManager.GetInstance(_testIndex);

            Assert.IsType<SingleNodeConnectionPool>(testClient.ConnectionSettings.ConnectionPool);
            Assert.Equal(config.ClientCerfificates, testClient.ConnectionSettings.ClientCertificates);
        }

        [Fact]
        [TestPriority(26)]
        public void InitializationWithCertificateSucceed() {
            var config = new ConnectionConfig {
                ClientCerfificate = new X509Certificate()
            };

            ElasticClientManager.Initialize(new List<string> {
                    string.Format(_testEndpoint, _testUsername, _testPassword)
                },
                _testIndex,
                config);

            var testClient = ElasticClientManager.GetInstance(_testIndex);

            Assert.IsType<SingleNodeConnectionPool>(testClient.ConnectionSettings.ConnectionPool);
            Assert.Contains(config.ClientCerfificate, testClient.ConnectionSettings.ClientCertificates.Cast<X509Certificate>().ToList());
        }

        [Fact]
        [TestPriority(30)]
        public void InitializationWithoutEndpointsShouldThrow() => Assert.Throws<ArgumentException>(
            delegate { ElasticClientManager.Initialize(null, _testIndex); });

        [Fact]
        [TestPriority(31)]
        public void InitializationWithInvalidConnectionPoolShouldThrow() => Assert.Throws<ArgumentOutOfRangeException>(
            delegate {
                const ConnectionPool pool = (ConnectionPool)int.MinValue;

                ElasticClientManager.Initialize(new List<string> {
                        string.Format(_testEndpoint, _testUsername, _testPassword)
                    },
                    _testIndex,
                    null,
                    pool);
            });
    }
}
