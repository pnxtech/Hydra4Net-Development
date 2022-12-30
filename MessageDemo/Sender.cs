using System;
using Hydra4NET;

namespace MessageDemo
{
	/**
		* MessageBody
		* This class represents the body type of a UMF message body's entry
		*/
	public class MessageBody
	{
			public string? Msg { get; set; }
			public int? Id { get; set; }
	}

	/**
		* Message
		* This class represents a UMF class with a body type of MessageBody.
		* See the CreateUMFMessage below for an example of how the body is set.
		*/
	public class Message: UMF<MessageBody>
	{
			public TestMsg()
			{
					To = "queuer-svcs:/";
					Frm = "sender-svcs:/";
					Typ = "testMsg";
			}
	}

	public class Sender
	{
		private Hydra _hydra;

		public Sender(Hydra hydra)
		{
			_hydra = hydra;
		}

		public void ProcessMessage(string type, string? message)
		{
		}
	}
}

