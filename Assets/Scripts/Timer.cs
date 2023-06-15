using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Timer : MonoBehaviour
{
    private Material material;
    
    private int totalTimeId = Shader.PropertyToID("_TotalTime");
    private float totalTime = 0;
    
    // Start is called before the first frame update
    void Start()
    {
        material = Resources.Load<Material>("Blackhole");
    }

    // Update is called once per frame
    void Update()
    {
        totalTime += Time.deltaTime;
        material.SetFloat(totalTimeId, totalTime);
    }
}
