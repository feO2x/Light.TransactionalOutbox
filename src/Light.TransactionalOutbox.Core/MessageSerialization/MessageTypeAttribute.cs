using System;
using Light.GuardClauses;

namespace Light.TransactionalOutbox.Core.MessageSerialization;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class MessageTypeAttribute : Attribute
{
    public MessageTypeAttribute(string name)
    {
        name.MustNotBeNullOrWhiteSpace();
        Names = [name];
    }
    
    public MessageTypeAttribute(string[] names)
    {
        names.MustNotBeNullOrEmpty();
        foreach (var name in names)
        {
            name.MustNotBeNullOrWhiteSpace();
        }

        Names = names;
    }
    
    public string PrimaryName => Names[0];

    public string[] Names { get; }
}