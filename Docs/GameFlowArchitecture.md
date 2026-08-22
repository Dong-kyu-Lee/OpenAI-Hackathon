# 게임 흐름 / 씬 전환 아키텍처 인수인계

이 문서는 타이틀 → 스테이지 선택 → 게임 플레이 → 결과로 이어지는 게임 전체 흐름과 씬 전환 시스템을 정리한 인수인계 문서다. 다른 세션이나 AI 에이전트가 이어서 작업할 때 먼저 읽는다.

프로젝트 전체 작업 규칙은 루트의 `AGENTS.md`가 최우선이다. 특히 `.unity`, `.prefab`, `.asset`, `.controller`, `.anim` 파일은 텍스트로 편집하지 않고 Unity Editor를 통해서만 변경한다.

**현재 상태: 코드와 ScriptableObject 에셋까지 완료됐다.** 씬 구성, 프리팹, Build Settings 등록은 아직 되어 있지 않다. 8장에서 ✅ 표시가 없는 항목을 그대로 따라 하면 동작한다.

---

## 1. 구현 범위

코드에 포함된 범위는 다음과 같다.

- 부트스트랩 → 타이틀 → 스테이지 선택 → 플레이 → 결과의 전체 흐름 상태 머신
- 흐름 씬과 게임플레이 씬의 Additive 로드/언로드
- N종류 스테이지를 데이터로 정의하고 선택 화면에서 목록으로 펼치기
- 주행 거리 도달에 의한 클리어 판정, 플레이어 사망에 의한 실패 판정
- 일시정지와 재개
- 결과 화면 표시 (클리어 여부 / 소요 시간 / 주행 거리 / 남은 체력)
- 같은 스테이지 다시하기 (스테이지 씬만 재로드)
- 스테이지 선택으로 돌아가기
- 전환 중복 요청 차단

다음 항목은 이 시스템에서 구현하지 않았다.

- **게임 설정 화면** — 요청에 따라 범위에서 제외했다. 타이틀의 설정 버튼은 `TitleScreenUI`가 `interactable = false`로 비활성화한다
- **AudioManager** — `AGENTS.md` 3.4 화이트리스트에는 있지만 아직 아무도 만들지 않았다
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
- **`Game.UI.asmdef`에 `UnityEngine.UI`와 `Unity.TextMeshPro`를 추가했다.** 게임 어셈블리 간 참조가 아니라 uGUI/TMP 엔진 어셈블리이므로 3.1의 의존성 방향과는 무관하다. 이게 없으면 UI 코드가 아예 컴파일되지 않는다

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
| `SystemScene` | 게임 내내 | `GameManager`, `SceneFlowController`, `PauseInputRelay`, `ObjectPoolManager`, EventSystem |
| `Title` | 타이틀 동안 | 타이틀 Canvas + `TitleScreenUI` |
| `StageSelect` | 선택 화면 동안 | 선택 Canvas + `StageSelectUI` |
| `UI_Scene` | 플레이 동안 | 게임플레이 Canvas + `PausePanelUI`, `ResultPanelUI` (+ 추후 HUD) |
| `Stage_XX` | 해당 스테이지 동안 | 맵, 플레이어, 카메라, `StageRunner` |

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

---

## 5. 이벤트 채널

`Game.UI` → `Game.App`, `Game.App` → `Game.Gameplay` 방향의 직접 참조가 전부 불가능하므로 모든 도메인 간 통신은 채널을 거친다.

### 새로 추가한 채널 타입

| 타입 | 페이로드 | 용도 |
|---|---|---|
| `GameStateEventChannelSO` | `GameState` | 상태 방송. App → UI |
| `StageRequestEventChannelSO` | `int` (카탈로그 순번) | 스테이지 플레이 요청. UI → App |
| `StageResultEventChannelSO` | `StageResult` | 스테이지 종료 통지. Gameplay → App, Gameplay → UI |

단순 요청은 **기존 `VoidEventChannelSO`의 에셋 인스턴스**를 늘려서 처리한다. 새 클래스를 만들지 않는다.

### 만들어야 할 채널 에셋

