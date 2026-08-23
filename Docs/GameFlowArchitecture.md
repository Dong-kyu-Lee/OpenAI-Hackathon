# 게임 흐름 / 씬 전환 아키텍처 인수인계

이 문서는 타이틀 → 스테이지 선택 → 게임 플레이 → 결과로 이어지는 게임 전체 흐름과 씬 전환 시스템을 정리한 인수인계 문서다. 다른 세션이나 AI 에이전트가 이어서 작업할 때 먼저 읽는다.

프로젝트 전체 작업 규칙은 루트의 `AGENTS.md`가 최우선이다. 특히 `.unity`, `.prefab`, `.asset`, `.controller`, `.anim` 파일은 텍스트로 편집하지 않고 Unity Editor를 통해서만 변경한다.

**현재 상태: 코드, ScriptableObject 에셋, 씬 구성, 프리팹, Build Settings 등록까지 완료됐다.**

남은 필수 작업은 **`Stage_XX` 씬에 `StageRunner`를 배치하는 것 하나뿐이다** (8.8). 현재 프로젝트 어느 씬·프리팹에도 `StageRunner`가 붙어 있지 않아, 스테이지에 들어가도 맵 스크롤 시작·클리어 거리 감시·결과 발신이 전부 일어나지 않는다. 8장에서 ✅ 표시가 없는 항목을 그대로 따라 하면 동작한다.

---

## 1. 구현 범위

코드에 포함된 범위는 다음과 같다.

- 부트스트랩 → 타이틀 → 스테이지 선택 → 플레이 → 결과의 전체 흐름 상태 머신
- 흐름 씬과 게임플레이 씬의 Additive 로드/언로드
- N종류 스테이지를 데이터로 정의하고, 선택 화면에 배치한 항목들을 카탈로그 순번에 배선하기
- 주행 거리 도달에 의한 클리어 판정, 플레이어 사망에 의한 실패 판정
- 일시정지와 재개
- 결과 화면 표시 (클리어 여부 / 소요 시간 / 주행 거리 / 남은 체력)
- 같은 스테이지 다시하기 (스테이지 씬만 재로드)
- 스테이지 선택으로 돌아가기
- 전환 중복 요청 차단
- 일시정지 중 옵션 화면 — 배경음/효과음 볼륨, 키 리바인딩, `PlayerPrefs` 저장과 복원
- 스테이지를 나가기 전 종료 확인 팝업
- 옵션이나 팝업이 떠 있는 동안 일시정지 입력 억제

다음 항목은 이 시스템에서 구현하지 않았다.

- **타이틀에서 여는 설정 화면** — 옵션 화면은 일시정지 메뉴를 통해서만 열린다. 타이틀의 설정 버튼은 `TitleScreenUI`가 `interactable = false`로 비활성화한다
- **AudioManager** — `AGENTS.md` 3.4 화이트리스트에는 있지만 아직 아무도 만들지 않았다. 그래서 **옵션의 볼륨 값은 `AudioSettingsSO`에 저장·복원되기만 하고 실제 소리에는 반영되지 않는다.** `AudioSettingsSO.Changed`를 구독해 믹서에 적용할 담당자가 필요하다
- **HUD (체력바, 현재 무기 표시)** — `PlayerHealthChanged` 채널이 이미 있으므로 별도 태스크로 붙이면 된다
- **로딩 화면 / 페이드** — 전환 중 한 프레임 동안 카메라가 없는 구간이 생긴다 (9장 참조)
- **스테이지 진행도 저장, 잠금 해제** — `AGENTS.md` 7장의 스코프 밖(세이브/로드)이다
- **결과 화면의 스테이지 이름 표시** — `StageResult`는 스테이지 식별 정보를 담지 않는다 (5장 참조)

---

## 2. 어셈블리와 의존성

asmdef 방향은 변경하지 않았다.

```text
Game.App ──────> Game.Core
   │  │  │
   │  │  └─────> Game.UI ───────> Game.Core
   │  └────────> Game.Gameplay ─> Game.Core
   └───────────> Game.Data ─────> Game.Core
```

- `Game.Gameplay`는 `Game.UI`를 모르고, `Game.UI`는 `Game.Gameplay`와 `Game.App`을 모른다
- 그래서 **UI 버튼은 App의 상태 머신을 직접 호출할 수 없다.** 전부 이벤트 채널로 요청만 보낸다
- **`Game.UI.asmdef`에 `UnityEngine.UI`, `Unity.TextMeshPro`, `Unity.InputSystem`을 추가했다.** 게임 어셈블리 간 참조가 아니라 uGUI/TMP/입력 엔진 어셈블리이므로 3.1의 의존성 방향과는 무관하다. 이게 없으면 UI 코드가 아예 컴파일되지 않는다. `Unity.InputSystem`은 키 리바인딩 UI(`KeyBindEntryUI`)가 `InputAction`을 직접 다뤄야 해서 필요하다

