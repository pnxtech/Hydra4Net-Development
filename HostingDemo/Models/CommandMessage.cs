using Hydra4NET;

namespace HostingDemo.Models;

/**
 * CommandMessage and Body
 * Define Message and Body classes for the Command message
 */
public class CommandMessageBody
{
    public string? Cmd { get; set; }
}
public class CommandMessage : UMF<CommandMessageBody>
{
    public CommandMessage()
    {
    }
}
