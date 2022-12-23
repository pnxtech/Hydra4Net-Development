using Hydra4NET;

namespace TestRig
{
    public class MyUMFMessage: UMF
    {
        public MyUMFMessage()
        {
            To = "hydra-router:/";
            From = "TestRig:/";
        }
    }

    public class MyMessageBody
    {
        public string? Field1 { get; set; }

        public void Body()
        {
            Field1 = "test";
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
            MyUMFMessage myUMF = new()
            {
                Body = new MyMessageBody()
                {
                    Field1 = "New value"
                }
            };
            var json = UMF.Serialize(myUMF);
            Console.WriteLine("Hydra Test CreateUMFMessage called");
            Console.WriteLine(json);
        }
    }
}