`GameState`와 `StageResult`가 `Game.Core.Flow`에 있는 이유는, App이 방송하고 UI가 받아야 하는 타입이라 양쪽이 공통으로 볼 수 있는 최하단에 있어야 하기 때문이다.

---

## 3. 씬 구성

씬 스택은 항상 아래 셋 중 하나다. `SystemScene`은 어떤 경로로도 언로드되지 않는다.

```text
[SystemScene] + [Title]
[SystemScene] + [StageSelect]
[SystemScene] + [Stage_XX] + [UI_Scene]    # Active Scene = Stage_XX
```

| 씬 | 수명 | 내용 |
|---|---|---|
| `Bootstrap` | 시작 시 1회 | `BootstrapLoader` 하나. 타이틀이 뜨면 언로드된다 |
| `SystemScene` | 게임 내내 | `GameManager` 오브젝트(`GameManager` + `SceneFlowController` + `PauseInputRelay` + `SettingsController`), `ObjectPoolManager` 프리팹, EventSystem, 비활성 `Camera` |
| `Title` | 타이틀 동안 | 타이틀 Canvas + `TitleScreenUI` |
| `StageSelect` | 선택 화면 동안 | 선택 Canvas + `StageSelectUI` + 씬에 직접 배치한 `StageEntryButton` 3개 |
| `UI_Scene` | 플레이 동안 | 게임플레이 Canvas + `PausePanelUI`, `ResultPanelUI`, `OptionsPanel` / `QuitConfirmPopup` 하위 화면 (+ 추후 HUD) |
| `Stage_XX` | 해당 스테이지 동안 | 맵, 플레이어, 카메라, `StageRunner`(**아직 미배치 — 8.8**) |

`SystemScene`의 `Camera`는 **비활성 상태로 배치만 돼 있다.** 켜면 `Stage_XX`의 카메라와 겹쳐 이중 렌더가 되므로, 전환 중 화면 공백을 메우는 용도로 쓰려면 9장 2번을 먼저 읽는다.

`ObjectPoolManager` 프리팹 인스턴스는 `SystemScene`과 `Stage_Tmp` 양쪽에 들어 있다. 싱글톤이 중복되지 않도록 **`Stage_Tmp` 쪽 인스턴스는 비활성으로 꺼 두었다.** 새 스테이지 씬을 만들 때도 이 프리팹을 넣지 않거나 꺼 둔다.

**`UI_Scene`의 Canvas는 반드시 Screen Space - Overlay다.** Camera 모드는 `Stage_XX`의 카메라를 인스펙터로 참조해야 하는데, 이건 Unity가 막아둔 씬 간 참조다 (`AGENTS.md` 2.2 규칙 3).

EventSystem은 `SystemScene`에 **하나만** 둔다. 흐름 씬과 `UI_Scene`에 각각 두면 중복 경고가 난다.

---

## 4. 상태 머신

```text
        BeginFlow
Boot ──────────────▶ Loading ──▶ Title
                                   │  ▲
                     StageSelect   │  │ TitleRequested
                     Requested     ▼  │
                                StageSelect
                                   │  ▲
                     StageRequested│  │ StageSelectRequested
                                   ▼  │
                     Loading ──▶ Playing ◀────┐
                                 │  │  │      │ ResumeRequested
                  PauseRequested │  │  └──────┤
                                 ▼  │         │
                              Paused ─────────┘
                                 │  │
                  StageFinished  │  │ RetryRequested
                                 ▼  ▼
                              Result ──▶ Loading ──▶ Playing
```

`GameManager`가 상태를 소유하고, 모든 전환 요청에 대해 **현재 상태가 허용하는 요청인지 먼저 검사한다.** 허용되지 않는 요청은 조용히 무시된다.

| 요청 | 허용되는 현재 상태 | 결과 |
|---|---|---|
| `BeginFlow` | Boot | Title |
| `StageSelectRequested` | Title | StageSelect |
| `StageSelectRequested` | Paused, Result | 스테이지 언로드 후 StageSelect |
| `TitleRequested` | StageSelect | Title |
| `StageRequested(index)` | StageSelect | Playing |
| `PauseRequested` | Playing | Paused |
| `ResumeRequested` | Paused | Playing |
| `RetryRequested` | Paused, Result | 스테이지 재로드 후 Playing |
| `StageFinished(result)` | Playing | Result |
| `QuitRequested` | Title | 애플리케이션 종료 |

**일시정지는 `Time.timeScale = 0`으로 구현한다.** `Result` 상태도 동일하게 0이다.

> `timeScale = 0` 상태에서 UI 연출을 넣을 때는 `Time.unscaledDeltaTime`을 써야 한다. Input System은 `timeScale`의 영향을 받지 않으므로 버튼과 일시정지 입력은 정상 동작한다.

