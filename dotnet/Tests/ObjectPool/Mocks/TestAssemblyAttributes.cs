// This file would only be used for demonstration - in a real test, you'd need 
// to create a separate test assembly with these attributes applied

using System;

[assembly: Overlook.Pool.Tests.HighPriorityFactory]
[assembly: Overlook.Pool.Tests.MediumPriorityFactory] 
[assembly: Overlook.Pool.Tests.DefaultPriorityFactory]

// Example of an assembly-level pool policy attribute
[assembly: Overlook.Pool.PoolPolicyAttribute<Overlook.Pool.Tests.IntegrationTests.TestItem, 
                                           Overlook.Pool.Tests.IntegrationTests.CustomPoolPolicy>] 