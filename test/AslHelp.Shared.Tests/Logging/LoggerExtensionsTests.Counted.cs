namespace AslHelp.Tests.Logging;

public partial class LoggerExtensionsTests
{
    private sealed class Counted
    {
        public int Renders { get; private set; }

        public override string ToString()
        {
            Renders++;

            return "counted";
        }
    }
}
