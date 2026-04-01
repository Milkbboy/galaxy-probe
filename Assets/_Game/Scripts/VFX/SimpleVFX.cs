using UnityEngine;

namespace DrillCorp.VFX
{
    /// <summary>
    /// 간단한 VFX 시스템 - 코드로 기본 이펙트 생성
    /// </summary>
    public static class SimpleVFX
    {
        private static Material _defaultParticleMaterial;

        /// <summary>
        /// 기본 파티클 머티리얼 가져오기
        /// </summary>
        private static Material GetDefaultParticleMaterial()
        {
            if (_defaultParticleMaterial == null)
            {
                // Unity 기본 파티클 머티리얼 사용
                _defaultParticleMaterial = new Material(Shader.Find("Particles/Standard Unlit"));
            }
            return _defaultParticleMaterial;
        }

        /// <summary>
        /// ParticleSystem에 기본 머티리얼 적용
        /// </summary>
        private static void ApplyDefaultMaterial(GameObject effectObj)
        {
            var renderer = effectObj.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.material = GetDefaultParticleMaterial();
            }
        }

        /// <summary>
        /// 근접 공격 이펙트 (붉은색 슬래시)
        /// </summary>
        public static void PlayMeleeHit(Vector3 position)
        {
            GameObject effectObj = new GameObject("MeleeHitVFX");
            effectObj.transform.position = position;

            var ps = effectObj.AddComponent<ParticleSystem>();
            ApplyDefaultMaterial(effectObj);

            var main = ps.main;
            main.startLifetime = 0.3f;
            main.startSpeed = 2f;
            main.startSize = 0.3f;
            main.startColor = new Color(1f, 0.3f, 0.2f, 0.8f); // 붉은색
            main.maxParticles = 8;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 8) });
            emission.rateOverTime = 0;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.1f;

            // 타겟(머신) 방향으로 향하게
            var cameraForward = Camera.main != null ? Camera.main.transform.forward : Vector3.forward;
            effectObj.transform.rotation = Quaternion.LookRotation(-cameraForward);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.red, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            ps.Play();
            Object.Destroy(effectObj, 1f);
        }

        /// <summary>
        /// 투사체 히트 이펙트 (노란색 스파크)
        /// </summary>
        public static void PlayProjectileHit(Vector3 position)
        {
            GameObject effectObj = new GameObject("ProjectileHitVFX");
            effectObj.transform.position = position;

            var ps = effectObj.AddComponent<ParticleSystem>();
            ApplyDefaultMaterial(effectObj);

            var main = ps.main;
            main.startLifetime = 0.4f;
            main.startSpeed = 3f;
            main.startSize = 0.15f;
            main.startColor = new Color(1f, 0.9f, 0.3f, 1f); // 노란색
            main.maxParticles = 12;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 12) });
            emission.rateOverTime = 0;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.yellow, 0f), new GradientColorKey(Color.red, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            ps.Play();
            Object.Destroy(effectObj, 1f);
        }

        /// <summary>
        /// 회피 이펙트 (청록색 잔상)
        /// </summary>
        public static void PlayDodge(Vector3 position)
        {
            GameObject effectObj = new GameObject("DodgeVFX");
            effectObj.transform.position = position;

            var ps = effectObj.AddComponent<ParticleSystem>();
            ApplyDefaultMaterial(effectObj);

            var main = ps.main;
            main.startLifetime = 0.5f;
            main.startSpeed = 0.5f;
            main.startSize = 0.5f;
            main.startColor = new Color(0f, 1f, 1f, 0.5f); // 청록색
            main.maxParticles = 5;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 5) });
            emission.rateOverTime = 0;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.3f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.cyan, 0f), new GradientColorKey(Color.blue, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            ps.Play();
            Object.Destroy(effectObj, 1f);
        }

        /// <summary>
        /// Bug가 데미지 받을 때 (녹색 스플래시)
        /// </summary>
        public static void PlayBugHit(Vector3 position)
        {
            GameObject effectObj = new GameObject("BugHitVFX");
            effectObj.transform.position = position;

            var ps = effectObj.AddComponent<ParticleSystem>();
            ApplyDefaultMaterial(effectObj);

            var main = ps.main;
            main.startLifetime = 0.25f;
            main.startSpeed = 2f;
            main.startSize = 0.2f;
            main.startColor = new Color(0.3f, 0.8f, 0.2f, 0.9f); // 녹색 (벌레 피)
            main.maxParticles = 6;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 6) });
            emission.rateOverTime = 0;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.5f, 1f, 0.3f), 0f), new GradientColorKey(new Color(0.2f, 0.5f, 0.1f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            ps.Play();
            Object.Destroy(effectObj, 1f);
        }

        /// <summary>
        /// 폭발 이펙트 (주황색 큰 폭발)
        /// </summary>
        public static void PlayExplosion(Vector3 position, float radius = 3f)
        {
            GameObject effectObj = new GameObject("ExplosionVFX");
            effectObj.transform.position = position;

            var ps = effectObj.AddComponent<ParticleSystem>();
            ApplyDefaultMaterial(effectObj);

            // 스케일 계산 (기본 반경 3 기준, 2배 키움)
            float scale = (radius / 3f) * 2f;

            var main = ps.main;
            main.startLifetime = 0.8f;
            main.startSpeed = 6f * scale;
            main.startSize = 1.5f * scale;
            main.startColor = new Color(1f, 0.6f, 0.1f, 1f); // 주황색
            main.maxParticles = 50;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 50) });
            emission.rateOverTime = 0;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f * scale;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.5f),
                new Keyframe(0.2f, 2f),
                new Keyframe(1f, 0.3f)
            ));

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(1f, 0.5f, 0f), 0.3f),
                    new GradientColorKey(new Color(0.8f, 0.2f, 0f), 0.7f),
                    new GradientColorKey(Color.black, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            ps.Play();
            Object.Destroy(effectObj, 1.5f);
        }

        /// <summary>
        /// 방어 이펙트 (회색 방패)
        /// </summary>
        public static void PlayArmorBlock(Vector3 position)
        {
            GameObject effectObj = new GameObject("ArmorBlockVFX");
            effectObj.transform.position = position;

            var ps = effectObj.AddComponent<ParticleSystem>();
            ApplyDefaultMaterial(effectObj);

            var main = ps.main;
            main.startLifetime = 0.3f;
            main.startSpeed = 1f;
            main.startSize = 0.4f;
            main.startColor = new Color(0.7f, 0.7f, 0.7f, 0.7f); // 회색
            main.maxParticles = 6;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 6) });
            emission.rateOverTime = 0;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.2f;

            // 위를 향하게
            effectObj.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.gray, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            ps.Play();
            Object.Destroy(effectObj, 1f);
        }
    }
}
