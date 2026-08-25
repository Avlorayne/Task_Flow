using System;
using System.Collections.Generic;
using UnityEngine;

namespace TaskFlow
{
    public abstract class Signal : IDisposable
    {
        // 要写对象池，但是会有何时释放的问题： 帧结束后没有被引用的就要被回收？怎么标记是否有被引用捕捉？

        public void Dispose()
        {
            // TODO 在此释放托管资源
        }
    }
}
