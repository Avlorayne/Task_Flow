namespace TaskFlow.Detection
{
    public abstract class Detection
    {
        public abstract bool Result();

        protected SignalContext Context;
    }
}