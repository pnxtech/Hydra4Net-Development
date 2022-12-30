using System;
using Hydra4NET;

namespace MessageDemo
{
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
            To = "sender-svcs:/";
            Frm = "external-client:/";
            Typ = "command";
        }
    }

    /**
     * Message and Body
     * Define Message and Body classes for the UMF message
     */
    public class SenderMessageBody
    {
        public string? Msg { get; set; }
        public int? Id { get; set; }
    }
    public class SenderMessage : UMF<SenderMessageBody>
    {
        public SenderMessage()
        {
            To = "queuer-svcs:/";
            Frm = "sender-svcs:/";
            Typ = "sender";
        }
    }

    public class Sender
    {
        private Hydra _hydra;

        public Sender(Hydra hydra)
        {
            _hydra = hydra;
        }

        public void ProcessMessage(string type, string message)
        {            
            switch (type) // Messages dispatcher
            {
                case "command":
                    ProcessCommandMessage(message);
                    break;
                case "sender":
                    ProcessSenderMessage(message);
                    break;
            }
        }

        private void ProcessSenderMessage(string message)
        {
            SenderMessage? msg = SenderMessage.Deserialize<SenderMessage>(message);
            if (msg != null)
            {
                Console.WriteLine($"Message received: {msg.Bdy?.Msg}");
            }
        }

        private void ProcessCommandMessage(string message)
        {
            CommandMessage? msg = CommandMessage.Deserialize<CommandMessage>(message);
            if (msg != null)
            {
                switch (msg.Bdy?.Cmd)
                {
                    case "start":
                        Console.WriteLine("Start message recieved!");
                        break;
                    case "stop":
                        // Stop();
                        break;
                }
            }
        }
    }
}