### 옵션과 종료 확인은 상태가 아니다

옵션 화면과 종료 확인 팝업은 `GameState`를 늘리지 않는다. 둘 다 `Paused` 안에서 `PausePanelUI`가 켜고 끄는 하위 화면이며, 상태 머신은 이들의 존재를 모른다.

```text
Paused ─ 옵션 버튼 ──▶ OptionsPanel 표시 (일시정지 메뉴는 숨김)
   │                      └─ 뒤로 ──▶ Paused 메뉴로 복귀
   └─ 선택으로 버튼 ─▶ QuitConfirmPopup 표시 (메뉴 위에 겹침)
                          ├─ 종료 ──▶ StageSelectRequested 발신
                          └─ 취소 ──▶ Paused 메뉴로 복귀
```

- 옵션은 일시정지 메뉴를 **대체**하고, 종료 확인 팝업은 그 위에 **겹쳐서** 뜬다
- 둘 중 하나라도 열려 있으면 `PausePanelUI`가 `PauseInputSuppressed`를 `true`로 발신해 일시정지 입력을 막는다. 하위 화면 위에서 ESC를 눌러 게임이 재개돼 버리는 것을 방지한다
- `Paused`를 벗어나면 `PausePanelUI`가 하위 화면을 전부 닫고 억제를 해제한다. `UI_Scene`이 언로드될 때도 `OnDisable`에서 억제를 푼다

---

## 5. 이벤트 채널

`Game.UI` → `Game.App`, `Game.App` → `Game.Gameplay` 방향의 직접 참조가 전부 불가능하므로 모든 도메인 간 통신은 채널을 거친다.

### 새로 추가한 채널 타입

| 타입 | 페이로드 | 용도 |
|---|---|---|
| `GameStateEventChannelSO` | `GameState` | 상태 방송. App → UI |
| `StageRequestEventChannelSO` | `int` (카탈로그 순번) | 스테이지 플레이 요청. UI → App |
| `StageResultEventChannelSO` | `StageResult` | 스테이지 종료 통지. Gameplay → App, Gameplay → UI |
| `BoolEventChannelSO` | `bool` | 켬/끔 통지. 현재는 일시정지 입력 억제에 쓴다. UI → App |

단순 요청은 **기존 `VoidEventChannelSO`의 에셋 인스턴스**를 늘려서 처리한다. 새 클래스를 만들지 않는다.

### 채널 에셋 ✅ 완료

`Assets/06.SO/Events/` 아래에 15개가 실제로 존재한다.

| 에셋 | 타입 | 발신 | 수신 |
|---|---|---|---|
| `PlayerDied` *(기존)* | Void | `PlayerHealth` | `StageScroller`, `StageRunner` |
| `PlayerHealthChanged` *(기존)* | Int | `PlayerHealth` | `StageRunner` (+ 추후 HUD) |
| `GameStateChanged` | GameState | `GameManager` | `PauseInputRelay`, `PausePanelUI`, `ResultPanelUI` |
| `StageRequested` | StageRequest | `StageEntryButton` | `GameManager` |
| `StageFinished` | StageResult | `StageRunner` | `GameManager`, `ResultPanelUI` |
| `GameplayStarted` | Void | `GameManager` | `StageRunner` |
| `GameplayStopped` | Void | `GameManager` | `StageRunner` |
| `TitleRequested` | Void | `StageSelectUI` | `GameManager` |
| `StageSelectRequested` | Void | `TitleScreenUI`, `PausePanelUI`(확인 팝업 승인 후), `ResultPanelUI` | `GameManager` |
| `PauseRequested` | Void | `PauseInputRelay` | `GameManager` |
| `ResumeRequested` | Void | `PauseInputRelay`, `PausePanelUI` | `GameManager` |
| `RetryRequested` | Void | `PausePanelUI`, `ResultPanelUI` | `GameManager` |
| `QuitRequested` | Void | `TitleScreenUI` | `GameManager` |
| `PauseInputSuppressed` | Bool | `PausePanelUI` | `PauseInputRelay` |
| `BindingsChanged` | Void | `KeyBindListUI` | `SettingsController` |

**구독은 `OnEnable`, 해제는 `OnDisable`에서 예외 없이 한다** (`AGENTS.md` 3.2). 모든 신규 컴포넌트가 이 규칙을 지키고 있다.

### StageResult가 스테이지 이름을 담지 않는 이유

`StageResult`는 `Game.Core`에 있고 `Game.Core`는 아무것도 참조하지 않는다. 스테이지 정의(`StageDefinitionSO`)는 `Game.Data`에 있으므로 Core의 구조체가 담을 수 없다. 결과 화면에 스테이지 이름을 띄우고 싶다면, `ResultPanelUI`가 `StageRequested` 채널도 구독해 마지막 순번을 기억하고 `StageCatalogSO`에서 조회하면 된다. 지금은 필요하지 않아 넣지 않았다.

