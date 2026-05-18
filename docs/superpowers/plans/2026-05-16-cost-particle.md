# Cost Particle System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 코인/경험치 등 int 기반 cost 이벤트 발생 시 아이콘 + 숫자 텍스트가 위로 떠오르며 페이드되는 파티클 효과 시스템을 구현한다.

**Architecture:** 기존 EventChannelSO + PoolManagerSo 패턴을 따른다. 파티클 전용 `CostParticleEvent`를 신설해 타입 안전한 채널 구독을 유지한다. `CostParticleManager`가 설정된 각 채널을 구독하고, 이벤트 수신 시 `CostParticleItem`을 풀에서 꺼내 LitMotion 애니메이션을 실행한다. `CostParticleBakerEditor`가 설정된 스프라이트를 하나의 `TMP_SpriteAsset`으로 자동 베이킹한다.

**Tech Stack:** Unity UGUI, TextMeshPro (TMP_SpriteAsset), LitMotion, Gamelib.ObjectPool.Runtime, Gamelib.EventSystem

---

## File Map

**신규 생성:**
- `Assets/Gamelib/EventSystem/ICostEvent.cs` — Amount + Position 인터페이스
- `Assets/Gamelib/EventSystem/CostParticleEvent.cs` — 파티클 전용 GameEvent (ICostEvent 구현)
- `Assets/00. Work/Goat/02. Scripts/Coin/Particle/CostTypeConfig.cs` — 타입별 설정 Serializable class
- `Assets/00. Work/Goat/02. Scripts/Coin/Particle/CostParticleConfigSO.cs` — 전체 설정 ScriptableObject
- `Assets/00. Work/Goat/02. Scripts/Coin/Particle/CostParticleItem.cs` — PoolableMono + LitMotion 애니메이션
- `Assets/00. Work/Goat/02. Scripts/Coin/Particle/CostParticleManager.cs` — MonoBehaviour, 구독 및 디스패치
- `Assets/00. Work/Goat/02. Scripts/Coin/Particle/Editor/CostParticleBakerEditor.cs` — Bake 버튼 (Editor only)

**수정:**
- `Assets/00. Work/Goat/02. Scripts/Events/CoinEvent.cs` — Position 필드, ICostEvent 구현
- `Assets/00. Work/Goat/02. Scripts/Events/ExpEvent.cs` — Position 필드, ICostEvent 구현
- `Assets/00. Work/Goat/02. Scripts/Coin/CoinManager.cs` — coinParticleChannelSO 추가, TryUseCoin/AddCoin 갱신

---

## Task 1: ICostEvent + CostParticleEvent

**Files:**
- Create: `Assets/Gamelib/EventSystem/ICostEvent.cs`
- Create: `Assets/Gamelib/EventSystem/CostParticleEvent.cs`

- [ ] **Step 1: ICostEvent 생성**

`Assets/Gamelib/EventSystem/ICostEvent.cs` 생성:

```csharp
using UnityEngine;

namespace Gamelib.EventSystem
{
    public interface ICostEvent
    {
        int Amount { get; }
        Vector3 Position { get; }
    }
}
```

- [ ] **Step 2: CostParticleEvent 생성**

`Assets/Gamelib/EventSystem/CostParticleEvent.cs` 생성:

```csharp
using UnityEngine;

namespace Gamelib.EventSystem
{
    public class CostParticleEvent : GameEvent, ICostEvent
    {
        public int amount;
        public Vector3 position;

        public int Amount => amount;
        public Vector3 Position => position;

        public CostParticleEvent Init(int amount, Vector3 position)
        {
            this.amount = amount;
            this.position = position;
            return this;
        }
    }
}
```

- [ ] **Step 3: 컴파일 확인**

Unity Editor에서 Console에 컴파일 에러가 없는지 확인.

- [ ] **Step 4: 커밋**

