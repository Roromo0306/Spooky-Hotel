using System;

public interface IModel
{
    void Subscribe(Action handler);
    void Unsubscribe(Action handler);
    void RaiseChange();
}
