using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class NormalLineFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Setting
    {
        public LayerMask layer;// Which layers to render
        public Material normalTexMat;// Material used to render normals
        public Material normalLineMat;// Material used to render normal lines
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPrePasses;// When to execute the render pass
        [Range(0.0f, 1.0f)]
        public float Edge = 0.5f;// Thickness of the normal lines
    }
    public Setting Settings = new Setting();
   
    public class DrawNormalTexturePass : ScriptableRenderPass
    {
        private Setting setting;
        ShaderTagId shaderTagId = new ShaderTagId("DepthOnly");// Use the "DepthOnly" pass to render normals
        FilteringSettings filter;
        NormalLineFeature feature;
        public DrawNormalTexturePass(Setting setting, NormalLineFeature feature)
        {
            this.setting = setting;
            this.feature = feature;

            RenderQueueRange renderQueueRange = new RenderQueueRange();
            renderQueueRange.lowerBound = 1000;
            renderQueueRange.upperBound = 3500;
            filter = new FilteringSettings(renderQueueRange, setting.layer);

            
        }



        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            base.Configure(cmd, cameraTextureDescriptor);
            int tempID = Shader.PropertyToID("_NormalTex");
            RenderTextureDescriptor desc = cameraTextureDescriptor;
            cmd.GetTemporaryRT(tempID, desc);

            // Use RTHandle to replace deprecated ConfigureTarget
            var rtHandle = UnityEngine.Rendering.RTHandles.Alloc(
                desc.width, desc.height, 1, DepthBits.None, desc.graphicsFormat,
                filterMode: FilterMode.Bilinear, wrapMode: TextureWrapMode.Clamp, name: "_NormalTex"
            );
            ConfigureTarget(rtHandle);
            ConfigureClear(ClearFlag.All, Color.black);
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
           CommandBuffer cmd = CommandBufferPool.Get("DrawNormalTexPass Draw Normal Texture");
            using (new ProfilingScope(cmd, new ProfilingSampler("DrawNormalTexPass")))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                var drawSettings = CreateDrawingSettings(shaderTagId, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
                drawSettings.overrideMaterial = setting.normalTexMat;
                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filter);
            }
            //context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
    public class DrawNormalLinePass : ScriptableRenderPass
    {
        private Setting setting;
        NormalLineFeature feature;

        public DrawNormalLinePass(Setting setting, NormalLineFeature feature)
        {
            this.setting = setting;
            this.feature = feature;
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
           
            cmd.ReleaseTemporaryRT(Shader.PropertyToID("_NormalLineTex"));
            cmd.ReleaseTemporaryRT(Shader.PropertyToID("_NormalTex"));
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("DrawNormalLinePass Draw Normal Lines");
            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
            setting.normalLineMat.SetFloat("_Edge", setting.Edge);
            int normalLineID = Shader.PropertyToID("_NormalLineTex");
            cmd.GetTemporaryRT(normalLineID, desc);
            cmd.Blit(normalLineID, normalLineID, setting.normalLineMat,0);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    private DrawNormalTexturePass _DrawNormalTexPass;
    private DrawNormalLinePass _DrawNormalLinePass; 
    /// <inheritdoc/>
    public override void Create()
    {
       _DrawNormalTexPass = new DrawNormalTexturePass(Settings, this);// Create the render pass
        _DrawNormalTexPass.renderPassEvent = Settings.renderPassEvent;// Set when to execute the render pass
        _DrawNormalLinePass = new DrawNormalLinePass(Settings, this);
        _DrawNormalLinePass.renderPassEvent = Settings.renderPassEvent;

    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_DrawNormalTexPass);// Enqueue the render pass
        renderer.EnqueuePass(_DrawNormalLinePass);
    }
}
