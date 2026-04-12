/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP
{
    public class SolarSystem : MonoBehaviour
    {
        [System.Serializable]
        public class PlanetData
        {
            public GameObject planet = null!;
            public float orbitRadius = 10f;
            public float orbitSpeed = 1f;
            public float rotationSpeed = 1f;
            public Vector3 orbitTilt = Vector3.zero;
        }

        public GameObject sun = null!;
        public PlanetData[] planets = null!;
        public float globalOrbitSpeedMultiplier = 1f;
        public float globalSizeMultiplier = 1f;

        private void Update()
        {
            if (planets == null) return;

            for (int i = 0; i < planets.Length; i++)
            {
                if (planets[i].planet == null) continue;

                // Calculate orbit position
                var angle = Time.time * planets[i].orbitSpeed * globalOrbitSpeedMultiplier;
                var orbitPosition = Quaternion.Euler(planets[i].orbitTilt) * new Vector3(
                    Mathf.Cos(angle) * planets[i].orbitRadius,
                    0,
                    Mathf.Sin(angle) * planets[i].orbitRadius
                );

                // Update planet position
                planets[i].planet.transform.position = transform.position + orbitPosition;

                // Rotate planet
                planets[i].planet.transform.Rotate(Vector3.up, planets[i].rotationSpeed * Time.deltaTime * 360f);
            }
        }
    }
}
