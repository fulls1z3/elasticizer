using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Elasticizer.Core;
using Elasticizer.Testing;
using Elasticizer.Tests.Mocks;
using Nest;
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

        private readonly MockDocument _mockDocumentWithoutId;
        private readonly MockDocument _mockDocumentWithId;

        public ElasticRepositoryTests() {
            _mockDocumentWithoutId = new MockDocument {
                Name = "mock #1",
                Value = "mock #1's value",
                CreationDate = DateTime.UtcNow,
                IsActive = true
            };

            _mockDocumentWithId = new MockDocument {
                Id = "99",
                Name = "mock #2",
                Value = "mock #2's value",
                CreationDate = DateTime.UtcNow,
                IsActive = false
            };

            ElasticClientManager.Initialize(new List<string> {
                    string.Format(_testEndpoint, _testUsername, _testPassword)
                },
                _testIndex);

            var client = ElasticClientManager.GetInstance(_testIndex);
            _mockRepo = new ElasticRepository<MockDocument>(client);
        }

        #region Create
        [Fact]
        [TestPriority(10)]
        public async Task CreateAsyncByItemWithIdShouldSucceed() {
            var id = await _mockRepo.CreateAsync(_mockDocumentWithId);

            Assert.Equal(_mockDocumentWithId.Id, id);
        }

        [Fact]
        [TestPriority(11)]
        public async Task CreateAsyncByItemWithoutIdShouldSucceed() {
            var id = await _mockRepo.CreateAsync(_mockDocumentWithoutId);

            Assert.False(string.IsNullOrWhiteSpace(id));
        }

        [Fact]
        [TestPriority(12)]
        public async Task CreateAsyncByExistingItemShouldFail() {
            var id = await _mockRepo.CreateAsync(_mockDocumentWithId);

            Assert.Null(id);
        }

        [Fact]
        [TestPriority(13)]
        public async Task CreateAsyncByNullItemShouldThrow() => await Assert.ThrowsAsync<ArgumentNullException>(
            async delegate { await _mockRepo.CreateAsync((MockDocument)null); });

        [Fact]
        [TestPriority(14)]
        public async Task CreateAsyncByItemsShouldSucceed() {
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
        [TestPriority(15)]
        public async Task CreateAsyncByEmptyItemsShouldThrow() => await Assert.ThrowsAsync<ArgumentException>(
            async delegate { await _mockRepo.CreateAsync(new List<MockDocument>()); });
        #endregion

        #region Get/Search
        [Fact]
        [TestPriority(20)]
        public async Task GetAsyncByIdShouldSucceed() {
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
        public async Task GetAsyncByEmptyIdShouldThrow() => await Assert.ThrowsAsync<ArgumentException>(
            async delegate { await _mockRepo.GetAsync(""); });

        [Fact]
        [TestPriority(22)]
        public async Task SearchAsyncByDescriptorShouldSucceed() {
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
        [TestPriority(23)]
        public async Task SearchAsyncByNullDescriptorShouldThrow() => await Assert.ThrowsAsync<ArgumentNullException>(
            async delegate { await _mockRepo.SearchAsync(null); });
        #endregion

        #region Update
        [Fact]
        [TestPriority(30)]
        public async Task UpdateAsyncByIdAndItemShouldSucceed() {
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
        [TestPriority(31)]
        public async Task UpdateAsyncByIdAndNonExistingItemShouldFail() {
            var id = await _mockRepo.UpdateAsync("0",
                new MockDocument {
                    UpdateDate = DateTime.UtcNow,
                    IsActive = true
                });

            Assert.Null(id);
        }

        [Fact]
        [TestPriority(32)]
        public async Task UpdateAsyncByEmptyIdAndItemShouldThrow() => await Assert.ThrowsAsync<ArgumentException>(
            async delegate { await _mockRepo.UpdateAsync("", new MockDocument()); });

        [Fact]
        [TestPriority(33)]
        public async Task UpdateAsyncByIdAndNullItemShouldThrow() => await Assert.ThrowsAsync<ArgumentNullException>(
            async delegate { await _mockRepo.UpdateAsync("0", null); });

        [Fact]
        [TestPriority(34)]
        public async Task UpdateAsyncByIAndPartShouldSucceed() {
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
        [TestPriority(35)]
        public async Task UpdateAsyncByNonExistingIdAndPartShouldFail() {
            var id = await _mockRepo.UpdateAsync("0",
                new {
                    UpdateDate = DateTime.UtcNow,
                    IsActive = true
                });

            Assert.Null(id);
        }

        [Fact]
        [TestPriority(36)]
        public async Task UpdateAsyncByEmptyIdAndPartShouldThrow() => await Assert.ThrowsAsync<ArgumentException>(
            async delegate { await _mockRepo.UpdateAsync("", new { }); });

        [Fact]
        [TestPriority(37)]
        public async Task UpdateAsyncByIdAndNullPartShouldThrow() => await Assert.ThrowsAsync<ArgumentNullException>(
            async delegate { await _mockRepo.UpdateAsync("0", (object)null); });

        [Fact]
        [TestPriority(38)]
        public async Task UpdateAsyncByIdsAndPartShouldSucceed() {
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
        [TestPriority(39)]
        public async Task UpdateAsyncByEmptyIdsAndPartShouldThrow() => await Assert.ThrowsAsync<ArgumentException>(
            async delegate { await _mockRepo.UpdateAsync(new List<string>(), new { }); });

        [Fact]
        [TestPriority(40)]
        public async Task UpdateAsyncByIdsAndNullPartShouldThrow() => await Assert.ThrowsAsync<ArgumentNullException>(
            async delegate { await _mockRepo.UpdateAsync(new List<string>{ "1", "2" }, null); });

        [Fact]
        [TestPriority(41)]
        public async Task UpdateAsyncBySelectorShouldSucceed() {
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
        [TestPriority(42)]
        public async Task UpdateByAsyncByNullSelectorShouldThrow() => await Assert.ThrowsAsync<ArgumentNullException>(
            async delegate { await _mockRepo.UpdateAsync(null); });
        #endregion

        #region Delete
        [Fact]
        [TestPriority(50)]
        public async Task DeleteAsyncByIdShouldSucceed() {
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
        public async Task DeleteAsyncByEmptyIdShouldThrow() => await Assert.ThrowsAsync<ArgumentException>(
            async delegate { await _mockRepo.DeleteAsync(""); });

        [Fact]
        [TestPriority(52)]
        public async Task DeleteAsyncByIdsShouldSucceed() {
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
        public async Task DeleteAsyncByEmptyIdsShouldThrow() => await Assert.ThrowsAsync<ArgumentException>(
            async delegate { await _mockRepo.DeleteAsync(new List<string>()); });

        [Fact]
        [TestPriority(54)]
        public async Task DeleteAsyncByDescriptorShouldSucceed() {
            var items = new List<MockDocument> {_mockDocumentWithId, _mockDocumentWithoutId};

            foreach (var item in items)
                item.IsActive = false;

            var createCount = await _mockRepo.CreateAsync(items);

            Assert.Equal(2, createCount);

            var deleteCount = await _mockRepo.DeleteAsync(x => x
                .Query(q => q
                    .Term(t => t
                        .Field(f => f.IsActive)
                        .Value(false)
                    )
                )
            );
            var searchResults = await _mockRepo.SearchAsync(x => x.MatchAll());

            Assert.Equal(2, deleteCount);

            Assert.NotNull(searchResults);
            Assert.Collection(searchResults);
            Assert.Empty(searchResults);
        }

        [Fact]
        [TestPriority(55)]
        public async Task DeleteAsyncByNullDescriptorShouldThrow() => await Assert.ThrowsAsync<ArgumentNullException>(
            async delegate { await _mockRepo.DeleteAsync((Func<DeleteByQueryDescriptor<MockDocument>, IDeleteByQueryRequest>)null); });
        #endregion
    }
}
