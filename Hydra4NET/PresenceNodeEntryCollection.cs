using System;
using System.Collections.Generic;
using static Hydra4NET.Hydra;

namespace Hydra4NET
{
    public class PresenceNodeEntryCollection : List<PresenceNodeEntry>
    {
        public PresenceNodeEntryCollection() : base() { }

        public PresenceNodeEntry? GetRandomEntry()
        {
            if (Count == 0)
                return null;
            return this[new Random().Next(Count)];
        }

        public void Shuffle()
        {
            if (Count == 0)
                return;
            // Shuffle array using Fisher-Yates shuffle
            // Leverage tuples for a quick swap ;-)
            Random rng = new Random();
            int n = Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (this[n], this[k]) = (this[k], this[n]);
            }
        }

    }
}
