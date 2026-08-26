using System.Collections.Generic;

namespace TaskFlow
{
    public interface IHandler
    {
        public void Handle(Queue<Signal> context);
    }
}