---

## 6. 전환 시퀀스

`SceneFlowController`는 **항상 언로드를 먼저 끝내고 로드한다.** 카메라와 EventSystem이 한 프레임이라도 겹치지 않게 하기 위해서다.

```text
시작
  Bootstrap.Start()
    → Load(SystemScene, Additive)
    → GameManager.BeginFlow("Bootstrap")
    → SetInitialFlowScene("Bootstrap")
    → Unload(Bootstrap) → Load(Title) → SetActiveScene(Title) → 상태 Title

타이틀 → 스테이지 선택
  Unload(Title) → Load(StageSelect) → SetActiveScene → 상태 StageSelect

스테이지 선택 → 플레이
  Unload(StageSelect) → Load(Stage_XX) → Load(UI_Scene)
    → SetActiveScene(Stage_XX) → 상태 Playing → GameplayStarted.Raise()

다시하기
  timeScale=1 → GameplayStopped.Raise()
    → Unload(Stage_XX) → Load(Stage_XX) → SetActiveScene
    → 상태 Playing → GameplayStarted.Raise()          [UI_Scene, SystemScene 유지]

스테이지 선택으로
  timeScale=1 → GameplayStopped.Raise()
    → Unload(Stage_XX) → Unload(UI_Scene) → Load(StageSelect) → 상태 StageSelect
```

`GameplayStarted`는 스테이지 씬 로드가 **완전히 끝난 뒤에** 발신된다. 로드가 끝나는 시점에는 `StageRunner.OnEnable`이 이미 실행돼 구독이 완료되어 있으므로 신호를 놓치지 않는다.

전환 중에는 `SceneFlowController.IsTransitioning`이 `true`가 되어 새 전환 요청이 경고와 함께 무시된다. 상태도 `Loading`이라 `GameManager`의 상태 검사에서도 한 번 더 걸러진다.

---

## 7. 클래스별 책임

### Game.Core

| 클래스 | 책임 |
|---|---|
| `GameState` | 흐름 단계 enum |
| `StageResult` | 종료된 플레이 1회의 요약 (readonly struct) |
| `GameStateEventChannelSO` / `StageRequestEventChannelSO` / `StageResultEventChannelSO` / `BoolEventChannelSO` | 채널 |

### Game.Data

| 클래스 | 책임 |
|---|---|
| `StageDefinitionSO` | 스테이지 1개의 이름, 씬 이름, 썸네일, 클리어 거리 |
| `StageCatalogSO` | 스테이지 목록과 순서. `TryGet(index, out definition)`과 `IndexOf(definition)` 제공 |
| `AudioSettingsSO` | 배경음/효과음 볼륨 보관과 변경 통지. 믹서 적용과 저장은 하지 않는다 |

`StageCatalogSO.IndexOf`는 씬에 직접 배치된 `StageEntryButton`이 자기 순번을 알아내기 위해 쓴다. 찾지 못하면 `StageCatalogSO.InvalidIndex`(-1)를 돌려준다.

### Game.App

| 클래스 | 책임 | 하지 않는 것 |
|---|---|---|
| `BootstrapLoader` | 시스템 씬을 올리고 흐름을 `GameManager`에 인계 | 자기 씬을 스스로 언로드하지 않는다 (코루틴이 죽는다) |
| `GameManager` | 상태 소유, 전환 가부 판단, `timeScale` 제어, 상태 방송 | 씬을 직접 로드하지 않는다 |
| `SceneFlowController` | 씬의 Additive 로드/언로드, `SetActiveScene`, 중복 전환 차단 | 어떤 전환이 옳은지 판단하지 않는다 |
| `PauseInputRelay` | 입력 1개를 현재 상태에 맞는 일시정지/재개 요청으로 변환, 억제 신호에 따라 입력 무시 | 상태를 바꾸지 않는다 |
| `SettingsController` | 볼륨과 키 바인딩을 `PlayerPrefs`에 저장/복원 | 설정 화면을 알지 못한다. 볼륨을 소리에 적용하지 않는다 |

`SceneFlowController`, `PauseInputRelay`, `SettingsController`는 **싱글톤이 아니다.** `GameManager`와 같은 GameObject에 붙여 `[SerializeField]`로 직접 참조한다. `AGENTS.md` 3.4의 싱글톤 화이트리스트를 늘리지 않기 위한 구조다.

`SettingsController`는 `Awake`에서 먼저 복원한 뒤 `OnEnable`에서 구독한다. 복원이 다시 저장으로 이어지는 왕복을 끊기 위한 순서이니 바꾸지 않는다. 저장 키는 `Settings.BgmVolume`, `Settings.SfxVolume`, `Settings.InputBindings` 셋이다.

### Game.Gameplay

