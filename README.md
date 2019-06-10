# Overview

Simple but very fast GPU vertex shader based animation system for Unity.Entities

The animationclip is converted to three textures that are sampled per vertex. Performing per vertex 2 bone skinning. All animation curves are converted to world space when baked into the texture, thus no local to world space transformation is required and all sampling can be performed directly in the vertex shader.

NOTE: This is not intended to be used or extended as a complete animation system. It is specifically made for the case of animating massive amounts of characters where each character has unique animation but the animation state is trivial. Specifically animations can't be blended. Only one clip can be played at a time per character.

GameObjectConversion pipeline is provided to bake existing Unity Character Rigs & AnimationClips into GPU Skin renderers.

This code is based on the Nordeus / Unity collaboration. I took the core code from the animation system, cleaned it up a whole lot and made a good workflow compatible with latest entities for it.
https://www.youtube.com/watch?v=0969LalB7vw

# Advantages:
* The state to transfer to GPU is 12 bytes per character for the animation state + 64 bytes for the local to world matrix. The CPU cost is incredibly low, it is simply transferring the 76 byte animation state.
* All animation clip sampling work happens in the vertex shader. The GPU cost can easily be scaled by providing skinned mesh LODs with fewer vertices, bone LOD is implicit in this approach.

# Disadvantages
* Currently you can only play one clip at a time (With additional work it might be possible to add support for two blendable clips)
* All parts of the animated character must be on a single SkinnedMeshRenderer. Eg. Attachments like swords can't be seperate Meshes but must be part of the skin mesh. (MeshRenderer based attachments could automatically be baked into the skin mesh, but this is not supported at the moment)
* A special shader must be used to render the character

# TODO / Known issues:
* The conversion pipeline currently creates an entity with transform hierarchy components for each bone in the hierarchy below the character. In this case none of it is used and it is simply wasted. This makes instantiation slow and causes massive overhead at runtime to keep all these unused transform nodes up to date... I'll make changes to the conversion pipeline to support proper stripping of unused entities.
* Currently no frustum culling
* Currently no LOD support
* HDRP is not supported at the moment. Lets create a custom code shader graph node that works with both HDRP & LWRP.
* Make more interesting example that use walk / attack / die animation clips
* Support for blending two animation clips
* Bake MeshRenderer based attachments into the skin mesh
* Bake into a single large texture with all 3 curve data samples next to each other for cache locality
* Fix low frame rate issue with looping clips (LastFrameLoopBug.unity scene)
