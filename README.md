# Elasticizer
> Please support this project by simply putting a Github star. Share this library with friends on Twitter and everywhere else you can.

**`Elasticizer`** is a lightweight library to use [ElasticSearch] with .NET, allowing basic CRUD operations by implementing the repository pattern on the top of [Elasticsearch.Net] and [NEST].

**NOTE**: This project is in experimental stage now, functionality is subject to slightly change.

You can have a look at **unit tests** for demo usage. Meanwhile, usage instructions will be provided on further releases.

> Built with `.NET Framework v4.6.2`, solution currently supports `ElasticSearch v5.x`.

## Prerequisites
Packages in this project depend on
- [Elasticsearch.Net v5.5.0](https://www.nuget.org/packages/Elasticsearch.Net)
- [NEST v5.5.0](https://www.nuget.org/packages/NEST)

> Older versions contain outdated dependencies, might produce errors.

## Getting started
### Installation
You can install **`Elasticizer`** by running following commands in the Package Manager Console
```
Install-Package Elasticizer.Domain -Pre
Install-Package Elasticizer.Core -Pre
```

### Solution architecture
The solution consists of 3 projects
```
Elasticizer Solution
├─ Elasticizer.Domain
│  - Shared domain classes
│  
├─ Elasticizer.Core
│  - Core functionality
│
└─ Elasticizer.Tests
   - Unit tests, mocks
```

### Running tests
Simply clone this repository and run the tests using `Unit Test Explorer`.

**NOTE**: You should provide the connection settings at the `app.config` before running the tests.

## License
The MIT License (MIT)

Copyright (c) 2017 [Burak Tasci]

[ElasticSearch]: https://www.elastic.co/
[Elasticsearch.Net]: https://github.com/elastic/elasticsearch-net
[NEST]: https://github.com/elastic/elasticsearch-net
[Burak Tasci]: http://www.buraktasci.com
