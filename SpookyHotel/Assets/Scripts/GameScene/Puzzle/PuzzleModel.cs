using System;
using UnityEngine.TextCore.Text;

public class PuzzleModel
{
    public const int Columns = 3;
    public const int Rows = 4;
    public const int CellCount = Columns * Rows;

    private ClienteSO[] _cells = new ClienteSO[CellCount];
    public ClienteSO[] Cells => _cells;

    private Action _onChange;
    public void Subscribe(Action handler) => _onChange += handler;
    public void Unsubscribe(Action handler) => _onChange -= handler;
    public void RaiseChange() => _onChange?.Invoke();

    public ClienteSO[] SpawnQueue { get; set; }

    public void PlaceAt(int index, ClienteSO character)
    {
        if (index < 0 || index >= CellCount) return;
        _cells[index] = character;
        RaiseChange();
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= CellCount) return;
        _cells[index] = null;
        RaiseChange();
    }

    public int IndexOf(ClienteSO c)
    {
        for (int i = 0; i < _cells.Length; i++)
            if (_cells[i] == c) return i;
        return -1;
    }
}
