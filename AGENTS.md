# AGENTS.md — AI 에이전트 작업 규칙

이 문서는 이 프로젝트에서 작업하는 **AI 에이전트(Claude Code, Codex 등)를 위한 지시문**이다.
작업을 시작하기 전에 전문을 읽고, 아래 규칙을 지켜서 작업한다.
규칙과 사용자의 지시가 충돌하면, 진행하기 전에 그 사실을 알리고 확인을 받는다.

---

## 0. 프로젝트 개요

- **장르**: 2D 사이드뷰 Run & Gun (자동 전진 + 사격 + 장애물 회피)
- **엔진**: Unity 6000.3.7f1, URP 2D (Renderer2D), Input System 1.18
- **규모**: 2주 해커톤 / 3인 팀 / 각자 AI 에이전트를 병렬로 사용
- **최우선 원칙**: **완성 > 구조 > 기능 수**

"빠르게"를 이유로 3장(아키텍처 규칙)을 어기지 않는다. 3인이 병렬로 AI를 돌리는 환경에서 구조가 무너지면 며칠 안에 팀 전체가 멈춘다. 속도와 구조가 충돌하면 **기능을 줄이지 구조를 줄이지 않는다.**

---

## 1. 절대 금지 사항 (Hard Rules)

예외 없음. 필요하면 **작업을 멈추고 사용자에게 요청**한다.

| # | 금지 | 이유 |
|---|---|---|
| 1 | `.unity`, `.prefab`, `.asset`, `.controller`, `.anim` 파일을 **텍스트 도구(Read/Write/Edit)로** 생성/편집 | YAML + fileID/GUID 구조가 손상되면 씬 복구가 불가능하다. 이 파일들은 **2.3의 에디터 경유 조작으로만** 다룬다 |
| 2 | `.meta` 파일 생성/수정/삭제 | GUID가 깨지면 프로젝트 전체의 참조가 끊긴다. Unity가 자동 생성한다 |
| 3 | `Library/`, `Temp/`, `obj/`, `Logs/`, `*.csproj`, `*.slnx` 수정 | 전부 자동 생성물이다 |
| 4 | `Packages/manifest.json` 수정, 패키지·에셋스토어·외부 라이브러리 추가 | 팀 전원 재임포트와 충돌을 유발한다. 필요하면 제안만 하고 승인을 받는다 |
| 5 | `ProjectSettings/*` 수정 (Tags, Layers, Physics2D, Input, Quality 포함) | 필요하면 "무엇을 어떻게 바꿔야 하는지"만 보고하고 직접 수정하지 않는다 |
| 6 | 요청받지 않은 파일의 리팩터링·포맷팅·정리·주석 추가 | diff가 커지면 리뷰가 불가능해지고 머지 충돌이 발생한다 |
| 7 | 기존 public API(메서드 시그니처, `[SerializeField]` 필드명) 변경 | 인스펙터 연결이 조용히 끊긴다. 변경이 필요하면 먼저 보고한다 |
| 8 | `git commit`, `git push`, `git reset`, 브랜치 삭제를 임의로 실행 | 사용자가 diff를 확인한 뒤 직접 한다. 명시적 요청이 있을 때만 실행한다 |

---

## 2. 폴더 구조 및 파일 배치

### 2.1 구조

새 파일은 반드시 아래 위치에 만든다. 애매하면 만들기 전에 묻는다.

```
Assets/
├─ 00.Scenes/          # 씬 (2.2 참조)
├─ 01.Scripts/
│   ├─ Core/           # 이벤트 채널, 인터페이스, 상수, 유틸  ← 의존성 없음
│   ├─ Data/           # ScriptableObject 정의 (수치/밸런스)
│   ├─ Gameplay/
│   │   ├─ Player/     # 이동, 사격, 피격
│   │   ├─ Enemy/      # 적 AI, 스포너
│   │   ├─ Weapon/     # 총, 투사체, 풀링
│   │   └─ Stage/      # 스크롤, 장애물, 구간 생성
│   ├─ UI/             # 화면 고정 UI (HUD, 메뉴, 결과 화면)
│   └─ App/            # 부트스트랩, 씬 로딩, 게임 상태 머신
├─ 02.Prefabs/         # 하위 폴더는 01.Scripts 도메인 구조와 동일하게
├─ 03.Sprites/
├─ 04.Animations/
├─ 05.Audio/
├─ 06.SO/              # ScriptableObject 에셋 인스턴스
├─ 07.UI/              # 폰트, UI 스프라이트
└─ 99.Sandbox/         # 개인 실험 영역
```

