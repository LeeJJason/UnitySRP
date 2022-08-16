using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace Copy
{
    public class CustomRenderPipeline : RenderPipeline
    {
        CameraRenderer renderer = new CameraRenderer();
        StringBuilder builder = new StringBuilder();
        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            foreach (Camera camera in cameras)
            {
                renderer.Render(context, camera);
            }
            //PritCamera(cameras);
        }

        private void PritCamera(Camera[] cameras) 
        {
            builder.Clear();
            builder.Append($"Camera Count : {cameras.Length}\n");
            foreach (Camera camera in cameras)
            {
                builder.AppendFormat("\t{0,-20}=>{1,-20}, {2}\n", camera.name, camera.cameraType, camera.hideFlags);
            }
            Debug.LogError(builder);
        }
    }
}