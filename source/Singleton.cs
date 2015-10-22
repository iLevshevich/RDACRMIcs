using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace RDACRMIcs
{
    public sealed class Singleton<T> 
        where T: class, new()
    {
        private static readonly Object s_lock = new Object();
        private static T instance = null;

        private Singleton()
        {
        }

        public static T Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                Monitor.Enter(s_lock);
                {
                    T temp = new T();
                    Interlocked.Exchange(ref instance, temp);
                }
                Monitor.Exit(s_lock);

                return instance;
            }
        }
    }
}