`Assets/06.SO/Events/` 아래에 만든다. 앞의 둘은 이미 있다.

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
| `StageSelectRequested` | Void | `TitleScreenUI`, `PausePanelUI`, `ResultPanelUI` | `GameManager` |
| `PauseRequested` | Void | `PauseInputRelay` | `GameManager` |
| `ResumeRequested` | Void | `PauseInputRelay`, `PausePanelUI` | `GameManager` |
| `RetryRequested` | Void | `PausePanelUI`, `ResultPanelUI` | `GameManager` |
| `QuitRequested` | Void | `TitleScreenUI` | `GameManager` |

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
| `GameStateEventChannelSO` / `StageRequestEventChannelSO` / `StageResultEventChannelSO` | 채널 |

### Game.Data

| 클래스 | 책임 |
|---|---|
| `StageDefinitionSO` | 스테이지 1개의 이름, 씬 이름, 썸네일, 클리어 거리 |
| `StageCatalogSO` | 스테이지 목록과 순서. `TryGet(index, out definition)` 제공 |

### Game.App

| 클래스 | 책임 | 하지 않는 것 |
|---|---|---|
| `BootstrapLoader` | 시스템 씬을 올리고 흐름을 `GameManager`에 인계 | 자기 씬을 스스로 언로드하지 않는다 (코루틴이 죽는다) |
| `GameManager` | 상태 소유, 전환 가부 판단, `timeScale` 제어, 상태 방송 | 씬을 직접 로드하지 않는다 |
| `SceneFlowController` | 씬의 Additive 로드/언로드, `SetActiveScene`, 중복 전환 차단 | 어떤 전환이 옳은지 판단하지 않는다 |
| `PauseInputRelay` | 입력 1개를 현재 상태에 맞는 일시정지/재개 요청으로 변환 | 상태를 바꾸지 않는다 |

`SceneFlowController`와 `PauseInputRelay`는 **싱글톤이 아니다.** `GameManager`와 같은 GameObject에 붙여 `[SerializeField]`로 직접 참조한다. `AGENTS.md` 3.4의 싱글톤 화이트리스트를 늘리지 않기 위한 구조다.

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
| `StageSelectUI` | 카탈로그를 항목으로 펼치기, 뒤로가기 |
| `StageEntryButton` | 스테이지 1개 표시, 클릭 시 순번 발신 |
| `PausePanelUI` | Paused 상태에서 패널 표시, 계속/다시하기/선택으로 |
| `ResultPanelUI` | 결과 값 채우기, Result 상태에서 패널 표시, 다시하기/선택으로 |

**`PausePanelUI`와 `ResultPanelUI`는 항상 활성인 오브젝트에 붙이고, `_panelRoot`로 지정한 자식 오브젝트만 켜고 끈다.** 컴포넌트가 붙은 오브젝트 자체를 꺼 두면 `OnEnable`이 실행되지 않아 상태 방송을 영영 받지 못한다.

---

## 8. 에디터에서 필요한 작업

코드만 구현된 상태이므로 아래를 전부 해야 동작한다. `AGENTS.md` 2.3에 따라 씬/프리팹 조작은 명시적 지시가 있을 때만 AI가 수행한다.

### 8.1 ProjectSettings (사용자 직접 — `AGENTS.md` 1장 5번)

Build Settings의 Scenes In Build를 아래로 재구성한다. **현재 존재하지 않는 `UI_Scene`(GUID `917ea29d…`)을 참조하고 있어 정리가 필요하다.**

```text
0. Assets/00.Scenes/Bootstrap.unity      ← 반드시 index 0
1. Assets/00.Scenes/SystemScene.unity
2. Assets/00.Scenes/Title.unity
3. Assets/00.Scenes/StageSelect.unity
4. Assets/00.Scenes/UI_Scene.unity
5. Assets/00.Scenes/Stage_Tmp.unity
```

### 8.2 ScriptableObject 에셋 ✅ 완료

Unity CLI(`unity command eval_file`)로 생성 및 설정을 마쳤다. 아래 13개가 실제로 존재한다.

`Assets/06.SO/Events/` — 채널 11개 신규 생성. 기존 `PlayerDied`, `PlayerHealthChanged`는 그대로 두었다.

