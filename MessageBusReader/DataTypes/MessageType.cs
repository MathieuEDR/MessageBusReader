namespace MessageBusReader.DataTypes;

public record MessageType(string Value)
{
    public static MessageType Unknown => new("unknown");
    public override string ToString() => Value;
}
