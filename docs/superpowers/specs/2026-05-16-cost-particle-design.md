# Cost Particle System Design

**Date:** 2026-05-16  
**Branch:** BBJ  
**Status:** Approved

## Summary

코인, 경험치 등 int 기반 cost 이벤트 발생 시 아이콘 + 숫자 텍스트가 위로 떠오르며 페이드되는 파티클 효과 시스템. 기존 EventChannelSO + PoolManagerSO 패턴을 따르며, TMP_SpriteAsset 자동 베이킹 에디터 툴을 포함한다.

---

## Architecture

### ICostEvent (interface)

```csharp
public interface ICostEvent
{
    int Amount { get; }
    Vector3 Position { get; }
}
```

모든 cost 이벤트가 구현하는 마커 인터페이스. CostParticleManager가 제네릭 핸들러에서 이 인터페이스로 데이터를 읽는다.

### 기존 이벤트 수정

`CoinEvent`, `ExpEvent` 등 기존 `GameEvent` 서브클래스에 `Vector3 Position` 필드 추가 및 `ICostEvent` 구현. 기존 `Init()` 메서드에 `Vector3 position` 파라미터 추가.

### CostTypeConfig (Serializable class)

```csharp
[Serializable]
public class CostTypeConfig
{
    public string typeName;
    public EventChannelSO eventChannel;
    public Sprite icon;
    public int spriteIndex;   // Bake 시 자동 기입
    public Color gainColor;
    public Color spendColor;
}
```

타입 하나당 하나의 config 항목. 직렬화되어 SO Inspector에서 리스트로 관리.

### CostParticleConfigSO (ScriptableObject)

```csharp
public class CostParticleConfigSO : ScriptableObject
{
    public List<CostTypeConfig> costTypes;
    public TMP_SpriteAsset spriteAsset;   // Bake로 자동 생성/갱신
    public PoolManagerSO poolManager;
    public PoolItemSO particlePoolItem;
    public int maxAtlasSize = 1024;
}
```

전체 설정의 단일 진입점. Custom Inspector에 "Bake SpriteAsset" 버튼 표시.

### CostParticleManager (MonoBehaviour)

- `CostParticleConfigSO` 주입
- `Start()`: `costTypes` 순회하며 각 `EventChannelSO`에 제네릭 핸들러 구독
- 이벤트 수신 시: `pool.Pop()` → `CostParticleItem.Play()` 호출
- `OnDisable()`: 모든 구독 해제

### CostParticleItem (PoolableMono)

```
CostParticleItem (RectTransform)
  └─ CanvasGroup          ← alpha 제어 (TMP SubMesh 포함)
  └─ TextMeshProUGUI      ← "<sprite=N>+100" 형태 출력
```

```csharp
public void Play(int amount, int spriteIndex, Color gainColor, Color spendColor, Vector3 worldPos)
```

**Position 좌표 변환**: `worldPos`는 월드 좌표. Canvas가 Screen Space - Overlay일 경우 `Camera.main.WorldToScreenPoint(worldPos)`로 스크린 좌표로 변환 후 `RectTransformUtility.ScreenPointToLocalPointInRectangle()`로 Canvas 로컬 좌표로 변환. `CostParticleManager`가 `Canvas` 레퍼런스를 주입받아 처리한다.

- `amount > 0`: gainColor, `+{amount}` 표시
- `amount < 0`: spendColor, `{amount}` 표시 (이미 음수)
- LitMotion `moveHandle`: Y축 위 이동, `Ease.OutCubic`
- LitMotion `alphaHandle`: 딜레이 후 1→0 페이드, `WithOnComplete` → `pool.Push(this)`
- `ResetItem()`: 두 핸들 `IsActive()` 확인 후 `Cancel()`, alpha 1 리셋

---

## 데이터 흐름

```
호출부 (UpgradeService 등)
  └─ CoinManager.TryUseCoin(amount, worldPos)
       └─ coinChannelSO.RaiseEvent(new CoinEvent().Init(-amount, worldPos))

CostParticleManager.HandleCostEvent<CoinEvent>(evt)
  └─ item = pool.Pop(particlePoolItem)
  └─ item.Play(evt.Amount, config.spriteIndex,
               config.gainColor, config.spendColor, evt.Position)

CostParticleItem 애니메이션 완료
  └─ pool.Push(this)
```

---

## Bake 에디터 동작

트리거: `CostParticleConfigSO` Inspector "Bake SpriteAsset" 버튼 클릭

1. `costTypes`에서 `Sprite[]` 수집
2. `Texture2D.PackTextures(sprites, padding: 2, maxAtlasSize)` → 아틀라스 생성
3. 아틀라스를 `Assets/.../CostParticleAtlas.png`로 저장
4. `TMP_SpriteAsset` 생성 또는 기존 asset 갱신 (glyph/character table 재구성, UV는 PackTextures 반환 `Rect[]` 기반)
5. 각 `CostTypeConfig.spriteIndex`를 리스트 순서대로 자동 기입
6. `CostParticleConfigSO.spriteAsset` 필드에 자동 연결
7. `AssetDatabase.SaveAssets()` + `ImportAsset()`

**조건**: 스프라이트 원본 텍스처 `Read/Write Enabled` 필요. 미설정 시 Bake 과정에서 에디터 경고 표시.

---

## 파일 목록

| 파일 | 위치 | 역할 |
|------|------|------|
| `ICostEvent.cs` | `Gamelib/EventSystem/` | interface |
| `CoinEvent.cs` | `Goat/02. Scripts/Events/` | Position 추가 |
| `ExpEvent.cs` | `Goat/02. Scripts/Events/` | Position 추가 |
| `CostTypeConfig.cs` | `Goat/02. Scripts/Coin/Particle/` | Serializable config |
| `CostParticleConfigSO.cs` | `Goat/02. Scripts/Coin/Particle/` | ScriptableObject |
| `CostParticleManager.cs` | `Goat/02. Scripts/Coin/Particle/` | MonoBehaviour |
| `CostParticleItem.cs` | `Goat/02. Scripts/Coin/Particle/` | PoolableMono |
| `CostParticleBakerEditor.cs` | `Editor/` | Bake 버튼 (Editor only) |

---

## 검증 방법 (Unity에서)

1. `CostParticleConfigSO` asset 생성 후 costTypes에 Coin config 추가
2. Sprite 할당 → "Bake SpriteAsset" 클릭 → SpriteAsset asset 자동 생성 확인
3. `CostParticleManager`를 씬에 배치, config SO 연결
4. 런타임에서 코인 소비/획득 이벤트 발생 → 파티클이 해당 위치에서 위로 떠오르며 페이드 확인
5. 여러 파티클 동시 발생 시 풀에서 정상적으로 Pop/Push 되는지 확인