- `Core/`는 팀 공용이다. **추가는 가능하지만 기존 파일의 수정·삭제는 하지 않는다.** 필요하면 보고한다.
- `99.Sandbox/`는 개인 실험 전용이다. 컨벤션이 적용되지 않는다. **본 게임 코드가 `99.Sandbox/`를 참조하는 코드는 절대 작성하지 않는다.**

### 2.2 씬 구조 (Additive 분리)

씬은 여러 개로 쪼개서 Additive로 겹쳐 로드한다. 씬 파일은 머지가 불가능하므로, 파일을 나누는 것이 유일한 충돌 방지책이다.

```
00.Scenes/
├─ Bootstrap.unity      # 시작 씬 (Build index 0). 로더 하나만 존재
├─ System_Scene.unity   # GameManager, AudioManager, ObjectPoolManager. 게임 내내 유지
├─ UI_Scene.unity       # 화면 고정 UI. 게임 내내 유지
└─ Stage_XX.unity       # 스테이지. 교체 시 언로드/로드
```

AI가 지킬 규칙:

1. **씬 파일을 텍스트로 만들거나 편집하지 않는다.** 씬 구성이 필요하면 2.3의 에디터 경유 조작을 쓴다. 명시적 요청이 없거나 에디터를 쓸 수 없으면 "어떤 씬에 무엇을 배치해야 하는지" 지시만 출력한다 (6.3 참조).
2. **게임 오브젝트를 씬에 직접 조립하는 것을 전제로 코드를 짜지 않는다.** 모든 게임 오브젝트는 프리팹으로 만들고 씬에는 배치만 한다고 가정한다.
3. **씬 간 인스펙터 참조를 전제로 하는 코드를 작성하지 않는다.** Unity가 막아둔 기능이다. 다른 씬의 오브젝트와 통신해야 하면 3.2의 이벤트 채널을 쓴다.
4. 씬 로드/언로드 코드는 `App/`에만 작성한다. `Gameplay/`나 `UI/`에서 `SceneManager`를 직접 호출하지 않는다.

### 2.3 에디터 조작 (Unity MCP / Unity CLI)

씬과 에셋은 **실행 중인 Unity 에디터를 경유해서 편집할 수 있다.** 직렬화를 Unity가 담당하므로 GUID와 fileID가 깨지지 않는다. 1장 1번이 금지하는 것은 텍스트 도구로 YAML을 직접 건드리는 것이지, 에디터 조작이 아니다.

**허용 경로 (이 둘만)**

| 경로 | 용도 |
|---|---|
| **Unity MCP** (`unity-mcp` 서버) | 우선 사용. 에디터에서 C# 실행, 콘솔 로그 확인, 씬 뷰/카메라 캡처로 결과 검증 |
| **Unity CLI** (`unity-cli` 스킬) | MCP를 쓸 수 없을 때의 대체 경로 |

`.meta` 파일 직접 생성/수정은 이 경로에서도 여전히 금지다 (1장 2번). 에디터가 알아서 만든다.

**발동 조건 — 명시적 요청이 있을 때만 조작한다.**

- "프리팹 만들어줘", "씬에 배치해줘", "인스펙터에 연결해줘"처럼 **사용자가 에디터 조작을 직접 지시한 경우에만** 실행한다.
- 코드 작성을 요청받은 상황에서 "이왕 하는 김에" 씬까지 조립하지 않는다. 코드만 쓰고 나머지는 6.3의 "에디터에서 필요한 작업"으로 남긴다.
- 요청이 애매하면 조작하지 말고 묻는다. 씬/프리팹 변경은 diff로 리뷰가 불가능해서, 잘못 건드리면 사용자가 알아채기 어렵다.

**조작할 때 지킬 것**

1. 실행 전에 무엇을 바꿀지 한두 줄로 먼저 말한다.
2. 요청받은 오브젝트만 건드린다. 같은 씬의 다른 오브젝트는 문제가 보여도 그대로 두고 보고만 한다 — 다른 팀원이 동시에 작업 중일 수 있다 (6.2와 동일).
3. 씬 저장 전에 무엇이 바뀌었는지 보고한다. 저장하면 되돌리기 어렵다.
4. 1장의 나머지 금지 사항은 에디터를 경유해도 그대로 적용된다. 특히 `ProjectSettings/*`(Tag, Layer, Physics2D 등)와 패키지 설치는 **에디터로도 하지 않는다.** 보고만 하고 사용자가 직접 한다.
5. 조작 후 콘솔에 에러가 없는지 확인하고, 결과를 6.3 형식으로 보고한다.

---

## 3. 아키텍처 규칙 (가장 중요)

### 3.1 의존성 방향 — Assembly Definition으로 강제

각 폴더에 `.asmdef`를 두고 참조 방향을 아래로만 허용한다.

