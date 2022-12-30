using System;
using Hydra4NET;

namespace MessageDemo
{
	/**
		* TestMsgBody
		* This class represents the body type of a UMF message body's entry
		*/
	public class TestMsgBody
	{
			public string? Msg { get; set; }
			public int? Id { get; set; }
	}

	/**
		* TestMsg
		* This class represents a UMF class with a body type of TestMsgBody.
		* See the CreateUMFMessage below for an example of how the body is set.
		*/
	public class TestMsg: UMF<TestMsgBody>
	{
			public TestMsg()
			{
					To = "hydra-router:/";
					Frm = "TestRig:/";
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

