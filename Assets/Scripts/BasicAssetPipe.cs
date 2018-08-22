using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

[ExecuteInEditMode]
public class BasicAssetPipe : RenderPipelineAsset
{
    public Color clearColor = Color.green;

#if UNITY_EDITOR
    [UnityEditor.MenuItem("SRP/Create BasicAssetPipe asset")]
    static void CreateBasicAssetPipeline()
    {
        var instance = ScriptableObject.CreateInstance<BasicAssetPipe>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/Resources/BasicAssetPipe.asset");
    }
#endif

    protected override IRenderPipeline InternalCreatePipeline()
    {
        return new BasicPipeInstance(clearColor);
    }
}

public class BasicPipeInstance : RenderPipeline
{
    private Color m_ClearColor = Color.black;
    private CommandBuffer cmd;
    private ShaderPassName basicPass = new ShaderPassName("BasicPass");
    private CullResults cull;

    public BasicPipeInstance(Color clearColor)
    {
        m_ClearColor = clearColor;
    }

    public override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        base.Render(context, cameras);
        if (cmd == null)
        {
            cmd = new CommandBuffer();
        }

        foreach (var camera in cameras)
        {
            // culling
            ScriptableCullingParameters cullingParams;
            if (!CullResults.GetCullingParameters(camera, out cullingParams))
            {
                continue;
            }
            cull = CullResults.Cull(ref cullingParams, context);

            // setup camera
            context.SetupCameraProperties(camera);

            // clear buffer
            cmd.Clear();
            cmd.ClearRenderTarget(true, true, Color.black, 1.0f);
            context.ExecuteCommandBuffer(cmd);

            // setup lighting
            SetUpDirectionalLightParam(cull.visibleLights);

            // draw gameobjects
            DrawGameObjects(context, camera);

            // draw skybox
            context.DrawSkybox(camera);

            context.Submit();
        }
    }

    private void DrawGameObjects(ScriptableRenderContext context, Camera camera)
    {
        var settings = new DrawRendererSettings(camera, basicPass);
        settings.sorting.flags = SortFlags.CommonOpaque;

        var filterSettings = new FilterRenderersSettings(true)
        {
            renderQueueRange = RenderQueueRange.opaque
        };

        context.DrawRenderers(cull.visibleRenderers, ref settings, filterSettings);
    }

    // Directional Light parametor to shader
    private void SetUpDirectionalLightParam(List<VisibleLight> visibleLights)
    {
        if (visibleLights.Count <= 0)
        {
            return;
        }

        foreach (var visibleLight in visibleLights)
        {
            if (visibleLight.lightType == LightType.Directional)
            {
                Vector4 dir = -visibleLight.localToWorld.GetColumn(2);
                Shader.SetGlobalVector(Shader.PropertyToID("_LightColor0"), visibleLight.finalColor);
                Shader.SetGlobalVector(Shader.PropertyToID("_WorldSpaceLightPos0"), new Vector4(dir.x, dir.y, dir.z, 0.0f));
                break;
            }
        }
    }

    public override void Dispose()
    {
        base.Dispose();

        if (cmd != null)
        {
            cmd.Dispose();
            cmd = null;
        }
    }
}
