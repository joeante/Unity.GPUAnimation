# Overview

Simple but very fast GPU vertex shader based animation system for Unity.Entities

The animationclip is converted to three textures that are sampled per vertex. All animation curves are converted to world space.

NOTE: This is not intended to be used or extended as a complete animation system. It is specifically made for the case of animating massive amounts of characters where each character has unique animation but the animation state is trivial. Specifically animations can't be blended. Only one clip can be played at a time per character.

GameObjectConversion pipeline is provided to bake existing Unity Character Rigs & AnimationClips into GPU Skin renderers.

# Advantages:
* The state to transfer to GPU is 12 bytes per character for the animation state + 64 bytes for the local to world matrix. Thus the CPU cost is incredibly low.
* All animation clip sampling work happens in the vertex shader. Thus the GPU cost can easily be scaled by providing skinned mesh LODs with fewer vertices

# Disadvantages
* Currently you can only play one clip at a time (With additional work it might be possible to add support for two blendable clips)
* All parts of the animated character must be on a single SkinnedMeshRenderer. Eg. Attachments like swords can't be seperate Meshes but must be part of the skin mesh. (MeshRenderer based attachments could automatically be baked into the skin mesh, but this is not supported at the moment)
* A special shader must be used to render the character

# TODO / Known issues:
* Currently no frustum culling
* HDRP / LWRP is not supported at the moment
* Make more interesting example that use walk / attack / die animation clips
