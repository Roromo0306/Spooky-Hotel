using UnityEngine;

[CreateAssetMenu(menuName = "Game/Habilidad", fileName = "HabilidadSO")]
public class AbilitySO : ScriptableObject
{
    [TextArea(1, 3)]
    public string descripcion;
    // Añade parámetros cuando definamos habilidades concretas.
}
