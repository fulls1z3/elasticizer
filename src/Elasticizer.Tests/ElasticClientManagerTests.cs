using System;
using System.Collections.Generic;
using System.Configuration;
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
            var settings = new ElasticSettings {
                ConnectionLimit = 80,
                KeepAliveTime = TimeSpan.FromMilliseconds(2000),
                KeepAliveInterval = TimeSpan.FromMilliseconds(2000),
                MaxRetries = 10,
                MaxRetryTimeout = TimeSpan.FromMinutes(2),
                RequestTimeout = TimeSpan.FromMinutes(2)
            };

            ElasticClientManager.Initialize(new List<string> {
                    string.Format(_testEndpoint, _testUsername, _testPassword)
                },
                _testIndex,
                settings);

            var testClient = ElasticClientManager.GetInstance(_testIndex);

            Assert.IsType<SingleNodeConnectionPool>(testClient.ConnectionSettings.ConnectionPool);
            Assert.Equal(settings.ConnectionLimit, testClient.ConnectionSettings.ConnectionLimit);
            Assert.Equal(settings.KeepAliveTime, testClient.ConnectionSettings.KeepAliveTime);
            Assert.Equal(settings.KeepAliveInterval, testClient.ConnectionSettings.KeepAliveInterval);

            if (testClient.ConnectionSettings.MaxRetries != null)
                Assert.Equal(settings.MaxRetries, testClient.ConnectionSettings.MaxRetries.Value);

            Assert.Equal(settings.MaxRetryTimeout, testClient.ConnectionSettings.MaxRetryTimeout);
            Assert.Equal(settings.RequestTimeout, testClient.ConnectionSettings.RequestTimeout);
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
