public interface IBookService
{
    // Pedir que se abra un libro (por ejemplo desde UI o desde gameplay)
    void OpenBook(BookDataSO bookData);

    // Cerrar el libro activo
    void CloseBook();

    // Consulta si hay libro abierto
    bool IsOpen { get; }

    // Obtener modelo actual (opcional) - para inyección/observadores
    BookModel GetModel();
}
