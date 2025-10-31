public interface IController<T> where T : IModel
{
    T Model { get; }
    bool IsStarted { get; }
}

