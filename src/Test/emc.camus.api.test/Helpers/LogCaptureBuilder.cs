using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Moq;

namespace emc.camus.api.test.Helpers;

internal static class LogCaptureBuilder
{
    public static (Mock<ILogger<T>> Mock, ConcurrentBag<(LogLevel Level, string Message)> Entries) Create<T>()
    {
        var loggerMock = new Mock<ILogger<T>>();
        var entries = new ConcurrentBag<(LogLevel Level, string Message)>();

        loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        loggerMock
            .Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(new InvocationAction(invocation =>
            {
                var level = (LogLevel)invocation.Arguments[0];
                var state = invocation.Arguments[2];
                var message = state?.ToString() ?? "";
                entries.Add((level, message));
            }));

        return (loggerMock, entries);
    }
}
