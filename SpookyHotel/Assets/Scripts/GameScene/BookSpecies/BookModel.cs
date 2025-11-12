using System;
using UnityEngine;
using Infrastructure.MVC; // tu ModelBase / IModel

/// <summary>
/// Modelo que mantiene páginas, índice, estado abierto y notifica cambios con RaiseChange().
/// Hereda de ModelBase (Infrastructure.MVC.ModelBase) que define Subscribe/Unsubscribe/RaiseChange.
/// </summary>
public class BookModel : ModelBase
{
    private Sprite[] _pages;
    private int _currentIndex;
    private bool _isOpen;

    public bool IsOpen => _isOpen;
    public int CurrentIndex => _currentIndex;
    public int PageCount => _pages != null ? _pages.Length : 0;

    public Sprite CurrentPage => (_pages != null && _currentIndex >= 0 && _currentIndex < _pages.Length)
        ? _pages[_currentIndex]
        : null;

    public BookModel(Sprite[] pages)
    {
        _pages = pages ?? new Sprite[0];
        _currentIndex = 0;
        _isOpen = true;
    }

    public void Next()
    {
        if (_pages == null || _pages.Length == 0) return;
        _currentIndex = Mathf.Clamp(_currentIndex + 1, 0, _pages.Length - 1);
        RaiseChange();
    }

    public void Prev()
    {
        if (_pages == null || _pages.Length == 0) return;
        _currentIndex = Mathf.Clamp(_currentIndex - 1, 0, _pages.Length - 1);
        RaiseChange();
    }

    public void SetOpen(bool open)
    {
        _isOpen = open;
        RaiseChange();
    }

    // Permite setear manualmente páginas (si quieres cambiar libro sin reinstanciar modelo)
    public void SetPages(Sprite[] pages)
    {
        _pages = pages ?? new Sprite[0];
        _currentIndex = 0;
        RaiseChange();
    }
}
