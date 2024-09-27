using System.Collections.Generic;
using UnityEngine;

namespace Metaballs.Particles
{
    public class Metaball2DParticles : MonoBehaviour
    {
        public static List<ParticleSystem> ParticleSystems = new();

        [SerializeField] private ParticleSystem _particleSystem;

        private void Awake()
        {
            if(_particleSystem == null) 
                if(!TryGetComponent(out _particleSystem))
                    _particleSystem = GetComponentInChildren<ParticleSystem>();

            if (_particleSystem == null) return;
            ParticleSystems.Add(_particleSystem);
        }

        private void OnDestroy()
        {
            if (_particleSystem == null) return;
            ParticleSystems.Remove(_particleSystem);
        }

        private void Reset()
        {
            if (_particleSystem == null)
                if (!TryGetComponent(out _particleSystem))
                    _particleSystem = GetComponentInChildren<ParticleSystem>();
        }
    }

}