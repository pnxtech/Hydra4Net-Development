using Hydra4NET;
using System.Text.Json;

namespace HydraTests
{
    public class MyUMFMessage: UMFBaseMessage
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

        public Tests(ref Hydra hydra)
        {
            _hydra = hydra;
        }

        public void CreateUMFMessage()
        {
            MyUMFMessage myUMF = new MyUMFMessage();
            //myUMF.Body = new MyMessageBody();
            Console.WriteLine("Hydra Test CreateUMFMessage called");
            Console.WriteLine(JsonSerializer.Serialize(myUMF));
        }
    }
}
