# Meshy AI 3D Character & Animation Rules in Unity

When importing 3D models with associated animations from **Meshy AI** into this Unity project:

1. **Rig Type:** Always use `ModelImporterAnimationType.Generic` (`animationType = 2`) for all Meshy FBX character and animation files.
2. **Avatar Setup:** Set `avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel` on the base character model FBX and assign its generated Avatar to the `Animator` component.
3. **Hierarchy & Component Placement:** Place the `Animator` component directly on the child GameObject containing the `Armature` root (e.g. `Visual/Armature`).
4. **Materials & Emission:** Ensure `_EmissionColor` is set to `(0, 0, 0, 1)` and `_EMISSION` is disabled unless intentional glow is required. Assign diffuse texture to `_BaseMap`.
5. **Culling & Offscreen Update:** Set `animator.cullingMode = AnimatorCullingMode.AlwaysAnimate` and `skinnedMeshRenderer.updateWhenOffscreen = true` to prevent camera frustum animation freezing.
