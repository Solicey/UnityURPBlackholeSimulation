using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VolumeLightingFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class LightingEventSetting
    {
        public RenderPassEvent Event = RenderPassEvent.BeforeRenderingPostProcessing;
    }
    
    public LightingEventSetting settings = new LightingEventSetting();

    class VolumeLightingPass : ScriptableRenderPass
    {
        RenderTargetIdentifier 
            currentTarget;
        VolumeLighting volumeLighting;
        Material volumeLightingMaterial;

        static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        static readonly int TempTargetId = Shader.PropertyToID("_TempTargetVolumLighting");
        static readonly int MaxStepId = Shader.PropertyToID("_MaxStep");
        static readonly int MaxDistanceId = Shader.PropertyToID("_MaxDistance");
        static readonly int StepSizeId = Shader.PropertyToID("_StepSize");
        static readonly int LightIntensityId = Shader.PropertyToID("_LightIntensity");

        static readonly string k_RenderTag = "Render Volume Lighting Effects";

        public VolumeLightingPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
            var shader = Shader.Find("PostEffect/VolumeLighting");
            if (shader == null)
            {
                Debug.LogError("PostEffect/VolumeLighting路径下无法找到着色器");
                return;
            }
            volumeLightingMaterial = CoreUtils.CreateEngineMaterial(shader);
        }
        
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!renderingData.cameraData.postProcessEnabled) return;
            var stac = VolumeManager.instance.stack;
            volumeLighting = stac.GetComponent<VolumeLighting>();
            if (volumeLighting == null)
            {
                Debug.LogError("VolumeLighting为空");
                return;
            }
            
            if (!volumeLighting.IsActive())
            {
                return;
            }
            var cmd = CommandBufferPool.Get(k_RenderTag);
            Render(cmd, ref renderingData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
        
        public void Setup(in RenderTargetIdentifier currentTarget)
        {
            this.currentTarget = currentTarget;
        }

        void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ref var cameraData = ref renderingData.cameraData;
            var source = currentTarget;
            int destination = TempTargetId;

            var w = cameraData.camera.scaledPixelWidth;
            var h = cameraData.camera.scaledPixelHeight;

            volumeLightingMaterial.SetInt(MaxStepId, volumeLighting.maxStep.value);
            volumeLightingMaterial.SetFloat(MaxDistanceId, volumeLighting.maxDistance.value);
            volumeLightingMaterial.SetFloat(StepSizeId, volumeLighting.stepSize.value);
            volumeLightingMaterial.SetFloat(LightIntensityId, volumeLighting.lightIntensity.value);
            int shaderPass = 0;
            cmd.SetGlobalTexture(MainTexId, source);
            cmd.GetTemporaryRT(destination, w, h, 0, FilterMode.Point, RenderTextureFormat.Default);
            cmd.Blit(source, destination);
            cmd.Blit(destination, source, volumeLightingMaterial, shaderPass);
        }

    }
    
    VolumeLightingPass volumeLightingPass;
    
    public override void Create()
    {
        volumeLightingPass = new VolumeLightingPass(settings.Event);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        volumeLightingPass.Setup(renderer.cameraColorTarget);
        renderer.EnqueuePass(volumeLightingPass);
    }

}