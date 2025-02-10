namespace Overlook;

[System.AttributeUsage(System.AttributeTargets.Struct)]
public sealed class DisallowDefaultConstructorAttribute : System.Attribute { }

[System.AttributeUsage(System.AttributeTargets.Property)]
public sealed class OptionalOnInitAttribute : System.Attribute { }
