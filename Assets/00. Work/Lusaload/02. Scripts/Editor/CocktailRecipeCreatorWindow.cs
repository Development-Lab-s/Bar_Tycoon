using System.Collections.Generic;
using _00._Work.Lusaload._02._Scripts.SO;
using UnityEditor;
using UnityEngine;

namespace _00._Work.Lusaload._02._Scripts.Editor
{
    public class CocktailRecipeCreatorWindow : EditorWindow
    {
        private const float AlcoholButtonWidth = 140f;
        private const float AlcoholButtonHeight = 52f;
        private const float ButtonSpacing = 6f;

        private AlcoholListSO _alcoholListSO;
        private CocktailRecipeDatabaseSO _recipeDatabaseSO;
        private CocktailRecipeSO _editingRecipe;

        private string _cocktailName = string.Empty;
        private Sprite _cocktailIcon;
        private string _saveFolderPath = "Assets/00. Work/Lusaload/05. SO/Cocktail";

        private Vector2 _windowScroll;
        private Vector2 _alcoholScroll;
        private Vector2 _selectedScroll;

        private readonly List<BaseAlcoholDataSO> _selectedAlcohols = new();

        [MenuItem("Tools/Cocktail/Recipe Creator")]
        private static void OpenFromMenu()
        {
            OpenCreateWindow(null, null);
        }

        public static void OpenCreateWindow(AlcoholListSO alcoholListSO, CocktailRecipeDatabaseSO databaseSO)
        {
            CocktailRecipeCreatorWindow window = GetWindow<CocktailRecipeCreatorWindow>("Recipe Creator");
            window.minSize = new Vector2(560f, 650f);

            window._alcoholListSO = alcoholListSO;
            window._recipeDatabaseSO = databaseSO;
            window._editingRecipe = null;

            window.ClearInput();
            window.TryFindAlcoholListSOIfNeeded();
            window.TryFindRecipeDatabaseSOIfNeeded();

            window.Show();
            window.Focus();
        }

        public static void OpenEditWindow(CocktailRecipeSO recipe, AlcoholListSO alcoholListSO, CocktailRecipeDatabaseSO databaseSO)
        {
            CocktailRecipeCreatorWindow window = GetWindow<CocktailRecipeCreatorWindow>("Recipe Editor");
            window.minSize = new Vector2(560f, 650f);

            window._editingRecipe = recipe;
            window._alcoholListSO = alcoholListSO;
            window._recipeDatabaseSO = databaseSO;

            window.TryFindAlcoholListSOIfNeeded();
            window.TryFindRecipeDatabaseSOIfNeeded();

            if (recipe != null)
                window.LoadRecipe(recipe);
            else
                window.ClearInput();

            window.Show();
            window.Focus();
        }

        private void OnEnable()
        {
            TryFindAlcoholListSOIfNeeded();
            TryFindRecipeDatabaseSOIfNeeded();
        }

        private void OnGUI()
        {
            _windowScroll = EditorGUILayout.BeginScrollView(_windowScroll);

            DrawTitle();
            EditorGUILayout.Space(8f);

            DrawReferenceFields();
            EditorGUILayout.Space(8f);

            DrawRecipeInfo();
            EditorGUILayout.Space(8f);

            DrawAlcoholButtons();
            EditorGUILayout.Space(8f);

            DrawSelectedAlcoholList();
            EditorGUILayout.Space(12f);

            DrawBottomButtons();
            EditorGUILayout.Space(8f);

            EditorGUILayout.EndScrollView();
        }

        private void DrawTitle()
        {
            string title = _editingRecipe == null ? "칵테일 레시피 생성" : "칵테일 레시피 수정";
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "AlcoholListSO의 술 목록을 선택하여 CocktailRecipeSO를 생성하거나 수정합니다.",
                MessageType.Info);
        }

        private void DrawReferenceFields()
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("데이터 참조", EditorStyles.boldLabel);

