using Hydra4NET;

namespace HostingDemo.Models;

/**
 * Message and Body
 * Define Message and Body classes for the UMF message
 */
public class SharedMessageBody
{
    public string? Msg { get; set; }
    public int? Id { get; set; }
}
public class SharedMessage : UMF<SharedMessageBody>
{
    public SharedMessage()
    {
    }
}

