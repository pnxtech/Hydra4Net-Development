using System.Text;
using Hydra4NET;
using static Hydra4NET.Hydra;

/**
 * TestRig
 * A simple testing class for trigger Hydra functionality during development
 **/
namespace TestRig
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

    public class PingMsgBody
    {
        public string Message { get; set; } = String.Empty;
    }
    public class PingMsg: UMF<PingMsgBody>
    {
        public PingMsg()
        {
            To = "hmr-service:/";
            Frm = "TestRig:/";
            Typ = "ping";
        }
    }

    public class QueueMsgBody
    {
        public string JobID { get; set; } = String.Empty;
        public string JobType { get; set; } = String.Empty;
        public string JobData { get; set; } = String.Empty;
    }
    public class QueueMsg: UMF<QueueMsgBody>
    {
    }




    public class Tests
    {
        private Hydra _hydra;

        public Tests(Hydra hydra)
        {
            _hydra = hydra;
        }

        /**
         * UMF message creation test
         */

        public void CreateUMFMessage()
        {
            TestMsg myUMF = new TestMsg();
            myUMF.Bdy.Msg = "New value";
            myUMF.Bdy.Id = 34;

            string json = myUMF.Serialize();
            Console.WriteLine("Hydra Test CreateUMFMessage called");
            Console.WriteLine(json);
        }


        public TestMsg? ParseTestMsg(string json)
        {
            return TestMsg.Deserialize<TestMsg>(json);

        }
        public PingMsg? ParsePingMsg(string json)
        {
            return PingMsg.Deserialize<PingMsg>(json);
        }
        public QueueMsg? ParseQueueMsg(string json)
        {
            return QueueMsg.Deserialize<QueueMsg>(json);
        }


        /**
         * UMF route parsing tests
         **/

        public void TestUMFParseRoutes()
        {
            //string to = "de571e9695c24c0eb12834ae5ee2f404-8u0f9wls7r@hydra-router:[get]/";
            string to = "de571e9695c24c0eb12834ae5ee2f404@hydra-router:[get]/";
            //string to = "hydra-router:[get]/";
            //string to = "hydra-router:/";
            UMFRouteEntry parsedEntry = UMF<TestMsg>.ParseRoute(to);
            Console.WriteLine(parsedEntry.ToString());
        }

        /**
         * Test presence
         */

        public async Task GetPresence(string serviceName)
        {
            List<PresenceNodeEntry>? entries = await _hydra.GetPresence(serviceName);
            if (entries.Count == 0)
            {
            }
        }


        /**
         * Send message
         */

        public async Task SendMessage()
        {
            PingMsg pingMessage = new();
            pingMessage.To = "hmr-service:/";
            pingMessage.Frm = $"{_hydra.InstanceID}@{_hydra.ServiceName}:/";
            pingMessage.Typ = "ping";
            string json = pingMessage.Serialize();
            await _hydra.SendMessage(pingMessage.To, json);
        }

        /** 
         * Messaging queuing
         */

        public async Task TestMessagingQueuing()
        {
            // Create and queue message
            QueueMsg queueMessage = new();
            queueMessage.To = "testrig-svcs:/";
            queueMessage.Frm = $"{_hydra.InstanceID}@{_hydra.ServiceName}:/";
            queueMessage.Typ = "job";
            queueMessage.Bdy.JobID = "1234";
            queueMessage.Bdy.JobType = "Sample Job";
            queueMessage.Bdy.JobData = "Test Data";
            await _hydra.QueueMessage(queueMessage.Serialize());

            // Retrieve queued message (dequeue)
            string json = await _hydra.GetQueueMessage("testrig-svcs");
            QueueMsg? qm = ParseQueueMsg(json);
            Console.WriteLine(qm?.Bdy.JobID);

            // Mark message as processed
            await _hydra.MarkQueueMessage(json, true);
        }
    }
}
