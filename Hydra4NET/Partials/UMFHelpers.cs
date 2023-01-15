using Hydra4NET.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

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

        public IUMF<TToBdy> CreateUMFResponse<TToBdy>(IUMF umf, string type, TToBdy bdy) where TToBdy : new()
            => CreateUMF(umf.Frm, type, bdy, umf.Mid);

        public string GetServiceFrom() => $"{InstanceID}@{ServiceName}:/";

        public IReceivedUMF? DeserializeReceviedUMF(string json) => ReceivedUMF.Deserialize(json);
    }
}
