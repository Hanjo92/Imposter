using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using System.Threading.Tasks;
using UnityEditor;
using System.IO;

public class ImposterTextureCreater : MonoBehaviour
{
	public enum TextureQuality
	{
		x256,
		x512,
		x1024,
		x2048,
		x4096
	}
	public TextureQuality quality = TextureQuality.x1024;
	private Vector2 TextureSize => quality switch
		{
			TextureQuality.x256 => 256 * Vector2.one,
			TextureQuality.x512 => 512 * Vector2.one,
			TextureQuality.x1024 => 1024 * Vector2.one,
			TextureQuality.x2048 => 2048 * Vector2.one,
			TextureQuality.x4096 => 4096 * Vector2.one,
			_ => Vector2.zero
		};

	public int TextureWidth => (int)TextureSize.x;
	public int TextureHeight => (int)TextureSize.y;
	private int SnapWidth => TextureWidth / snapHorizontal;
	private int SnapHeight => TextureHeight / snapVertical;

	public Camera snapCam;
	public GameObject target;

	[Min(0f)] public float snapDistance = 3f;

	[Header("Vertical options")]
	public int snapHorizontal = 8;
	[Range(0f, 180f)] public float yAngleRange = 180f;

	[Header("Horizontal options")]
	public int snapVertical = 4;
	[Range(-90f, 90f)]public float bottomXAngle = 45f;

	public List<Vector3> cameraPositions = new List<Vector3>();

	public void CalcSnapPositions()
	{
		cameraPositions.Clear();
		if(target == null)
			return;

		var targetYAngle = Quaternion.Euler(0, target.transform.eulerAngles.y, 0);
		for(int j = 0; j < snapVertical; j++)
		{
			float xEuler = Mathf.Lerp(bottomXAngle, -90f, (float)j / (snapVertical - 1));
			for(int i = 0; i < snapHorizontal; i++)
			{
				float yEuler = Mathf.Lerp(-yAngleRange, yAngleRange, (float)i / (snapHorizontal - 1));
				var angle = Quaternion.Euler(xEuler, yEuler, 0f);
				var cameraPosition = target.transform.position;
				cameraPosition += targetYAngle * angle * Vector3.forward * snapDistance;

				cameraPositions.Add(cameraPosition);
			}
		}
	}

	[Button("CalcSnapPositions")]
	public bool setSnapPositions = false;

	[Button("CreateLOD")]
	public bool createLOD = false;
	public void CreateLOD()
	{
		if(target == null) return;

		if( cameraPositions.Count <= 0 )
			CalcSnapPositions();

		SnapShots();
	}

	public async Task SnapShots()
	{
		if(snapCam == null) return;

		sample = new Texture2D(TextureWidth, TextureHeight);
		var clearP = sample.GetPixels();
		for(int i = 0; i < clearP.Length; i++)
		{
			clearP[i] = Color.clear;
		}
		sample.SetPixels(clearP);

		for(int i = 0; i < cameraPositions.Count; i++)
		{
			SetCameraPosition(cameraPositions[i]);
			WriteTexture(i, ref sample);
			sample.Apply();
			await Task.Yield();
		}

		void SetCameraPosition(Vector3 position)
		{
			snapCam.transform.position = position;
			snapCam.transform.LookAt(target.transform);
		}
	}

	public Texture2D sample;
	public Texture2D image;
	private void WriteTexture(int index, ref Texture2D targetTex)
	{
		if(snapCam == null) return;

		GL.Clear(true, true, Color.clear);
		RenderTexture currentRT = RenderTexture.active;
		snapCam.Render();
		RenderTexture.active = snapCam.targetTexture;
		image = new Texture2D(snapCam.targetTexture.width, snapCam.targetTexture.height);
		image.ReadPixels(new Rect(0, 0, snapCam.targetTexture.width, snapCam.targetTexture.height), 0, 0);
		image.Apply();
		GL.Clear(true, true, Color.clear);
		RenderTexture.active = currentRT;

		Color[] colors = ResizeTexture(image, SnapWidth, SnapHeight).GetPixels();

		int c = index % snapHorizontal;
		int r = index / snapHorizontal;
		
		targetTex.SetPixels(c * SnapWidth, r * SnapHeight, SnapWidth, SnapHeight, colors);
	}

	private Texture2D ResizeTexture(Texture2D origin, int resizeWidth, int resizeHeight)
	{
		var resizeTexture = new Texture2D(resizeWidth, resizeHeight);
		var witdthRatio = (float)origin.width / resizeWidth;
		var heightRatio = (float)origin.height / resizeHeight;

		var pixels = origin.GetPixels32();
		for(int h = 0; h < resizeHeight; h++)
		{
			var yCoord = (int)(h * heightRatio);
			for(int w = 0; w < resizeWidth; w++)
			{
				var xCoord = (int)(w * witdthRatio);
				resizeTexture.SetPixel(w, h, pixels[yCoord * origin.width + xCoord]);
			}
		}

		return resizeTexture;
	}

	[Button("SaveTexture")]
	public bool save = false;
	private const string SaveDirectory = "Imposter";
	public void SaveTexture()
	{
		if(sample == null)
			return;

		var directoryPath = Path.Combine(Application.dataPath, SaveDirectory);
		if(Directory.Exists(directoryPath) == false)
			Directory.CreateDirectory(directoryPath);

		var textureName = target.name + ".png";
		var assetPath = Path.Combine(directoryPath, textureName);
		var byteData = sample.EncodeToPNG();
		File.WriteAllBytes(assetPath, byteData);
	}

	[Header("Gizmo options")]
	public float gizmoSize = 0.5f;
	public Color cameraPositionColor = Color.white;
	public Color cameraForwardColor = Color.green;
	private void OnDrawGizmos()
	{
		if(target == null) return;

		var targetPos = target.transform.position;
		foreach(var pos in cameraPositions)
		{
			Gizmos.color = cameraPositionColor;
			Gizmos.DrawSphere(pos, gizmoSize);
			Gizmos.color = cameraForwardColor;
			Gizmos.DrawLine(pos, Vector3.Lerp(pos, targetPos, 0.5f));
		}
	}
}
