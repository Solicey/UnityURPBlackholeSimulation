using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VolumeLighting : VolumeComponent , IPostProcessComponent
{
    [Range(0, 3)]
    public FloatParameter lightIntensity = new FloatParameter(0);
    public FloatParameter stepSize = new FloatParameter(0.1f);
    public FloatParameter maxDistance = new FloatParameter(1000);
    public IntParameter maxStep = new IntParameter(200);
    public bool IsActive() => lightIntensity.value>0;
    public bool IsTileCompatible() => false;
}