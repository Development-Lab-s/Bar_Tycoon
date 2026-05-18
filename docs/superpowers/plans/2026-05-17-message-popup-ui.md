# MessagePopupUI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Show a centered auto-dismissing popup message when the player clicks a customer whose food hasn't been crafted yet, using an EventChannelSO-based UIChannel.

**Architecture:** `MessageEvent` is raised by `PlayerOrderHandlerSO` onto a `UIChannel` (EventChannelSO asset). `MessagePopupUI` subscribes to that channel in OnEnable/OnDisable and shows/hides a panel with TMP text, auto-hiding after 2 seconds via coroutine.

**Tech Stack:** Unity C#, TextMeshPro, EventChannelSO (Gamelib.EventSystem), ScriptableObject asset wiring.

---

## File Map

| Action | Path | Responsibility |
|---|---|---|
| CREATE | `Assets/00. Work/BBJ/02. Scripts/Event/UIEvents.cs` | `MessageEvent` definition |
| CREATE | `Assets/00. Work/BBJ/02. Scripts/UI/MessagePopupUI.cs` | Subscribe, show, auto-hide |
| MODIFY | `Assets/00. Work/BBJ/02. Scripts/Player/PlayerOrderHandlerSO.cs` | Raise MessageEvent when food not ready |
| ASSET | `Assets/00. Work/BBJ/05. SO/EventChneel/UIChannel.asset` | EventChannelSO instance for UI events |

> Tasks 1, 2, 3 are independent — run in parallel. Task 4 runs after all three compile cleanly.

---

### Task 1: UIEvents.cs — MessageEvent

**Files:**
- Create: `Assets/00. Work/BBJ/02. Scripts/Event/UIEvents.cs`

- [ ] **Step 1: Create UIEvents.cs**

```csharp
using Gamelib.EventSystem;

namespace BBJ.EventSystem
{
    public class MessageEvent : GameEvent
    {
        public string Message { get; }
        public MessageEvent(string message) => Message = message;
    }
}
```

- [ ] **Step 2: Check compilation via Unity console**

Unity MCP: call `read_console` and confirm no errors mentioning `UIEvents` or `MessageEvent`.
Expected: no error lines. If errors appear, fix before proceeding.

- [ ] **Step 3: Commit**

```
git add "Assets/00. Work/BBJ/02. Scripts/Event/UIEvents.cs" "Assets/00. Work/BBJ/02. Scripts/Event/UIEvents.cs.meta"
git commit -m "feat: add MessageEvent to BBJ UIEvents"
```

---

### Task 2: MessagePopupUI.cs

**Files:**
- Create: `Assets/00. Work/BBJ/02. Scripts/UI/MessagePopupUI.cs`

- [ ] **Step 1: Create MessagePopupUI.cs**

```csharp
using System.Collections;
using BBJ.EventSystem;
using Gamelib.EventSystem;
using TMPro;
using UnityEngine;

namespace BBJ.UI
{
    public class MessagePopupUI : MonoBehaviour
    {
        [SerializeField] private EventChannelSO _uiChannel;
        [SerializeField] private GameObject     _panel;
        [SerializeField] private TMP_Text       _label;
        [SerializeField] private float          _displayDuration = 2f;

        private Coroutine _hideRoutine;

        private void OnEnable()  => _uiChannel?.AddListener<MessageEvent>(OnMessage);
        private void OnDisable() => _uiChannel?.RemoveListener<MessageEvent>(OnMessage);

        private void OnMessage(MessageEvent e)
        {
            _label.text = e.Message;
            _panel.SetActive(true);

            if (_hideRoutine != null) StopCoroutine(_hideRoutine);
            _hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(_displayDuration);
            _panel.SetActive(false);
            _hideRoutine = null;
        }
    }
}
```

- [ ] **Step 2: Check compilation via Unity console**

Unity MCP: call `read_console`. Confirm no errors for `MessagePopupUI`.
If `TMPro` is missing, verify TextMeshPro is installed in Package Manager.

- [ ] **Step 3: Commit**

```
git add "Assets/00. Work/BBJ/02. Scripts/UI/MessagePopupUI.cs" "Assets/00. Work/BBJ/02. Scripts/UI/MessagePopupUI.cs.meta"
git commit -m "feat: add MessagePopupUI with auto-hide coroutine"
```

