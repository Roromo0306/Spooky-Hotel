using UnityEngine;

[CreateAssetMenu(fileName = "CameraSettings", menuName = "Camera/Settings")]
public class CameraSettingsSO : ScriptableObject
{
    [Header("Velocidad y sensibilidad")]
    public float moveSpeed = 5f;
    public float mouseSensitivity = 0.1f;

    [Header("Límites de movimiento")]
    public Vector3 minLimits = new Vector3(-10f, 5f, -10f);
    public Vector3 maxLimits = new Vector3(10f, 15f, 10f);
}
