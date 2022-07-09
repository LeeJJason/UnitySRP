using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Conditional = System.Diagnostics.ConditionalAttribute;

public class MyPipeline : RenderPipeline
{
    bool dynamicBatching;
    bool instancing;

    const int maxVisibleLights = 4;

    static int visibleLightColorsId = Shader.PropertyToID("_VisibleLightColors");
    static int visibleLightDirectionsOrPositionsId = Shader.PropertyToID("_VisibleLightDirectionsOrPositions");
    static int visibleLightAttenuationsId = Shader.PropertyToID("_VisibleLightAttenuations");
    static int visibleLightSpotDirectionsId = Shader.PropertyToID("_VisibleLightSpotDirections");

    Vector4[] visibleLightColors = new Vector4[maxVisibleLights];
    Vector4[] visibleLightDirectionsOrPositions = new Vector4[maxVisibleLights];
    Vector4[] visibleLightAttenuations = new Vector4[maxVisibleLights];
    Vector4[] visibleLightSpotDirections = new Vector4[maxVisibleLights];

    public MyPipeline(bool dynamicBatching, bool instancing) 
    {
        GraphicsSettings.lightsUseLinearIntensity = true;
        this.dynamicBatching = dynamicBatching;
        this.instancing = instancing;
    }

    void ConfigureLights(ref CullingResults cull)
    {
        int i = 0;
        Vector4 attenuation = Vector4.zero;
        for (i = 0; i < cull.visibleLights.Length; i++)
        {
            if (i == maxVisibleLights)
            {
                break;
            }
            attenuation.w = 1;

            VisibleLight light = cull.visibleLights[i];
            visibleLightColors[i] = light.finalColor;
            
            if (light.lightType == LightType.Directional)
            {
                Vector4 v = light.localToWorldMatrix.GetColumn(2);
                v.x = -v.x;
                v.y = -v.y;
                v.z = -v.z;
                visibleLightDirectionsOrPositions[i] = v;
            }
            else
            {
                visibleLightDirectionsOrPositions[i] = light.localToWorldMatrix.GetColumn(3);
                attenuation.x = 1f / Mathf.Max(light.range * light.range, 0.00001f);

                if (light.lightType == LightType.Spot)
                {
                    Vector4 v = light.localToWorldMatrix.GetColumn(2);
                    v.x = -v.x;
                    v.y = -v.y;
                    v.z = -v.z;
                    visibleLightSpotDirections[i] = v;

                    float outerRad = Mathf.Deg2Rad * 0.5f * light.spotAngle;
                    float outerCos = Mathf.Cos(outerRad);
                    float outerTan = Mathf.Tan(outerRad);
                    float innerCos = Mathf.Cos(Mathf.Atan(((64f - 18f) / 64f) * outerTan));
                    float angleRange = Mathf.Max(innerCos - outerCos, 0.001f);
                    attenuation.z = 1f / angleRange;
                    attenuation.w = -outerCos * attenuation.z;
                }
            }
            visibleLightAttenuations[i] = attenuation;
        }

        for (; i < maxVisibleLights; i++)
        {
            visibleLightColors[i] = Color.clear;
        }
    }
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (var camera in cameras) 
        {
            Render(context, camera);
        }
    }
    CommandBuffer buffer = new CommandBuffer() { name = "Camera Render" };
    private void Render(ScriptableRenderContext context, Camera camera) 
    {
        
        //获取剔除参数，并执行剔除
        if(!camera.TryGetCullingParameters(out ScriptableCullingParameters parameters)) 
        {
            return;
        }
#if UNITY_EDITOR
        if (camera.cameraType == CameraType.SceneView)
        {
            // UI 渲染在 Game中单独处理，Scene 中需要单独处理
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }
#endif

        CullingResults cull = context.Cull(ref parameters);
        context.SetupCameraProperties(camera);

        CameraClearFlags flag = camera.clearFlags;
        //buffer.BeginSample("Buffer Sample");
        buffer.ClearRenderTarget((flag & CameraClearFlags.Depth) != 0, (flag & CameraClearFlags.Color) != 0, camera.backgroundColor);
        ConfigureLights(ref cull);
        // 将buffer中的clear 放在渲染相关同级
        buffer.BeginSample("Buffer Sample");

        buffer.SetGlobalVectorArray(visibleLightColorsId, visibleLightColors);
        buffer.SetGlobalVectorArray(visibleLightDirectionsOrPositionsId, visibleLightDirectionsOrPositions);
        buffer.SetGlobalVectorArray(visibleLightAttenuationsId, visibleLightAttenuations);
        buffer.SetGlobalVectorArray(visibleLightSpotDirectionsId, visibleLightSpotDirections);

        //buffer.EndSample("Buffer Sample");
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();

        //Tell Unity which geometry to draw, based on its LightMode Pass tag value
        ShaderTagId shaderTagId = new ShaderTagId("SRPDefaultUnlit");
        // Tell Unity how to sort the geometry, based on the current Camera
        var sortingSettings = new SortingSettings(camera);
        sortingSettings.criteria = SortingCriteria.CommonOpaque;
        var drawSettings = new DrawingSettings(shaderTagId, sortingSettings);
        drawSettings.enableDynamicBatching = dynamicBatching;
        drawSettings.enableInstancing = instancing;
        //Tell Unity how to filter the culling results, to further specify which geometry to draw. Use FilteringSettings.defaultValue to specify no filtering
        var filterSetting = new FilteringSettings(RenderQueueRange.opaque);
        context.DrawRenderers(cull, ref drawSettings, ref filterSetting);

        if (camera.clearFlags == CameraClearFlags.Skybox && RenderSettings.skybox != null)
        {
            context.DrawSkybox(camera);
        }

        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawSettings.enableDynamicBatching = true;

        filterSetting.renderQueueRange = RenderQueueRange.transparent;
        context.DrawRenderers(cull, ref drawSettings, ref filterSetting);

        DrawDefaultPipeline(context, camera, cull);

        buffer.EndSample("Buffer Sample");
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
        //Instruct the graphics API to perform all scheduled commands
        context.Submit();
    }

    Material errorMaterial;
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    private void DrawDefaultPipeline(ScriptableRenderContext context, Camera camera, CullingResults cull) 
    {
        if(errorMaterial == null) 
        {
            Shader errorShader = Shader.Find("Hidden/InternalErrorShader");
            errorMaterial = new Material(errorShader) { hideFlags = HideFlags.HideAndDontSave};
        }


        ShaderTagId shaderTagId = new ShaderTagId("ForwardBase");
        var sortingSettings = new SortingSettings(camera);
        var drawSettings = new DrawingSettings(shaderTagId, sortingSettings);
        drawSettings.SetShaderPassName(1, new ShaderTagId("PrepassBase"));
        drawSettings.SetShaderPassName(2, new ShaderTagId("Always"));
        drawSettings.SetShaderPassName(3, new ShaderTagId("Vertex"));
        drawSettings.SetShaderPassName(4, new ShaderTagId("VertexLMRGBM"));
        drawSettings.SetShaderPassName(5, new ShaderTagId("VertexLM"));
        drawSettings.overrideMaterial = errorMaterial;
        var filterSetting = FilteringSettings.defaultValue;
        context.DrawRenderers(cull, ref drawSettings, ref filterSetting);
    }
}
