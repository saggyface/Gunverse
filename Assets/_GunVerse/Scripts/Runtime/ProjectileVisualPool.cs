using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace GunVerse
{
    public static class ProjectileVisualPool
    {
        private const string POOL_ROOT_NAME = "ProjectileVisualPool";
        private const int POOL_PREWARM_COUNT = 2;
        private const ulong SELF_SOURCE_FLAG = 1UL << 63;

        private static readonly Dictionary<ulong, GameObject> prototypes = new Dictionary<ulong, GameObject>();
        private static readonly Dictionary<ulong, Stack<ProjectileVisual>> pools =
            new Dictionary<ulong, Stack<ProjectileVisual>>();
        private static readonly Dictionary<ProjectileVisualKey, ProjectileVisual> activeVisuals =
            new Dictionary<ProjectileVisualKey, ProjectileVisual>();

        private readonly struct ProjectileVisualKey : System.IEquatable<ProjectileVisualKey>
        {
            private readonly ulong shooterClientId;
            private readonly uint shotId;

            public ProjectileVisualKey(ulong shooter, uint shot)
            {
                shooterClientId = shooter;
                shotId = shot;
            }

            public bool Equals(ProjectileVisualKey other)
            {
                return shooterClientId == other.shooterClientId && shotId == other.shotId;
            }

            public override bool Equals(object obj)
            {
                return obj is ProjectileVisualKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return System.HashCode.Combine(shooterClientId, shotId);
            }
        }

        private static NetworkManager networkManager;
        private static Transform poolRoot;
        private static bool initialized;

        public static void Initialize(NetworkManager manager)
        {
            if (manager == null)
            {
                return;
            }

            if (initialized && networkManager == manager)
            {
                return;
            }

            networkManager = manager;
            CreatePoolRoot();
            prototypes.Clear();
            pools.Clear();
            initialized = true;
        }

        public static ProjectileVisual Spawn(
            ProjectileShotData shot,
            bool localShooter,
            Vector3 renderedMuzzle,
            GameObject sourceOverride)
        {
            if (!initialized)
            {
                Initialize(NetworkManager.Singleton);
            }

            ulong sourceKey = GetSourceKey(shot);
            GameObject source = sourceOverride;

            if (shot.selfProjectile)
            {
                if (source == null && shot.selfProjectileNetworkObjectId != 0)
                {
                    source = FindSpawnedSource(shot.selfProjectileNetworkObjectId);
                }

                if (source == null)
                {
                    source = FindSourcePrefab((uint)shot.visualPrefabHash);
                }

                if (source == null)
                {
                    source = FindSourcePrefab((uint)shot.weaponHash);
                }
            }
            else if (source == null)
            {
                source = FindSourcePrefab((uint)shot.visualPrefabHash);
                if (source == null)
                {
                    source = FindSourcePrefab((uint)shot.weaponHash);
                }
            }

            if (!prototypes.TryGetValue(sourceKey, out GameObject prototype))
            {
                if (!TryBuildPrototype(sourceKey, source))
                {
                    ProjectileTrace.Log(
                        "visual missing prefab hash=" + (uint)shot.visualPrefabHash +
                        " self=" + shot.selfProjectile +
                        " object=" + shot.selfProjectileNetworkObjectId);
                    return null;
                }

                prototype = prototypes[sourceKey];
            }

            ProjectileVisual visual = GetPooledVisual(sourceKey, prototype);
            if (visual == null)
            {
                return null;
            }

            visual.SourceKey = sourceKey;
            visual.transform.SetParent(poolRoot, false);
            if (!visual.Begin(shot, localShooter, renderedMuzzle))
            {
                Return(visual);
                return null;
            }

            activeVisuals[new ProjectileVisualKey(shot.shooter, shot.shotId)] = visual;
            return visual;
        }

        public static void ReturnByShot(ulong shooterClientId, uint shotId)
        {
            ProjectileVisualKey key = new ProjectileVisualKey(shooterClientId, shotId);
            if (activeVisuals.TryGetValue(key, out ProjectileVisual visual))
            {
                Return(visual);
            }
        }

        public static void Return(ProjectileVisual visual)
        {
            if (visual == null)
            {
                return;
            }

            ulong sourceKey = visual.SourceKey;
            activeVisuals.Remove(new ProjectileVisualKey(visual.ShooterClientId, visual.ShotId));
            visual.ResetForPool();
            visual.transform.SetParent(poolRoot, false);
            visual.gameObject.SetActive(false);

            if (!pools.TryGetValue(sourceKey, out Stack<ProjectileVisual> pool))
            {
                pool = new Stack<ProjectileVisual>();
                pools[sourceKey] = pool;
            }

            pool.Push(visual);
        }

        public static void ReturnAll()
        {
            if (poolRoot == null)
            {
                return;
            }

            ProjectileVisual[] visuals = poolRoot.GetComponentsInChildren<ProjectileVisual>(true);
            foreach (ProjectileVisual visual in visuals)
            {
                if (visual.IsActive)
                {
                    Return(visual);
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            prototypes.Clear();
            pools.Clear();
            activeVisuals.Clear();
            networkManager = null;
            poolRoot = null;
            initialized = false;
        }

        private static ulong GetSourceKey(in ProjectileShotData shot)
        {
            if (shot.selfProjectile && shot.selfProjectileNetworkObjectId != 0)
            {
                return SELF_SOURCE_FLAG | shot.selfProjectileNetworkObjectId;
            }

            return (uint)shot.visualPrefabHash;
        }

        private static ProjectileVisual GetPooledVisual(ulong sourceKey, GameObject prototype)
        {
            if (pools.TryGetValue(sourceKey, out Stack<ProjectileVisual> pool) && pool.Count > 0)
            {
                return pool.Pop();
            }

            GameObject instance = Object.Instantiate(prototype, poolRoot, false);
            instance.SetActive(false);
            ProjectileVisual visual = instance.GetComponent<ProjectileVisual>();
            if (visual == null)
            {
                Object.Destroy(instance);
                return null;
            }

            visual.enabled = true;
            return visual;
        }

        private static bool TryBuildPrototype(ulong sourceKey, GameObject source)
        {
            if (prototypes.ContainsKey(sourceKey))
            {
                return true;
            }

            if (source == null)
            {
                return false;
            }

            GunItem sourceGun = source.GetComponent<GunItem>();
            Vector3 sourceScale = sourceGun != null && source.scene.IsValid()
                ? sourceGun.OriginalWorldScale
                : source.transform.localScale;

            GameObject prototype = Object.Instantiate(source, poolRoot, false);
            prototype.name = source.name + "_ProjectileVisualPrototype";
            prototype.SetActive(false);
            StripVisual(prototype);
            prototype.transform.localScale = sourceScale;

            ProjectileVisual visual = prototype.AddComponent<ProjectileVisual>();
            visual.enabled = false;

            prototypes[sourceKey] = prototype;
            pools[sourceKey] = new Stack<ProjectileVisual>();

            for (int index = 0; index < POOL_PREWARM_COUNT; index++)
            {
                GameObject instance = Object.Instantiate(prototype, poolRoot, false);
                instance.SetActive(false);
                ProjectileVisual pooledVisual = instance.GetComponent<ProjectileVisual>();
                if (pooledVisual != null)
                {
                    pooledVisual.enabled = true;
                    pools[sourceKey].Push(pooledVisual);
                }
            }

            ProjectileTrace.Log(
                "visual prototype key=" + sourceKey +
                " prefab=" + source.name +
                " self=" + ((sourceKey & SELF_SOURCE_FLAG) != 0));
            return true;
        }

        private static GameObject FindSpawnedSource(ulong networkObjectId)
        {
            if (networkManager == null ||
                networkManager.SpawnManager == null ||
                networkManager.SpawnManager.SpawnedObjects == null)
            {
                return null;
            }

            return networkManager.SpawnManager.SpawnedObjects.TryGetValue(
                networkObjectId,
                out NetworkObject networkObject) && networkObject != null
                ? networkObject.gameObject
                : null;
        }

        private static GameObject FindSourcePrefab(uint hash)
        {
            if (networkManager == null)
            {
                return null;
            }

            foreach (NetworkPrefab networkPrefab in networkManager.NetworkConfig.Prefabs.Prefabs)
            {
                if (networkPrefab.Prefab == null ||
                    networkPrefab.Override != NetworkPrefabOverride.None ||
                    !networkPrefab.Prefab.TryGetComponent(out NetworkObject networkObject))
                {
                    continue;
                }

                if (networkObject.PrefabIdHash == hash)
                {
                    return networkPrefab.Prefab;
                }
            }

            return null;
        }

        private static void StripVisual(GameObject visual)
        {
            foreach (NetworkBehaviour behaviour in visual.GetComponentsInChildren<NetworkBehaviour>(true))
            {
                behaviour.enabled = false;
            }

            foreach (NetworkObject networkObject in visual.GetComponentsInChildren<NetworkObject>(true))
            {
                networkObject.enabled = false;
            }

            foreach (NetworkTransform networkTransform in visual.GetComponentsInChildren<NetworkTransform>(true))
            {
                networkTransform.enabled = false;
            }

            foreach (Rigidbody rigidbody in visual.GetComponentsInChildren<Rigidbody>(true))
            {
                rigidbody.isKinematic = true;
                rigidbody.detectCollisions = false;
                rigidbody.useGravity = false;
            }

            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            foreach (GunItemLabelVisual label in visual.GetComponentsInChildren<GunItemLabelVisual>(true))
            {
                label.gameObject.SetActive(false);
            }

            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = true;
            }

            SetLayerRecursively(visual, 0);
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;
            foreach (Transform child in root.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private static void CreatePoolRoot()
        {
            if (poolRoot != null)
            {
                return;
            }

            GameObject root = new GameObject(POOL_ROOT_NAME);
            poolRoot = root.transform;

            if (networkManager != null)
            {
                poolRoot.SetParent(networkManager.transform, false);
            }
            else
            {
                Object.DontDestroyOnLoad(root);
            }
        }
    }
}