---

### Task 3: PlayerOrderHandlerSO — raise MessageEvent

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Player/PlayerOrderHandlerSO.cs`

Current file for reference:
```csharp
// OnCustomerClicked — current guard that silently returns:
if (ticket.WorkPhase != OrderWorkPhase.ReadyForServer) return;
```

- [ ] **Step 1: Add _uiChannel field and raise event**

Replace the full file content with:

```csharp
using BBJ.Customer;
using BBJ.EventSystem;
using BBJ.Order;
using BBJ.Schedule;
using Gamelib.EventSystem;
using UnityEngine;

namespace BBJ.Player
{
    [CreateAssetMenu(fileName = "PlayerOrderHandler", menuName = "Tycoon/Player/OrderHandler")]
    public class PlayerOrderHandlerSO : ScriptableObject
    {
        [SerializeField] private EventChannelSO _orderChannel;
        [SerializeField] private EventChannelSO _uiChannel;

        public void OnCustomerClicked(CustomerAgent customer)
        {
            if (customer == null) return;
            if (!customer.IsReadyForOrder) return;

            var ticket = customer.ActiveTicket;
            if (ticket == null || ticket.IsTerminal) return;

            if (ticket.WorkPhase != OrderWorkPhase.ReadyForServer)
            {
                _uiChannel?.RaiseEvent(new MessageEvent("제작하지 않았습니다."));
                return;
            }

            customer.AssignedServer?.GetModule<ISchedulable>()?.CancelWork();
            _orderChannel?.RaiseEvent(new PlayerOrderTakeEvent(ticket));
        }
    }
}
```

- [ ] **Step 2: Check compilation via Unity console**

Unity MCP: call `read_console`. Confirm no errors for `PlayerOrderHandlerSO`.

- [ ] **Step 3: Commit**

```
git add "Assets/00. Work/BBJ/02. Scripts/Player/PlayerOrderHandlerSO.cs"
git commit -m "feat: raise MessageEvent when food not ready in PlayerOrderHandlerSO"
```

---

### Task 4: Unity Editor Setup (run after Tasks 1–3 compile cleanly)

**Depends on:** Tasks 1, 2, 3 all passing compilation.

- [ ] **Step 1: Create UIChannel.asset**

Unity MCP `manage_asset`:
- Action: create
- Type: `EventChannelSO`
- Path: `Assets/00. Work/BBJ/05. SO/EventChneel/UIChannel.asset`

- [ ] **Step 2: Create MessagePopupUI GameObject in scene**

In the Main scene, under the existing UI Canvas:
- Create a child Panel named `MessagePopupUI`
  - Anchored: center, pivot center, size ~400×80
  - Add a TMP_Text child named `Label` (centered, font size 24)
- Add `MessagePopupUI` component to the Panel object
- Set panel `SetActive(false)` as default (inactive in hierarchy)

Unity MCP `manage_gameobject` or `manage_ui` to create and configure.

- [ ] **Step 3: Wire MessagePopupUI references in Inspector**

On the `MessagePopupUI` component:
- `_uiChannel` → `UIChannel.asset`
- `_panel` → the Panel GameObject itself
- `_label` → the `Label` TMP_Text child
- `_displayDuration` → `2` (default, already set in code)

- [ ] **Step 4: Wire PlayerOrderHandlerSO**

Find the `PlayerOrderHandler.asset` SO in the Project window.
In Inspector, assign:
- `_uiChannel` → `UIChannel.asset`

- [ ] **Step 5: Save scene and verify in Play mode**

Unity MCP `manage_editor`: enter Play mode.
- Click a customer whose order is not yet in `ReadyForServer` phase.
- Confirm: popup appears at screen center, shows "제작하지 않았습니다.", disappears after ~2 seconds.
- Click a second time while popup is visible: confirm timer resets (popup stays for another 2s).

- [ ] **Step 6: Commit**

```
git add "Assets/00. Work/BBJ/05. SO/EventChneel/UIChannel.asset" "Assets/00. Work/BBJ/05. SO/EventChneel/UIChannel.asset.meta" "Assets/00. Work/BBJ/01. Scene/Main.unity"
git commit -m "feat: wire UIChannel and MessagePopupUI in scene"
```
