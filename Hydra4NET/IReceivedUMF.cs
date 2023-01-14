using System.Text.Json;

namespace Hydra4NET
{
    public interface IReceivedUMF : IUMF<JsonElement>
    {
        IUMF<TBdy> ToUMF<TBdy>() where TBdy : new();
    }
}