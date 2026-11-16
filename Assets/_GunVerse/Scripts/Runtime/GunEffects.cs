using UnityEngine;
using UnityEngine.Rendering;

namespace GunVerse
{
    public static class GunEffects
    {
        private const string EFFECT_MATERIAL_PATH = "Effects/EffectMaterial";
        private const string TRAIL_MATERIAL_PATH = "Effects/TrailMaterial";
        private const float MUZZLE_FLASH_LIFETIME = 0.06f;
        private const float TRAIL_MIN_VERTEX_DISTANCE = 0.05f;
        private const float TRAIL_START_ALPHA = 0.9f;
        private const float TRAIL_MID_ALPHA = 0.5f;
        private const float TRAIL_MID_TIME = 0.45f;
        private const float TRAIL_MIN_WIDTH = 0.02f;
        private const float MIN_PROJECTILE_SPEED = 0.1f;
        private const float IMPACT_LIFETIME = 0.12f;
        private const float SPARK_LIFETIME = 0.18f;
        private const float SPARK_GRAVITY = 12f;
        private const float SPARK_MIN_SPEED = 2.5f;
        private const float SPARK_MAX_SPEED = 5f;
        private const float SPARK_SPREAD = 0.8f;
        private const float IMPACT_START_SCALE = 0.08f;
        private const float IMPACT_END_SCALE = 0.22f;
        private const float SPARK_START_SCALE = 0.04f;
        private const float SPARK_END_SCALE = 0.005f;
        private const int SPARK_COUNT = 2;
        private const float MIN_DIRECTION_SQR = 0.0001f;
        private const int IGNORE_RAYCAST_LAYER = 2;
        private const int PICKUP_BURST_COUNT = 10;
        private const int PICKUP_BURST_POOL_SIZE = 4;
        private const float PICKUP_BURST_MIN_LIFETIME = 0.3f;
        private const float PICKUP_BURST_MAX_LIFETIME = 0.5f;
        private const float PICKUP_BURST_MIN_SPEED = 1.8f;
        private const float PICKUP_BURST_MAX_SPEED = 3.4f;
        private const float PICKUP_BURST_MIN_SIZE = 0.06f;
        private const float PICKUP_BURST_MAX_SIZE = 0.12f;
        private const float PICKUP_BURST_GRAVITY = 0.6f;
        private const int PICKUP_BURST_MAX_PARTICLES = 64;

        private static readonly int BASE_COLOR_ID = Shader.PropertyToID("_BaseColor");
        private static readonly bool muzzleFlashDisabled = true;
        private static readonly MaterialPropertyBlock colorBlock = new MaterialPropertyBlock();
        private static readonly Color MuzzleFlashColor = new Color(1f, 0.75f, 0.3f, 1f);
        private static readonly Color ImpactColor = new Color(0.85f, 0.75f, 0.55f, 1f);
        private static readonly Color PlayerImpactColor = new Color(0.85f, 0.15f, 0.1f, 1f);

        private static Material effectMaterial;
        private static bool hasLoadedMaterial;
        private static Material trailMaterial;
        private static bool hasLoadedTrailMaterial;
        private static ParticleSystem[] pickupBurstPool;
        private static GameObject pickupBurstRoot;
        private static int pickupBurstCursor;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPickupBurstPool()
        {
            pickupBurstPool = null;
            pickupBurstRoot = null;
            pickupBurstCursor = 0;
        }

        public static void SpawnMuzzleFlash(Vector3 position, Vector3 direction, float scale)
        {
            // Muzzle flash VFX is currently broken; keep fully disabled until fixed.
            if (muzzleFlashDisabled)
            {
                return;
            }

            Quaternion rotation = direction.sqrMagnitude > MIN_DIRECTION_SQR
                ? Quaternion.LookRotation(direction)
                : Quaternion.identity;

            CreateEffect(
                PrimitiveType.Sphere,
                position,
                rotation,
                MuzzleFlashColor,
                MUZZLE_FLASH_LIFETIME,
                Vector3.one * (scale * 0.5f),
                Vector3.one * scale,
                Vector3.zero,
                0f);
        }

