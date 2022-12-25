using Hydra4NET;

namespace TestRig
{
    public class TestMsgBody
    {
        public string? Msg { get; set; }
        public int? Id { get; set; }
    }

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

        public void CreateUMFMessage()
        {
            TestMsg myUMF = new();
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
