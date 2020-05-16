using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = System.Object;


namespace Unity.GPUAnimation
{
	public struct AnimationTextures : IEquatable<AnimationTextures>
	{
		public Texture2D Animation0;
		public Texture2D Animation1;
		public Texture2D Animation2;

		public bool Equals(AnimationTextures other)
		{
			return Animation0 == other.Animation0 && Animation1 == other.Animation1 && Animation2 == other.Animation2;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = (ReferenceEquals(Animation0, null) ? 0 : Animation0.GetHashCode());
				hashCode = (hashCode * 397) ^ (ReferenceEquals(Animation1, null) ? 0 : Animation1.GetHashCode());
				hashCode = (hashCode * 397) ^ (ReferenceEquals(Animation2, null) ? 0 : Animation2.GetHashCode());
				return hashCode;
			}
		}
	}

	public static class KeyframeTextureBaker
	{
		

		
		public class BakedData
		{
			public AnimationTextures AnimationTextures;
			public Mesh[]            BakedMeshes;
			public float             Framerate;

			public List<AnimationClipData> Animations = new List<AnimationClipData>();

			public Dictionary<string, AnimationClipData> AnimationsDictionary = new Dictionary<string, AnimationClipData>();
		}

		public class AnimationClipData
		{
			public AnimationClip Clip;
			public int PixelStart;
			public int PixelEnd;
		}

		public static BakedData BakeClips(GameObject animationRoot, SkinnedMeshRenderer[] renderers, AnimationClip[] animationClips, float framerate)
		{
			// @TODO: warning about more than one materials

			// Before messing about with some arbitrary game object hierarchy.
			// Instantiate the character, but make sure it's inactive so it doesn't trigger any unexpected systems. 
			var wasActive = animationRoot.activeSelf;
			animationRoot.SetActive(false);
			var instance = GameObject.Instantiate(animationRoot, Vector3.zero, Quaternion.identity);
			animationRoot.SetActive(wasActive);
			
			instance.transform.localScale = Vector3.one;
			var skinRenderer = instance.GetComponentInChildren<SkinnedMeshRenderer>();

			BakedData bakedData = new BakedData();
			bakedData.BakedMeshes = new Mesh[renderers.Length];

			bakedData.BakedMeshes[0] = CreateMesh(renderers[0].sharedMesh, null);
			for (int i = 1;i<renderers.Length;i++)
				bakedData.BakedMeshes[i] = CreateMesh(renderers[i].sharedMesh, renderers[0].sharedMesh);

			bakedData.Framerate = framerate;

			var sampledBoneMatrices = new List<Matrix4x4[,]>();

			int numberOfKeyFrames = 0;

			for (int i = 0; i < animationClips.Length; i++)
			{
				var sampledMatrix = SampleAnimationClip(instance, animationClips[i], skinRenderer, bakedData.Framerate);
				sampledBoneMatrices.Add(sampledMatrix);

				numberOfKeyFrames += sampledMatrix.GetLength(0);
			}

			int numberOfBones = sampledBoneMatrices[0].GetLength(1);

			var tex0 = bakedData.AnimationTextures.Animation0 = new Texture2D(numberOfKeyFrames, numberOfBones, TextureFormat.RGBAFloat, false);
			tex0.wrapMode = TextureWrapMode.Clamp;
			tex0.filterMode = FilterMode.Point;
			tex0.anisoLevel = 0;

			var tex1 = bakedData.AnimationTextures.Animation1 = new Texture2D(numberOfKeyFrames, numberOfBones, TextureFormat.RGBAFloat, false);
			tex1.wrapMode = TextureWrapMode.Clamp;
			tex1.filterMode = FilterMode.Point;
			tex1.anisoLevel = 0;

			var tex2 = bakedData.AnimationTextures.Animation2 = new Texture2D(numberOfKeyFrames, numberOfBones, TextureFormat.RGBAFloat, false);
			tex2.wrapMode = TextureWrapMode.Clamp;
			tex2.filterMode = FilterMode.Point;
			tex2.anisoLevel = 0;

			Color[] texture0Color = new Color[tex0.width * tex0.height];
			Color[] texture1Color = new Color[tex0.width * tex0.height];
			Color[] texture2Color = new Color[tex0.width * tex0.height];

			int runningTotalNumberOfKeyframes = 0;
			for (int i = 0; i < sampledBoneMatrices.Count; i++)
			{
				for (int boneIndex = 0; boneIndex < sampledBoneMatrices[i].GetLength(1); boneIndex++)
				{
					//Color previousRotation = new Color();

					for (int keyframeIndex = 0; keyframeIndex < sampledBoneMatrices[i].GetLength(0); keyframeIndex++)
					{
						//var rotation = GetRotation(Quaternion.LookRotation(sampledBoneMatrices[i][keyframeIndex, boneIndex].GetColumn(2),
						//													sampledBoneMatrices[i][keyframeIndex, boneIndex].GetColumn(1)));

						//if (keyframeIndex != 0)
						//{
						//	if (Distance(previousRotation, rotation) > Distance(Negate(rotation), previousRotation))
						//	{
						//		rotation = new Color(-rotation.r, -rotation.g, -rotation.b, -rotation.a);
						//	}
						//}

						//var translation = GetTranslation(sampledBoneMatrices[i][keyframeIndex, boneIndex].GetColumn(3), rotation);

						//previousRotation = rotation;
						//int index = Get1DCoord(runningTotalNumberOfKeyframes + keyframeIndex, boneIndex, bakedData.TranslationTexture.width);
						//translations[index] = translation;
						//rotations[index] = rotation;

						int index = Get1DCoord(runningTotalNumberOfKeyframes + keyframeIndex, boneIndex, tex0.width);

						texture0Color[index] = sampledBoneMatrices[i][keyframeIndex, boneIndex].GetRow(0);
						texture1Color[index] = sampledBoneMatrices[i][keyframeIndex, boneIndex].GetRow(1);
						texture2Color[index] = sampledBoneMatrices[i][keyframeIndex, boneIndex].GetRow(2);
					}
				}

				AnimationClipData clipData = new AnimationClipData
				{
					Clip = animationClips[i],
					PixelStart = runningTotalNumberOfKeyframes + 1,
					PixelEnd = runningTotalNumberOfKeyframes + sampledBoneMatrices[i].GetLength(0) - 1
				};

				if (clipData.Clip.wrapMode == WrapMode.Default) clipData.PixelEnd -= 1;

				bakedData.Animations.Add(clipData);

				runningTotalNumberOfKeyframes += sampledBoneMatrices[i].GetLength(0);
			}

			tex0.SetPixels(texture0Color);
			tex1.SetPixels(texture1Color);
			tex2.SetPixels(texture2Color);

			runningTotalNumberOfKeyframes = 0;
			for (int i = 0; i < sampledBoneMatrices.Count; i++)
			{
				for (int boneIndex = 0; boneIndex < sampledBoneMatrices[i].GetLength(1); boneIndex++)
				{
					for (int keyframeIndex = 0; keyframeIndex < sampledBoneMatrices[i].GetLength(0); keyframeIndex++)
					{
						//int d1_index = Get1DCoord(runningTotalNumberOfKeyframes + keyframeIndex, boneIndex, bakedData.Texture0.width);

						Color pixel0 = tex0.GetPixel(runningTotalNumberOfKeyframes + keyframeIndex, boneIndex);
						Color pixel1 = tex1.GetPixel(runningTotalNumberOfKeyframes + keyframeIndex, boneIndex);
						Color pixel2 = tex2.GetPixel(runningTotalNumberOfKeyframes + keyframeIndex, boneIndex);

						if ((Vector4)pixel0 != sampledBoneMatrices[i][keyframeIndex, boneIndex].GetRow(0))
						{
							Debug.LogError("Error at (" + (runningTotalNumberOfKeyframes + keyframeIndex) + ", " + boneIndex + ") expected " + Format(sampledBoneMatrices[i][keyframeIndex, boneIndex].GetRow(0)) + " but got " + Format(pixel0));
						}
						if ((Vector4)pixel1 != sampledBoneMatrices[i][keyframeIndex, boneIndex].GetRow(1))
						{
							Debug.LogError("Error at (" + (runningTotalNumberOfKeyframes + keyframeIndex) + ", " + boneIndex + ") expected " + Format(sampledBoneMatrices[i][keyframeIndex, boneIndex].GetRow(1)) + " but got " + Format(pixel1));
						}
						if ((Vector4)pixel2 != sampledBoneMatrices[i][keyframeIndex, boneIndex].GetRow(2))
						{
							Debug.LogError("Error at (" + (runningTotalNumberOfKeyframes + keyframeIndex) + ", " + boneIndex + ") expected " +   Format(sampledBoneMatrices[i][keyframeIndex, boneIndex].GetRow(2)) + " but got " + Format(pixel2));
						}
					}
				}
				runningTotalNumberOfKeyframes += sampledBoneMatrices[i].GetLength(0);
			}

			tex0.Apply(false, true);
			tex1.Apply(false, true);
			tex2.Apply(false, true);
			
			bakedData.AnimationsDictionary = new Dictionary<string, AnimationClipData>();
			foreach (var clipData in bakedData.Animations)
			{
				bakedData.AnimationsDictionary[clipData.Clip.name] = clipData;
			}
			
			GameObject.DestroyImmediate(instance);

			return bakedData;
		}

		public static string Format(Vector4 v)
		{
			return "(" + v.x + ", " + v.y + ", " + v.z + ", " + v.w + ")";
		}

		public static string Format(Color v)
		{
			return "(" + v.r + ", " + v.g + ", " + v.b + ", " + v.a + ")";
		}

		private static Mesh CreateMesh(Mesh sourceMesh, Mesh highLODSourceMesh)
		{
			Mesh newMesh = new Mesh();
			var boneWeights = sourceMesh.boneWeights;

			sourceMesh.CopyMeshData(newMesh);

			Vector3[] vertices = sourceMesh.vertices;
			Vector2[] boneIds = new Vector2[sourceMesh.vertexCount];
			Vector2[] boneInfluences = new Vector2[sourceMesh.vertexCount];

			int[] boneRemapping = null;

			var totalBonesCount = sourceMesh.bindposes.Length;
			if (highLODSourceMesh != null)
			{
				totalBonesCount = highLODSourceMesh.bindposes.Length;
				
				var originalBindPoseMatrices = highLODSourceMesh.bindposes;
				var newBindPoseMatrices = sourceMesh.bindposes;
				
				boneRemapping = new int[newBindPoseMatrices.Length];
				for (int i = 0; i < boneRemapping.Length; i++)
				{
					boneRemapping[i] = Array.FindIndex(originalBindPoseMatrices, x => x == newBindPoseMatrices[i]);
					//@TODO: Why compare bind post matrix. Just compare the actual transform reference of the game object
					if (boneRemapping[i] == -1)
						Debug.LogError("Failed to find bone in high lod mesh");
				}
			}
			
			for (int i = 0; i < sourceMesh.vertexCount; i++)
			{
				int boneIndex0 = boneWeights[i].boneIndex0;
				int boneIndex1 = boneWeights[i].boneIndex1;

				if (boneRemapping != null)
				{
					boneIndex0 = boneRemapping[boneIndex0];
					boneIndex1 = boneRemapping[boneIndex1];
				}

				boneIds[i] = new Vector2((boneIndex0 + 0.5f) / totalBonesCount, (boneIndex1 + 0.5f) / totalBonesCount);

				float mostInfluentialBonesWeight = boneWeights[i].weight0 + boneWeights[i].weight1;

				boneInfluences[i] = new Vector2(boneWeights[i].weight0 / mostInfluentialBonesWeight, boneWeights[i].weight1 / mostInfluentialBonesWeight);
			}

			newMesh.vertices = vertices;
			newMesh.uv2 = boneIds;
			newMesh.uv3 = boneInfluences;

			return newMesh;
		}

		private static Matrix4x4[,] SampleAnimationClip(GameObject root, AnimationClip clip, SkinnedMeshRenderer renderer, float framerate)
		{
			var bindPoses = renderer.sharedMesh.bindposes;
			var bones = renderer.bones;
			Matrix4x4[,] boneMatrices = new Matrix4x4[Mathf.CeilToInt(framerate * clip.length) + 3, bones.Length];
			for (int i = 1; i < boneMatrices.GetLength(0) - 1; i++)
			{
				float t = (float)(i - 1) / (boneMatrices.GetLength(0) - 3);

				var oldWrapMode = clip.wrapMode;
				clip.wrapMode = WrapMode.Clamp;
				clip.SampleAnimation(root, t * clip.length);
				clip.wrapMode = oldWrapMode;
				
				for (int j = 0; j < bones.Length; j++)
					boneMatrices[i, j] = bones[j].localToWorldMatrix * bindPoses[j];
			}

			for (int j = 0; j < bones.Length; j++)
			{
				boneMatrices[0, j] = boneMatrices[boneMatrices.GetLength(0) - 2, j];
				boneMatrices[boneMatrices.GetLength(0) - 1, j] = boneMatrices[1, j];
			}

			return boneMatrices;
		}

		#region Util methods

		public static void CopyMeshData(this Mesh originalMesh, Mesh newMesh)
		{
			var vertices = originalMesh.vertices;

			newMesh.vertices = vertices;
			newMesh.triangles = originalMesh.triangles;
			newMesh.normals = originalMesh.normals;
			newMesh.uv = originalMesh.uv;
			newMesh.tangents = originalMesh.tangents;
			newMesh.name = originalMesh.name;
		}

		private static float Distance(Color r1, Color r2)
		{
			return Mathf.Abs(r1.r - r2.r) + Mathf.Abs(r1.g - r2.g) + Mathf.Abs(r1.b - r2.b) + Mathf.Abs(r1.a - r2.a);
		}

		private static Color Negate(Color c)
		{
			return new Color(-c.r, -c.g, -c.b, -c.a);
		}

		private static Color GetTranslation(Vector4 rawTranslation, Color rotation)
		{
			Quaternion rot = new Quaternion(rotation.r, rotation.g, rotation.b, rotation.a);
			Quaternion trans = new Quaternion(rawTranslation.x, rawTranslation.y, rawTranslation.z, 0) * rot;

			return new Color(trans.x, trans.y, trans.z, trans.w) * 0.5f;
		}

		private static Color GetRotation(Quaternion rotation)
		{
			return new Color(rotation.x, rotation.y, rotation.z, rotation.w);
		}

		private static int Get1DCoord(int x, int y, int width)
		{
			return y * width + x;
		}

		#endregion
}
}