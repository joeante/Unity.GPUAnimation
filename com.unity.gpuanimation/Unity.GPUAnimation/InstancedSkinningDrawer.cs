using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace Unity.GPUAnimation
{
    public class InstancedSkinningDrawer : IDisposable
    {
        private const int PreallocatedBufferSize = 1024;

        private ComputeBuffer argsBuffer;

        private readonly uint[] indirectArgs = new uint[5] { 0, 0, 0, 0, 0 };

        private ComputeBuffer textureCoordinatesBuffer;
        private ComputeBuffer objectToWorldBuffer;

        private Material material;

        private Mesh mesh;
        
        public unsafe InstancedSkinningDrawer(Material srcMaterial, Mesh meshToDraw, AnimationTextures animTexture)
        {
            this.mesh = meshToDraw;
            this.material = new Material(srcMaterial);

            argsBuffer = new ComputeBuffer(1, indirectArgs.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            indirectArgs[0] = mesh.GetIndexCount(0);
            indirectArgs[1] = (uint)0;
            argsBuffer.SetData(indirectArgs);

            objectToWorldBuffer = new ComputeBuffer(PreallocatedBufferSize, 16 * sizeof(float));
            textureCoordinatesBuffer = new ComputeBuffer(PreallocatedBufferSize, 3 * sizeof(float));
	
            this.material.SetBuffer("textureCoordinatesBuffer", textureCoordinatesBuffer);
            this.material.SetBuffer("objectToWorldBuffer", objectToWorldBuffer);
            this.material.SetTexture("_AnimationTexture0", animTexture.Animation0);
            this.material.SetTexture("_AnimationTexture1", animTexture.Animation1);
            this.material.SetTexture("_AnimationTexture2", animTexture.Animation2);
        }

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(material);
		
            if (argsBuffer != null) argsBuffer.Dispose();
            if (objectToWorldBuffer != null) objectToWorldBuffer.Dispose();
            if (textureCoordinatesBuffer != null) textureCoordinatesBuffer.Dispose();
        }
        
        public void Draw(NativeArray<float3> TextureCoordinates, NativeArray<float4x4> ObjectToWorld, ShadowCastingMode shadowCastingMode, bool receiveShadows)
        {
            // CHECK: Systems seem to be called when exiting playmode once things start getting destroyed, such as the mesh here.
            if (mesh == null || material == null) 
                return;

            int count = TextureCoordinates.Length;
            if (count == 0) 
                return;

            if (count > objectToWorldBuffer.count)
            {
                objectToWorldBuffer.Dispose();
                textureCoordinatesBuffer.Dispose();
                
                objectToWorldBuffer = new ComputeBuffer(TextureCoordinates.Length, 16 * sizeof(float));
                textureCoordinatesBuffer = new ComputeBuffer(TextureCoordinates.Length, 3 * sizeof(float));
            }

            this.material.SetBuffer("textureCoordinatesBuffer", textureCoordinatesBuffer);
            this.material.SetBuffer("objectToWorldBuffer", objectToWorldBuffer);
            
            Profiler.BeginSample("Modify compute buffers");

            Profiler.BeginSample("Shader set data");

            objectToWorldBuffer.SetData(ObjectToWorld, 0, 0, count);
            textureCoordinatesBuffer.SetData(TextureCoordinates, 0, 0, count);
            
            Profiler.EndSample();

            Profiler.EndSample();

            //indirectArgs[1] = (uint)data.Count;
            indirectArgs[1] = (uint)count;
            argsBuffer.SetData(indirectArgs);

            Graphics.DrawMeshInstancedIndirect(mesh, 0, material, new Bounds(Vector3.zero, 1000000 * Vector3.one), argsBuffer, 0, new MaterialPropertyBlock(), shadowCastingMode, receiveShadows);
        }
    }
}