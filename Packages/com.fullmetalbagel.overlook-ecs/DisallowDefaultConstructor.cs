namespace Overlook.Ecs;

[System.AttributeUsage(System.AttributeTargets.Struct)]
internal sealed class DisallowDefaultConstructorAttribute : System.Attribute { }

[System.AttributeUsage(System.AttributeTargets.Property)]
internal sealed class OptionalOnInit : System.Attribute { }
