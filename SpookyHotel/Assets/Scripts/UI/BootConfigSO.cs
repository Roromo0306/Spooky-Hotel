using UnityEngine;

[CreateAssetMenu(menuName = "Config/BootConfig", fileName = "BootConfig")]
public class BootConfigSO : ScriptableObject
{
    [Tooltip("Nombre de la siguiente escena que el Bootstrapper cargará.")]
    public string nextScene;
}
