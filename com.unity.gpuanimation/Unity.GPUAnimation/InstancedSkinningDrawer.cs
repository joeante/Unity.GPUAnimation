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

        private ComputeBuffer[] argsBuffers;

        private readonly uint[] indirectArgs = new uint[5] { 0, 0, 0, 0, 0 };

        private ComputeBuffer textureCoordinatesBuffer;
        private ComputeBuffer objectToWorldBuffer;

        private Material[] materials;

        private Mesh mesh;
        
        unsafe void init(Material[] srcMaterials, Mesh meshToDraw, AnimationTextures animTexture)
        {
            this.mesh = meshToDraw;

            this.materials = new Material[ srcMaterials.Length ];
            this.argsBuffers = new ComputeBuffer[ srcMaterials.Length ];
            for (int mat = 0; mat < srcMaterials.Length; ++mat)
            {
                this.materials[mat] = new Material(srcMaterials[mat]);
                
                this.materials[mat].SetBuffer("textureCoordinatesBuffer", textureCoordinatesBuffer);
                this.materials[mat].SetBuffer("objectToWorldBuffer", objectToWorldBuffer);
                this.materials[mat].SetTexture("_AnimationTexture0", animTexture.Animation0);
                this.materials[mat].SetTexture("_AnimationTexture1", animTexture.Animation1);
                this.materials[mat].SetTexture("_AnimationTexture2", animTexture.Animation2);
                
                argsBuffers[mat] = new ComputeBuffer(1, indirectArgs.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
                
                indirectArgs[0] = mesh.GetIndexCount(0);
                indirectArgs[1] = (uint)0;
                argsBuffers[mat].SetData(indirectArgs);
            }

            objectToWorldBuffer = new ComputeBuffer(PreallocatedBufferSize, 16 * sizeof(float));
            textureCoordinatesBuffer = new ComputeBuffer(PreallocatedBufferSize, 3 * sizeof(float));
	
        }
        
        public unsafe InstancedSkinningDrawer(Material srcMaterial, Mesh meshToDraw, AnimationTextures animTexture)
        {
            init(new Material[] { srcMaterial }, meshToDraw, animTexture);
        }

        public unsafe InstancedSkinningDrawer(Material[] srcMaterials, Mesh meshToDraw, AnimationTextures animTexture)
        {
            init( srcMaterials, meshToDraw, animTexture);
        }

        public void Dispose()
        {
            if (materials != null)
            {
                for ( int mat=0; mat<materials.Length; ++mat )
                    UnityEngine.Object.DestroyImmediate(materials[mat]);
                materials = null;
            }
		
            if (argsBuffers != null)
            {
                for ( int ab=0; ab<argsBuffers.Length; ++ab )
                    argsBuffers[ab].Dispose();
                argsBuffers = null;
            }

            if (objectToWorldBuffer != null) objectToWorldBuffer.Dispose();
            if (textureCoordinatesBuffer != null) textureCoordinatesBuffer.Dispose();
        }
        
        public void Draw(NativeArray<float3> TextureCoordinates, NativeArray<float4x4> ObjectToWorld, ShadowCastingMode shadowCastingMode, bool receiveShadows)
        {
            // CHECK: Systems seem to be called when exiting playmode once things start getting destroyed, such as the mesh here.
            if (mesh == null || materials == null || materials[0]==null ) 
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
            
            Profiler.BeginSample("Modify compute buffers");

            Profiler.BeginSample("Shader set data");

            objectToWorldBuffer.SetData(ObjectToWorld, 0, 0, count);
            textureCoordinatesBuffer.SetData(TextureCoordinates, 0, 0, count);

            for (int mat = 0; mat < materials.Length; ++mat)
            {
                this.materials[mat].SetBuffer("textureCoordinatesBuffer", textureCoordinatesBuffer);
                this.materials[mat].SetBuffer("objectToWorldBuffer", objectToWorldBuffer);
            }

            indirectArgs[1] = (uint)count;

            for (int smi = 0; smi < mesh.subMeshCount; ++smi)
            {
                indirectArgs[0] = mesh.GetIndexCount(smi);
                indirectArgs[2] = mesh.GetIndexStart(smi);
                indirectArgs[3] = mesh.GetBaseVertex(smi);
                argsBuffers[smi].SetData(indirectArgs);
            }
            
            Profiler.EndSample();

            Profiler.EndSample();
            
            // todo:  use one argbuffer and an offset?
            for (int smi = 0; smi < mesh.subMeshCount; ++smi)
            {
                Graphics.DrawMeshInstancedIndirect(mesh, smi, materials[smi], 
                    new Bounds(Vector3.zero, 1000000 * Vector3.one), 
                    argsBuffers[smi], 0, 
                    new MaterialPropertyBlock(), shadowCastingMode, receiveShadows);
            }
        }
    }
}