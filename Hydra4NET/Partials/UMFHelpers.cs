using Hydra4NET.Internal;

namespace Hydra4NET
{
    public partial class Hydra
    {
        public IUMF<TBdy> CreateUMF<TBdy>(string to, string type, TBdy bdy, string? rmid = null) where TBdy : new()
        {
            //if no route specified then add default route
            if (!to.Contains(":"))
                to += ":/";
            return new UMF<TBdy>()
            {
                To = to,
                Typ = type,
                Frm = GetServiceFrom(),
                Bdy = bdy,
                Rmid = rmid
            };
        }

        public IUMF<TToBdy> CreateUMFResponse<TToBdy>(IUMF fromUmf, string type, TToBdy bdy) where TToBdy : new()
            => CreateUMF(fromUmf.Frm, type, bdy, fromUmf.Mid);

        public string GetServiceFrom() => $"{InstanceID}@{ServiceName}:/";

        public IReceivedUMF? DeserializeReceviedUMF(string json) => ReceivedUMF.Deserialize(json);
    }
}
