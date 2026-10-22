namespace AslHelp.Tests;

internal sealed record TestError(string Message) : ResultError
{
    public override string Message { get; } = Message;
}
