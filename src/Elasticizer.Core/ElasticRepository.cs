using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elasticizer.Domain;
using Elasticsearch.Net;
using Nest;

namespace Elasticizer.Core {
    public class ElasticRepository<T>
        where T : class, IIndex {
        private readonly ElasticClient _client;
        private readonly int _maxRetries;

        public ElasticRepository(ElasticClient client) {
            _client = client;

            if (_client != null)
                _maxRetries = _client.ConnectionSettings.MaxRetries ?? 3;
        }

        public async Task<T> GetAsync(string id) {
            var response = await _client.GetAsync<T>(id);

            if (!(response.IsValid && response.Found))
                return null;

            var res = response.Source;
            res.Id = id;

            return res;
        }

        public async Task<IList<T>> SearchAsync(Func<SearchDescriptor<T>, ISearchRequest> descriptor) {
            var response = await _client.SearchAsync(descriptor);

            var res = new List<T>();

            if (!(response.IsValid && response.Total > 0))
                return res;

            foreach (var hit in response.Hits) {
                var item = hit.Source;
                item.Id = hit.Id;

                res.Add(item);
            }

            return res;
        }

        public async Task<string> CreateAsync(T item, Refresh refresh = Refresh.WaitFor) {
            dynamic response;

            if (string.IsNullOrWhiteSpace(item.Id))
                response = await _client.IndexAsync(item,
                    x => x
                        .Refresh(refresh));
            else
                response = await _client.CreateAsync(item,
                    x => x
                        .Refresh(refresh));

            if (!(response.IsValid && !string.IsNullOrWhiteSpace(response.Id)))
                return null;

            return response.Id;
        }

        public async Task<int> CreateAsync(IList<T> items, Refresh refresh = Refresh.WaitFor) {
            var descriptor = new BulkDescriptor();

            foreach (var item in items)
                if (string.IsNullOrWhiteSpace(item.Id))
                    descriptor.Index<T>(x => x
                        .Document(item));
                else
                    descriptor.Create<T>(x => x
                        .Document(item));

            descriptor.Refresh(refresh);

            var response = await _client.BulkAsync(descriptor);

            return !response.IsValid ? 0 : response.Items.Count;
        }

        public async Task<string> UpdateAsync(string id, T obj, Refresh refresh = Refresh.WaitFor) {
            var response = await _client.UpdateAsync<T>(id,
                x => x
                    .Doc(obj)
                    .RetryOnConflict(_maxRetries)
                    .Refresh(refresh));

            if (!(response.IsValid && !string.IsNullOrWhiteSpace(response.Id)))
                return null;

            return response.Id;
        }

        public async Task<string> UpdateAsync(string id, object obj, Refresh refresh = Refresh.WaitFor) {
            var response = await _client.UpdateAsync<T, object>(id,
                x => x
                    .Doc(obj)
                    .RetryOnConflict(_maxRetries)
                    .Refresh(refresh));

            if (!(response.IsValid && !string.IsNullOrWhiteSpace(response.Id)))
                return null;

            return response.Id;
        }

        public async Task<int> UpdateAsync(IList<string> ids, object obj, Refresh refresh = Refresh.WaitFor) {
            var descriptor = new BulkDescriptor();

            foreach (var id in ids)
                descriptor.Update<T, object>(x => x.Id(id)
                    .Doc(obj)
                    .RetriesOnConflict(_maxRetries)
                );

            descriptor.Refresh(refresh);

            var response = await _client.BulkAsync(descriptor);

            return !response.IsValid ? 0 : response.Items.Count;
        }

        public async Task<bool> DeleteAsync(string id, Refresh refresh = Refresh.WaitFor) {
            if (string.IsNullOrWhiteSpace(id))
                return false;

            var response = await _client.DeleteAsync<T>(id,
                x => x
                    .Refresh(refresh));

            return response.IsValid;
        }

        public async Task<int> DeleteAsync(IList<string> ids, Refresh refresh = Refresh.WaitFor) {
            var descriptor = new BulkDescriptor();

            foreach (var id in ids.Where(x => !string.IsNullOrWhiteSpace(x)))
                descriptor.Delete<T>(x => x
                    .Id(id));

            descriptor.Refresh(refresh);

            var response = await _client.BulkAsync(descriptor);

            return !response.IsValid ? 0 : response.Items.Count;
        }

        public async Task<long> DeleteAsync(Func<DeleteByQueryDescriptor<T>, IDeleteByQueryRequest> descriptor,
                                            Refresh refresh = Refresh.WaitFor) {
            var response = await _client.DeleteByQueryAsync(descriptor);

            await _client.RefreshAsync(_client.ConnectionSettings.DefaultIndex);

            return !response.IsValid ? 0 : response.Deleted;
        }
    }
}
