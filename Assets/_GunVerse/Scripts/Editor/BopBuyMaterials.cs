using UnityEditor;
using UnityEngine;

namespace GunVerse.EditorTools
{
    public static class BopBuyMaterials
    {
        private const string MATERIALS_FOLDER = "Assets/_GunVerse/Materials";
        private const string URP_LIT_SHADER = "Universal Render Pipeline/Lit";

        private const string FLOOR_PATH = MATERIALS_FOLDER + "/BopBuyFloor.mat";
        private const string WALL_PATH = MATERIALS_FOLDER + "/BopBuyWall.mat";
        private const string CEILING_PATH = MATERIALS_FOLDER + "/BopBuyCeiling.mat";
        private const string BLUE_PATH = MATERIALS_FOLDER + "/BopBuyBlue.mat";
        private const string YELLOW_PATH = MATERIALS_FOLDER + "/BopBuyYellow.mat";
        private const string COUNTER_PATH = MATERIALS_FOLDER + "/BopBuyCounter.mat";
        private const string METAL_PATH = MATERIALS_FOLDER + "/BopBuyMetal.mat";
        private const string CARPET_PATH = MATERIALS_FOLDER + "/BopBuyCarpet.mat";
        private const string WOOD_FLOOR_PATH = MATERIALS_FOLDER + "/BopBuyWoodFloor.mat";
        private const string CHARCOAL_CARPET_PATH = MATERIALS_FOLDER + "/BopBuyCharcoalCarpet.mat";
        private const string COIN_RUBBER_PATH = MATERIALS_FOLDER + "/BopBuyCoinRubber.mat";
        private const string BLACK_WALL_PATH = MATERIALS_FOLDER + "/BopBuyBlackWall.mat";
        private const string WHITE_BLOCK_PATH = MATERIALS_FOLDER + "/BopBuyWhiteBlock.mat";
        private const string BLACK_TRUSS_PATH = MATERIALS_FOLDER + "/BopBuyBlackTruss.mat";
        private const string DUCT_PATH = MATERIALS_FOLDER + "/BopBuyDuct.mat";
        private const string BLUE_FABRIC_PATH = MATERIALS_FOLDER + "/BopBuyBlueFabric.mat";
        private const string YELLOW_TREE_PATH = MATERIALS_FOLDER + "/BopBuyYellowTree.mat";
        private const string WHITE_COUNTER_PATH = MATERIALS_FOLDER + "/BopBuyWhiteCounter.mat";
        private const string GREY_LOCKER_PATH = MATERIALS_FOLDER + "/BopBuyGreyLocker.mat";
        private const string CHROME_PATH = MATERIALS_FOLDER + "/BopBuyChrome.mat";
        private const string RED_ACCENT_PATH = MATERIALS_FOLDER + "/BopBuyRedAccent.mat";
        private const string WOOD_TABLE_PATH = MATERIALS_FOLDER + "/BopBuyWoodTable.mat";

        public static Material Floor => GetOrCreate(FLOOR_PATH, ArtPalette.BopBuy.Floor, 0.25f, 0f);
        public static Material Wall => GetOrCreate(WALL_PATH, ArtPalette.BopBuy.Wall, 0.12f, 0f);
        public static Material Ceiling => GetOrCreate(CEILING_PATH, ArtPalette.BopBuy.Ceiling, 0.08f, 0f);
        public static Material Blue => GetOrCreate(BLUE_PATH, ArtPalette.BopBuy.Blue, 0.3f, 0f);
        public static Material Yellow => GetOrCreate(YELLOW_PATH, ArtPalette.BopBuy.Yellow, 0.35f, 0f);
        public static Material Counter => GetOrCreate(COUNTER_PATH, ArtPalette.BopBuy.Counter, 0.3f, 0.05f);
        public static Material Metal => GetOrCreate(METAL_PATH, ArtPalette.BopBuy.Metal, 0.4f, 0.35f);
        public static Material Carpet => GetOrCreate(CARPET_PATH, ArtPalette.BopBuy.Carpet, 0.05f, 0f);
        public static Material WoodFloor => GetOrCreate(WOOD_FLOOR_PATH, ArtPalette.BopBuy.WoodAccent, 0.28f, 0f);
        public static Material CharcoalCarpet => GetOrCreate(CHARCOAL_CARPET_PATH, ArtPalette.BopBuy.CharcoalCarpet, 0.03f, 0f);
        public static Material CoinRubber => GetOrCreate(COIN_RUBBER_PATH, ArtPalette.BopBuy.CoinRubber, 0.12f, 0f);
        public static Material BlackWall => GetOrCreate(BLACK_WALL_PATH, ArtPalette.BopBuy.CoinRubber, 0.1f, 0f);
        public static Material WhiteBlock => GetOrCreate(WHITE_BLOCK_PATH, ArtPalette.BopBuy.WhiteBlock, 0.1f, 0f);
        public static Material BlackTruss => GetOrCreate(BLACK_TRUSS_PATH, ArtPalette.BopBuy.CoinRubber, 0.25f, 0.35f);
        public static Material Duct => GetOrCreate(DUCT_PATH, ArtPalette.Darken(ArtPalette.BopBuy.Metal, 0.8f), 0.3f, 0.25f);
        public static Material BlueFabric => GetOrCreate(BLUE_FABRIC_PATH, ArtPalette.BopBuy.BlueFabric, 0.05f, 0f);
        public static Material YellowTree => GetOrCreate(YELLOW_TREE_PATH, ArtPalette.BopBuy.Yellow, 0.2f, 0f);
        public static Material WhiteCounter => GetOrCreate(WHITE_COUNTER_PATH, ArtPalette.BopBuy.WhiteCounter, 0.25f, 0f);
        public static Material GreyLocker => GetOrCreate(GREY_LOCKER_PATH, ArtPalette.BopBuy.Metal, 0.25f, 0.2f);
        public static Material Chrome => GetOrCreate(CHROME_PATH, ArtPalette.BopBuy.Chrome, 0.7f, 0.65f);
        public static Material RedAccent => GetOrCreate(RED_ACCENT_PATH, ArtPalette.BopBuy.Accent, 0.3f, 0f);
        public static Material WoodTable => GetOrCreate(WOOD_TABLE_PATH, ArtPalette.BopBuy.WoodAccent, 0.25f, 0f);

        private static Material GetOrCreate(string path, Color color, float smoothness, float metallic)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                EnsureFolder(MATERIALS_FOLDER);
                Shader shader = Shader.Find(URP_LIT_SHADER);
                if (shader == null)
                {
                    Debug.LogError("[BopBuyMaterials] URP Lit shader not found.");
                    return null;
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            bool isDirty = false;

            if (material.HasProperty("_BaseColor") && material.GetColor("_BaseColor") != color)
            {
                material.SetColor("_BaseColor", color);
                isDirty = true;
            }

            if (!Mathf.Approximately(material.GetFloat("_Smoothness"), smoothness))
            {
                material.SetFloat("_Smoothness", smoothness);
                isDirty = true;
            }

            if (!Mathf.Approximately(material.GetFloat("_Metallic"), metallic))
            {
                material.SetFloat("_Metallic", metallic);
                isDirty = true;
            }

            if (isDirty)
            {
                EditorUtility.SetDirty(material);
            }

            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            string folderName = System.IO.Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