| 클래스 | 책임 |
|---|---|
| `StageRunner` | 시작/정지 신호를 맵 스트리밍·스크롤 구동으로 변환, 클리어 거리와 사망 감시, 결과 발신 |

`StageRunner`는 남은 체력을 플레이어에서 직접 읽지 않고 `PlayerHealthChanged` 채널의 마지막 값을 기억한다. 플레이어가 동적으로 스폰되어도 참조가 끊기지 않는다.

종료 판정 시에는 `MapScrollController.StopScrolling()`만 호출하고 `StopStreaming()`은 호출하지 않는다. 결과 화면 뒤로 맵이 그대로 남아 있어야 하기 때문이다. `StopStreaming()`은 `GameplayStopped`를 받았을 때, 즉 실제 정리 시점에만 부른다.

### Game.UI

| 클래스 | 책임 |
|---|---|
| `TitleScreenUI` | 시작 / 설정(비활성) / 종료 버튼 → 요청 채널 |
| `StageSelectUI` | 씬에 배치된 항목들을 카탈로그 순번·요청 채널에 배선, 뒤로가기 |
| `StageEntryButton` | 인스펙터로 지정된 스테이지 1개 표시, 클릭 시 순번 발신 |
| `PausePanelUI` | Paused 상태에서 패널 표시, 계속/다시하기/옵션/나가기, 하위 화면 조율과 입력 억제 |
| `QuitConfirmPopupUI` | 정말 나갈지 되묻고 선택 결과만 이벤트로 알림 |
| `OptionsPanelUI` | 볼륨 슬라이더를 `AudioSettingsSO`에 반영, 닫힘 알림 |
| `KeyBindListUI` | 리바인딩 대상 액션 목록 생성, 초기화 버튼, 변경 시 저장 요청 발신 |
| `KeyBindEntryUI` | 액션 1개의 표시와 리바인딩 수행 |
| `ResultPanelUI` | 결과 값 채우기, Result 상태에서 패널 표시, 다시하기/선택으로 |

**`PausePanelUI`와 `ResultPanelUI`는 항상 활성인 오브젝트에 붙이고, `_panelRoot`로 지정한 자식 오브젝트만 켜고 끈다.** 컴포넌트가 붙은 오브젝트 자체를 꺼 두면 `OnEnable`이 실행되지 않아 상태 방송을 영영 받지 못한다.

`PausePanelUI`는 `_panelRoot`와 함께 `_backgroundPanel`(뒤를 덮는 반투명 배경)도 켜고 끈다. **둘 다 반드시 연결해야 한다.** `_backgroundPanel`이 비어 있으면 패널을 켤 때 예외가 난다.

`QuitConfirmPopupUI`와 `OptionsPanelUI`는 반대로 **자기 오브젝트를 직접 켜고 끄므로 각 하위 화면의 루트 오브젝트에 붙인다.** 이들은 채널을 알지 못하고 `Open` / `Close`와 C# 이벤트로만 `PausePanelUI`와 대화한다. 덕분에 나중에 타이틀에서도 같은 옵션 패널을 재사용할 수 있다.

`StageEntryButton`은 `OnValidate`로 에디터에서 스테이지를 지정하는 즉시 이름과 썸네일을 미리 보여준다. Play 중에는 동작하지 않는 편집용 코드다.

---

## 8. 에디터에서 필요한 작업

8.8을 제외한 나머지는 완료됐다. 아래는 무엇이 어떻게 연결돼 있는지에 대한 기록이자, 새 스테이지나 새 화면을 붙일 때 따라갈 절차다. `AGENTS.md` 2.3에 따라 씬/프리팹 조작은 명시적 지시가 있을 때만 AI가 수행한다.

### 8.1 ProjectSettings ✅ 완료 (사용자 직접 — `AGENTS.md` 1장 5번)

Build Settings의 Scenes In Build는 현재 아래와 같다.

```text
0. Assets/00.Scenes/Bootstrap.unity      ← 반드시 index 0
1. Assets/00.Scenes/SystemScene.unity
2. Assets/00.Scenes/Title.unity
3. Assets/00.Scenes/StageSelect.unity
4. Assets/00.Scenes/Stage_Tmp.unity
5. Assets/00.Scenes/UI_Scene.unity
6. Assets/00.Scenes/SampleScene.unity    ← 비활성 잔재. 파일은 이미 삭제됐다
```

Additive 로드는 이름으로 찾으므로 4번과 5번의 순서는 상관없다. index 0이 `Bootstrap`인 것만 지켜진다.

**6번 항목은 삭제된 `SampleScene`을 가리키는 잔재다.** `enabled: 0`이라 빌드는 깨지지 않지만, 정리하려면 사용자가 Build Profiles 창에서 직접 지운다.

### 8.2 ScriptableObject 에셋 ✅ 완료

