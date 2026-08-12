namespace TaskFlow
{
    public abstract class Detection
    {
        public abstract bool Result { get; }

        protected DetectionContext Context;
    }
}