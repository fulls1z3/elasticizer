using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elasticizer.Domain;
using Elasticsearch.Net;
using Nest;

namespace Elasticizer.Core {
    public class ElasticRepository<T>
        where T : class, IDocument {
        private readonly ElasticClient _client;
        private readonly int _maxRetries;

        public ElasticRepository(ElasticClient client) {
            _client = client;

            if (_client != null)
                _maxRetries = _client.ConnectionSettings.MaxRetries ?? 3;
        }

        public async Task<T> GetAsync(string id) {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException(string.Format(Utils.ARGUMENT_EMPTY_MESSAGE, nameof(id)), nameof(id));

            var response = await _client.GetAsync<T>(id);

            if (!(response.IsValid && response.Found))
                return null;

            var res = response.Source;
            res.Id = id;

            return res;
        }

        public async Task<IList<T>> SearchAsync(Func<SearchDescriptor<T>, ISearchRequest> descriptor) {
            if (descriptor == null)
                throw new ArgumentNullException(string.Format(Utils.ARGUMENT_NULL_MESSAGE, nameof(descriptor)), nameof(descriptor));

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
            if (item == null)
                throw new ArgumentNullException(string.Format(Utils.ARGUMENT_NULL_MESSAGE, nameof(item)), nameof(item));

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
            if (!items.HasItems())
                throw new ArgumentException(string.Format(Utils.ARGUMENT_EMPTY_LIST_MESSAGE, nameof(items)), nameof(items));

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

        public async Task<bool> UpdateAsync(string id, object part, Refresh refresh = Refresh.WaitFor) {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException(string.Format(Utils.ARGUMENT_EMPTY_MESSAGE, nameof(id)), nameof(id));

            if (part == null)
                throw new ArgumentNullException(string.Format(Utils.ARGUMENT_NULL_MESSAGE, nameof(part)), nameof(part));

            var response = await _client.UpdateAsync<T, object>(id,
                x => x
                    .Doc(part)
                    .RetryOnConflict(_maxRetries)
                    .Refresh(refresh));

            return response.IsValid && !string.IsNullOrWhiteSpace(response.Id);
        }

        public async Task<long> UpdateAsync(IList<string> ids, object part, Refresh refresh = Refresh.WaitFor) {
            if (!ids.HasItems())
                throw new ArgumentException(string.Format(Utils.ARGUMENT_EMPTY_LIST_MESSAGE, nameof(ids)), nameof(ids));

            if (part == null)
                throw new ArgumentNullException(string.Format(Utils.ARGUMENT_NULL_MESSAGE, nameof(part)), nameof(part));

            var descriptor = new BulkDescriptor();

            foreach (var id in ids)
                descriptor.Update<T, object>(x => x.Id(id)
                    .Doc(part)
                    .RetriesOnConflict(_maxRetries)
                );

            descriptor.Refresh(refresh);

            var response = await _client.BulkAsync(descriptor);

            return !(response.IsValid && response.Items.Count > 0) ? 0 : response.Items.Count;
        }

        public async Task<long> UpdateAsync(Func<UpdateByQueryDescriptor<T>, IUpdateByQueryRequest> selector,
                                            Refresh refresh = Refresh.WaitFor) {
            if (selector == null)
                throw new ArgumentNullException(string.Format(Utils.ARGUMENT_NULL_MESSAGE, nameof(selector)), nameof(selector));

            var response = await _client.UpdateByQueryAsync(selector);

            if (refresh != Refresh.False)
                await _client.RefreshAsync(_client.ConnectionSettings.DefaultIndex);

            return !(response.IsValid && response.Updated > 0) ? 0 : response.Updated;
        }

        public async Task<bool> DeleteAsync(string id, Refresh refresh = Refresh.WaitFor) {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException(string.Format(Utils.ARGUMENT_EMPTY_MESSAGE, nameof(id)), nameof(id));

            var response = await _client.DeleteAsync<T>(id,
                x => x
                    .Refresh(refresh));

            return response.IsValid;
        }

        public async Task<bool> DeleteAsync(IList<string> ids, Refresh refresh = Refresh.WaitFor) {
            if (!ids.HasItems())
                throw new ArgumentException(string.Format(Utils.ARGUMENT_EMPTY_LIST_MESSAGE, nameof(ids)), nameof(ids));

            var descriptor = new BulkDescriptor();

            foreach (var id in ids.Where(x => !string.IsNullOrWhiteSpace(x)))
                descriptor.Delete<T>(x => x
                    .Id(id));

            descriptor.Refresh(refresh);

            var response = await _client.BulkAsync(descriptor);

            return response.IsValid;
        }

        public async Task<long> DeleteAsync(Func<DeleteByQueryDescriptor<T>, IDeleteByQueryRequest> descriptor,
                                            Refresh refresh = Refresh.WaitFor) {
            if (descriptor == null)
                throw new ArgumentNullException(string.Format(Utils.ARGUMENT_NULL_MESSAGE, nameof(descriptor)), nameof(descriptor));

            var response = await _client.DeleteByQueryAsync(descriptor);

            await _client.RefreshAsync(_client.ConnectionSettings.DefaultIndex);

            return !response.IsValid ? 0 : response.Deleted;
        }
    }
}