            _alcoholListSO = (AlcoholListSO)EditorGUILayout.ObjectField(
                "Alcohol List SO",
                _alcoholListSO,
                typeof(AlcoholListSO),
                false);

            _recipeDatabaseSO = (CocktailRecipeDatabaseSO)EditorGUILayout.ObjectField(
                "Recipe Database SO",
                _recipeDatabaseSO,
                typeof(CocktailRecipeDatabaseSO),
                false);

            _editingRecipe = (CocktailRecipeSO)EditorGUILayout.ObjectField(
                "Editing Recipe",
                _editingRecipe,
                typeof(CocktailRecipeSO),
                false);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("AlcoholListSO 자동 찾기"))
                TryFindAlcoholListSO();

            if (GUILayout.Button("RecipeDatabaseSO 자동 찾기"))
                TryFindRecipeDatabaseSO();

            EditorGUILayout.EndHorizontal();

            if (_alcoholListSO == null)
                EditorGUILayout.HelpBox("AlcoholListSO를 연결해줘.", MessageType.Warning);

            if (_recipeDatabaseSO == null)
                EditorGUILayout.HelpBox("Recipe Database SO는 선택 사항입니다. 연결하면 생성 후 recipes에 자동 등록됩니다.", MessageType.None);

            EditorGUILayout.EndVertical();
        }

        private void DrawRecipeInfo()
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("레시피 정보", EditorStyles.boldLabel);

            _cocktailName = EditorGUILayout.TextField("Cocktail Name", _cocktailName);
            _cocktailIcon = (Sprite)EditorGUILayout.ObjectField("Cocktail Icon", _cocktailIcon, typeof(Sprite), false);
            _saveFolderPath = EditorGUILayout.TextField("Save Folder", _saveFolderPath);

            EditorGUILayout.EndVertical();
        }

        private void DrawAlcoholButtons()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("술 선택", EditorStyles.boldLabel);

            if (_alcoholListSO == null || _alcoholListSO.alcoholList == null || _alcoholListSO.alcoholList.Count == 0)
            {
                EditorGUILayout.HelpBox("표시할 술이 없습니다.", MessageType.None);
                EditorGUILayout.EndVertical();
                return;
            }

            _alcoholScroll = EditorGUILayout.BeginScrollView(_alcoholScroll, GUILayout.Height(230f));

            float availableWidth = Mathf.Max(100f, position.width - 40f);
            int columnCount = Mathf.Max(1, Mathf.FloorToInt(availableWidth / (AlcoholButtonWidth + ButtonSpacing)));

            int currentColumn = 0;
            EditorGUILayout.BeginHorizontal();

            for (int i = 0; i < _alcoholListSO.alcoholList.Count; i++)
            {
                BaseAlcoholDataSO alcohol = _alcoholListSO.alcoholList[i];
                if (alcohol == null)
                    continue;

                Texture previewTexture = GetSpritePreviewTexture(alcohol.alcoholImage);
                GUIContent content = new GUIContent(alcohol.alcoholName, previewTexture);

                if (GUILayout.Button(content, GUILayout.Width(AlcoholButtonWidth), GUILayout.Height(AlcoholButtonHeight)))
                {
                    if (_selectedAlcohols.Contains(alcohol))
                    {
                        Debug.Log($"{alcohol.alcoholName} 은(는) 이미 선택된 재료입니다.");
                    }
                    else
                    {
                        _selectedAlcohols.Add(alcohol);
                        GUI.FocusControl(null);
                        Repaint();
                    }
                }

                currentColumn++;

                if (currentColumn >= columnCount && i < _alcoholListSO.alcoholList.Count - 1)
                {
                    currentColumn = 0;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void DrawSelectedAlcoholList()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("선택된 재료", EditorStyles.boldLabel);

            if (_selectedAlcohols.Count == 0)
            {
                EditorGUILayout.HelpBox("선택된 재료가 없습니다.", MessageType.None);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"총 {_selectedAlcohols.Count}개 선택됨");

            if (GUILayout.Button("전체 삭제", GUILayout.Width(90f)))
            {
                _selectedAlcohols.Clear();
                Repaint();
            }

            EditorGUILayout.EndHorizontal();

            _selectedScroll = EditorGUILayout.BeginScrollView(_selectedScroll, GUILayout.Height(190f));

            for (int i = 0; i < _selectedAlcohols.Count; i++)
            {
                BaseAlcoholDataSO alcohol = _selectedAlcohols[i];
                if (alcohol == null)
                    continue;

                EditorGUILayout.BeginHorizontal("box");

                Texture previewTexture = GetSpritePreviewTexture(alcohol.alcoholImage);
                GUILayout.Label(previewTexture, GUILayout.Width(28f), GUILayout.Height(28f));
                EditorGUILayout.LabelField($"{i + 1}. {alcohol.alcoholName}");

                if (GUILayout.Button("위", GUILayout.Width(35f)) && i > 0)
                {
                    (_selectedAlcohols[i - 1], _selectedAlcohols[i]) = (_selectedAlcohols[i], _selectedAlcohols[i - 1]);
                    Repaint();
                }

                if (GUILayout.Button("아래", GUILayout.Width(35f)) && i < _selectedAlcohols.Count - 1)
                {
                    (_selectedAlcohols[i + 1], _selectedAlcohols[i]) = (_selectedAlcohols[i], _selectedAlcohols[i + 1]);
                    Repaint();
                }

                if (GUILayout.Button("삭제", GUILayout.Width(45f)))
                {
                    _selectedAlcohols.RemoveAt(i);
                    GUI.FocusControl(null);
                    Repaint();
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawBottomButtons()
        {
            GUI.enabled = _alcoholListSO != null;

            EditorGUILayout.BeginHorizontal();

            if (_editingRecipe == null)
            {
                if (GUILayout.Button("레시피 생성", GUILayout.Height(34f)))
                    CreateRecipeSO();
            }
            else
            {
                if (GUILayout.Button("현재 레시피 수정 저장", GUILayout.Height(34f)))
                    UpdateRecipeSO();

                if (GUILayout.Button("수정 취소", GUILayout.Height(34f)))
                {
                    _editingRecipe = null;
                    ClearInput();
                    Repaint();
                }
            }

            if (GUILayout.Button("입력 초기화", GUILayout.Height(34f)))
            {
                ClearInput();
                Repaint();
            }

            EditorGUILayout.EndHorizontal();

            GUI.enabled = true;
        }

        private Texture GetSpritePreviewTexture(Sprite sprite)
        {
            if (sprite == null)
                return null;

            Texture previewTexture = AssetPreview.GetAssetPreview(sprite);

            if (previewTexture == null)
                previewTexture = AssetPreview.GetMiniThumbnail(sprite);

            if (previewTexture == null)
                previewTexture = sprite.texture;

            return previewTexture;
        }

        private void TryFindAlcoholListSOIfNeeded()
        {
            if (_alcoholListSO == null)
                TryFindAlcoholListSO();
        }

        private void TryFindRecipeDatabaseSOIfNeeded()
        {
            if (_recipeDatabaseSO == null)
                TryFindRecipeDatabaseSO();
        }

        private void TryFindAlcoholListSO()
        {
            string[] guids = AssetDatabase.FindAssets("t:AlcoholListSO");
            if (guids.Length == 0)
                return;

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            _alcoholListSO = AssetDatabase.LoadAssetAtPath<AlcoholListSO>(path);
        }

        private void TryFindRecipeDatabaseSO()
        {
            string[] guids = AssetDatabase.FindAssets("t:CocktailRecipeDatabaseSO");
            if (guids.Length == 0)
                return;

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            _recipeDatabaseSO = AssetDatabase.LoadAssetAtPath<CocktailRecipeDatabaseSO>(path);
        }

        private void LoadRecipe(CocktailRecipeSO recipe)
        {
            if (recipe == null)
                return;

            _cocktailName = recipe.cocktailName;
            _cocktailIcon = recipe.cocktailIcon;
            _selectedAlcohols.Clear();

            if (recipe.cocktailRecipeList != null)
                _selectedAlcohols.AddRange(recipe.cocktailRecipeList);
        }

        private void CreateRecipeSO()
        {
            if (_alcoholListSO == null)
            {
                EditorUtility.DisplayDialog("생성 실패", "AlcoholListSO를 연결해주세요.", "확인");
                return;
            }

            if (string.IsNullOrWhiteSpace(_cocktailName))
            {
                EditorUtility.DisplayDialog("생성 실패", "칵테일 이름을 입력해주세요.", "확인");
                return;
            }

            if (_selectedAlcohols.Count == 0)
            {
                EditorUtility.DisplayDialog("생성 실패", "재료를 하나 이상 선택해주세요.", "확인");
                return;
            }

            string assetPath = EditorUtility.SaveFilePanelInProject(
                "Create Cocktail Recipe",
                _cocktailName,
                "asset",
                "레시피 SO를 저장할 위치를 선택해주세요.",
                _saveFolderPath);

            if (string.IsNullOrEmpty(assetPath))
                return;

            CocktailRecipeSO recipe = CreateInstance<CocktailRecipeSO>();
            recipe.cocktailName = _cocktailName;
            recipe.cocktailIcon = _cocktailIcon;
            recipe.cocktailRecipeList = new List<BaseAlcoholDataSO>(_selectedAlcohols);

            AssetDatabase.CreateAsset(recipe, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (_recipeDatabaseSO != null)
                AddRecipeToDatabase(recipe);

            Selection.activeObject = recipe;
            EditorUtility.FocusProjectWindow();

            Debug.Log($"{_cocktailName} 레시피 SO 생성 완료");
        }

        private void UpdateRecipeSO()
        {
            if (_editingRecipe == null)
            {
                EditorUtility.DisplayDialog("수정 실패", "수정할 레시피가 없습니다.", "확인");
                return;
            }

            if (string.IsNullOrWhiteSpace(_cocktailName))
            {
                EditorUtility.DisplayDialog("수정 실패", "칵테일 이름을 입력해주세요.", "확인");
                return;
            }

            if (_selectedAlcohols.Count == 0)
            {
                EditorUtility.DisplayDialog("수정 실패", "재료를 하나 이상 선택해주세요.", "확인");
                return;
            }

            Undo.RecordObject(_editingRecipe, "Update Cocktail Recipe");

            _editingRecipe.cocktailName = _cocktailName;
            _editingRecipe.cocktailIcon = _cocktailIcon;
            _editingRecipe.cocktailRecipeList = new List<BaseAlcoholDataSO>(_selectedAlcohols);

            EditorUtility.SetDirty(_editingRecipe);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = _editingRecipe;
            Debug.Log($"{_editingRecipe.cocktailName} 레시피 수정 완료");
        }

        private void AddRecipeToDatabase(CocktailRecipeSO recipe)
        {
            if (_recipeDatabaseSO == null || recipe == null)
                return;

            if (_recipeDatabaseSO.recipes == null)
                _recipeDatabaseSO.recipes = new List<CocktailRecipeSO>();

            if (_recipeDatabaseSO.recipes.Contains(recipe))
                return;

            Undo.RecordObject(_recipeDatabaseSO, "Add Cocktail Recipe");
            _recipeDatabaseSO.recipes.Add(recipe);
            EditorUtility.SetDirty(_recipeDatabaseSO);
            AssetDatabase.SaveAssets();
        }

        private void ClearInput()
        {
            _cocktailName = string.Empty;
            _cocktailIcon = null;
            _selectedAlcohols.Clear();
        }
    }
}
