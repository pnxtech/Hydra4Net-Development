namespace Hydra4NET.Internal
{
    internal class InboundMessage : IInboundMessage
    {
        public IReceivedUMF? ReceivedUMF { get; set; }
        public string Type { get; set; } = "";
        public string MessageJson { get; set; } =  "";
    }

    internal class InboundMessage<T> : InboundMessage, IInboundMessage<T>
    {
        public new IUMF<T>? ReceivedUMF { get; set; }       
    }
}
