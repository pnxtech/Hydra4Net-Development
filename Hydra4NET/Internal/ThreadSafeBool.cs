using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Hydra4NET.Internal
{
    internal class ThreadSafeBool
    {
        private const int FALSE_VAL = 0;
        private const int TRUE_VAL = 1;
        private int _val; //defaults to FALSE_VAL (0)

        public bool Value
        {
            //read or set in thread-safe manner
            get => Interlocked.CompareExchange(ref _val, TRUE_VAL, TRUE_VAL) == TRUE_VAL;
            set
            {
                if (value)
                    Interlocked.CompareExchange(ref _val, TRUE_VAL, FALSE_VAL);
                else
                    Interlocked.CompareExchange(ref _val, FALSE_VAL, TRUE_VAL);
            }
        }

        public static implicit operator ThreadSafeBool(bool v) => new ThreadSafeBool { Value = v };
    }
}
