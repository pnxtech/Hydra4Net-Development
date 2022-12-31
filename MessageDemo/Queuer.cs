using Hydra4NET;
using MessageDemo.Models;

namespace MessageDemo
{
	public class Queuer
	{
		private Hydra _hydra;

		public Queuer(Hydra hydra)
		{
			_hydra = hydra;
		}

        public async Task ProcessMessage(string type, string message)
        {
        }
    }
}

