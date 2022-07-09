using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/MyPipelineAsset")]
public class MyPipelineAsset : RenderPipelineAsset
{
    [SerializeField]
    private bool dynamicBatching;

    [SerializeField]
    private bool instancing;
    protected override RenderPipeline CreatePipeline()
    {
        return new MyPipeline(dynamicBatching, instancing);
    }
}
