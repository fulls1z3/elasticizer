using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

[assembly:AssemblyTitle("Elasticizer.Tests")]
[assembly:AssemblyProduct("Elasticizer.Tests")]
[assembly:AssemblyCopyright("Copyright (c) 2017 Burak Tasci")]

[assembly:ComVisible(false)]

[assembly:Guid("bfd38ec5-a0a7-4b3e-88e6-f03d3989bbe5")]

[assembly:CollectionBehavior(DisableTestParallelization = true)]
[assembly:TestCollectionOrderer("XunitOrderer.TestCollectionOrderer", "XunitOrderer")]
