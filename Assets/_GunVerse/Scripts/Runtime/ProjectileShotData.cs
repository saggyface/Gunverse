using Unity.Netcode;
using UnityEngine;

namespace GunVerse
{
    public struct ProjectileShotData : INetworkSerializable
    {
        public uint shotId;
        public uint shotGroupId;
        public ulong shooter;
        public bool selfProjectile;
        public ulong selfProjectileNetworkObjectId;
        public Vector3 origin;
        public Vector3 direction;
        public Vector3 spin;
        public float speed;
        public float radius;
        public float gravityScale;
        public float lifetime;
        public float bounceDamping;
        public float angularDamping;
        public float mass;
        public float damage;
        public float releaseDistance;
        public float spawnGraceSeconds;
        public int maxBounces;
        public int visualPrefabHash;
        public int weaponHash;
        public int pelletIndex;
        public int pelletCount;
        public int burstIndex;
        public int burstCount;
        public double serverTime;
        public bool useItemShape;
        public byte shapeType;
        public Vector3 localHalfExtents;
        public Vector3 localCenterOffset;
        public Vector3 localCenter;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref shotId);
            serializer.SerializeValue(ref shotGroupId);
            serializer.SerializeValue(ref shooter);
            serializer.SerializeValue(ref selfProjectile);
            serializer.SerializeValue(ref selfProjectileNetworkObjectId);
            serializer.SerializeValue(ref origin);
            serializer.SerializeValue(ref direction);
            serializer.SerializeValue(ref spin);
            serializer.SerializeValue(ref speed);
            serializer.SerializeValue(ref radius);
            serializer.SerializeValue(ref gravityScale);
            serializer.SerializeValue(ref lifetime);
            serializer.SerializeValue(ref bounceDamping);
            serializer.SerializeValue(ref angularDamping);
            serializer.SerializeValue(ref mass);
            serializer.SerializeValue(ref damage);
            serializer.SerializeValue(ref releaseDistance);
            serializer.SerializeValue(ref spawnGraceSeconds);
            serializer.SerializeValue(ref maxBounces);
            serializer.SerializeValue(ref visualPrefabHash);
            serializer.SerializeValue(ref weaponHash);
            serializer.SerializeValue(ref pelletIndex);
            serializer.SerializeValue(ref pelletCount);
            serializer.SerializeValue(ref burstIndex);
            serializer.SerializeValue(ref burstCount);
            serializer.SerializeValue(ref serverTime);
            serializer.SerializeValue(ref useItemShape);
            serializer.SerializeValue(ref shapeType);
            serializer.SerializeValue(ref localHalfExtents);
            serializer.SerializeValue(ref localCenterOffset);
            serializer.SerializeValue(ref localCenter);
        }
    }

    public struct ProjectileShotVolley : INetworkSerializable
    {
        public ProjectileShotData[] Shots;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            int shotCount = Shots?.Length ?? 0;
            serializer.SerializeValue(ref shotCount);

            if (serializer.IsReader)
            {
                Shots = new ProjectileShotData[shotCount];
            }

            for (int index = 0; index < shotCount; index++)
            {
                Shots[index].NetworkSerialize(serializer);
            }
        }
    }
}
