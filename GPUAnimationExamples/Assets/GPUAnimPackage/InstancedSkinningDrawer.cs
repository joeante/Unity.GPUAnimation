using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace GPUAnimPackage
{
    public class InstancedSkinningDrawer : IDisposable
    {
        private const int PreallocatedBufferSize = 32 * 1024;

        private ComputeBuffer argsBuffer;

        private readonly uint[] indirectArgs = new uint[5] { 0, 0, 0, 0, 0 };

        private ComputeBuffer textureCoordinatesBuffer;
        private ComputeBuffer objectRotationsBuffer;
        private ComputeBuffer objectPositionsBuffer;

        public NativeList<float3> TextureCoordinates;
        public NativeList<float4> ObjectPositions;
        public NativeList<quaternion> ObjectRotations;


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

            objectRotationsBuffer = new ComputeBuffer(PreallocatedBufferSize, 16);
            objectPositionsBuffer = new ComputeBuffer(PreallocatedBufferSize, 16);
            textureCoordinatesBuffer = new ComputeBuffer(PreallocatedBufferSize, 12);

            TextureCoordinates = new NativeList<float3>(PreallocatedBufferSize, Allocator.Persistent);
            ObjectPositions = new NativeList<float4>(PreallocatedBufferSize, Allocator.Persistent);
            ObjectRotations = new NativeList<quaternion>(PreallocatedBufferSize, Allocator.Persistent);
		
            material.SetBuffer("textureCoordinatesBuffer", textureCoordinatesBuffer);
            material.SetBuffer("objectPositionsBuffer", objectPositionsBuffer);
            material.SetBuffer("objectRotationsBuffer", objectRotationsBuffer);
            material.SetTexture("_AnimationTexture0", bakedData.Texture0);
            material.SetTexture("_AnimationTexture1", bakedData.Texture1);
            material.SetTexture("_AnimationTexture2", bakedData.Texture2);
        }

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(material);
		
            if (argsBuffer != null) argsBuffer.Dispose();
            if (objectPositionsBuffer != null) objectPositionsBuffer.Dispose();
            if (ObjectPositions.IsCreated) ObjectPositions.Dispose();

            if (objectRotationsBuffer != null) objectRotationsBuffer.Dispose();
            if (ObjectRotations.IsCreated) ObjectRotations.Dispose();

            if (textureCoordinatesBuffer != null) textureCoordinatesBuffer.Dispose();
            if (TextureCoordinates.IsCreated) TextureCoordinates.Dispose();
        }

        public void Draw()
        {
            if (objectRotationsBuffer == null) return;

            int count = UnitToDrawCount;
            if (count == 0) return;

            Profiler.BeginSample("Modify compute buffers");

            Profiler.BeginSample("Shader set data");

            objectPositionsBuffer.SetData((NativeArray<float4>)ObjectPositions, 0, 0, count);
            objectRotationsBuffer.SetData((NativeArray<quaternion>)ObjectRotations, 0, 0, count);
            textureCoordinatesBuffer.SetData((NativeArray<float3>)TextureCoordinates, 0, 0, count);

            material.SetBuffer("textureCoordinatesBuffer", textureCoordinatesBuffer);
            material.SetBuffer("objectPositionsBuffer", objectPositionsBuffer);
            material.SetBuffer("objectRotationsBuffer", objectRotationsBuffer);
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
                return ObjectPositions.Length;
            }
        }
    }
}