```text
GameplayStarted.asset          VoidEventChannelSO
GameplayStopped.asset          VoidEventChannelSO
TitleRequested.asset           VoidEventChannelSO
StageSelectRequested.asset     VoidEventChannelSO
PauseRequested.asset           VoidEventChannelSO
ResumeRequested.asset          VoidEventChannelSO
RetryRequested.asset           VoidEventChannelSO
QuitRequested.asset            VoidEventChannelSO
GameStateChanged.asset         GameStateEventChannelSO
StageRequested.asset           StageRequestEventChannelSO
StageFinished.asset            StageResultEventChannelSO
```

`Assets/06.SO/Stage/` — 정의 1개와 카탈로그 1개.

**`StageDefinition_Stage01.asset`**

| 필드 | 설정된 값 | 비고 |
|---|---|---|
| Display Name | `Stage 01` | 선택 화면에 표시된다 |
| Scene Name | `Stage_Tmp` | **Build Settings에 등록된 씬 파일명과 정확히 일치해야 한다.** 현재는 임시 씬을 가리킨다 |
| Thumbnail | *(비움)* | 비우면 `StageEntryButton`이 Image를 끈다 |
| Clear Distance | `480` | `MapScrollSettings.InitialSpeed = 8`이므로 **약 60초 분량**이다. 밸런싱용 값이니 자유롭게 바꾼다 |

**`StageCatalog.asset`** — `_stages[0]`에 위 정의를 연결했다. 스테이지를 추가할 때는 `StageDefinition`을 하나 더 만들어 이 배열에 넣으면 되고, 코드 수정은 필요 없다.

> 스테이지 씬이 `Stage_Tmp` 하나뿐이라 정의도 1개만 만들었다. 스테이지가 늘어나면 정의를 추가하고 카탈로그 배열에 등록한다.

### 8.3 SystemScene

| 오브젝트 | 컴포넌트 | 연결 |
|---|---|---|
| `GameManager` | `GameManager` | Scene Flow → 같은 오브젝트의 `SceneFlowController` / Stage Catalog → `StageCatalog.asset` / 채널 11개 |
| | `SceneFlowController` | Ui Scene Name = `UI_Scene` |
| | `PauseInputRelay` | Pause Action → `InputSystem_Actions`의 **UI/Cancel** / 채널 3개 |
| `ObjectPoolManager` | *(기존 프리팹 배치)* | |
| `EventSystem` | EventSystem + InputSystemUIInputModule | |

`GameManager`의 Title Scene Name / Stage Select Scene Name 기본값은 `Title` / `StageSelect`다.

### 8.4 Bootstrap

`BootstrapLoader` 컴포넌트 하나만 있는 오브젝트를 둔다. System Scene Name 기본값은 `SystemScene`이다. Main Camera는 남겨 둬도 되고 지워도 된다.

### 8.5 Title

Canvas (Overlay) 아래에 시작 / 설정 / 종료 버튼을 만들고, Canvas에 `TitleScreenUI`를 붙여 버튼 3개와 `StageSelectRequested`, `QuitRequested` 채널을 연결한다.

### 8.6 StageSelect

1. `02.Prefabs/UI/StageEntryButton.prefab`을 만든다 — Button + 이름 TMP_Text + 썸네일 Image, 루트에 `StageEntryButton` 컴포넌트
2. Canvas (Overlay) 아래에 목록 컨테이너를 만들고 Vertical/Grid Layout Group을 붙인다
3. Canvas에 `StageSelectUI`를 붙이고 Catalog / Entry Prefab / Entry Parent / Back Button / `StageRequested` / `TitleRequested`를 연결한다

### 8.7 UI_Scene (신규 생성)

Canvas는 **Screen Space - Overlay**. EventSystem은 넣지 않는다.

- 항상 활성인 `PausePanel` 오브젝트에 `PausePanelUI` → `_panelRoot`에는 실제로 켜고 끌 자식 패널을 연결
- 항상 활성인 `ResultPanel` 오브젝트에 `ResultPanelUI` → 마찬가지
- 두 패널의 자식 오브젝트는 기본 비활성으로 둬도 된다 (`Awake`에서 어차피 꺼진다)

### 8.8 Stage_XX

기존 스테이지 씬에 `StageRunner`를 붙인 오브젝트를 하나 추가하고 연결한다.