```bash
git add "Assets/Gamelib/EventSystem/ICostEvent.cs" "Assets/Gamelib/EventSystem/CostParticleEvent.cs"
git add "Assets/Gamelib/EventSystem/ICostEvent.cs.meta" "Assets/Gamelib/EventSystem/CostParticleEvent.cs.meta"
git commit -m "feat: add ICostEvent interface and CostParticleEvent"
```

---

## Task 2: CoinEvent + ExpEvent 수정

**Files:**
- Modify: `Assets/00. Work/Goat/02. Scripts/Events/CoinEvent.cs`
- Modify: `Assets/00. Work/Goat/02. Scripts/Events/ExpEvent.cs`

- [ ] **Step 1: CoinEvent에 Position 추가 및 ICostEvent 구현**

`Assets/00. Work/Goat/02. Scripts/Events/CoinEvent.cs` 전체 교체:

```csharp
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Events
{
    public class CoinEvent : GameEvent, ICostEvent
    {
        public int amount;
        public Vector3 position;

        public int Amount => amount;
        public Vector3 Position => position;

        public CoinEvent Init(int amount, Vector3 position = default)
        {
            this.amount = amount;
            this.position = position;
            return this;
        }
    }
}
```

- [ ] **Step 2: ExpEvent에 Position 추가 및 ICostEvent 구현**

`Assets/00. Work/Goat/02. Scripts/Events/ExpEvent.cs` 전체 교체:

```csharp
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Events
{
    public class ExpEvent : GameEvent, ICostEvent
    {
        public int amount;
        public Vector3 position;

        public int Amount => amount;
        public Vector3 Position => position;

        public ExpEvent Init(int amount, Vector3 position = default)
        {
            this.amount = amount;
            this.position = position;
            return this;
        }
    }
}
```

- [ ] **Step 3: 기존 Init() 호출부 확인**

`position = default`이므로 기존 `Init(amount)` 호출부는 컴파일 에러 없이 유지됨. Unity Console에서 에러 없음을 확인.

- [ ] **Step 4: 커밋**

```bash
git add "Assets/00. Work/Goat/02. Scripts/Events/CoinEvent.cs"
git add "Assets/00. Work/Goat/02. Scripts/Events/ExpEvent.cs"
git commit -m "feat: add ICostEvent and Position to CoinEvent, ExpEvent"
```

---

## Task 3: CostTypeConfig + CostParticleConfigSO

**Files:**
- Create: `Assets/00. Work/Goat/02. Scripts/Coin/Particle/CostTypeConfig.cs`
- Create: `Assets/00. Work/Goat/02. Scripts/Coin/Particle/CostParticleConfigSO.cs`

- [ ] **Step 1: Particle 폴더 생성 확인**

Unity Editor에서 `Assets/00. Work/Goat/02. Scripts/Coin/` 아래 `Particle` 폴더가 없으면 생성.

- [ ] **Step 2: CostTypeConfig 생성**

`Assets/00. Work/Goat/02. Scripts/Coin/Particle/CostTypeConfig.cs` 생성:

```csharp
using System;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Coin.Particle
{
    [Serializable]
    public class CostTypeConfig
    {
        public string typeName;
        public EventChannelSO eventChannel;
        public Sprite icon;
        public int spriteIndex;
        public Color gainColor = Color.green;
        public Color spendColor = Color.red;
    }
}
```

- [ ] **Step 3: CostParticleConfigSO 생성**

`Assets/00. Work/Goat/02. Scripts/Coin/Particle/CostParticleConfigSO.cs` 생성:

```csharp
using System.Collections.Generic;
using Gamelib.ObjectPool.Runtime;
using TMPro;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Coin.Particle
{
    [CreateAssetMenu(fileName = "CostParticleConfig", menuName = "Goat/CostParticleConfig", order = 0)]
    public class CostParticleConfigSO : ScriptableObject
    {
        public List<CostTypeConfig> costTypes = new();
        public TMP_SpriteAsset spriteAsset;
        public PoolManagerSo poolManager;
        public PoolItemSo particlePoolItem;
        public int maxAtlasSize = 1024;
    }
}
```

- [ ] **Step 4: 컴파일 확인**

Unity Console에서 에러 없음 확인.

