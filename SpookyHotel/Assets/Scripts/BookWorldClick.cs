using UnityEngine;

/// <summary>
/// Componente para un libro en el mundo 2D.
/// Al hacer click sobre el objeto (con Collider2D), llama a BookController.OpenDefaultBook().
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BookWorldClick : MonoBehaviour
{
    [Header("Referencia al controlador del libro (UI/servicio)")]
    public BookController bookController;

    private void OnMouseDown()
    {
        if (bookController == null)
        {
            Debug.LogWarning("[BookWorldClick] bookController no asignado en " + name);
            return;
        }

        bookController.OpenDefaultBook();
    }
}
