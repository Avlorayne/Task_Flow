namespace TaskFlow
{
    public interface IHandler
    {
        public void Handle(Signal signal);
    }
}