| 어셈블리 | 참조 가능 대상 |
|---|---|
| `Game.App` | `Core`, `Data`, `Gameplay`, `UI` |
| `Game.UI` | `Core`, `Data` |
| `Game.Gameplay` | `Core`, `Data` |
| `Game.Data` | `Core` |
| `Game.Core` | 없음 |

**핵심 결과: `Gameplay`는 `Game.UI`를 참조할 수 없고, `Game.UI`는 `Gameplay`를 참조할 수 없다.**

- asmdef의 참조 목록에 새 항목을 추가해야 하는 상황이 생기면, **추가하지 말고 설계가 잘못된 것을 의심한다.** 먼저 보고하고 이벤트 채널로 우회할 수 있는지 검토한다.
- 순환 참조가 필요해 보이면 그 시점에 작업을 멈추고 보고한다.

### 3.2 도메인 간 통신 — ScriptableObject 이벤트 채널

`Core`에 채널을 정의하고 양쪽이 그것만 참조한다. SO는 씬에 속하지 않는 에셋이므로 어느 씬에서든 같은 인스턴스를 참조할 수 있다.

```csharp
// Core/Events/IntEventChannelSO.cs
[CreateAssetMenu(menuName = "Events/Int Event Channel")]
public class IntEventChannelSO : ScriptableObject
{
    public event Action<int> OnRaised;
    public void Raise(int value) => OnRaised?.Invoke(value);
}
```

- 발신: `_hpChangedChannel.Raise(currentHp);`
- 수신: `OnEnable`에서 구독, **`OnDisable`에서 반드시 해제.** 해제 누락은 에디터에서 다음 Play 때 에러로 나타난다.
- 채널로 **매 프레임 좌표를 흘려보내지 않는다.** 이벤트 채널은 상태 변화 알림용이다.

### 3.3 통신 방식 선택 기준

| 상황 | 사용 |
|---|---|
| 같은 GameObject 또는 자식 | 직접 참조 (`[SerializeField]`, `Awake`에서 `GetComponent` 캐싱) |
| 같은 도메인 내 다른 오브젝트 | 인터페이스 참조 (`IDamageable` 등) |
| 도메인 간 (Gameplay ↔ UI ↔ App) | 이벤트 채널만 |
| 전역 상태 | 3.4의 화이트리스트 싱글톤만 |

### 3.4 싱글톤 화이트리스트

**아래 3개만 허용한다. 그 외에 `Instance` 프로퍼티를 가진 클래스를 새로 만들지 않는다.**

- `GameManager` (게임 상태 머신)
- `AudioManager`
- `ObjectPoolManager`

전부 `App` 또는 `Core`에 두고 `System_Scene`에 1개씩만 존재한다고 가정한다. 새 싱글톤이 필요해 보이면 만들지 말고 보고한다.

### 3.5 금지 패턴

```csharp
// 작성 금지
FindObjectOfType<X>() / FindAnyObjectByType<X>()   // 은닉 의존성
GameObject.Find("Player")                           // 문자열 결합
Update() 안의 GetComponent / Instantiate / Destroy  // 성능 + GC
public 필드로 인스펙터 노출                           // → [SerializeField] private
숫자 리터럴 직접 사용 (5f, 100, 0.3f …)              // → ScriptableObject 또는 const
Resources.Load                                      // → SerializeField 참조
Input.GetKey 등 레거시 입력 API                       // → Input System (InputSystem_Actions)
async void (이벤트 핸들러 제외)
```

```csharp
// 대체
[SerializeField] private PlayerStatsSO _stats;
private Rigidbody2D _rb;
private void Awake() => _rb = GetComponent<Rigidbody2D>();
```

### 3.6 수치는 전부 ScriptableObject로

이동속도, 점프력, 연사속도, 데미지, 적 체력, 스크롤 속도 등 **밸런싱 대상 숫자를 코드에 하드코딩하지 않는다.**

- 정의는 `01.Scripts/Data/`, 에셋 인스턴스는 `Assets/06.SO/`
- 이렇게 하면 밸런싱에 코드 수정·재컴파일·머지 충돌이 발생하지 않는다.

### 3.7 총알·이펙트·적은 반드시 오브젝트 풀링

Run & Gun은 초당 수십 발이 발사된다. `Instantiate` / `Destroy`를 직접 호출하지 않고 `ObjectPoolManager`를 경유한다.

풀링 대상 컴포넌트는 상태 초기화를 `Awake`가 아니라 **`OnEnable`에서** 한다. 재사용 시 `Awake`는 다시 호출되지 않는다.

---

## 4. 코드 컨벤션

