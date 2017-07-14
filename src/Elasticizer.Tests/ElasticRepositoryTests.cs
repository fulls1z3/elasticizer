using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Elasticizer.Core;
using Elasticizer.Tests.Mocks;
using Nest;
using Xunit;
using XunitOrderer;

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
            var deleteCount = await _mockRepo.DeleteAsync(x => x.MatchAll());
            var items = new List<MockDocument> {_mockDocumentWithId, _mockDocumentWithoutId};
            var count = await _mockRepo.CreateAsync(items);

            Assert.Equal(2, deleteCount);
            Assert.Equal(2, count);
        }

        [Fact]
        [TestPriority(15)]
        public async Task CreateAsyncByExistingItemsShouldFail() {
            var items = new List<MockDocument> {_mockDocumentWithId};
            var count = await _mockRepo.CreateAsync(items);

            Assert.Equal(0, count);
        }

        [Fact]
        [TestPriority(16)]
        public async Task CreateAsyncByEmptyItemsShouldThrow() => await Assert.ThrowsAsync<ArgumentException>(
            async delegate { await _mockRepo.CreateAsync(new List<MockDocument>()); });
        #endregion

        #region Get/Search
        [Fact]
        [TestPriority(20)]
        public async Task GetAsyncByIdShouldSucceed() {
            var item1 = await _mockRepo.GetAsync(_mockDocumentWithId.Id);
            var item2 = await _mockRepo.GetAsync("xyz");

            Assert.NotNull(item1);
            Assert.Equal(_mockDocumentWithId.Id, item1.Id);
            Assert.Equal(_mockDocumentWithId.Name, item1.Name);
            Assert.Equal(_mockDocumentWithId.Value, item1.Value);
            Assert.Equal(_mockDocumentWithId.IsActive, item1.IsActive);

            Assert.Null(item2);
        }

        [Fact]
        [TestPriority(21)]
        public async Task GetAsyncByEmptyIdShouldThrow() => await Assert.ThrowsAsync<ArgumentException>(
            async delegate { await _mockRepo.GetAsync(""); });

        [Fact]
        [TestPriority(22)]
        public async Task SearchAsyncByDescriptorShouldSucceed() {
            var items1 = await _mockRepo.SearchAsync(x => x.MatchAll());
            var items2 = await _mockRepo.SearchAsync(x => x
                .Query(q => q
                    .Term(t => t
                        .Field(f => f.IsActive)
                        .Value(false)
                    )
                )
            );
            var items3 = await _mockRepo.SearchAsync(x => x
                .Query(q => q
                    .Term(t => t
                        .Field(f => f.Name)
                        .Value("xyz")
                    )
                )
            );

            Assert.NotEmpty(items1);
            Assert.Equal(2, items1.Count);

            Assert.NotEmpty(items2);
            Assert.Equal(1, items2.Count);

            Assert.NotNull(items3);
            Assert.Empty(items3);
        }

        [Fact]
        [TestPriority(23)]
        public async Task SearchAsyncByNullDescriptorShouldThrow() => await Assert.ThrowsAsync<ArgumentNullException>(
            async delegate { await _mockRepo.SearchAsync(null); });
        #endregion

        #region Replace/Update
        [Fact]
        [TestPriority(31)]
        public async Task UpdateAsyncByIAndPartShouldSucceed() {
            var searchResults = await _mockRepo.SearchAsync(x => x
                .Query(q => !q
                    .Term(t => t
                        .Field(f => f.Id)
                        .Value(_mockDocumentWithId.Id)
                    )
                )
            );

            var item1 = await _mockRepo.GetAsync(_mockDocumentWithId.Id);
            var item2 = searchResults.First();

            var utcNow = DateTime.UtcNow;

            var updateResult1 = await _mockRepo.UpdateAsync(item1.Id,
                new {
                    UpdateDate = utcNow,
                    IsActive = false
                });
            var updateResult2 = await _mockRepo.UpdateAsync(item2.Id,
                new {
                    UpdateDate = utcNow,
                    IsActive = false
                });

            var updatedItem1 = await _mockRepo.GetAsync(item1.Id);
            var updatedItem2 = await _mockRepo.GetAsync(item2.Id);

            Assert.True(updateResult1);
            Assert.NotNull(updatedItem1);
            Assert.Equal(utcNow, updatedItem1.UpdateDate);
            Assert.False(updatedItem1.IsActive);

            Assert.True(updateResult2);
            Assert.NotNull(updatedItem2);
            Assert.Equal(utcNow, updatedItem2.UpdateDate);
            Assert.False(updatedItem2.IsActive);
        }

        [Fact]
        [TestPriority(32)]
        public async Task UpdateAsyncByNonExistingIdAndPartShouldFail() {
            var updateResult = await _mockRepo.UpdateAsync("0",
                new {
                    UpdateDate = DateTime.UtcNow,
                    IsActive = true
                });

            Assert.False(updateResult);
        }

        [Fact]
        [TestPriority(33)]
        public async Task UpdateAsyncByEmptyIdAndPartShouldThrow() => await Assert.ThrowsAsync<ArgumentException>(
            async delegate { await _mockRepo.UpdateAsync("", new { }); });

        [Fact]
        [TestPriority(34)]
        public async Task UpdateAsyncByIdAndNullPartShouldThrow() => await Assert.ThrowsAsync<ArgumentNullException>(
            async delegate { await _mockRepo.UpdateAsync("0", null); });

        [Fact]
        [TestPriority(35)]
        public async Task UpdateAsyncByIdsAndPartShouldSucceed() {
            var items = await _mockRepo.SearchAsync(x => x.MatchAll());
            var ids = items.Select(x => x.Id).ToList();

            const string value = "corrected value";
            var utcNow = DateTime.UtcNow;

            var count = await _mockRepo.UpdateAsync(ids,
                new {
                    Value = value,
                    UpdateDate = utcNow
                });
            items = await _mockRepo.SearchAsync(x => x.MatchAll());

            Assert.Equal(2, count);

            foreach (var item in items) {
                Assert.NotNull(item);
                Assert.Equal(value, item.Value);
                Assert.Equal(utcNow, item.UpdateDate);
            }
        }

        [Fact]
        [TestPriority(36)]
        public async Task UpdateAsyncByNonExistingIdsAndPartShouldFail() {
            var count = await _mockRepo.UpdateAsync(new List<string> {"xyz"},
                new {
                    UpdateDate = DateTime.UtcNow,
                    IsActive = true
                });

            Assert.Equal(0, count);
        }

        [Fact]
        [TestPriority(37)]
        public async Task UpdateAsyncByEmptyIdsAndPartShouldThrow() => await Assert.ThrowsAsync<ArgumentException>(
            async delegate { await _mockRepo.UpdateAsync(new List<string>(), new { }); });

        [Fact]
        [TestPriority(38)]
        public async Task UpdateAsyncByIdsAndNullPartShouldThrow() => await Assert.ThrowsAsync<ArgumentNullException>(
            async delegate { await _mockRepo.UpdateAsync(new List<string> {"1", "2"}, null); });

        [Fact]
        [TestPriority(39)]
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
            var items = await _mockRepo.SearchAsync(x => x.MatchAll());

            Assert.Equal(2, count);

            foreach (var item in items) {
                Assert.NotNull(item);
                Assert.Equal(value, item.Value);
            }
        }

        [Fact]
        [TestPriority(40)]
        public async Task UpdateAsyncByNonExistingSelectorShouldFail() {
            var count = await _mockRepo.UpdateAsync(x => x
                .Query(q => q
                    .Term(t => t
                        .Field(f => f.Id)
                        .Value("xyz")
                    )
                )
                .Script(s => s
                    .Inline("ctx._source.value = 'corrected value #2';")
                )
            );

            Assert.Equal(0, count);
        }

        [Fact]
        [TestPriority(41)]
        public async Task UpdateByAsyncByNullSelectorShouldThrow() => await Assert.ThrowsAsync<ArgumentNullException>(
            async delegate { await _mockRepo.UpdateAsync(null); });
        #endregion

        #region Delete
        [Fact]
        [TestPriority(50)]
        public async Task DeleteAsyncByIdShouldSucceed() {
            var searchResults = await _mockRepo.SearchAsync(x => x
                .Query(q => !q
                    .Term(t => t
                        .Field(f => f.Id)
                        .Value(_mockDocumentWithId.Id)
                    )
                )
            );
            var mockDocumentWithoutId = searchResults.First();

            var deleteResult1 = await _mockRepo.DeleteAsync(_mockDocumentWithId.Id);
            var deleteResult2 = await _mockRepo.DeleteAsync(mockDocumentWithoutId.Id);

            var items = await _mockRepo.SearchAsync(x => x.MatchAll());

            Assert.True(deleteResult1);
            Assert.True(deleteResult2);

            Assert.NotNull(items);
            Assert.Collection(items);
            Assert.Empty(items);
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

            var results = await _mockRepo.SearchAsync(x => x.MatchAll());
            var ids = results.Select(x => x.Id).ToList();

            var deleteResult = await _mockRepo.DeleteAsync(ids);
            results = await _mockRepo.SearchAsync(x => x.MatchAll());

            Assert.Equal(2, count);
            Assert.True(deleteResult);

            Assert.NotNull(results);
            Assert.Collection(results);
            Assert.Empty(results);
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

            var count = await _mockRepo.CreateAsync(items);

            var deleteCount = await _mockRepo.DeleteAsync(x => x
                .Query(q => q
                    .Term(t => t
                        .Field(f => f.IsActive)
                        .Value(false)
                    )
                )
            );
            var searchResults = await _mockRepo.SearchAsync(x => x.MatchAll());

            Assert.Equal(2, count);
            Assert.Equal(2, deleteCount);

            Assert.NotNull(searchResults);
            Assert.Collection(searchResults);
            Assert.Empty(searchResults);
        }

        [Fact]
        [TestPriority(55)]
        public async Task DeleteAsyncByNonExistingDescriptorShouldFail() {
            var deleteCount = await _mockRepo.DeleteAsync(x => x
                .Query(q => q
                    .Term(t => t
                        .Field(f => f.Id)
                        .Value("xyz")
                    )
                )
            );

            Assert.Equal(0, deleteCount);
        }

        [Fact]
        [TestPriority(56)]
        public async Task DeleteAsyncByNullDescriptorShouldThrow() => await Assert.ThrowsAsync<ArgumentNullException>(
            async delegate { await _mockRepo.DeleteAsync((Func<DeleteByQueryDescriptor<MockDocument>, IDeleteByQueryRequest>)null); });
        #endregion
    }
}
