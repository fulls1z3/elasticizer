# Elasticizer

> Please support this project by simply putting a Github star. Share this library with friends on Twitter and everywhere else you can.

**`Elasticizer`** is a lightweight [ElasticSearch] client library for .NET

**NOTE**: This project is in experimental stage now, functionality is subject to slightly change.

You can have a look at **unit tests** for demo usage. Meanwhile, usage instructions will be provided on further releases.

> Built with `.NET Framework v4.6.2`, solution currently supports `ElasticSearch v5.x`.

## Prerequisites
Packages in this seed project depend on
- [ElasticSearch v5.4.0](https://www.nuget.org/packages/Elasticsearch.Net/5.4.0)
- [NEST v5.4.0](https://www.nuget.org/packages/NEST/5.4.0)

> Older versions contain outdated dependencies, might produce errors.

## Getting started
### Installation
You can install **`Elasticizer** by running following command in the Package Manager Console
```
Install-Package Elasticizer.Domain -Pre
Install-Package Elasticizer.Core -Pre
```

### Solution architecture
The solution consists of 4 projects
```
Elasticizer Solution
├─ Elasticizer.Domain
│  - Shared domain classes
│  
├─ Elasticizer.Core
│  - Core functionality
│  
├─ Elasticizer.Testing
│  - Testing library, mocks
│  
└─ Elasticizer.Tests
   - Unit tests
```


### Running tests
In order to run unit tests, simply clone this repository and run the tests using `Unit Test Explorer`.

**NOTE**: You should provide the connection settings at the `app.config` before running the tests.

## License
The MIT License (MIT)

Copyright (c) 2017 [Burak Tasci]

[ElasticSearch]: http://elastic.co
