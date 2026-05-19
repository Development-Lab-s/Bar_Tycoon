# MessagePopupUI Design

**Date:** 2026-05-17
**Branch:** BBJ
**Scope:** BBJ folder only

## Summary

Show a centered message popup when the player clicks a customer whose food hasn't been crafted yet ("제작하지 않았습니다."). Uses existing EventChannelSO pattern with a new UIChannel.

## Components

### 1. UIEvents.cs
- Path: `Assets/00. Work/BBJ/02. Scripts/Event/UIEvents.cs`
- Defines `MessageEvent : GameEvent` with a single `string Message` property.

### 2. UIChannel.asset
- Path: `Assets/00. Work/BBJ/05. SO/EventChneel/UIChannel.asset`
- An `EventChannelSO` asset. No new class needed.

### 3. MessagePopupUI.cs
- Path: `Assets/00. Work/BBJ/02. Scripts/UI/MessagePopupUI.cs`
- MonoBehaviour. Single reusable instance on a Canvas panel, screen center.
- `[SerializeField] EventChannelSO _uiChannel`
- `OnEnable/OnDisable`: subscribe/unsubscribe to `MessageEvent`
- On receive: set TMP text, activate panel, start auto-hide coroutine (default 2s)
- If message arrives while showing: cancel previous coroutine, restart timer

### 4. PlayerOrderHandlerSO.cs (modification)
- Path: `Assets/00. Work/BBJ/02. Scripts/Player/PlayerOrderHandlerSO.cs`
- Add `[SerializeField] private EventChannelSO _uiChannel;`
- In `OnCustomerClicked`, when `ticket.WorkPhase != OrderWorkPhase.ReadyForServer`:
  raise `MessageEvent("제작하지 않았습니다.")` before returning.

## Data Flow

```
Player clicks customer
  → PlayerOrderHandlerSO.OnCustomerClicked()
  → ticket.WorkPhase != ReadyForServer
  → _uiChannel.RaiseEvent(new MessageEvent("제작하지 않았습니다."))
  → MessagePopupUI receives event
  → shows panel, auto-hides after 2s
```

## Unity Editor Setup

1. Create `UIChannel.asset` (EventChannelSO) at `BBJ/05. SO/EventChneel/`
2. Add `MessagePopupUI` GameObject to scene Canvas (centered)
3. Assign `UIChannel` to `MessagePopupUI._uiChannel`
4. Assign `UIChannel` to `PlayerOrderHandlerSO._uiChannel` in the SO inspector
