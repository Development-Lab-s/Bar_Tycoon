using UnityEditor;
using UnityEngine;
using _00._Work.Lusaload._02._Scripts.SO;

public static class AlcoholAssetCreator
{
    private const string Folder = "Assets/00. Work/Lusaload/05. SO/Alcohol";

    [MenuItem("Tools/Lusaload/Create Drink & Garnish Assets")]
    public static void CreateAll()
    {
        var listSO = AssetDatabase.LoadAssetAtPath<AlcoholListSO>($"{Folder}/_AlcoholListSO.asset");

        Make("OrangeJuice",    "오렌지 주스",   IngredientCategory.Drink,   listSO);
        Make("PineappleJuice", "파인애플 주스", IngredientCategory.Drink,   listSO);
        Make("CranberryJuice", "크랜베리 주스", IngredientCategory.Drink,   listSO);
        Make("TonicWater",     "토닉 워터",     IngredientCategory.Drink,   listSO);
        Make("LemonJuice",     "레몬 주스",     IngredientCategory.Drink,   listSO);
        Make("Grenadine",      "그레나딘 시럽", IngredientCategory.Drink,   listSO);

        Make("Lime",           "라임",          IngredientCategory.Garnish, listSO);
        Make("Cherry",         "체리",          IngredientCategory.Garnish, listSO);
        Make("Mint",           "민트",          IngredientCategory.Garnish, listSO);
        Make("Salt",           "소금",          IngredientCategory.Garnish, listSO);
        Make("Olive",          "올리브",        IngredientCategory.Garnish, listSO);
        Make("LemonSlice",     "레몬 슬라이스", IngredientCategory.Garnish, listSO);

        if (listSO != null) EditorUtility.SetDirty(listSO);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[AlcoholAssetCreator] Drink 6개 + Garnish 6개 생성 완료");
    }

    private static void Make(string fileName, string displayName, IngredientCategory cat, AlcoholListSO list)
    {
        var path = $"{Folder}/{fileName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<BaseAlcoholDataSO>(path);
        if (existing != null)
        {
            if (list != null && !list.alcoholList.Contains(existing))
                list.alcoholList.Add(existing);
            return;
        }

        var so = ScriptableObject.CreateInstance<BaseAlcoholDataSO>();
        so.alcoholName = displayName;
        so.category    = cat;
        AssetDatabase.CreateAsset(so, path);

        if (list != null && !list.alcoholList.Contains(so))
            list.alcoholList.Add(so);
    }
}
