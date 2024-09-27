# 2D Metaballs
 2D Metaballs in Screen Space (using Particle System)

![](Previews/Preview.gif)

## Overview
A simple screen space 2D metaball system using URP's Renderer Feature. This project is partly based on [daniel-ilett's Metaball System](https://github.com/daniel-ilett/metaballs-urp).

This version leverages the Paricle System in Unity to simulate 2D particle physics.

### For particle system:
- Attach Metaball2DParticles.cs to Particle System GameObject
- Adjustable particle size and color

### For Renderer Feature:
- Set the max limit for number of metaballs and outline size
- Noise texture for distortion

## Limitations
Currently, metaballs will only be drawn/previewed in Play mode. Since it is a screen space effect, then the metablls will draw over any object visible by the camera. To ensure that objects are rendered over the metaballs, you can add a new overlay camera with a dedicated layer and Renderer for rendering the foreground objects.
The shader is also setup to work only with orthographic camera.

## Software
Created in Unity 2021.3.29f1 with URP 12.1.12.