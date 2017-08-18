# Change Log
All notable changes to this project will be documented in this file.

## v1.0.0-rc.3 - 2017-07-23
### Bug fixes
- `AssemblyName` for `TestCollectionOrderer` (closes [#15](https://github.com/fulls1z3/elasticizer/issue/15))

### Features
- add `Aggs` feature (closes [#14](https://github.com/fulls1z3/elasticizer/issue/14))
- add missing `ConnectionConfiguration` members (closes [#16](https://github.com/fulls1z3/elasticizer/issue/16))
- update `ElasticSearch.NET` and `NEST` to v5.5.0

## v1.0.0-rc.1 - 2017-07-14
### Bug fixes
- reverse order for conditional statements such as `!response.IsValid` (closes [#8](https://github.com/fulls1z3/elasticizer/issue/8))
- throw exceptions on `null`/`empty` arguments (closes [#10](https://github.com/fulls1z3/elasticizer/issue/10))

### Features
- add `UpdateByQuery` feature  (closes [#9](https://github.com/fulls1z3/elasticizer/issue/9))
- change method signatures with more relevant ones (closes [#11](https://github.com/fulls1z3/elasticizer/issue/11))
- update `Testing` project with `XunitOrderer` package (closes [#13](https://github.com/fulls1z3/elasticizer/issue/13))
- remove `UpdateAsync(IList<string> ids, T obj, ...)` overload (closes [#7](https://github.com/fulls1z3/elasticizer/issue/7))
- remove `Replace` method (closes [#12](https://github.com/fulls1z3/elasticizer/issue/12))

## v1.0.0-beta.2 - 2017-07-13
### Bug fixes
- Resolved invalid `AssemblyVersion` (closes [#5](https://github.com/fulls1z3/elasticizer/issue/5))

### Features
- add `DeleteByQuery` feature (closes [#1](https://github.com/fulls1z3/elasticizer/issue/1))
- add `IIndex` interface and depend on it (closes [#4](https://github.com/fulls1z3/elasticizer/issue/4))