- 파일 1개 = public 클래스 1개, 파일명 = 클래스명
- 네이밍: 클래스/메서드 `PascalCase`, 지역변수/파라미터 `camelCase`, private 필드 `_camelCase`, 상수 `PascalCase`, 인터페이스 `IXxx`, ScriptableObject `XxxSO`
- 네임스페이스 필수, 폴더 경로와 일치시킨다: `Game.Gameplay.Player`
- 클래스 하나 = 책임 하나. `PlayerController` 하나가 이동 + 사격 + 체력 + 애니메이션을 모두 담당하게 만들지 않는다 (`PlayerMovement`, `PlayerShooter`, `PlayerHealth`로 분리)
- 물리 이동은 `FixedUpdate` + `Rigidbody2D`, 입력 감지는 `Update`
- 입력은 `Assets/InputSystem_Actions.inputactions`를 통해서만 처리한다

---

## 5. Git 관련 규칙

- **커밋과 푸시를 임의로 실행하지 않는다.** 명시적 요청이 있을 때만 한다.
- 커밋 메시지는 다음의 규칙을 따른다.
- `.meta` 파일은 짝이 되는 에셋과 **반드시 같이** 스테이징한다. meta 누락은 다른 팀원 환경에서 참조를 끊는다.
- 커밋 전 `git status`로 `Library/`, `Temp/`, `Logs/`가 포함되지 않았는지 확인한다.
- 충돌한 `.unity` / `.prefab` 파일을 **직접 머지하려 시도하지 않는다.** 어느 쪽을 버릴지 사용자에게 확인받는다.
- 명시적 요청이 없다면 `master` 브랜치에 직접 커밋은 금지한다.

---

## 6. 작업 프로토콜

### 6.1 작업 시작 전

1. 이 문서를 읽는다.
2. 관련 기존 코드를 검색해서 읽는다. **새 클래스를 만들기 전에 같은 역할을 하는 클래스가 이미 있는지 반드시 확인한다.** 중복 클래스 양산이 이 프로젝트가 망가지는 1순위 경로다.
3. 3~5줄로 계획을 제시한다.
   - 수정할 파일 / 새로 만들 파일
   - 어느 어셈블리(`Core`/`Data`/`Gameplay`/`UI`/`App`)에 속하는지
   - 사용자가 에디터에서 해야 할 작업이 무엇인지
4. 승인을 받은 뒤 코드를 작성한다.

### 6.2 작업 중

- **한 세션 = 한 기능.** 여러 기능을 한 번에 섞지 않는다.
- **요청 범위 밖의 파일은 문제가 보여도 수정하지 않는다.** 발견한 문제는 보고만 하고 넘어간다. 다른 팀원이 그 파일을 동시에 작업 중일 수 있다.
- 확신이 없으면 추측해서 만들지 말고 질문한다.

### 6.3 작업 완료 시 반드시 출력할 것

```
## 변경 파일
- (신규) Assets/01.Scripts/Gameplay/Player/PlayerDash.cs
- (수정) Assets/01.Scripts/Gameplay/Player/PlayerMovement.cs

## 에디터에서 필요한 작업        ← 생략 금지
1. Player 프리팹에 PlayerDash 컴포넌트 추가
2. PlayerDash의 Stats 필드에 06.SO/PlayerStats.asset 연결
3. Tag "Dashing" 추가 필요 (ProjectSettings 변경 — 1장 5번 항목)

## 검증 방법
- Play 후 Shift 입력 시 0.2초간 무적 대시
```

2.3의 에디터 조작을 직접 수행했다면, "에디터에서 필요한 작업" 항목을 **"에디터에서 수행한 작업"** 으로 바꿔 쓴다. 어떤 씬/프리팹의 무엇을 바꿨는지, 저장했는지, 사용자가 직접 눈으로 확인해야 할 지점이 어디인지를 함께 적는다. 씬 변경은 diff에 드러나지 않으므로 이 보고가 유일한 기록이다.

---

## 7. 스코프 규칙

- **요청받은 것만 만든다.** 요청 범위를 넘는 기능을 스스로 추가하지 않는다.
- 좋은 아이디어가 떠오르면 구현하지 말고 **한 줄로 제안만** 한다. 채택 여부는 사용자가 정한다.
- 아래는 이 프로젝트의 스코프 밖이다. 요청받아도 먼저 재확인한다.
  - 멀티플레이, 네트워크
  - 세이브/로드, 온라인 랭킹
  - 절차적 레벨 생성
  - 커스텀 에디터 툴 (작업 시간 대비 효과가 확실할 때만)
- 프로토타입 단계에서 **범용 프레임워크를 만들지 않는다.** 지금 필요한 한 가지 케이스만 동작하게 만들고, 두 번째 케이스가 생겼을 때 일반화한다.
- 사용자가 "일단 빠르게"라고 하면 **기능 범위를 줄여서 대응하고, 3장의 아키텍처 규칙을 생략해서 대응하지 않는다.**
