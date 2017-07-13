using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Elasticizer.Core;
using Elasticizer.Testing;
using Elasticizer.Tests.Mocks;
using Xunit;

namespace Elasticizer.Tests {
    [TestPriority(20)]
    public class ElasticRepositoryTests : TestClassBase {
        // NOTE: You should provide the connection settings at the `app.config` before running the tests
        private readonly string _testEndpoint = ConfigurationManager.AppSettings["TEST_ENDPOINT"];

        private readonly string _testUsername = ConfigurationManager.AppSettings["TEST_USERNAME"];
        private readonly string _testPassword = ConfigurationManager.AppSettings["TEST_PASSWORD"];
        private readonly string _testIndex = ConfigurationManager.AppSettings["TEST_INDEX"];
        private readonly ElasticRepository<MockDocument> _mockRepo;

        private readonly MockDocument _mockDocumentWithId;
        private readonly MockDocument _mockDocumentWithoutId;

        public ElasticRepositoryTests() {
            _mockDocumentWithId = new MockDocument {
                Id = "99",
                Name = "mock #2",
                Value = "mock #2's value",
                CreationDate = DateTime.UtcNow,
                IsActive = false
            };

            _mockDocumentWithoutId = new MockDocument {
                Name = "mock #1",
                Value = "mock #1's value",
                CreationDate = DateTime.UtcNow,
                IsActive = true
            };

            ElasticClientManager.Initialize(new List<string> {
                    string.Format(_testEndpoint, _testUsername, _testPassword)
                },
                _testIndex);

            var client = ElasticClientManager.GetInstance(_testIndex);
            _mockRepo = new ElasticRepository<MockDocument>(client);
        }

        [Fact]
        [TestPriority(10)]
        public async Task CreateAsyncWithIdShouldSucceed() {
            var id = await _mockRepo.CreateAsync(_mockDocumentWithId);

            Assert.Equal(_mockDocumentWithId.Id, id);
        }

        [Fact]
        [TestPriority(11)]
        public async Task CreateAsyncWithoutIdShouldSucceed() {
            var id = await _mockRepo.CreateAsync(_mockDocumentWithoutId);

            Assert.False(string.IsNullOrWhiteSpace(id));
        }

        [Fact]
        [TestPriority(12)]
        public async Task CreateAsyncExistingItemShouldFail() {
            var id = await _mockRepo.CreateAsync(_mockDocumentWithId);

            Assert.Null(id);
        }

        [Fact]
        [TestPriority(13)]
        public async Task CreateAsyncBulkShouldSucceed() {
            var searchResults = await _mockRepo.SearchAsync(x => x.MatchAll());
            var deleteFlags = new List<bool>();

            foreach (var item in searchResults)
                deleteFlags.Add(await _mockRepo.DeleteAsync(item.Id));

            var items = new List<MockDocument> {_mockDocumentWithId, _mockDocumentWithoutId};
            var count = await _mockRepo.CreateAsync(items);

            foreach (var flag in deleteFlags)
                Assert.True(flag);

            Assert.Equal(2, count);
        }

        [Fact]
        [TestPriority(14)]
        public async Task CreateAsyncEmptyItemsShouldThrow() => await Assert.ThrowsAsync<ArgumentException>(
            async delegate {
                var empty = new List<MockDocument>();

                await _mockRepo.CreateAsync(empty);
            });

        [Fact]
        [TestPriority(20)]
        public async Task GetAsyncShouldSucceed() {
            var result1 = await _mockRepo.GetAsync(_mockDocumentWithId.Id);
            var result2 = await _mockRepo.GetAsync("xyz");

            Assert.NotNull(result1);
            Assert.Equal(_mockDocumentWithId.Id, result1.Id);
            Assert.Equal(_mockDocumentWithId.Name, result1.Name);
            Assert.Equal(_mockDocumentWithId.Value, result1.Value);
            Assert.Equal(_mockDocumentWithId.IsActive, result1.IsActive);

            Assert.Null(result2);
        }

        [Fact]
        [TestPriority(21)]
        public async Task GetAsyncEmptyIdShouldThrow() => await Assert.ThrowsAsync<ArgumentException>(async delegate {
            await _mockRepo.GetAsync("");
        });

