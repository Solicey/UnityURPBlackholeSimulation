using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Blackhole : VolumeComponent, IPostProcessComponent
{
    public bool IsActive()
    {
        return active;
    }

    public bool IsTileCompatible() => false;
}
