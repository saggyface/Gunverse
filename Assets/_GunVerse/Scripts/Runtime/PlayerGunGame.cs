using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace GunVerse
{
    public sealed class PlayerGunGame : NetworkBehaviour
    {
        private readonly NetworkVariable<int> level = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<FixedString128Bytes> weaponName = new NetworkVariable<FixedString128Bytes>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public int Level => level.Value;
        public string WeaponName => weaponName.Value.ToString();

        internal void SetProgressServer(int newLevel, string newWeaponName)
        {
            if (!IsServer)
            {
                return;
            }

            int clampedLevel = Mathf.Max(0, newLevel);
            FixedString128Bytes sanitizedName = new FixedString128Bytes(newWeaponName ?? string.Empty);

            if (!weaponName.Value.Equals(sanitizedName))
            {
                weaponName.Value = sanitizedName;
            }

            if (level.Value != clampedLevel)
            {
                level.Value = clampedLevel;
            }
        }
    }
}
