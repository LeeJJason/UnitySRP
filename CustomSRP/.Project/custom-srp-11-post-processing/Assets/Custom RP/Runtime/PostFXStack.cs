using UnityEngine;
using UnityEngine.Rendering;

public partial class PostFXStack {

	public enum Pass {
		BloomCombine,
		AdditiveBlurring,
		BloomHorizontal,
		BloomHorizontalE,
		BloomPrefilter,
		BloomVertical,
		BloomVerticalE,
		Copy
	}

	const string bufferName = "Post FX";

	const int maxBloomPyramidLevels = 16;

	int
		bloomBicubicUpsamplingId = Shader.PropertyToID("_BloomBicubicUpsampling"),
		bloomIntensityId = Shader.PropertyToID("_BloomIntensity"),
		bloomPrefilterId = Shader.PropertyToID("_BloomPrefilter"),
		bloomThresholdId = Shader.PropertyToID("_BloomThreshold"),
		fxSourceId = Shader.PropertyToID("_PostFXSource"),
		fxSource2Id = Shader.PropertyToID("_PostFXSource2");

	CommandBuffer buffer = new CommandBuffer {
		name = bufferName
	};

	ScriptableRenderContext context;

	Camera camera;

	PostFXSettings settings;

	int bloomPyramidId;

	public bool IsActive => settings != null;

	public PostFXStack () {
		bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
		for (int i = 1; i < maxBloomPyramidLevels * 2; i++) {
			Shader.PropertyToID("_BloomPyramid" + i);
		}
	}

	public void Setup (
		ScriptableRenderContext context, Camera camera, PostFXSettings settings
	) {
		this.context = context;
		this.camera = camera;
		this.settings =
			camera.cameraType <= CameraType.SceneView ? settings : null;
		ApplySceneViewState();
	}

	public void Render (int sourceId) {
		//Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
		//DoDownsampling(sourceId);
		//DoGaussian(sourceId);
		//DoAdditiveBlurring(sourceId);
		DoHalfResolution(sourceId);
		//DoBloom(sourceId);
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}

	void DoDownsampling(int sourceId)
	{
		buffer.BeginSample("Bloom");
		PostFXSettings.BloomSettings bloom = settings.Bloom;
		int width = camera.pixelWidth / 2, height = camera.pixelHeight / 2;
		RenderTextureFormat format = RenderTextureFormat.Default;
		int fromId = sourceId, toId = bloomPyramidId;
		int i;
		for (i = 0; i < bloom.maxIterations; i++)
		{
			if (height < bloom.downscaleLimit || width < bloom.downscaleLimit)
			{
				break;
			}
			buffer.GetTemporaryRT(
				toId, width, height, 0, FilterMode.Point, format
			);
			Draw(fromId, toId, Pass.Copy);
			fromId = toId;
			toId += 1;
			width /= 2;
			height /= 2;
		}

		Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);