`Assets/06.SO/Events/` — 채널 13개 신규 생성. 기존 `PlayerDied`, `PlayerHealthChanged`는 그대로 두어 총 15개다.

```text
GameplayStarted.asset          VoidEventChannelSO
GameplayStopped.asset          VoidEventChannelSO
TitleRequested.asset           VoidEventChannelSO
StageSelectRequested.asset     VoidEventChannelSO
PauseRequested.asset           VoidEventChannelSO
ResumeRequested.asset          VoidEventChannelSO
RetryRequested.asset           VoidEventChannelSO
QuitRequested.asset            VoidEventChannelSO
BindingsChanged.asset          VoidEventChannelSO
GameStateChanged.asset         GameStateEventChannelSO
StageRequested.asset           StageRequestEventChannelSO
StageFinished.asset            StageResultEventChannelSO
PauseInputSuppressed.asset     BoolEventChannelSO
```

`Assets/06.SO/Settings/AudioSettings.asset` — `AudioSettingsSO` 1개. 기본 볼륨은 배경음·효과음 모두 0.8이다.

`Assets/06.SO/Stage/` — 정의 3개와 카탈로그 1개.

| 에셋 | Display Name | Scene Name | Thumbnail | Clear Distance |
|---|---|---|---|---|
| `StageDefinition_Stage00.asset` | `Stage 00 (Tutorial)` | `Stage_Tmp` | 연결됨 | `50` |
| `StageDefinition_Stage01.asset` | `Stage 01` | `Stage_Tmp` | 연결됨 | `50` |
| `StageDefinition_Stage02.asset` | `Stage 02` | `Stage_Tmp` | 연결됨 | `50` |

- Scene Name은 **Build Settings에 등록된 씬 파일명과 정확히 일치해야 한다.** 스테이지 씬이 아직 `Stage_Tmp` 하나뿐이라 셋 다 같은 씬을 가리키는 임시 상태다. 실제 스테이지 씬이 생기면 각각 바꾼다
- Clear Distance `50`은 동작 확인용으로 짧게 잡은 값이다. `MapScrollSettings.InitialSpeed = 8` 기준 약 6초 분량이니, 실제 밸런싱 때는 늘린다
- Thumbnail을 비우면 `StageEntryButton`이 Image를 끈다

**`StageCatalog.asset`** — `_stages`에 위 세 정의를 Stage00, Stage01, Stage02 순으로 연결했다. **이 배열의 순서가 곧 `StageRequested` 채널로 오가는 순번**이므로, 순서를 바꾸면 씬에 배치된 항목의 배선도 함께 달라진다(코드 수정은 필요 없다 — 8.6 참조).

### 8.3 SystemScene ✅ 완료

| 오브젝트 | 컴포넌트 | 연결 |
|---|---|---|
| `GameManager` | `GameManager` | Scene Flow → 같은 오브젝트의 `SceneFlowController` / Stage Catalog → `StageCatalog.asset` / 채널 11개 |
| | `SceneFlowController` | Ui Scene Name = `UI_Scene` |
| | `PauseInputRelay` | Pause Action → `InputSystem_Actions`의 **UI/Cancel** / 채널 4개(`PauseInputSuppressed` 포함) |
| | `SettingsController` | Audio Settings → `AudioSettings.asset` / Input Actions → `InputSystem_Actions` / `BindingsChanged` |
| `ObjectPoolManager` | *(프리팹 인스턴스)* | |
| `EventSystem` | EventSystem + InputSystemUIInputModule | |
| `Camera` | Camera (URP) | **비활성.** 3장 참조 |

`GameManager`의 Title Scene Name / Stage Select Scene Name 기본값은 `Title` / `StageSelect`다.

### 8.4 Bootstrap ✅ 완료

`BootstrapLoader` 컴포넌트 하나만 있는 오브젝트가 들어 있다. System Scene Name 기본값은 `SystemScene`이다.

### 8.5 Title ✅ 완료

Canvas (Overlay) 아래에 시작 / 설정 / 종료 버튼이 있고, `TitleScreenUI`가 버튼 3개와 `StageSelectRequested`, `QuitRequested` 채널에 연결돼 있다. 설정 버튼은 코드가 `Awake`에서 비활성화한다.

### 8.6 StageSelect ✅ 완료

목록을 코드로 생성하지 않고 **씬에 직접 배치한다.** 스테이지 선택 화면이 단순 목록이 아니라 맵 형태(`MapRoot` + 항목 + 연결선 `Links`)라, 위치와 모양을 씬에서 눈으로 잡는 편이 낫기 때문이다.

현재 구성:

