using System;

public abstract class ModelBase : IModel
{
    private Action _onChange;
    public virtual void Subscribe(Action handler) => _onChange += handler;
    public virtual void Unsubscribe(Action handler) => _onChange -= handler;
    public virtual void RaiseChange() => _onChange?.Invoke();
}