		for (i -= 1; i >= 0; i--)
		{
			buffer.ReleaseTemporaryRT(bloomPyramidId + i);
		}
		buffer.EndSample("Bloom");
	}

	void DoGaussian(int sourceId) 
	{
		buffer.BeginSample("Bloom");
		PostFXSettings.BloomSettings bloom = settings.Bloom;
		int width = camera.pixelWidth / bloom.ScaleSize, height = camera.pixelHeight / bloom.ScaleSize;
		RenderTextureFormat format = RenderTextureFormat.Default;
		int fromId = sourceId, toId = bloomPyramidId + 1;
		int i;
		for (i = 0; i < bloom.maxIterations; i++)
		{
			if (height < bloom.downscaleLimit || width < bloom.downscaleLimit)
			{
				break;
			}

			int midId = toId - 1;
			buffer.GetTemporaryRT(
				midId, width, height, 0, FilterMode.Bilinear, format
			);

			buffer.GetTemporaryRT(
				toId, width, height, 0, FilterMode.Bilinear, format
			);

			Draw(fromId, midId, bloom.HorizontalExtend ? Pass.BloomHorizontalE : Pass.BloomHorizontal);
			Draw(midId, toId, bloom.VerticalExtend ? Pass.BloomVerticalE : Pass.BloomVertical);
			fromId = toId;
			toId += 2;
			width /= bloom.ScaleSize;
			height /= bloom.ScaleSize;
		}

		Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);

		for (i -= 1; i >= 0; i--)
		{
			buffer.ReleaseTemporaryRT(fromId);
			buffer.ReleaseTemporaryRT(fromId - 1);
			fromId -= 2;
		}
		buffer.EndSample("Bloom");
	}

	void DoAdditiveBlurring(int sourceId)
	{
		buffer.BeginSample("Bloom");
		PostFXSettings.BloomSettings bloom = settings.Bloom;
		int width = camera.pixelWidth / bloom.ScaleSize, height = camera.pixelHeight / bloom.ScaleSize;
		if (bloom.maxIterations == 0 || height < bloom.downscaleLimit || width < bloom.downscaleLimit)
		{
			Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
			buffer.EndSample("Bloom");
			return;
		}

		RenderTextureFormat format = RenderTextureFormat.Default;
		int fromId = sourceId, toId = bloomPyramidId + 1;
		int i;
		for (i = 0; i < bloom.maxIterations; i++)
		{
			if (height < bloom.downscaleLimit || width < bloom.downscaleLimit)
			{
				break;
			}

			int midId = toId - 1;
			buffer.GetTemporaryRT(
				midId, width, height, 0, FilterMode.Bilinear, format
			);

			buffer.GetTemporaryRT(
				toId, width, height, 0, FilterMode.Bilinear, format
			);

			Draw(fromId, midId, bloom.HorizontalExtend ? Pass.BloomHorizontalE : Pass.BloomHorizontal);
			Draw(midId, toId, bloom.VerticalExtend ? Pass.BloomVerticalE : Pass.BloomVertical);
			fromId = toId;
			toId += 2;
			width /= bloom.ScaleSize;
			height /= bloom.ScaleSize;
		}

		//Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
		buffer.SetGlobalFloat(
			bloomBicubicUpsamplingId, bloom.bicubicUpsampling ? 1f : 0f
		);
		if (i > 1)
		{
			//释放最后一次迭代的水平绘制所用的纹理
			buffer.ReleaseTemporaryRT(fromId - 1);
			//目的地设置为低一级的水平绘制所用的纹理
			toId -= 5;

			for (i -= 1; i > 0; i--)
			{
				//设置上一级的垂直绘制纹理作为第二输入
				buffer.SetGlobalTexture(fxSource2Id, toId + 1);
				//fromId 为当前级的输出作为输入，toId 为上一级的水平绘制纹理作为输出目标纹理
				Draw(fromId, toId, Pass.AdditiveBlurring);
				buffer.ReleaseTemporaryRT(fromId);
				buffer.ReleaseTemporaryRT(toId + 1);
				fromId = toId;
				toId -= 2;
			}
		}
		else
		{
			buffer.ReleaseTemporaryRT(bloomPyramidId);
		}

		buffer.SetGlobalTexture(fxSource2Id, sourceId);
		Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.AdditiveBlurring);
		buffer.ReleaseTemporaryRT(fromId);
		buffer.EndSample("Bloom");
	}

	void DoHalfResolution(int sourceId)
	{
		buffer.BeginSample("Bloom");
		PostFXSettings.BloomSettings bloom = settings.Bloom;
		int width = camera.pixelWidth / bloom.ScaleSize, height = camera.pixelHeight / bloom.ScaleSize;
		if (bloom.maxIterations == 0 || height < bloom.downscaleLimit * 2 || width < bloom.downscaleLimit * 2)
		{
			Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
			buffer.EndSample("Bloom");
			return;
		}

		RenderTextureFormat format = RenderTextureFormat.Default;
		buffer.GetTemporaryRT(
			bloomPrefilterId, width, height, 0, FilterMode.Bilinear, format
		);
		Draw(sourceId, bloomPrefilterId, Pass.Copy);
		width /= 2;
		height /= 2;

		int fromId = bloomPrefilterId, toId = bloomPyramidId + 1;
		int i;
		for (i = 0; i < bloom.maxIterations; i++)
		{
			if (height < bloom.downscaleLimit || width < bloom.downscaleLimit)
			{
				break;
			}

			int midId = toId - 1;
			buffer.GetTemporaryRT(
				midId, width, height, 0, FilterMode.Bilinear, format
			);

			buffer.GetTemporaryRT(
				toId, width, height, 0, FilterMode.Bilinear, format
			);

			Draw(fromId, midId, bloom.HorizontalExtend ? Pass.BloomHorizontalE : Pass.BloomHorizontal);
			Draw(midId, toId, bloom.VerticalExtend ? Pass.BloomVerticalE : Pass.BloomVertical);
			fromId = toId;
			toId += 2;
			width /= bloom.ScaleSize;
			height /= bloom.ScaleSize;
		}
		buffer.ReleaseTemporaryRT(bloomPrefilterId);

		//Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
		buffer.SetGlobalFloat(
			bloomBicubicUpsamplingId, bloom.bicubicUpsampling ? 1f : 0f
		);
		if (i > 1)
		{
			//释放最后一次迭代的水平绘制所用的纹理
			buffer.ReleaseTemporaryRT(fromId - 1);
			//目的地设置为低一级的水平绘制所用的纹理
			toId -= 5;

			for (i -= 1; i > 0; i--)
			{
				//设置上一级的垂直绘制纹理作为第二输入
				buffer.SetGlobalTexture(fxSource2Id, toId + 1);
				//fromId 为当前级的输出作为输入，toId 为上一级的水平绘制纹理作为输出目标纹理
				Draw(fromId, toId, Pass.AdditiveBlurring);
				buffer.ReleaseTemporaryRT(fromId);
				buffer.ReleaseTemporaryRT(toId + 1);
				fromId = toId;
				toId -= 2;
			}
		}
		else
		{
			buffer.ReleaseTemporaryRT(bloomPyramidId);
		}

		buffer.SetGlobalTexture(fxSource2Id, sourceId);
		Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.AdditiveBlurring);
		buffer.ReleaseTemporaryRT(fromId);
		buffer.EndSample("Bloom");
	}

	void DoBloom (int sourceId) {
		buffer.BeginSample("Bloom");
		PostFXSettings.BloomSettings bloom = settings.Bloom;
		int width = camera.pixelWidth / 2, height = camera.pixelHeight / 2;
		
		if (
			bloom.maxIterations == 0 || bloom.intensity <= 0f ||
			height < bloom.downscaleLimit * 2 || width < bloom.downscaleLimit * 2
		) {
			Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
			buffer.EndSample("Bloom");
			return;
		}

		Vector4 threshold;
		threshold.x = Mathf.GammaToLinearSpace(bloom.threshold);
		threshold.y = threshold.x * bloom.thresholdKnee;
		threshold.z = 2f * threshold.y;
		threshold.w = 0.25f / (threshold.y + 0.00001f);
		threshold.y -= threshold.x;
		buffer.SetGlobalVector(bloomThresholdId, threshold);

		RenderTextureFormat format = RenderTextureFormat.Default;
		buffer.GetTemporaryRT(
			bloomPrefilterId, width, height, 0, FilterMode.Bilinear, format
		);
		Draw(sourceId, bloomPrefilterId, Pass.BloomPrefilter);
		width /= 2;
		height /= 2;

		int fromId = bloomPrefilterId, toId = bloomPyramidId + 1;
		int i;
		for (i = 0; i < bloom.maxIterations; i++) {
			if (height < bloom.downscaleLimit || width < bloom.downscaleLimit) {
				break;
			}
			int midId = toId - 1;
			buffer.GetTemporaryRT(
				midId, width, height, 0, FilterMode.Bilinear, format
			);
			buffer.GetTemporaryRT(
				toId, width, height, 0, FilterMode.Bilinear, format
			);
			Draw(fromId, midId, Pass.BloomHorizontal);
			Draw(midId, toId, Pass.BloomVertical);
			fromId = toId;
			toId += 2;
			width /= 2;
			height /= 2;
		}

		buffer.ReleaseTemporaryRT(bloomPrefilterId);
		buffer.SetGlobalFloat(
			bloomBicubicUpsamplingId, bloom.bicubicUpsampling ? 1f : 0f
		);
		buffer.SetGlobalFloat(bloomIntensityId, 1f);
		if (i > 1) {
			buffer.ReleaseTemporaryRT(fromId - 1);
			toId -= 5;
			for (i -= 1; i > 0; i--) {
				buffer.SetGlobalTexture(fxSource2Id, toId + 1);
				Draw(fromId, toId, Pass.BloomCombine);
				buffer.ReleaseTemporaryRT(fromId);
				buffer.ReleaseTemporaryRT(toId + 1);
				fromId = toId;
				toId -= 2;
			}
		}
		else {
			buffer.ReleaseTemporaryRT(bloomPyramidId);
		}
		buffer.SetGlobalFloat(bloomIntensityId, bloom.intensity);
		buffer.SetGlobalTexture(fxSource2Id, sourceId);
		Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.BloomCombine);
		buffer.ReleaseTemporaryRT(fromId);
		buffer.EndSample("Bloom");
	}

	void Draw (
		RenderTargetIdentifier from, RenderTargetIdentifier to, Pass pass
	) {
		buffer.SetGlobalTexture(fxSourceId, from);
		buffer.SetRenderTarget(
			to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
		);
		buffer.DrawProcedural(
			Matrix4x4.identity, settings.Material, (int)pass,
			MeshTopology.Triangles, 3
		);
	}
}