        [Fact]
        [TestPriority(30)]
        public async Task SearchAsyncShouldSucceed() {
            var searchResults1 = await _mockRepo.SearchAsync(x => x.MatchAll());
            var searchResults2 = await _mockRepo.SearchAsync(x => x
                .Query(q => q
                    .Term(t => t
                        .Field(f => f.IsActive)
                        .Value(false)
                    )
                )
            );
            var searchResults3 = await _mockRepo.SearchAsync(x => x
                .Query(q => q
                    .Term(t => t
                        .Field(f => f.Name)
                        .Value("xyz")
                    )
                )
            );

            Assert.NotEmpty(searchResults1);
            Assert.Equal(2, searchResults1.Count);

            Assert.NotEmpty(searchResults2);
            Assert.Equal(1, searchResults2.Count);

            Assert.NotNull(searchResults3);
            Assert.Empty(searchResults3);
        }

        [Fact]
        [TestPriority(31)]
        public async Task SearchAsyncNullDescriptorShouldThrow() => await Assert.ThrowsAsync<ArgumentNullException>(
            async delegate { await _mockRepo.SearchAsync(null); });

        [Fact]
        [TestPriority(40)]
        public async Task UpdateAsyncShouldSucceed() {
            var result1 = await _mockRepo.GetAsync(_mockDocumentWithId.Id);
            var searchResults = await _mockRepo.SearchAsync(x => x
                .Query(q => !q
                    .Term(t => t
                        .Field(f => f.Id)
                        .Value(_mockDocumentWithId.Id)
                    )
                )
            );
            var result2 = searchResults.First();

            var utcNow = DateTime.UtcNow;

            var id1 = await _mockRepo.UpdateAsync(result1.Id,
                new MockDocument {
                    UpdateDate = utcNow,
                    IsActive = true
                });
            var id2 = await _mockRepo.UpdateAsync(result2.Id,
                new MockDocument {
                    UpdateDate = utcNow,
                    IsActive = true
                });

            result1 = await _mockRepo.GetAsync(id1);
            result2 = await _mockRepo.GetAsync(id2);

            Assert.NotNull(result1);
            Assert.Equal(DateTime.MinValue, result1.CreationDate);
            Assert.Equal(utcNow, result1.UpdateDate);
            Assert.True(result1.IsActive);

            Assert.NotNull(searchResults);
            Assert.Equal(DateTime.MinValue, result2.CreationDate);
            Assert.Equal(utcNow, result2.UpdateDate);
            Assert.True(result2.IsActive);
        }

        [Fact]
        [TestPriority(41)]
        public async Task UpdateAsyncNonExistingItemShouldFail() {
            var id = await _mockRepo.UpdateAsync("0",
                new MockDocument {
                    UpdateDate = DateTime.UtcNow,
                    IsActive = true
                });

            Assert.Null(id);
        }

        [Fact]
        [TestPriority(42)]
        public async Task UpdateAsyncEmptyIdShouldThrow() => await Assert.ThrowsAsync<ArgumentException>(
            async delegate { await _mockRepo.UpdateAsync("", new MockDocument()); });

        [Fact]
        [TestPriority(43)]
        public async Task UpdateAsyncWithAnonymousObjectShouldSucceed() {
            var result1 = await _mockRepo.GetAsync(_mockDocumentWithId.Id);
            var searchResults = await _mockRepo.SearchAsync(x => x
                .Query(q => !q
                    .Term(t => t
                        .Field(f => f.Id)
                        .Value(_mockDocumentWithId.Id)
                    )
                )
            );
            var result2 = searchResults.First();

            var utcNow = DateTime.UtcNow;

            var id1 = await _mockRepo.UpdateAsync(result1.Id,
                new {
                    UpdateDate = utcNow,
                    IsActive = false
                });
            var id2 = await _mockRepo.UpdateAsync(result2.Id,
                new {
                    UpdateDate = utcNow,
                    IsActive = false
                });

            result1 = await _mockRepo.GetAsync(id1);
            result2 = await _mockRepo.GetAsync(id2);

            Assert.NotNull(result1);
            Assert.Equal(utcNow, result1.UpdateDate);
            Assert.False(result1.IsActive);

            Assert.NotNull(searchResults);
            Assert.Equal(utcNow, result2.UpdateDate);
            Assert.False(result2.IsActive);
        }

        [Fact]
        [TestPriority(44)]
        public async Task UpdateAsyncNonExistingItemWithAnonymousObjectShouldFail() {
            var id = await _mockRepo.UpdateAsync("0",
                new {
                    UpdateDate = DateTime.UtcNow,
                    IsActive = true
                });

            Assert.Null(id);
        }

        [Fact]
        [TestPriority(45)]
        public async Task UpdateAsyncEmptyIdWithAnonymousShouldThrow() => await Assert.ThrowsAsync<ArgumentException>(
            async delegate { await _mockRepo.UpdateAsync("", new { }); });

