using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

internal class CurvaturePass : ScriptableRenderPass
{
    internal Material curvatureMaterial;
    
    public FilterMode filterMode { get; set; }
    public CurvatureFeature.Settings settings;

    RenderTargetIdentifier source;
    RenderTargetIdentifier destination;
    int temporaryRTId = Shader.PropertyToID("_TempRT");

    int sourceId;
    int destinationId;

    string m_ProfilerTag;

    public CurvaturePass(string tag)
    {
        m_ProfilerTag = tag;
        ConfigureInput(ScriptableRenderPassInput.Normal);
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        RenderTextureDescriptor blitTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        blitTargetDescriptor.depthBufferBits = 0;

        var renderer = renderingData.cameraData.renderer;

        sourceId = -1;
        source = renderer.cameraColorTarget;
        destinationId = temporaryRTId;
        cmd.GetTemporaryRT(destinationId, blitTargetDescriptor, filterMode);
        destination = new RenderTargetIdentifier(destinationId);
    }

    /// <inheritdoc/>
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

        Blit(cmd, source, destination, curvatureMaterial, settings.blitMaterialPassIndex);
        Blit(cmd, destination, source);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    /// <inheritdoc/>
    public override void FrameCleanup(CommandBuffer cmd)
    {
        if (destinationId != -1)
            cmd.ReleaseTemporaryRT(destinationId);

        if (source == destination && sourceId != -1)
            cmd.ReleaseTemporaryRT(sourceId);
    }
}

public class CurvatureFeature : ScriptableRendererFeature
{
    private const string CurvatureMaterialPath = "Packages/com.branchpanic.curvature/Runtime/Materials/Curvature.mat";
    
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        public int blitMaterialPassIndex = -1;
    }

    public Settings settings = new Settings();
    private CurvaturePass blitPass;

    public override void Create()
    {
        blitPass = new CurvaturePass(name);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        blitPass.curvatureMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(CurvatureMaterialPath);

        blitPass.renderPassEvent = settings.renderPassEvent;
        blitPass.settings = settings;
        renderer.EnqueuePass(blitPass);
    }
}