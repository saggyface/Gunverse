using UnityEngine;

namespace GunVerse
{
    public enum GameplayMapType
    {
        GroceryStore = 0,
        BopBuy = 1
    }

    public static class GameplayMapCatalog
    {
        public const string GROCERY_STORE_SCENE_NAME = "GroceryStore";
        public const string BOPBUY_SCENE_NAME = "BopBuy";

        public static int Count => 2;

        public static string GetSceneName(GameplayMapType map)
        {
            return map switch
            {
                GameplayMapType.BopBuy => BOPBUY_SCENE_NAME,
                _ => GROCERY_STORE_SCENE_NAME
            };
        }

        public static string GetDisplayName(GameplayMapType map)
        {
            return map switch
            {
                GameplayMapType.BopBuy => "BopBuy",
                _ => "Grocery Store"
            };
        }

        public static GameplayMapType GetNext(GameplayMapType map)
        {
            int next = ((int)map + 1) % Mathf.Max(1, Count);
            return (GameplayMapType)next;
        }

        public static bool IsValid(GameplayMapType map)
        {
            return map == GameplayMapType.GroceryStore || map == GameplayMapType.BopBuy;
        }
    }
}
