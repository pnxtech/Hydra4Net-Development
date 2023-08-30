namespace Hydra4NET
{
    public enum DebugEventType
    {
        MessageReceived,
        SendMessage,
        QueueReceived,
        SendQueue,
        SendBroadcastMessage,
        MarkQueueMessage,
        Register
    }

    public class DebugEvent
    {
        public DebugEventType EventType { get; set; }
        public string? UMF { get; set; }
        public string Message { get; set; } = "";
    }
}
