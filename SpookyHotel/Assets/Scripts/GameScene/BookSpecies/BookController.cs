using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controller que conecta View <-> Model y expone API para abrir libros.
/// Intenta resolver el IBookService por ServiceLocator si no se le asigna via inspector.
/// </summary>
public class BookController : MonoBehaviour
{
    [Header("View (assign)")]
    public BookView view;

    [Header("Open button (book icon)")]
    public Button openBookButton;

    [Header("Service (optional)")]
    public MonoBehaviour bookServiceBehaviour; // if you don't have ServiceLocator, drag a component that implements IBookService
    private IBookService _bookService;

    [Header("Optional: assign a BookDataSO to open when the icon is clicked")]
    public BookDataSO defaultBook;

    private BookModel _model;

    private void Awake()
    {
        // Resolve service: first try inspector field
        if (bookServiceBehaviour != null && bookServiceBehaviour is IBookService)
        {
            _bookService = bookServiceBehaviour as IBookService;
        }
        else
        {
            // optional: attempt to resolve via a ServiceLocator (uncomment if you have it)
            // _bookService = ServiceLocator.Get<IBookService>();
        }

        if (openBookButton != null)
        {
            openBookButton.onClick.RemoveAllListeners();
            openBookButton.onClick.AddListener(OnOpenButtonClicked);
        }

        // Wire view buttons
        if (view != null)
        {
            if (view.prevButton != null) { view.prevButton.onClick.RemoveAllListeners(); view.prevButton.onClick.AddListener(OnPrev); }
            if (view.nextButton != null) { view.nextButton.onClick.RemoveAllListeners(); view.nextButton.onClick.AddListener(OnNext); }
            if (view.closeButton != null) { view.closeButton.onClick.RemoveAllListeners(); view.closeButton.onClick.AddListener(OnClose); }
        }
    }

    private void OnDestroy()
    {
        if (openBookButton != null) openBookButton.onClick.RemoveAllListeners();
        if (view != null)
        {
            if (view.prevButton != null) view.prevButton.onClick.RemoveAllListeners();
            if (view.nextButton != null) view.nextButton.onClick.RemoveAllListeners();
            if (view.closeButton != null) view.closeButton.onClick.RemoveAllListeners();
        }
        UnsubscribeModel();
    }

    // ------------ NUEVO MÉTODO PÚBLICO ------------

    /// <summary>
    /// Abre el libro por defecto (defaultBook), usando el servicio si existe.
    /// Este método lo puede llamar tanto el botón UI como el objeto del mundo.
    /// </summary>
    public void OpenDefaultBook()
    {
        if (defaultBook == null)
        {
            Debug.LogWarning("[BookController] OpenDefaultBook llamado pero defaultBook es null. Asigna un BookDataSO en el inspector.");
            return;
        }

        // Si tenemos servicio, se lo delegamos
        if (_bookService != null)
        {
            _bookService.OpenBook(defaultBook);

            // Obtener modelo del servicio
            _model = _bookService.GetModel();

            // Suscribir y refrescar vista
            SubscribeModel();
            RefreshViewFromModel();
        }
        else
        {
            // Fallback: abrir directamente sin servicio
            OpenBookDirectly(defaultBook);
        }
    }

    // ----------------------------------------------

    // Antes el botón hacía todo. Ahora solo llama al método público.
    private void OnOpenButtonClicked()
    {
        OpenDefaultBook();
    }

    // Convenience API if you want to open a specific BookDataSO directly from this controller
    public void OpenBookDirectly(BookDataSO book)
    {
        if (book == null) return;
        // create local model and show
        UnsubscribeModel();
        _model = new BookModel(book.pages);
        SubscribeModel();
        RefreshViewFromModel();
    }

    private void SubscribeModel()
    {
        UnsubscribeModel();
        if (_model != null)
        {
            _model.Subscribe(OnModelChanged);
        }
    }

    private void UnsubscribeModel()
    {
        if (_model != null)
        {
            _model.Unsubscribe(OnModelChanged);
        }
    }

    private void OnModelChanged()
    {
        RefreshViewFromModel();
    }

    private void RefreshViewFromModel()
    {
        if (view == null) return;
        if (_model == null || !_model.IsOpen)
        {
            view.Hide();
            return;
        }

        view.Show();
        view.SetPage(_model.CurrentPage);
        view.SetNavEnabled(_model.CurrentIndex > 0, _model.CurrentIndex < _model.PageCount - 1);
    }

    private void OnNext()
    {
        if (_model != null) _model.Next();
    }

    private void OnPrev()
    {
        if (_model != null) _model.Prev();
    }

    private void OnClose()
    {
        if (_model != null) _model.SetOpen(false);
        UnsubscribeModel();
        if (view != null) view.Hide();
    }
}
