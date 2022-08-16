using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Copy
{
	public partial class CameraRenderer
	{
		// Tell Unity which geometry to draw, based on its LightMode Pass tag value
		static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
		static ShaderTagId copyUnlitShaderTagId = new ShaderTagId("CopySRPDefaultUnlit");
		
		ScriptableRenderContext context;
		CullingResults cullingResults;

		Camera camera;
		const string bufferName = "Render Camera";

		CommandBuffer buffer = new CommandBuffer
		{
			name = bufferName
		};

		public void Render(ScriptableRenderContext context, Camera camera)
		{
			this.context = context;
			this.camera = camera;

			PrepareBuffer();
			PrepareForSceneWindow();

			if (!Cull())
				return;
			
			Setup();
			DrawVisibleGeometry();
			DrawUnsupportedShaders();
			DrawGizmos();
			Submit();
		}

		void DrawVisibleGeometry()
		{
			var sortingSettings = new SortingSettings(camera)
			{
				criteria = SortingCriteria.CommonOpaque
			};
			var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings);
			drawingSettings.SetShaderPassName(1, copyUnlitShaderTagId);

			var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
			context.DrawRenderers(
				cullingResults, ref drawingSettings, ref filteringSettings
			);

			context.DrawSkybox(camera);

			sortingSettings.criteria = SortingCriteria.CommonTransparent;
			drawingSettings.sortingSettings = sortingSettings;
			filteringSettings.renderQueueRange = RenderQueueRange.transparent;

			context.DrawRenderers(
				cullingResults, ref drawingSettings, ref filteringSettings
			);
		}

		
		/// <summary>
		/// 首先设置
		/// </summary>
		void Setup()
		{
			context.SetupCameraProperties(camera);
			CameraClearFlags flags = camera.clearFlags;
			buffer.ClearRenderTarget(flags <= CameraClearFlags.Depth, flags <= CameraClearFlags.Color, flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
			buffer.BeginSample(SampleName);
			
			ExecuteBuffer();

		}

		bool Cull()
		{
			if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
			{
				cullingResults = context.Cull(ref p);
				return true;
			}
			return false;
		}
		void ExecuteBuffer()
		{
			context.ExecuteCommandBuffer(buffer);
			buffer.Clear();
		}
		void Submit()
		{
			buffer.EndSample(SampleName);
			ExecuteBuffer();
			context.Submit();
		}
	}
}