        public static TrailRenderer AttachProjectileTrail(GameObject projectileInstance, Color color, float width, float length, float speed)
        {
            Material material = GetTrailMaterial();
            if (material == null || projectileInstance == null)
            {
                return null;
            }

            float clampedWidth = Mathf.Max(width, TRAIL_MIN_WIDTH);
            TrailRenderer trail = projectileInstance.AddComponent<TrailRenderer>();
            trail.sharedMaterial = material;
            trail.time = Mathf.Max(0f, length) / Mathf.Max(speed, MIN_PROJECTILE_SPEED);
            trail.minVertexDistance = TRAIL_MIN_VERTEX_DISTANCE;
            trail.autodestruct = false;
            trail.emitting = true;
            trail.alignment = LineAlignment.View;
            trail.textureMode = LineTextureMode.Stretch;
            trail.numCapVertices = 0;
            trail.numCornerVertices = 0;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.widthCurve = new AnimationCurve(new Keyframe(0f, clampedWidth), new Keyframe(1f, 0f));
            trail.colorGradient = CreateTrailGradient(color);
            trail.Clear();
            return trail;
        }

        private static Gradient CreateTrailGradient(Color color)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[]
                {
                    new GradientAlphaKey(TRAIL_START_ALPHA, 0f),
                    new GradientAlphaKey(TRAIL_MID_ALPHA, TRAIL_MID_TIME),
                    new GradientAlphaKey(0f, 1f)
                });
            return gradient;
        }

        public static void SpawnPickupBurst(Vector3 position, Color color)
        {
            ParticleSystem burst = GetPickupBurst();
            if (burst == null)
            {
                return;
            }

            burst.Clear(true);
            burst.transform.position = position;

            ParticleSystem.MainModule main = burst.main;
            main.startColor = color;

            burst.Play();
            burst.Emit(PICKUP_BURST_COUNT);
        }

        public static void SpawnImpact(Vector3 position, Vector3 normal, bool isPlayerHit)
        {
            Vector3 impactNormal = normal.sqrMagnitude > MIN_DIRECTION_SQR ? normal.normalized : Vector3.up;
            Color color = isPlayerHit ? PlayerImpactColor : ImpactColor;

            CreateEffect(
                PrimitiveType.Sphere,
                position,
                Quaternion.LookRotation(impactNormal),
                color,
                IMPACT_LIFETIME,
                Vector3.one * IMPACT_START_SCALE,
                Vector3.one * IMPACT_END_SCALE,
                Vector3.zero,
                0f);

            for (int i = 0; i < SPARK_COUNT; i++)
            {
                Vector3 sparkDirection = (impactNormal + Random.insideUnitSphere * SPARK_SPREAD).normalized;
                Vector3 velocity = sparkDirection * Random.Range(SPARK_MIN_SPEED, SPARK_MAX_SPEED);

                CreateEffect(
                    PrimitiveType.Cube,
                    position,
                    Random.rotation,
                    color,
                    SPARK_LIFETIME,
                    Vector3.one * SPARK_START_SCALE,
                    Vector3.one * SPARK_END_SCALE,
                    velocity,
                    SPARK_GRAVITY);
            }
        }

        private static GameObject CreateEffect(
            PrimitiveType primitiveType,
            Vector3 position,
            Quaternion rotation,
            Color color,
            float lifetime,
            Vector3 startScale,
            Vector3 endScale,
            Vector3 velocity,
            float gravity)
        {
            GameObject effect = GameObject.CreatePrimitive(primitiveType);
            effect.name = "GunEffect";
            effect.layer = IGNORE_RAYCAST_LAYER;
            effect.transform.SetPositionAndRotation(position, rotation);

            Collider effectCollider = effect.GetComponent<Collider>();
            if (effectCollider != null)
            {
                effectCollider.enabled = false;
                Object.Destroy(effectCollider);
            }

            MeshRenderer meshRenderer = effect.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = GetEffectMaterial();
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            colorBlock.Clear();
            colorBlock.SetColor(BASE_COLOR_ID, color);
            meshRenderer.SetPropertyBlock(colorBlock);

            TransientEffect transientEffect = effect.AddComponent<TransientEffect>();
            transientEffect.Initialize(lifetime, startScale, endScale, velocity, gravity);
            return effect;
        }

        private static ParticleSystem GetPickupBurst()
        {
            if (pickupBurstPool == null)
            {
                CreatePickupBurstPool();
            }

            if (pickupBurstPool == null || pickupBurstPool.Length == 0)
            {
                return null;
            }

            ParticleSystem burst = pickupBurstPool[pickupBurstCursor];
            pickupBurstCursor = (pickupBurstCursor + 1) % pickupBurstPool.Length;
            return burst;
        }

        private static void CreatePickupBurstPool()
        {
            Material material = GetTrailMaterial();
            if (material == null)
            {
                material = GetEffectMaterial();
            }

            if (material == null)
            {
                return;
            }

            pickupBurstRoot = new GameObject("PickupBurstPool");
            Object.DontDestroyOnLoad(pickupBurstRoot);
            pickupBurstPool = new ParticleSystem[PICKUP_BURST_POOL_SIZE];

            for (int i = 0; i < pickupBurstPool.Length; i++)
            {
                pickupBurstPool[i] = CreatePickupBurstSystem(pickupBurstRoot.transform, material, i);
            }
        }

        private static ParticleSystem CreatePickupBurstSystem(Transform parent, Material material, int index)
        {
            GameObject burstObject = new GameObject("PickupBurst" + index);
            burstObject.transform.SetParent(parent, false);

            ParticleSystem particleSystem = burstObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particleSystem.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = PICKUP_BURST_MAX_LIFETIME;
            main.startLifetime = new ParticleSystem.MinMaxCurve(PICKUP_BURST_MIN_LIFETIME, PICKUP_BURST_MAX_LIFETIME);
            main.startSpeed = new ParticleSystem.MinMaxCurve(PICKUP_BURST_MIN_SPEED, PICKUP_BURST_MAX_SPEED);
            main.startSize = new ParticleSystem.MinMaxCurve(PICKUP_BURST_MIN_SIZE, PICKUP_BURST_MAX_SIZE);
            main.startColor = ArtPalette.Ui.Cream;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = PICKUP_BURST_GRAVITY;
            main.maxParticles = PICKUP_BURST_MAX_PARTICLES;

            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(CreatePickupBurstGradient());

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
                1f,
                new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));

            ParticleSystemRenderer particleRenderer = burstObject.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sharedMaterial = material;
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return particleSystem;
        }

        private static Gradient CreatePickupBurstGradient()
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            return gradient;
        }

        private static Material GetEffectMaterial()
        {
            if (!hasLoadedMaterial)
            {
                effectMaterial = Resources.Load<Material>(EFFECT_MATERIAL_PATH);
                hasLoadedMaterial = true;

                if (effectMaterial == null)
                {
                    Debug.LogWarning($"[{nameof(GunEffects)}] Missing effect material at Resources/{EFFECT_MATERIAL_PATH}.");
                }
            }

            return effectMaterial;
        }

        private static Material GetTrailMaterial()
        {
            if (!hasLoadedTrailMaterial)
            {
                trailMaterial = Resources.Load<Material>(TRAIL_MATERIAL_PATH);
                hasLoadedTrailMaterial = true;

                if (trailMaterial == null)
                {
                    Debug.LogWarning($"[{nameof(GunEffects)}] Missing trail material at Resources/{TRAIL_MATERIAL_PATH}.");
                }
            }

            return trailMaterial;
        }
    }
}
