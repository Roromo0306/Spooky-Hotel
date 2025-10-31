using System;
using System.Threading.Tasks;
using UnityEngine;

public abstract class ControllerBase<T> : MonoBehaviour, IController<T> where T : class, IModel
{
    private T _model;
    public T Model
    {
        get => _model;
        protected set => SetModel(value);
    }

    public bool IsStarted { get; private set; }

    [SerializeField] private bool startOnAwake = true;

    private void SetModel(T newModel)
    {
        if (newModel == _model) return;

        if (_model != null)
            _model.Unsubscribe(OnModelChangeInternal);

        _model = newModel;

        if (_model != null)
        {
            _model.Subscribe(OnModelChangeInternal);
            OnModelChangeInternal();
        }
    }

    private void OnModelChangeInternal() => OnModelChange();

    protected abstract Task OnStartController();
    protected abstract void OnModelChange();
    protected abstract void OnDestroyController();

    protected virtual async void Awake()
    {
        IsStarted = false;
        if (startOnAwake)
            await StartController();
    }

    public async Task StartController()
    {
        await OnStartController();
        IsStarted = true;
    }

    protected virtual void OnDestroy()
    {
        IsStarted = false;
        if (_model != null)
            _model.Unsubscribe(OnModelChangeInternal);
        OnDestroyController();
    }
}

