namespace Hydra4NET.Internal
{
    internal class InboundMessage : IInboundMessage
    {
        public IReceivedUMF? ReceivedUMF { get; set; }
        public string Type { get; set; }
        public string MessageJson { get; set; }
    }
}
