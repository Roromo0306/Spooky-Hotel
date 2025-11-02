using System.Collections.Generic;

public class ClienteManagerModel : ModelBase
{
    private int _currentIndex = -1;
    public int CurrentIndex => _currentIndex;

    private List<ClienteSO> _clientes = new List<ClienteSO>();
    public IReadOnlyList<ClienteSO> Clientes => _clientes.AsReadOnly();

    private bool _isProcessing = false;
    public bool IsProcessing => _isProcessing;

    public void SetQueue(IEnumerable<ClienteSO> clientes)
    {
        _clientes = new List<ClienteSO>(clientes);
        _currentIndex = -1;
        RaiseChange();
    }

    public void StartProcessing()
    {
        _isProcessing = true;
        RaiseChange();
    }

    public void StopProcessing()
    {
        _isProcessing = false;
        RaiseChange();
    }

    public void AdvanceIndex()
    {
        _currentIndex++;
        RaiseChange();
    }

    public ClienteSO? GetCurrentCliente()
    {
        if (_currentIndex >= 0 && _currentIndex < _clientes.Count)
            return _clientes[_currentIndex];
        return null;
    }

    public bool HasMore()
    {
        return (_currentIndex + 1) < _clientes.Count;
    }
}
