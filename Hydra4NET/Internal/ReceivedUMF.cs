using Hydra4NET.Helpers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hydra4NET.Internal
{
    /// <summary>
    /// The UMF class that's used to implement an untyped UMF and body message pair.
    /// </summary>
    internal class ReceivedUMF : UMF<JsonElement>, IReceivedUMF
    {
        public ReceivedUMF() : base() { }

        /// <summary>
        /// The original message's JSON value
        /// </summary>
        [JsonIgnore] //prevent System.Text.Json from serializing / deserializing
        public string MessageJson { get; private set; } = "";

        /// <summary>
        /// Deserializes a UMF JSON message into an untyped UMF class instance
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static new ReceivedUMF? Deserialize(string message)
        {
            var umf = StandardSerializer.Deserialize<ReceivedUMF>(message);
            if (umf != null)
                umf.MessageJson = message;
            return umf;
        }

        /// <summary>
        /// Casts an untyped UMF instance to a typed instance
        /// </summary>
        /// <typeparam name="TBdy"></typeparam>
        /// <returns></returns>
        public IUMF<TBdy> ToUMF<TBdy>() where TBdy : new() => UMF<TBdy>.Deserialize(MessageJson)!;
    }
}
