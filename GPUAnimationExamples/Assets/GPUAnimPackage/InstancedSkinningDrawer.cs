using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Object = System.Object;

namespace GPUAnimPackage
{
    public class InstancedSkinningDrawer : IDisposable
    {
        private const int PreallocatedBufferSize = 32 * 1024;

        private ComputeBuffer argsBuffer;

        private readonly uint[] indirectArgs = new uint[5] { 0, 0, 0, 0, 0 };

        private ComputeBuffer textureCoordinatesBuffer;
        private ComputeBuffer objectToWorldBuffer;

        public NativeList<float3> TextureCoordinates;
        public NativeList<float4x4> ObjectToWorld;


        private Material material;

        private Mesh mesh;

        private KeyframeTextureBaker.BakedData bakedData;
	

        public unsafe InstancedSkinningDrawer(Material material, Mesh meshToDraw, KeyframeTextureBaker.BakedData  bakedData)
        {
            this.bakedData = bakedData;
            this.mesh = meshToDraw;
            this.material = new Material(material);

            argsBuffer = new ComputeBuffer(1, indirectArgs.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            indirectArgs[0] = mesh.GetIndexCount(0);
            indirectArgs[1] = (uint)0;
            argsBuffer.SetData(indirectArgs);

            objectToWorldBuffer = new ComputeBuffer(PreallocatedBufferSize, 16 * sizeof(float));
            textureCoordinatesBuffer = new ComputeBuffer(PreallocatedBufferSize, 3 * sizeof(float));

            ObjectToWorld = new NativeList<float4x4>(PreallocatedBufferSize, Allocator.Persistent);
            TextureCoordinates = new NativeList<float3>(PreallocatedBufferSize, Allocator.Persistent);
		
            material.SetBuffer("textureCoordinatesBuffer", textureCoordinatesBuffer);
            material.SetBuffer("objectToWorldBuffer", objectToWorldBuffer);
            material.SetTexture("_AnimationTexture0", bakedData.Texture0);
            material.SetTexture("_AnimationTexture1", bakedData.Texture1);
            material.SetTexture("_AnimationTexture2", bakedData.Texture2);
        }

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(material);
		
            if (argsBuffer != null) argsBuffer.Dispose();

            if (objectToWorldBuffer != null) objectToWorldBuffer.Dispose();
            if (ObjectToWorld.IsCreated) ObjectToWorld.Dispose();

            if (textureCoordinatesBuffer != null) textureCoordinatesBuffer.Dispose();
            if (TextureCoordinates.IsCreated) TextureCoordinates.Dispose();
        }

        public void Draw()
        {
            if (objectToWorldBuffer == null)
                return;

            int count = UnitToDrawCount;
            if (count == 0) return;

            Profiler.BeginSample("Modify compute buffers");

            Profiler.BeginSample("Shader set data");

            objectToWorldBuffer.SetData((NativeArray<float4x4>)ObjectToWorld, 0, 0, count);
            textureCoordinatesBuffer.SetData((NativeArray<float3>)TextureCoordinates, 0, 0, count);

            material.SetBuffer("textureCoordinatesBuffer", textureCoordinatesBuffer);
            material.SetBuffer("objectToWorldBuffer", objectToWorldBuffer);
            material.SetTexture("_AnimationTexture0", bakedData.Texture0);
            material.SetTexture("_AnimationTexture1", bakedData.Texture1);
            material.SetTexture("_AnimationTexture2", bakedData.Texture2);
            Profiler.EndSample();

            Profiler.EndSample();

            // CHECK: Systems seem to be called when exiting playmode once things start getting destroyed, such as the mesh here.
            if (mesh == null || material == null) return;

            //indirectArgs[1] = (uint)data.Count;
            indirectArgs[1] = (uint)count;
            argsBuffer.SetData(indirectArgs);

            Graphics.DrawMeshInstancedIndirect(mesh, 0, material, new Bounds(Vector3.zero, 1000000 * Vector3.one), argsBuffer, 0, new MaterialPropertyBlock(), ShadowCastingMode.Off, true);
        }

        public int UnitToDrawCount
        {
            get
            {
                return ObjectToWorld.Length;
            }
        }
    }
}