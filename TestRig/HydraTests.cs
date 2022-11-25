using Hydra4NET;

namespace HydraTests
{
    public class Tests
    {
        private ref Hydra hydraObj;

        public Tests(ref Hydra? hydra)
        {
            hydraObj = hydra;
        }

        public void TestFunction1()
        {
            Console.WriteLine("Hydra Test TestFunction1 called");
        }
    }
}
