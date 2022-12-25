using Hydra4NET;

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
            TestMsg myUMF = new();
            myUMF.Bdy.Msg = "New value";
            myUMF.Bdy.Id = 34;

            string json = myUMF.Serialize();
            Console.WriteLine("Hydra Test CreateUMFMessage called");
            Console.WriteLine(json);
        }

        /**
         * UMF route parsing tests
         **/

        public TestMsg? ParseTestMsg(string json)
        {
            return TestMsg.Deserialize<TestMsg>(json);
        }

        public void TestUMFParseRoutes()
        {
            //string to = "de571e9695c24c0eb12834ae5ee2f404-8u0f9wls7r@hydra-router:[get]/";
            string to = "de571e9695c24c0eb12834ae5ee2f404@hydra-router:[get]/";
            //string to = "hydra-router:[get]/";
            //string to = "hydra-router:/";
            UMFRouteEntry parsedEntry = UMF<TestMsg>.ParseRoute(to);
            Console.WriteLine(parsedEntry.ToString());
        }
    }
}   
