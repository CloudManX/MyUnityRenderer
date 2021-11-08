# Unity SRP Playground

## What is this About?
This is a playground for learning and experimenting the capability of Unity's Scriptable Rendering Pipeline. The goal of this project is to research and investigate how computer graphic theories about real-time rendering can work seamlessly in mordern commerical game engines like Unity3D. In current stage, codes are originated from https://catlikecoding.com/unity/tutorials/custom-srp/ in recognition of author's credit.

## Phase I: Studying Catlike Coding's Custom SRP
### Sample Images
* GPU Instancing and Unlit Shader
![](Assets/StreamingAssets/Screenshot_2021-11-07_18-09-02.png)
* Directional Lighting and Shadow with Baked GI 
![](Assets/StreamingAssets/Screenshot_2021-11-07_20-13-30.png)
* Spot and Point Light
![](Assets/StreamingAssets/Screenshot_2021-11-07_18-13-57.png)
* Spot and Point Light with HDR PostFX
![](Assets/StreamingAssets/Screenshot_2021-11-07_18-14-18.png)
### Lighting
* Directional Light
* Transparency
  * Aphla Clipping
* Light Model
  * Shading Model: PBR and Cook-Torrence Model
    * Geometry: SchlickGGX
    * NDF: Trowbridge-Reitz GGX
    * Frenel: Fresnel-Schilick Approximation
  * Environmental Mapping and Reflection Probe
* Point and Spot Lights
  * Point Light
    * Distance Attenuation and Light Range
  * Spot Light
    * Direction and Inner and Outer Spot Angle
  * PerObject Light Data
* Global Illumination
  * Light Baking for Static Objects
  * Light Mapping and Sampling
  * Meta Pass
  * Light Probe
  * LPPV
  * Emissive Surface
  * Light Delegate
### Shadows
* Directional Shadows
  * Shadow Map
  * Shadow Caster Pass
  * Applying shadow with Light Attenuation
  * CSM and Culling Spheres
  * CSM Shadow Fading between Cascades
  * Depth Bias and Normal bias
  * Shadow Pancaking
  * PCF Filtering 
  * Clipped Shadows and Dithered Shadows for Trasparent Objects
* Shadow Masks for GI
  * Occlusion Probes and LPPV
  * Shadow Mixing With CSM
* Point and Spot Shadows 
  * Dedicated Atlas
  * Normal Bias for Point and Spot Light
  * Clamped Sampling
  * FOV Bias
### Texturing
* Albedo
* Emission
* MODS(Metallic, Occlusion, Detail and Smoothness)
* Normal Maps and Tangent Space Transformation
* Detailed Normal Map
### PostFX
* Single Triangle Drawing
* Bloom
  * Gaussian Filtering
  * Bloom Pyramid and Additive Blurring
  * Threshold and Leg function
  * Additive Bloom and Scattering Bloom
* HDR
  * Firefly Elimination
  * Tone Mapping
    * Reinhard
    * Neutral
    * ACES
* Color Grading
  * Post Exposure
  * Contrast
  * Color Filter
  * Hue Shift
  * Saturation
  * White Balance


  * Split Toning
  * Channel Mixer
  * Shadows Midtones Highlights
  * 3D LUT
### Particles
* Flipbook Blending
* Fading Near Camera
* ViewSpace Depth and Soft Billboard Particles
* ViewSpace Copy and Distortion
### LOD
* Dithering and Cross-Fading
### FXAA