| 필드 | 연결 대상 |
|---|---|
| Stage Definition | 이 씬에 해당하는 정의. `Stage_Tmp`라면 `StageDefinition_Stage01.asset` |
| Stream Manager / Scroll Controller | 씬 안의 `MapRuntime` |
| Gameplay Started / Stopped, Player Died, Player Health Changed | 해당 채널 |
| Stage Finished | `StageFinished.asset` |

**맵이 씬 진입과 동시에 자동으로 흐르지 않게 해야 한다.** 스크롤과 스트리밍의 시작 권한은 이제 `StageRunner`에 있다.

---

## 9. 알려진 제약과 미해결 항목

1. **오브젝트 풀에 남는 총알** — `ObjectPoolManager`는 `SystemScene`에 있고 풀링된 인스턴스는 그 아래에 부모가 설정된다. 스테이지 씬을 언로드해도 **날아가던 총알과 이펙트는 살아남는다.** 다시하기 직후 이전 판의 총알이 보일 수 있다.
   - 근본 해결은 `ObjectPoolManager`에 전체 반환 API를 추가하는 것인데, `AGENTS.md` 2.1이 `Core/` 기존 파일의 수정을 금지하므로 **손대지 않았다.** 필요해지면 팀에 보고하고 승인을 받아야 한다.

2. **전환 중 카메라 공백** — 언로드를 먼저 하므로 한두 프레임 카메라가 없다. 로딩 페이드를 `SystemScene`에 넣으면 가려진다. 지금은 넣지 않았다.

3. **일시정지 입력이 UI/Cancel과 공유된다** — 전용 액션이 없어 `InputSystem_Actions`의 `UI/Cancel`을 재사용한다. EventSystem이 같은 입력을 소비해 이중 반응이 생기면, `InputSystem_Actions`에 `Player/Pause` 액션을 추가하고 `PauseInputRelay`의 참조만 바꾸면 된다. `.inputactions` 수정은 생성 코드 재생성이 필요하므로 사용자가 직접 한다.

4. **남은 체력 초기값** — `StageRunner`는 `PlayerHealthChanged`의 마지막 값을 기억한다. 같은 씬 로드 안에서 `PlayerHealth.OnEnable`과 `StageRunner.OnEnable`의 실행 순서는 보장되지 않으므로, 최초 통지를 놓치면 첫 피해나 패시브 드레인이 발생할 때까지 0으로 남는다. 현재 `PlayerStatsSO`에 패시브 드레인이 있어 실질적인 문제는 없다.

5. **`SampleScene`, `MapTest`, `Player`** — 흐름에 포함되지 않은 기존 테스트 씬이다. 정리 여부는 사용자가 판단한다.

6. **`Assets/01.Scripts/Test.cs`** — `01.Scripts/` 루트에 있고 네임스페이스가 없다. `AGENTS.md` 2.1 위반이지만 이번 작업 범위 밖이라 손대지 않았다.

---

## 10. 검증 방법

1. Build Settings를 8.1대로 구성하고 `Bootstrap` 씬에서 Play
2. 타이틀이 뜨고 Hierarchy에 `SystemScene` + `Title`만 남아 있는지 확인 (`Bootstrap`이 사라져야 한다)
3. 시작 → 스테이지 목록이 카탈로그 개수만큼 생성되는지 확인
4. 스테이지 선택 → `Stage_XX` + `UI_Scene`이 함께 올라오고 `StageSelect`가 사라지는지, 맵이 흐르기 시작하는지 확인
5. ESC → 일시정지 패널이 뜨고 맵이 멈추는지, 다시 ESC로 재개되는지 확인
6. 죽을 때까지 방치 → 결과 화면에 STAGE FAILED와 시간/거리/체력이 표시되는지 확인
7. Clear Distance를 작게(예: 50) 설정하고 다시 플레이 → STAGE CLEAR가 뜨는지 확인
8. 다시하기 → `Stage_XX`만 다시 로드되고 `UI_Scene`은 유지되는지 Hierarchy에서 확인
9. 스테이지 선택으로 → `Stage_XX`와 `UI_Scene`이 모두 사라지는지 확인
10. 전 과정에서 콘솔에 에러가 없는지 확인
