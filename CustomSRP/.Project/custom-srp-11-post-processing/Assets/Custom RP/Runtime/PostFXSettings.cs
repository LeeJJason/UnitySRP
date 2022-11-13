using UnityEngine;

[CreateAssetMenu(menuName = "Rendering/Custom Post FX Settings")]
public class PostFXSettings : ScriptableObject {

	[SerializeField]
	Shader shader = default;

	[System.Serializable]
	public struct BloomSettings {
		[Range(0f, 16f)]
		public int maxIterations;

		[Min(1f)]
		public int downscaleLimit;

		public bool bicubicUpsampling;

		[Min(0f)]
		public float threshold;

		[Range(0f, 1f)]
		public float thresholdKnee;

		[Min(0f)]
		public float intensity;

		public bool HorizontalExtend;
		public bool VerticalExtend;
		[Min(1)]
		public int ScaleSize;
	}

	[SerializeField]
	BloomSettings bloom = default;
	

	public BloomSettings Bloom => bloom;

	[System.NonSerialized]
	Material material;

	public Material Material {
		get {
			if (material == null && shader != null) {
				material = new Material(shader);
				material.hideFlags = HideFlags.HideAndDontSave;
			}
			return material;
		}
	}
}