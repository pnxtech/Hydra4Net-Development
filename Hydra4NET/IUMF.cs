namespace Hydra4NET
{
    public interface IUMF
    {
        /// <summary>
        /// The customizable body of the message
        /// </summary>
        object? Bdy { get; set; }

        /// <summary>
        /// The service name and instance of the message
        /// </summary>
        string Frm { get; set; }

        /// <summary>
        /// The message ID of the instance
        /// </summary>
        string Mid { get; set; }

        /// <summary>
        /// (Optional) The message ID of the message to which this message is responding
        /// </summary>
        string? Rmid { get; set; }

        /// <summary>
        /// The service to which the message is to be delivered
        /// </summary>
        string To { get; set; }

        /// <summary>
        /// Iso 8601 timestamp on whic hthe message what created
        /// </summary>
        string Ts { get; set; }

        /// <summary>
        /// The type of message.  Used to deserialize the body
        /// </summary>
        string Typ { get; set; }

        /// <summary>
        /// The UMF version of the message
        /// </summary>
        string Ver { get; set; }

        /// <summary>
        /// Serializes this UMF message to Json
        /// </summary>
        /// <returns></returns>
        string Serialize();

        UMFRouteEntry GetRouteEntry();
    }

    public interface IUMF<TBdy> : IUMF
    {
        /// <summary>
        /// The customizable typed body of the message
        /// </summary>
        new TBdy Bdy { get; set; }
    }
}