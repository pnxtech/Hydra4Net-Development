using System;

namespace Hydra4NET
{
    public class HydraException : Exception
    {
        public enum ErrorType
        {
            Other,
            InitializationError,
            NotInitialized
            //TODO: add more as required
        }

        public ErrorType Type { get; private set; }

        public HydraException(string message, ErrorType type = ErrorType.Other) : base(message) { Type = type; }

        public HydraException(string message, Exception innerException, ErrorType type = ErrorType.Other) : base(message, innerException) { Type = type; }
    }
}
