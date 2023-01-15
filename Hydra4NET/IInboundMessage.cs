namespace Hydra4NET
{
    public interface IInboundMessage
    {
        IReceivedUMF? ReceivedUMF { get; set; }
        string MessageJson { get; set; }
        string Type { get; set; }
    }

    public interface IInboundMessage<T> : IInboundMessage
    {
        new IUMF<T>? ReceivedUMF { get; set; }
        
    }
}