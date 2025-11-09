using UnityEngine;
using Infrastructure.MVC;

public class CameraModel : ModelBase
{
    public Vector3 TargetPosition;
    public CameraSettingsSO settings;

    public CameraModel(CameraSettingsSO settings, Vector3 initialPosition)
    {
        this.settings = settings;
        TargetPosition = initialPosition;
    }
}