        [Fact]
        [TestPriority(46)]
        public async Task UpdateAsyncBulkWithAnonymousObjectShouldSucceed() {
            var searchResponse = await _mockRepo.SearchAsync(x => x.MatchAll());
            var ids = searchResponse.Select(x => x.Id).ToList();

            const string value = "corrected value";
            var utcNow = DateTime.UtcNow;

            var count = await _mockRepo.UpdateAsync(ids,
                new {
                    Value = value,
                    UpdateDate = utcNow
                });

            searchResponse = await _mockRepo.SearchAsync(x => x.MatchAll());

            Assert.Equal(2, count);

            foreach (var item in searchResponse) {
                Assert.NotNull(item);
                Assert.Equal(value, item.Value);
                Assert.Equal(utcNow, item.UpdateDate);
            }
        }

        [Fact]
        [TestPriority(47)]
        public async Task UpdateAsyncBulkEmptyItemsWithAnonymousShouldThrow() => await Assert
            .ThrowsAsync<ArgumentException>(async delegate {
                var empty = new List<string>();

                await _mockRepo.UpdateAsync(empty, new { });
            });

        [Fact]
        [TestPriority(48)]
        public async Task UpdateByQueryAsyncShouldSucceed() {
            const string value = "corrected value #2";

            var count = await _mockRepo.UpdateAsync(x => x
                .Query(q => q
                    .Term(t => t
                        .Field(f => f.IsActive)
                        .Value(false)
                    )
                )
                .Script(s => s
                    .Inline($"ctx._source.value = '{value}';")
                )
            );

            var searchResponse = await _mockRepo.SearchAsync(x => x.MatchAll());

            Assert.Equal(2, count);

            foreach (var item in searchResponse) {
                Assert.NotNull(item);
                Assert.Equal(value, item.Value);
            }
        }

        [Fact]
        [TestPriority(49)]
        public async Task UpdateByQueryAsyncNullSelectorShouldThrow() => await
            Assert.ThrowsAsync<ArgumentNullException>(async delegate { await _mockRepo.UpdateAsync(null); });

        [Fact]
        [TestPriority(50)]
        public async Task DeleteAsyncShouldSucceed() {
            var deleteFlag1 = await _mockRepo.DeleteAsync(_mockDocumentWithId.Id);
            var searchResults = await _mockRepo.SearchAsync(x => x
                .Query(q => !q
                    .Term(t => t
                        .Field(f => f.Id)
                        .Value(_mockDocumentWithId.Id)
                    )
                )
            );
            var mockDocumentWithoutId = searchResults.First();
            var deleteFlag2 = await _mockRepo.DeleteAsync(mockDocumentWithoutId.Id);

            var searchResponse = await _mockRepo.SearchAsync(x => x.MatchAll());

            Assert.True(deleteFlag1);
            Assert.True(deleteFlag2);

            Assert.NotNull(searchResponse);
            Assert.Collection(searchResponse);
            Assert.Empty(searchResponse);
        }

        [Fact]
        [TestPriority(51)]
        public async Task DeleteAsyncEmptyIdShouldThrow() => await Assert.ThrowsAsync<ArgumentException>(
            async delegate { await _mockRepo.DeleteAsync(""); });

        [Fact]
        [TestPriority(52)]
        public async Task DeleteAsyncBulkShouldSucceed() {
            var items = new List<MockDocument> {_mockDocumentWithId, _mockDocumentWithoutId};
            var count = await _mockRepo.CreateAsync(items);

            Assert.Equal(2, count);

            var searchResults = await _mockRepo.SearchAsync(x => x.MatchAll());
            var ids = searchResults.Select(x => x.Id).ToList();

            var deleteFlag = await _mockRepo.DeleteAsync(ids);
            searchResults = await _mockRepo.SearchAsync(x => x.MatchAll());

            Assert.Equal(2, deleteFlag);

            Assert.NotNull(searchResults);
            Assert.Collection(searchResults);
            Assert.Empty(searchResults);
        }

        [Fact]
        [TestPriority(53)]
        public async Task DeleteByQueryAsyncShouldSucceed() {
            var items = new List<MockDocument> {_mockDocumentWithId, _mockDocumentWithoutId};

            foreach (var item in items)
                item.IsActive = false;

            var count = await _mockRepo.CreateAsync(items);

            Assert.Equal(2, count);

            var deleteFlag = await _mockRepo.DeleteAsync(x => x
                .Query(q => q
                    .Term(t => t
                        .Field(f => f.IsActive)
                        .Value(false)
                    )
                )
            );
            var searchResults = await _mockRepo.SearchAsync(x => x.MatchAll());

            Assert.Equal(2, deleteFlag);

            Assert.NotNull(searchResults);
            Assert.Collection(searchResults);
            Assert.Empty(searchResults);
        }
    }
}
