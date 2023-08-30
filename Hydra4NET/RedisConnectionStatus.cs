using System;

namespace Hydra4NET
{
    public enum ConnectionStatus
    {
        Disconnected,
        Connected,
        Reconnected
    }

    public class RedisConnectionStatus
    {
        internal RedisConnectionStatus(ConnectionStatus status, string? message = null, Exception? exception = null)
        {
            Status = status;
            Message = message;
            Exception = exception;
        }

        public ConnectionStatus Status { get; private set; }
        public string? Message { get; private set; }
        public Exception? Exception { get; private set; }
    }
}