- [ ] **Step 5: 커밋**

```bash
git add "Assets/00. Work/Goat/02. Scripts/Coin/Particle/CostTypeConfig.cs"
git add "Assets/00. Work/Goat/02. Scripts/Coin/Particle/CostParticleConfigSO.cs"
git add "Assets/00. Work/Goat/02. Scripts/Coin/Particle/CostTypeConfig.cs.meta"
git add "Assets/00. Work/Goat/02. Scripts/Coin/Particle/CostParticleConfigSO.cs.meta"
git commit -m "feat: add CostTypeConfig and CostParticleConfigSO"
```

---

## Task 4: CostParticleItem

**Files:**
- Create: `Assets/00. Work/Goat/02. Scripts/Coin/Particle/CostParticleItem.cs`

- [ ] **Step 1: CostParticleItem 생성**

`Assets/00. Work/Goat/02. Scripts/Coin/Particle/CostParticleItem.cs` 생성:

```csharp
using System;
using Gamelib.ObjectPool.Runtime;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Coin.Particle
{
    public class CostParticleItem : PoolableMono
    {
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private CanvasGroup _canvasGroup;

        private MotionHandle _moveHandle;
        private MotionHandle _alphaHandle;
        private Action _onComplete;

        private const float MoveDuration = 0.8f;
        private const float MoveDistance = 80f;
        private const float FadeDelay = 0.4f;
        private const float FadeDuration = 0.4f;

        public void Play(int amount, int spriteIndex, Color gainColor, Color spendColor,
            Vector2 anchoredPos, Action onComplete)
        {
            _onComplete = onComplete;

            RectTransform rect = transform as RectTransform;
            rect.anchoredPosition = anchoredPos;

            _text.color = amount >= 0 ? gainColor : spendColor;
            string sign = amount >= 0 ? "+" : "";
            _text.text = $"<sprite={spriteIndex}>{sign}{amount}";
            _canvasGroup.alpha = 1f;

            if (_moveHandle.IsActive()) _moveHandle.Cancel();
            if (_alphaHandle.IsActive()) _alphaHandle.Cancel();

            _moveHandle = LMotion.Create(anchoredPos, anchoredPos + Vector2.up * MoveDistance, MoveDuration)
                .WithEase(Ease.OutCubic)
                .BindToAnchoredPosition(rect);

            _alphaHandle = LMotion.Create(1f, 0f, FadeDuration)
                .WithDelay(FadeDelay)
                .WithOnComplete(() => _onComplete?.Invoke())
                .Bind(a => _canvasGroup.alpha = a);
        }

        public override void ResetItem()
        {
            if (_moveHandle.IsActive()) _moveHandle.Cancel();
            if (_alphaHandle.IsActive()) _alphaHandle.Cancel();
            _canvasGroup.alpha = 1f;
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인**

Unity Console에서 에러 없음 확인.

- [ ] **Step 3: 커밋**

```bash
git add "Assets/00. Work/Goat/02. Scripts/Coin/Particle/CostParticleItem.cs"
git add "Assets/00. Work/Goat/02. Scripts/Coin/Particle/CostParticleItem.cs.meta"
git commit -m "feat: add CostParticleItem with LitMotion animation"
```

---

## Task 5: CostParticleManager

**Files:**
- Create: `Assets/00. Work/Goat/02. Scripts/Coin/Particle/CostParticleManager.cs`

- [ ] **Step 1: CostParticleManager 생성**

`Assets/00. Work/Goat/02. Scripts/Coin/Particle/CostParticleManager.cs` 생성:

```csharp
using System;
using System.Collections.Generic;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Coin.Particle
{
    public class CostParticleManager : MonoBehaviour
    {
        [SerializeField] private CostParticleConfigSO _config;
        [SerializeField] private Canvas _canvas;

        private readonly List<(EventChannelSO channel, Action<CostParticleEvent> handler)> _subscriptions = new();

        private void Start()
        {
            _config.poolManager.InitializePool(transform);

            foreach (CostTypeConfig costType in _config.costTypes)
            {
                CostTypeConfig captured = costType;
                Action<CostParticleEvent> handler = evt => SpawnParticle(evt, captured);

                costType.eventChannel.AddListener(handler);
                _subscriptions.Add((costType.eventChannel, handler));
            }
        }

        private void OnDisable()
        {
            foreach ((EventChannelSO channel, Action<CostParticleEvent> handler) in _subscriptions)
                channel.RemoveListener(handler);
            _subscriptions.Clear();
        }

        private void SpawnParticle(CostParticleEvent evt, CostTypeConfig config)
        {
            CostParticleItem item = _config.poolManager.Pop<CostParticleItem>(_config.particlePoolItem);
            if (item == null) return;

            Vector2 anchoredPos = WorldToCanvasPos(evt.Position);
            item.Play(evt.Amount, config.spriteIndex, config.gainColor, config.spendColor,
                anchoredPos, () => _config.poolManager.Push(item));
        }

        private Vector2 WorldToCanvasPos(Vector3 worldPos)
        {
            Camera cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform, screenPos, cam, out Vector2 localPos);
            return localPos;
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인**

Unity Console에서 에러 없음 확인.

- [ ] **Step 3: 커밋**

```bash
git add "Assets/00. Work/Goat/02. Scripts/Coin/Particle/CostParticleManager.cs"
git add "Assets/00. Work/Goat/02. Scripts/Coin/Particle/CostParticleManager.cs.meta"
git commit -m "feat: add CostParticleManager"
```

---

## Task 6: CoinManager 수정

**Files:**
- Modify: `Assets/00. Work/Goat/02. Scripts/Coin/CoinManager.cs`

`CoinManager`에 파티클 전용 채널(`coinParticleChannelSO`)을 추가하고, 코인 변경 시 `CostParticleEvent`를 발행한다.

- [ ] **Step 1: CoinManager 수정**

`Assets/00. Work/Goat/02. Scripts/Coin/CoinManager.cs` 전체 교체:

```csharp
using System;
using _00._Work.Goat._02._Scripts.Coin.CoinDatas;
using _00._Work.Goat._02._Scripts.Events;
using _00._Work.Goat._02._Scripts.SaveCode;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Coin
{
    public class CoinManager : MonoBehaviour
    {
        [SerializeField] private EventChannelSO coinChannelSO;
        [SerializeField] private EventChannelSO coinParticleChannelSO;
        [SerializeField] private SaveFileNameSO saveFileNameSO;

        private CoinData _coinData;
        public int CurrentCoin => _coinData.coin;

        public event Action<int> OnChangeCoin;

        private JsonSaveService _jsonSaveService;

        private void Awake()
        {
            _jsonSaveService = new JsonSaveService(saveFileNameSO);
            LoadCoin();
            coinChannelSO.AddListener<CoinEvent>(HandleCoinEvent);
        }

        private void OnDestroy()
        {
            coinChannelSO.RemoveListener<CoinEvent>(HandleCoinEvent);
        }

        private void LoadCoin()
        {
            _coinData = _jsonSaveService.Load<CoinData>();

            if (_coinData == null)
            {
                _coinData = new CoinData();
                _coinData.coin = 0;
            }

            OnChangeCoin?.Invoke(_coinData.coin);
        }

        private void HandleCoinEvent(CoinEvent coin)
        {
            AddCoin(coin.amount, coin.position);
        }

        public void AddCoin(int amount, Vector3 worldPos = default)
        {
            _coinData.coin += amount;

            if (_coinData.coin < 0)
                _coinData.coin = 0;

            SaveAndNotify();
            coinParticleChannelSO?.RaiseEvent(new CostParticleEvent().Init(amount, worldPos));
        }

        public bool TryUseCoin(int amount, Vector3 worldPos = default)
        {
            if (_coinData.coin < amount)
            {
                Debug.Log("돈이 부족합니다");
                return false;
            }

            _coinData.coin -= amount;
            SaveAndNotify();
            coinParticleChannelSO?.RaiseEvent(new CostParticleEvent().Init(-amount, worldPos));

            return true;
        }

        private void SaveAndNotify()
        {
            OnChangeCoin?.Invoke(_coinData.coin);
            _jsonSaveService.Save(_coinData);
        }
    }
}
```

> **주의:** `CostParticleEvent`를 using하려면 `using Gamelib.EventSystem;`이 이미 포함되어 있으므로 별도 using 불필요.

- [ ] **Step 2: 컴파일 확인**

Unity Console에서 에러 없음 확인. 기존 `TryUseCoin(amount)` 호출부는 `worldPos = default`로 하위 호환됨.

- [ ] **Step 3: Inspector 확인**

씬에서 CoinManager 컴포넌트에 `Coin Particle Channel SO` 필드가 노출되는지 확인. (아직 SO를 연결하지 않아도 됨)

- [ ] **Step 4: 커밋**

```bash
git add "Assets/00. Work/Goat/02. Scripts/Coin/CoinManager.cs"
git commit -m "feat: CoinManager raises CostParticleEvent on coin change"
```

---

## Task 7: CostParticleBakerEditor

**Files:**
- Create: `Assets/00. Work/Goat/02. Scripts/Coin/Particle/Editor/CostParticleBakerEditor.cs`

- [ ] **Step 1: Editor 폴더 생성**

Unity Editor에서 `Assets/00. Work/Goat/02. Scripts/Coin/Particle/` 아래 `Editor` 폴더 생성.

- [ ] **Step 2: CostParticleBakerEditor 생성**

`Assets/00. Work/Goat/02. Scripts/Coin/Particle/Editor/CostParticleBakerEditor.cs` 생성:

```csharp
using System.Collections.Generic;
using System.IO;
using _00._Work.Goat._02._Scripts.Coin.Particle;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;

namespace _00._Work.Goat._02._Scripts.Coin.Particle.Editor
{
    [CustomEditor(typeof(CostParticleConfigSO))]
    public class CostParticleBakerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Bake SpriteAsset", GUILayout.Height(30)))
                BakeSpriteAsset((CostParticleConfigSO)target);
        }

        private void BakeSpriteAsset(CostParticleConfigSO config)
        {
            if (config.costTypes == null || config.costTypes.Count == 0)
            {
                Debug.LogWarning("[CostParticleBaker] costTypes가 비어 있습니다.");
                return;
            }

            // 1. 스프라이트 수집 및 텍스처 추출
            // configIndices: 각 sprite가 어느 costType[j]에서 왔는지 추적 (null 건너뜀 대응)
            List<Sprite> sprites = new List<Sprite>();
            List<Texture2D> spriteTex = new List<Texture2D>();
            List<int> configIndices = new List<int>();

            for (int j = 0; j < config.costTypes.Count; j++)
            {
                CostTypeConfig costType = config.costTypes[j];
                if (costType.icon == null)
                {
                    Debug.LogWarning($"[CostParticleBaker] '{costType.typeName}' icon이 없습니다. 건너뜁니다.");
                    continue;
                }

                Sprite s = costType.icon;
                string texPath = AssetDatabase.GetAssetPath(s.texture);
                TextureImporter importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
                if (importer != null && !importer.isReadable)
                {
                    Debug.LogWarning($"[CostParticleBaker] {s.texture.name} Read/Write 활성화 중...");
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                }

                int x = (int)s.rect.x;
                int y = (int)s.rect.y;
                int w = (int)s.rect.width;
                int h = (int)s.rect.height;

                Texture2D extracted = new Texture2D(w, h, TextureFormat.RGBA32, false);
                extracted.SetPixels(s.texture.GetPixels(x, y, w, h));
                extracted.Apply();

                sprites.Add(s);
                spriteTex.Add(extracted);
                configIndices.Add(j);
            }

            if (sprites.Count == 0)
            {
                Debug.LogWarning("[CostParticleBaker] 유효한 스프라이트가 없습니다.");
                return;
            }

            // 2. 아틀라스 팩킹
            Texture2D atlas = new Texture2D(config.maxAtlasSize, config.maxAtlasSize, TextureFormat.RGBA32, false);
            Rect[] uvRects = atlas.PackTextures(spriteTex.ToArray(), 2, config.maxAtlasSize);

            // 3. 아틀라스 저장 (Application.dataPath = "[Project]/Assets")
            string saveDir = "Assets/00. Work/Goat/05. SO/CostParticle";
            if (!AssetDatabase.IsValidFolder(saveDir))
                AssetDatabase.CreateFolder("Assets/00. Work/Goat/05. SO", "CostParticle");

            string atlasPath = saveDir + "/CostParticleAtlas.png";
            string fullPath = Application.dataPath + atlasPath.Substring("Assets".Length);
            File.WriteAllBytes(fullPath, atlas.EncodeToPNG());
            AssetDatabase.ImportAsset(atlasPath);

            TextureImporter atlasImporter = AssetImporter.GetAtPath(atlasPath) as TextureImporter;
            if (atlasImporter != null)
            {
                atlasImporter.textureType = TextureImporterType.Default;
                atlasImporter.isReadable = false;
                atlasImporter.SaveAndReimport();
            }

            Texture2D atlasTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);

            // 4. TMP_SpriteAsset 생성 또는 갱신
            string spriteAssetPath = saveDir + "/CostParticleSpriteAsset.asset";
            TMP_SpriteAsset spriteAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(spriteAssetPath);
            if (spriteAsset == null)
            {
                spriteAsset = CreateInstance<TMP_SpriteAsset>();
                AssetDatabase.CreateAsset(spriteAsset, spriteAssetPath);
            }

            spriteAsset.spriteSheet = atlasTexture;
            spriteAsset.spriteGlyphTable.Clear();
            spriteAsset.spriteCharacterTable.Clear();

            for (int i = 0; i < sprites.Count; i++)
            {
                Rect uv = uvRects[i];
                int pw = spriteTex[i].width;
                int ph = spriteTex[i].height;

                TMP_SpriteGlyph glyph = new TMP_SpriteGlyph
                {
                    index = (uint)i,
                    metrics = new GlyphMetrics(pw, ph, 0, ph, pw),
                    glyphRect = new GlyphRect(
                        Mathf.RoundToInt(uv.x * atlas.width),
                        Mathf.RoundToInt(uv.y * atlas.height),
                        pw, ph),
                    scale = 1f,
                    atlasIndex = 0
                };
                spriteAsset.spriteGlyphTable.Add(glyph);

                TMP_SpriteCharacter character = new TMP_SpriteCharacter((uint)(0xE000 + i), glyph)
                {
                    name = sprites[i].name,
                    scale = 1f
                };
                spriteAsset.spriteCharacterTable.Add(character);

                // 5. spriteIndex 자동 기입 (configIndices로 null 건너뜀 대응)
                config.costTypes[configIndices[i]].spriteIndex = i;
            }

            spriteAsset.UpdateLookupTables();

            // 6. config에 자동 연결
            config.spriteAsset = spriteAsset;

            EditorUtility.SetDirty(spriteAsset);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CostParticleBaker] Bake 완료: {sprites.Count}개 스프라이트 → {spriteAssetPath}");
        }
    }
}
```

- [ ] **Step 3: 컴파일 확인**

Unity Console에서 에러 없음 확인.

- [ ] **Step 4: 커밋**

```bash
git add "Assets/00. Work/Goat/02. Scripts/Coin/Particle/Editor/CostParticleBakerEditor.cs"
git add "Assets/00. Work/Goat/02. Scripts/Coin/Particle/Editor/CostParticleBakerEditor.cs.meta"
git commit -m "feat: add CostParticleBakerEditor with Bake SpriteAsset button"
```

---

## Task 8: Unity 에셋 세팅 및 검증

코드 작성 완료 후 Unity Editor에서 에셋과 씬을 구성한다.

- [ ] **Step 1: CostParticleItem Prefab 생성**

1. Unity Hierarchy에서 Canvas 아래 빈 GameObject 생성 → 이름 `CostParticleItem`
2. `RectTransform` 확인 (Canvas 자식이면 자동)
3. `CanvasGroup` 컴포넌트 추가
4. TextMeshPro - Text (UI) 컴포넌트 추가 → 이름 `Text`
   - Font Size: 36
   - Alignment: Center Middle
   - Raycast Target: 해제
5. `CostParticleItem` 스크립트 추가 → `_text`, `_canvasGroup` 필드 연결
6. Hierarchy에서 Project로 드래그해 Prefab 저장 (`Assets/00. Work/Goat/04. Prefabs/CostParticleItem.prefab` 권장)
7. Hierarchy에서 임시 오브젝트 삭제

- [ ] **Step 2: PoolItemSo 에셋 생성**

1. Project에서 우클릭 → `Object Pool/Pool Item` → 이름 `CostParticlePoolItem`
2. `Prefab` 필드에 Step 1에서 만든 `CostParticleItem` Prefab 연결
3. `Init Count`: 10
4. `Pooling Name`: "CostParticleItem"

- [ ] **Step 3: CostParticleConfigSO 에셋 생성**

1. Project에서 우클릭 → `Goat/CostParticleConfig` → 이름 `CostParticleConfig`
2. 필드 연결:
   - `Pool Manager`: 기존 PoolManagerSo 에셋 연결
   - `Particle Pool Item`: Step 2에서 만든 `CostParticlePoolItem` 연결
   - `Max Atlas Size`: 1024
3. `Cost Types` 리스트에 항목 추가:
   - `Type Name`: "Coin"
   - `Event Channel`: 파티클 전용 새 EventChannelSO 생성 (`CoinParticleChannel.asset`)
   - `Icon`: 코인 아이콘 Sprite 연결
   - `Gain Color`: 초록색
   - `Spend Color`: 빨간색
4. Inspector 하단 **"Bake SpriteAsset"** 버튼 클릭
5. `Assets/00. Work/Goat/05. SO/CostParticle/` 아래 `CostParticleAtlas.png`와 `CostParticleSpriteAsset.asset`이 생성되는지 확인
6. `Sprite Asset` 필드에 자동 연결되었는지 확인

- [ ] **Step 4: TMP_SpriteAsset을 TextMeshPro Default Settings에 등록**

1. `Edit → Project Settings → TextMesh Pro → Settings`
2. `Sprite Assets` 리스트에 `CostParticleSpriteAsset` 추가
   (또는 CostParticleItem Prefab의 TextMeshProUGUI에 직접 Sprite Asset 연결)

- [ ] **Step 5: CostParticleManager 씬 배치**

1. 씬의 Canvas 아래 빈 GameObject 추가 → 이름 `CostParticleManager`
2. `CostParticleManager` 스크립트 추가
3. 필드 연결:
   - `Config`: `CostParticleConfig` SO 연결
   - `Canvas`: 상위 Canvas 연결
4. CoinManager GameObject Inspector에서 `Coin Particle Channel SO` 필드에 `CoinParticleChannel.asset` 연결

- [ ] **Step 6: PoolManagerSo에 CostParticlePoolItem 등록**

1. PoolManagerSo 에셋의 `Item List`에 `CostParticlePoolItem` 추가

- [ ] **Step 7: 런타임 검증**

1. Play Mode 진입
2. 코인 소비 트리거 (업그레이드 버튼 클릭 등)
3. 확인 사항:
   - 파티클 오브젝트가 소비 위치 근처에 스폰되는지
   - 아이콘 + 숫자(`<sprite=0>-50`)가 올바르게 표시되는지
   - 위로 이동하며 페이드 아웃되는지
   - 애니메이션 완료 후 풀로 반환되는지 (Hierarchy에서 `CostParticleManager` 자식 오브젝트 확인)
4. 여러 번 연속 소비 시 풀에서 정상적으로 Pop/Push 반복되는지 확인

- [ ] **Step 8: 최종 커밋**

```bash
git add -A
git commit -m "feat: complete cost particle system asset setup"
```