1. `02.Prefabs/UI/StageEntryButton.prefab` — Button + 이름 TMP_Text + 썸네일 Image, 루트에 `StageEntryButton`
2. Canvas (Overlay) → `MapViewport` → `MapRoot` 아래에 위 프리팹 인스턴스 3개와 `Links`(연결선 Image)를 원하는 위치에 배치
3. 각 인스턴스의 `Definition` 필드에 표시할 `StageDefinition`을 인스펙터로 지정 (지정 즉시 이름·썸네일이 씬 뷰에 반영된다)
4. Canvas의 `StageSelectUI`에 Catalog / **Entries(배치한 항목 3개)** / Back Button / `StageRequested` / `TitleRequested` 연결

**스테이지를 추가할 때**: 정의 에셋을 만들어 카탈로그 배열에 넣고, 이 씬에 프리팹 인스턴스를 하나 더 놓아 `Definition`을 지정한 뒤 `StageSelectUI`의 Entries 배열에 추가한다. 순번은 `StageSelectUI`가 카탈로그에서 조회해 넣으므로 손으로 적지 않는다.

배선 실패는 Play 시작 시 콘솔 에러로 드러난다 — 빈 슬롯, `Definition` 미지정, 카탈로그에 없는 정의, 같은 정의가 두 항목에 지정된 경우를 각각 구분해 알려준다.

### 8.7 UI_Scene ✅ 완료

Canvas는 **Screen Space - Overlay**. EventSystem은 넣지 않았다.

- 항상 활성인 오브젝트의 `PausePanelUI` → `_panelRoot`(`PanelRoot`)와 `_backgroundPanel`(`BackgroundPanel`)에 켜고 끌 자식을 연결. 버튼 4개(계속/다시하기/옵션/나가기)와 하위 화면 2개, 채널 5개 연결
- `OptionsPanel` 프리팹 인스턴스 — `OptionsPanelUI`(볼륨 슬라이더 2개, 뒤로) + 자식의 `KeyBindListUI`(`KeyBindEntry` 프리팹, 리바인딩 대상 액션 목록, 초기화 버튼, `BindingsChanged`)
- `QuitConfirmRoot` — `QuitConfirmPopupUI`, 확인/취소 버튼
- 항상 활성인 오브젝트의 `ResultPanelUI` → `StatsRoot`(HealthRow/DistanceRow/TimeRow), `OutcomeText`, 다시하기/선택으로 버튼
- 하위 화면 오브젝트는 기본 비활성으로 둬도 된다 (`Awake`에서 어차피 꺼진다)

### 8.8 Stage_XX ⬜ **남은 작업**

**현재 어느 씬·프리팹에도 `StageRunner`가 배치돼 있지 않다.** 이것 없이는 스테이지에 들어가도 맵이 흐르지 않고, 클리어·사망 판정도, 결과 화면도 뜨지 않는다.

`Stage_Tmp`에 `StageRunner`를 붙인 오브젝트를 하나 추가하고 아래를 연결한다.

| 필드 | 연결 대상 |
|---|---|
| Stage Definition | 이 씬에 해당하는 정의. 지금은 셋 다 `Stage_Tmp`를 가리키므로 `StageDefinition_Stage01.asset`을 쓴다 |
| Stream Manager / Scroll Controller | 씬 안의 `MapRuntime` |
| Gameplay Started / Stopped, Player Died, Player Health Changed | 해당 채널 |
| Stage Finished | `StageFinished.asset` |

**맵이 씬 진입과 동시에 자동으로 흐르지 않게 해야 한다.** 스크롤과 스트리밍의 시작 권한은 이제 `StageRunner`에 있다.

---

## 9. 알려진 제약과 미해결 항목

1. **`StageRunner` 미배치** — 8.8. 현재 가장 큰 구멍이며, 흐름 전체가 여기서 끊긴다.

2. **볼륨 설정이 소리에 반영되지 않는다** — `AudioSettingsSO`에 값이 쓰이고 `PlayerPrefs`에 저장·복원되지만, `Changed`를 구독해 실제 믹서에 적용할 `AudioManager`가 아직 없다. 옵션의 슬라이더를 움직여도 지금은 들리는 소리가 변하지 않는다.

3. **오브젝트 풀에 남는 총알** — `ObjectPoolManager`는 `SystemScene`에 있고 풀링된 인스턴스는 그 아래에 부모가 설정된다. 스테이지 씬을 언로드해도 **날아가던 총알과 이펙트는 살아남는다.** 다시하기 직후 이전 판의 총알이 보일 수 있다.
   - 근본 해결은 `ObjectPoolManager`에 전체 반환 API를 추가하는 것인데, `AGENTS.md` 2.1이 `Core/` 기존 파일의 수정을 금지하므로 **손대지 않았다.** 필요해지면 팀에 보고하고 승인을 받아야 한다.
   - 관련해서 `PooledHitEffect`는 외부 스크립트(`CFX_AutoDestructShuriken`)가 자기를 직접 비활성화해도 풀로 돌아오도록 `OnDisable`에서 회수한다. 인스턴스가 새는 문제는 이걸로 막혔지만, 위의 "씬을 넘어 살아남는 총알"은 여전히 남아 있다.

