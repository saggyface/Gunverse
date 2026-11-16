using System.Collections.Generic;
using UnityEngine;

namespace GunVerse
{
    [CreateAssetMenu(menuName = "Gun Verse/Gun Game Ladder", fileName = "GunGameLadder")]
    public sealed class GunGameLadder : ScriptableObject
    {
        [SerializeField] private List<GameObject> weapons = new List<GameObject>();

        public int Count => weapons.Count;

        public GameObject GetWeapon(int index)
        {
            return index >= 0 && index < weapons.Count ? weapons[index] : null;
        }

        public string GetWeaponName(int index)
        {
            GameObject weapon = GetWeapon(index);
            return weapon != null && weapon.TryGetComponent(out GunItem gunItem)
                ? gunItem.DisplayName
                : string.Empty;
        }
    }
}
