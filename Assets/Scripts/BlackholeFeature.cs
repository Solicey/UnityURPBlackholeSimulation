using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BlackholeFeature : ScriptableRendererFeature
{
    public Material material;
    
    class BlackholePass : ScriptableRenderPass
    {
        private static readonly string s_RenderTag = "Blackhole Post Effects";
        private static readonly int s_TempRTId = Shader.PropertyToID("_TempRT");
        private static readonly int s_TotalTimeId = Shader.PropertyToID("_TotalTime");
        
        private RenderTargetIdentifier currTarget;
        private Material material;
        private float totalTime = 0.0f;
        
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            material = Resources.Load<Material>("Blackhole");
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!renderingData.cameraData.postProcessEnabled) return;
            var stk = VolumeManager.instance.stack;
            Blackhole blackhole = stk.GetComponent<Blackhole>();

            if (blackhole == null)
            {
                return;
            }

            if (!blackhole.IsActive())
            {
                return;
            }

            var cmd = CommandBufferPool.Get(s_RenderTag);

            ref var cameraData = ref renderingData.cameraData;
            var width = cameraData.camera.scaledPixelWidth;
            var height = cameraData.camera.scaledPixelHeight;
            
            // material.SetFloat(s_TotalTimeId, 0);
            cmd.GetTemporaryRT(s_TempRTId, width, height);
            cmd.Blit(currTarget, s_TempRTId);
            cmd.Blit(s_TempRTId, currTarget, material, 0);
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }

        public void Setup(in RenderTargetIdentifier curr, in Material material)
        {
            this.currTarget = curr;
            this.material = material;
        }
    }

    BlackholePass m_BlackholePass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_BlackholePass = new BlackholePass();

        // Configures where the render pass should be injected.
        m_BlackholePass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        m_BlackholePass.Setup(renderer.cameraColorTarget, material);
        renderer.EnqueuePass(m_BlackholePass);
    }
}


