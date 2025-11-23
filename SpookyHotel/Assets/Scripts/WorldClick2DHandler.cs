using UnityEngine;

/// <summary>
/// Detecta clicks del ratón en el mundo 2D y lanza InvokeClick()
/// en los DocumentWorldView que haya bajo el cursor.
/// </summary>
public class WorldClick2DHandler : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void Update()
    {
        // Botón izquierdo del ratón
        if (Input.GetMouseButtonDown(0))
        {
            if (targetCamera == null) return;

            Vector3 mousePos = Input.mousePosition;
            Vector3 worldPos = targetCamera.ScreenToWorldPoint(mousePos);
            Vector2 worldPos2D = new Vector2(worldPos.x, worldPos.y);

            // Raycast 2D en el punto del click
            RaycastHit2D hit = Physics2D.Raycast(worldPos2D, Vector2.zero);

            if (hit.collider != null)
            {
                // Intentamos obtener un DocumentWorldView en el objeto clicado
                var docView = hit.collider.GetComponent<DocumentWorldView>();
                if (docView != null)
                {
                    docView.InvokeClick();
                }
            }
        }
    }
}
