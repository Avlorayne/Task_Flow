using System;
using System.Collections.Generic;

namespace TaskFlow
{
    public class Signal : IDisposable
    {
        public Dictionary<string, string> PropertyHeader;

        public void Dispose()
        {
            // TODO 在此释放托管资源
        }
    }
}
