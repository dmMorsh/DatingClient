namespace DatingClient.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class QueryIgnoreAttribute : Attribute { }