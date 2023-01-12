using System.Text.Json;

namespace Hydra4NET
{
    public interface IReceivedUMF : IUMF<JsonElement>
    {
        UMF<TBdy> ToUMF<TBdy>() where TBdy : new();
    }
}