4. **전환 중 카메라 공백** — 언로드를 먼저 하므로 한두 프레임 카메라가 없다. `SystemScene`에 카메라 오브젝트가 하나 놓여 있지만 **비활성이라 아직 이 공백을 메우지 않는다.** 켜면 `Stage_XX` 카메라와 동시에 렌더되므로, 쓰려면 전환 중에만 켜지도록 `SceneFlowController`가 제어하거나 로딩 페이드를 함께 넣어야 한다.

5. **일시정지 입력이 UI/Cancel과 공유된다** — 전용 액션이 없어 `InputSystem_Actions`의 `UI/Cancel`을 재사용한다. 옵션·확인 팝업 위에서의 오작동은 `PauseInputSuppressed` 억제 채널로 막았지만, EventSystem이 같은 입력을 소비해 생기는 이중 반응까지 없앤 것은 아니다. 문제가 되면 `InputSystem_Actions`에 `Player/Pause` 액션을 추가하고 `PauseInputRelay`의 참조만 바꾸면 된다. `.inputactions` 수정은 생성 코드 재생성이 필요하므로 사용자가 직접 한다.

6. **남은 체력 초기값** — `StageRunner`는 `PlayerHealthChanged`의 마지막 값을 기억한다. 같은 씬 로드 안에서 `PlayerHealth.OnEnable`과 `StageRunner.OnEnable`의 실행 순서는 보장되지 않으므로, 최초 통지를 놓치면 첫 피해나 패시브 드레인이 발생할 때까지 0으로 남는다. 현재 `PlayerStatsSO`에 패시브 드레인이 있어 실질적인 문제는 없다.

7. **Play 중 바꾼 볼륨이 `AudioSettings.asset`에 남는다** — ScriptableObject의 일반적인 성질이다. 실제 저장소는 `PlayerPrefs`라 빌드 동작에는 영향이 없지만, 인스펙터에 보이는 값이 마지막 플레이 값이라 git diff에 잡힐 수 있다.

8. **`MapTest`** — 흐름에 포함되지 않은 테스트 씬이다. `ObjectPoolManager`와 `PlayerHealthText`가 여기 들어 있다. 정리 여부는 사용자가 판단한다. (`SampleScene`은 삭제됐고 `Player.unity`는 `Stage_Tmp.unity`로 이름이 바뀌었다.)

9. **`Assets/01.Scripts/Test.cs`** — `01.Scripts/` 루트에 있고 네임스페이스가 없다. `AGENTS.md` 2.1 위반이지만 이번 작업 범위 밖이라 손대지 않았다.

---

## 10. 검증 방법

8.8을 마친 뒤 `Bootstrap` 씬에서 Play해 아래를 순서대로 확인한다.

1. 타이틀이 뜨고 Hierarchy에 `SystemScene` + `Title`만 남아 있는지 확인 (`Bootstrap`이 사라져야 한다)
2. 시작 → 스테이지 항목 3개가 뜨고 각각 이름과 썸네일이 채워져 있는지, 콘솔에 배선 에러가 없는지 확인
3. 스테이지 선택 → `Stage_XX` + `UI_Scene`이 함께 올라오고 `StageSelect`가 사라지는지, 맵이 흐르기 시작하는지 확인
4. ESC → 일시정지 패널과 배경이 함께 뜨고 맵이 멈추는지, 다시 ESC로 재개되는지 확인
5. 일시정지 → 옵션 → 슬라이더를 움직이고 키를 하나 리바인딩한 뒤 뒤로 → 일시정지 메뉴로 돌아오는지 확인. **옵션이 떠 있는 동안 ESC를 눌러도 게임이 재개되지 않아야 한다**
6. Play를 껐다 켜서 다시 옵션 진입 → 5번에서 바꾼 볼륨과 키가 유지되는지 확인 (`PlayerPrefs` 복원)
7. 일시정지 → 나가기 → 확인 팝업에서 취소 → 일시정지 메뉴로 돌아오는지, 팝업 위에서 ESC가 막히는지 확인
8. 다시 나가기 → 종료 → 스테이지 선택으로 나가지는지 확인
9. 스테이지 클리어 거리(현재 `50`)까지 진행 → 결과 화면에 STAGE CLEAR와 시간/거리/체력이 표시되는지 확인
10. 죽을 때까지 방치 → STAGE FAILED가 뜨는지 확인
11. 다시하기 → `Stage_XX`만 다시 로드되고 `UI_Scene`은 유지되는지 Hierarchy에서 확인
12. 스테이지 선택으로 → `Stage_XX`와 `UI_Scene`이 모두 사라지는지 확인
13. 전 과정에서 콘솔에 에러가 없는지 확인
