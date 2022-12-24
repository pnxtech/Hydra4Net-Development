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
    }
}
