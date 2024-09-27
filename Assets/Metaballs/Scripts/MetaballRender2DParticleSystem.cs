using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Metaballs.Particles
{
    public class MetaballRender2DParticleSystem : ScriptableRendererFeature
    {
        [System.Serializable]
        public class MetaballRender2DSettingsParticleSystem
        {
            [Tooltip("Metaball Material.")]
            public Material metaballMaterial;

            [Range(0f, 1f), Tooltip("Outline size.")]
            public float outlineSize = 1.0f;

            [Tooltip("How many particles for metaballs.")]
            public int maxParticleCount = 100;

            [Tooltip("Noise Texture for distortion.")]
            public Texture2D noiseTexture;

            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        }

        public MetaballRender2DSettingsParticleSystem settings = new();

        class MetaballRender2DPassParticleSystem : ScriptableRenderPass
        {
            public Texture2D noiseTexture;
            public float outlineSize;
            public int maxParticleCount = 100;
            public Material metaballMaterial;

            private RenderTargetIdentifier _src;
            private string _profilerTag;

            private ComputeBuffer _metaballData;
            private ComputeBuffer _metaballColorData;

            private int _propID_MetaballCount, _propID_OutlineSize, _propID_CameraSize, _propID_MetaballPSData, _propID_MetaballPSColorData, _propID_NoiseTex;

            public void Setup(RenderTargetIdentifier source)
            {
                _src = source;
            }

            public MetaballRender2DPassParticleSystem(string profilerTag)
            {
                _profilerTag = profilerTag;

                _propID_MetaballCount = Shader.PropertyToID("_MetaballCount");
                _propID_OutlineSize = Shader.PropertyToID("_OutlineSize");
                _propID_CameraSize = Shader.PropertyToID("_CameraSize");
                _propID_MetaballPSData = Shader.PropertyToID("_MetaballPSData");
                _propID_MetaballPSColorData = Shader.PropertyToID("_MetaballPSColorData");
                _propID_NoiseTex = Shader.PropertyToID("_NoiseTex");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (Metaball2DParticles.ParticleSystems == null || Metaball2DParticles.ParticleSystems.Count == 0 || metaballMaterial == null) return;

                // First, get the particle counts for each system and total count
                // We have aggregated all the particles into one list. Alternatively, we can also split this up
                int totalParticleCount = 0;
                List<ParticleSystem.Particle> particles = new();
                int[] psParticleCount = new int[Metaball2DParticles.ParticleSystems.Count];

                for (int i = 0; i < Metaball2DParticles.ParticleSystems.Count; i++)
                {
                    ParticleSystem ps = Metaball2DParticles.ParticleSystems[i];
                    ParticleSystem.Particle[] p = new ParticleSystem.Particle[ps.particleCount];
                    int count = ps.GetParticles(p);
                    totalParticleCount += count;
                    particles.AddRange(p);
                    psParticleCount[i] = count;
                }
                if (totalParticleCount == 0) return;
                totalParticleCount = Mathf.Min(totalParticleCount, maxParticleCount);

                // Set up our buffers with all the particle data
                Vector4[] md = new Vector4[totalParticleCount];
                Vector4[] mdColor = new Vector4[totalParticleCount];
                bool exceededMaxCount = false;
                for (int psIndex = 0, particleIndex = 0; psIndex < Metaball2DParticles.ParticleSystems.Count; psIndex++)
                {
                    var ps = Metaball2DParticles.ParticleSystems[psIndex];
                    var count = psParticleCount[psIndex];
                    for (int i = 0; i < count; i++, particleIndex++)
                    {
                        if(particleIndex >= maxParticleCount)
                        {
                            exceededMaxCount = true;
                            break;
                        }
                        var p = particles[particleIndex];
                        Vector2 pos = renderingData.cameraData.camera.WorldToScreenPoint(p.position);
                        float radius = p.GetCurrentSize(ps) * .5f;
                        var c = p.GetCurrentColor(ps);

                        md[particleIndex] = new Vector4(pos.x, pos.y, radius, 1f); // The 4th value is a placeholder.
                        mdColor[particleIndex] = new(c.r * 0.00390625f, c.g * 0.00390625f, c.b * 0.00390625f, c.a * 0.00390625f);
                    }
                    if(exceededMaxCount) break;
                }
                _metaballData.SetData(md);
                _metaballColorData.SetData(mdColor);

                // Rendering metalballs
                CommandBuffer cmd = CommandBufferPool.Get(_profilerTag);

                metaballMaterial.SetInt(_propID_MetaballCount, totalParticleCount);
                metaballMaterial.SetFloat(_propID_OutlineSize, outlineSize);
                metaballMaterial.SetFloat(_propID_CameraSize, renderingData.cameraData.camera.orthographicSize);
                metaballMaterial.SetBuffer(_propID_MetaballPSData, _metaballData);
                metaballMaterial.SetBuffer(_propID_MetaballPSColorData, _metaballColorData);
                metaballMaterial.SetTexture(_propID_NoiseTex, noiseTexture);

                cmd.Blit(_src, _src, metaballMaterial);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                base.OnCameraSetup(cmd, ref renderingData);

                if (_metaballData != null) return;

                _metaballData = new(maxParticleCount, sizeof(float) * 4);
                _metaballColorData = new(maxParticleCount, sizeof(float) * 4);
                Vector4[] md = new Vector4[maxParticleCount];
                Vector4[] mdColor = new Vector4[maxParticleCount];

                _metaballData.SetData(md);
                _metaballColorData.SetData(mdColor);
            }

            internal void DisposeInternal()
            {
                _metaballData?.Dispose();
                _metaballColorData?.Dispose();
            }
        }

        private MetaballRender2DPassParticleSystem _pass;

        public override void Create()
        {
            name = "MetaballsPS (2D)";

            _pass = new("MetaballsPS2D")
            {
                metaballMaterial = settings.metaballMaterial,
                outlineSize = settings.outlineSize,
                noiseTexture = settings.noiseTexture,
                maxParticleCount = settings.maxParticleCount,

                renderPassEvent = settings.renderPassEvent
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            _pass.Setup(renderer.cameraColorTarget);
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            _pass.DisposeInternal();
            base.Dispose(disposing);
        }
    }

}

