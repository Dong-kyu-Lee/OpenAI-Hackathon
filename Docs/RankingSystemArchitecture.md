# 랭킹 시스템 아키텍처

이 문서는 무한 모드의 주행 거리 랭킹 시스템 구조와 확장 지점을 설명한다. 현재 구현은 Windows 및 Web 빌드에서 사용할 수 있는 로컬 랭킹을 제공하며, UI와 저장소 사이에 인터페이스 경계를 두어 추후 외부 SDK 기반 랭킹으로 교체할 수 있도록 구성되어 있다.

프로젝트 전체 규칙과 씬 흐름은 각각 `AGENTS.md`, `Docs/GameFlowArchitecture.md`를 우선한다.

---

## 1. 구현 범위

현재 시스템이 제공하는 기능은 다음과 같다.

- 무한 모드 종료 시 도달 거리를 랭킹 등록 대상으로 사용한다.
- 결과 화면 위에 이름 입력 팝업을 표시한다.
- 이름을 비워 두면 `GUEST_001` 형식의 이름을 자동 생성한다.
- 거리가 높은 순으로 정렬하고, 같은 거리에서는 먼저 등록한 기록을 앞에 둔다.
- 최대 보관 개수, 이름 길이, 보드 ID, 게스트 접두사를 데이터 에셋에서 설정한다.
- 타이틀 화면의 랭킹 버튼을 통해 저장된 기록을 조회한다.
- 현재 저장 구현은 `PlayerPrefs`에 JSON 문자열을 기록한다.
- 저장 구현은 `IRankingRepository` 뒤에 격리되어 있다.

현재 범위에 포함되지 않는 기능은 다음과 같다.

- 사용자 계정과 인증
- 여러 기기 사이의 기록 동기화
- 서버 권위 점수 검증 및 치팅 방지
- 페이지네이션, 시즌, 친구 랭킹, 플레이어 주변 순위
- 로컬 기록을 외부 서비스로 자동 이전하는 마이그레이션

---

## 2. 설계 목표

### 2.1 도메인 간 직접 참조 방지

`Game.UI`와 `Game.App`은 서로를 직접 참조하지 않는다. 등록과 조회 요청 및 결과는 `Game.Core`에 정의된 ScriptableObject 이벤트 채널을 통해 전달한다.

```text
Game.UI ── 이벤트 요청 ──> Game.Core.Events <── 구독 ── Game.App
Game.UI <── 이벤트 결과 ── Game.Core.Events <── 발신 ── Game.App
```

UI는 로컬 저장인지 외부 SDK 저장인지 알지 못한다. 저장소 구현이 교체되어도 UI의 입력, 로딩, 성공 및 실패 처리 흐름은 유지된다.

### 2.2 저장소 역전

저장 기능의 계약은 `Game.Core.Ranking.IRankingRepository`가 소유한다.

```text
RankingService
    └─ IRankingRepository
         ├─ LocalRankingRepository       # 현재 사용
         └─ ExternalRankingRepository    # 추후 SDK 어댑터
```

Unity Inspector는 인터페이스 필드를 직접 직렬화할 수 없기 때문에 `RankingService`는 `_repositoryComponent`를 `MonoBehaviour`로 직렬화하고, `Awake`에서 `IRankingRepository`로 변환한다. 연결된 컴포넌트가 인터페이스를 구현하지 않으면 서비스가 오류를 보고하고 요청을 실패 처리한다.

### 2.3 비동기 계약

로컬 저장은 즉시 완료되지만 저장소 계약은 처음부터 `Task`와 `CancellationToken`을 사용한다. 네트워크 SDK로 교체할 때 UI와 서비스 계층을 동기 API에서 비동기 API로 다시 설계하지 않기 위한 결정이다.

---

## 3. 어셈블리와 책임

### Game.Core

다른 도메인이 공유하는 저장소 계약, 값 타입, 이벤트 채널을 소유한다. Unity SDK나 특정 외부 랭킹 SDK에 의존하지 않는다.

| 구성 요소 | 책임 |
|---|---|
| `IRankingRepository` | 랭킹 조회와 등록의 비동기 저장소 계약 |
| `RankingEntry` | 플레이어 이름, 거리, 등록 순서로 구성된 표시용 기록 |
| `RankingSubmissionRequest` | UI가 서비스에 전달하는 이름과 원본 거리 |
| `RankingSubmissionResult` | 등록 성공 여부, 오류, 등록 항목, 최신 목록 |
| `RankingSnapshot` | 조회 성공 여부와 정렬된 랭킹 목록 |
| `RankingSubmissionRequestEventChannelSO` | UI에서 App으로 등록 요청 전달 |
| `RankingSubmissionResultEventChannelSO` | App에서 UI로 등록 결과 전달 |
| `RankingSnapshotEventChannelSO` | App에서 UI로 조회 결과 전달 |

랭킹 새로고침 요청은 페이로드가 필요하지 않으므로 공용 `VoidEventChannelSO`를 재사용한다.

### Game.Data

`RankingSettingsSO`가 코드 수정 없이 조정할 수 있는 정책 값을 제공한다.

| 설정 | 현재 값 | 의미 |
|---|---:|---|
| `BoardId` | `endless` | 저장 키 또는 외부 서비스의 보드 식별자 |
| `MaxEntries` | `100` | 조회 및 로컬 보관 최대 개수 |
| `MaxNameLength` | `12` | 정규화 후 허용되는 최대 이름 길이 |
| `GuestPrefix` | `GUEST` | 빈 이름에 사용할 접두사 |

`MaxNameLength`의 최솟값은 `GUEST_001`과 같은 기본 이름을 담을 수 있도록 9로 제한된다.

### Game.App

| 구성 요소 | 책임 |
|---|---|
| `RankingService` | 요청 구독, 입력 정규화, 무한 모드 검증, 저장소 호출, 결과 발신 |
| `LocalRankingRepository` | PlayerPrefs JSON 읽기·쓰기, 게스트 이름 발급, 정렬, 개수 제한 |

`RankingService`는 저장 형식과 외부 SDK API를 알지 않는다. 반대로 저장소는 UI 팝업이나 씬 전환을 알지 않는다.

### Game.UI

| 구성 요소 | 책임 |
|---|---|
| `RankingSubmissionPopupUI` | 무한 모드 결과 감지, 이름 입력, 거리 표시, 등록 요청과 결과 처리 |
| `RankingBoardPanelUI` | 조회 요청, 빈 목록과 오류 표시, 랭킹 행 생성 |
| `RankingEntryUI` | 순위, 이름, 거리 한 줄 표시 |
| `TitleScreenUI` | 랭킹 버튼과 패널의 열기·닫기 조정 |

---

## 4. 씬과 프리팹 구성

### SystemScene

`RankingSystem.prefab` 인스턴스가 존재한다.

```text
RankingSystem
├─ LocalRankingRepository
└─ RankingService
```

`SystemScene`은 게임 흐름 동안 유지되므로 `RankingService`도 타이틀, 스테이지 선택, 플레이 사이에서 유지된다. 서비스는 활성화될 때 이벤트를 구독하고 비활성화될 때 해제한다. 파괴될 때 진행 중인 저장소 작업의 CancellationToken을 취소한다.

### UI_Scene

`RankingSubmissionPopup.prefab` 인스턴스가 게임플레이 Canvas의 마지막 형제로 배치된다. 전체 화면 입력 차단 배경을 포함하며 기본 상태는 비활성이다.

팝업 컴포넌트 자체는 활성 오브젝트에 남고 `_popupRoot`만 켜고 끈다. 컴포넌트를 붙인 오브젝트 전체를 비활성화하면 이벤트 구독도 해제되어 스테이지 종료 이벤트를 받을 수 없으므로 이 구조를 유지해야 한다.

### Title

- `RankingButton.prefab`: 랭킹보드를 연다.
- `RankingBoardPanel.prefab`: 스크롤 가능한 랭킹 목록을 표시한다.
- `RankingEntry.prefab`: 목록의 한 행으로 런타임에 생성된다.

랭킹 행은 조회된 기록 수만큼 생성한 뒤 패널이 다시 열릴 때 재사용한다. 현재 최대 100개이고 타이틀에서만 생성되므로 전역 오브젝트 풀 대상은 아니다.

---

## 5. 이벤트 채널

| 에셋 | 페이로드 | 발신 | 수신 |
|---|---|---|---|
| `RankingRefreshRequested` | 없음 | `RankingBoardPanelUI` | `RankingService` |
| `RankingSnapshot` | `RankingSnapshot` | `RankingService` | `RankingBoardPanelUI` |
| `RankingSubmissionRequested` | `RankingSubmissionRequest` | `RankingSubmissionPopupUI` | `RankingService` |
| `RankingSubmissionResult` | `RankingSubmissionResult` | `RankingService` | `RankingSubmissionPopupUI` |
| `StageFinished` | `StageResult` | 스테이지 진행 시스템 | `GameManager`, 결과 UI, `RankingSubmissionPopupUI` |

모든 구독은 `OnEnable`, 해제는 `OnDisable`에서 수행한다.

---

## 6. 등록 흐름

```text
무한 스테이지 종료
  └─ StageFinished(StageResult)
       ├─ GameManager: Result 상태로 전환
       └─ RankingSubmissionPopupUI
            ├─ 현재 선택 스테이지가 무한 모드인지 확인
            ├─ 거리 표시 및 입력 필드 초기화
            └─ 팝업 표시

사용자가 확인 선택
  └─ RankingSubmissionRequested(name, distance)
       └─ RankingService
            ├─ 저장소와 설정 연결 확인
            ├─ 현재 스테이지의 무한 모드 여부 재검증
            ├─ 거리와 이름 정규화
            └─ IRankingRepository.SubmitAsync(...)
                 └─ RankingSubmissionResult
                      ├─ 성공: 팝업 닫기
                      └─ 실패: 확인 버튼 복원 및 오류 표시
```

유한 스테이지에서 `StageFinished`가 발생하면 팝업은 열리지 않는다. UI와 서비스 양쪽에서 무한 모드 여부를 확인하므로 잘못된 UI 상태만으로 유한 스테이지 기록이 저장되지 않는다.

### 입력 정규화

- `NaN`, 무한대, 음수 거리는 거부한다.
- 소수 거리 값은 내림하여 정수 점수로 저장한다.
- `int.MaxValue`보다 큰 값은 `int.MaxValue`로 제한한다.
- 이름에서 제어 문자를 제거하고 앞뒤 공백을 없앤다.
- 이름이 최대 길이를 넘으면 잘라낸다.
- 정규화한 이름이 비어 있으면 저장소가 게스트 이름을 생성한다.

등록 중 확인 버튼은 비활성화된다. 중복 등록 요청이 도착하면 서비스는 실패 결과를 반환한다.

---

## 7. 조회 흐름

```text
타이틀에서 Ranking 버튼 선택
  └─ RankingBoardPanelUI.Open()
       ├─ 패널 표시
       └─ RankingRefreshRequested
            └─ RankingService
                 └─ IRankingRepository.GetEntriesAsync(...)
                      └─ RankingSnapshot
                           ├─ 성공 + 기록 있음: 행 바인딩
                           ├─ 성공 + 빈 목록: NO RECORDS 표시
                           └─ 실패: 오류 표시
```

동시에 여러 새로고침이 요청되면 진행 중인 조회가 끝날 때까지 추가 요청은 무시한다.

---

## 8. 로컬 저장 형식

### 저장 키

```text
Ranking.Local.v1.<BoardId>
```

현재 설정에서는 다음 키를 사용한다.

```text
Ranking.Local.v1.endless
```

손상된 데이터가 발견되면 원본 문자열을 다음 키에 복사하고 메모리에서는 빈 랭킹으로 복구한다.

```text
Ranking.Local.v1.<BoardId>.CorruptBackup
```

### JSON 스키마

```json
{
  "version": 1,
  "nextGuestNumber": 2,
  "nextSubmissionOrder": 4,
  "entries": [
    {
      "playerName": "GUEST_001",
      "distance": 321,
      "submissionOrder": 3
    }
  ]
}
```

| 필드 | 의미 |
|---|---|
| `version` | 로컬 JSON 스키마 버전 |
| `nextGuestNumber` | 다음 자동 게스트 번호 |
| `nextSubmissionOrder` | 동점 정렬에 사용하는 단조 증가 순서 |
| `entries` | 저장된 기록 배열 |

게스트 번호는 001부터 999까지 증가한 뒤 다시 001로 돌아온다. 게스트 이름은 표시 편의를 위한 값이며 고유 식별자가 아니다.

### 정렬과 보관 정책

1. 이름이 비었거나 거리가 음수인 잘못된 항목을 제거한다.
2. 거리를 기준으로 내림차순 정렬한다.
3. 거리가 같으면 `submissionOrder`가 작은 기록을 먼저 표시한다.
4. 정렬 후 `MaxEntries`를 초과한 하위 기록을 제거한다.

---

## 9. Windows와 Web 빌드

저장소는 플랫폼별 파일 경로를 직접 다루지 않고 Unity의 `PlayerPrefs` API만 사용한다.

- Windows Standalone에서는 Unity가 PlayerPrefs를 사용자별 로컬 저장소에 유지한다.
- Web 빌드에서는 브라우저의 IndexedDB를 사용하며, 브라우저 데이터 삭제·비공개 모드·저장 정책의 영향을 받는다.
- Unity 공식 문서상 Web PlayerPrefs 저장 한도는 1MB이다. 현재 최대 100개의 간단한 JSON 기록은 이 범위보다 충분히 작지만, 기록 필드나 보드 수를 크게 늘릴 때는 다시 계산해야 한다.
- 등록 직후 `PlayerPrefs.Save()`를 호출하므로 정상 종료를 기다리지 않고 변경 내용을 저장하도록 요청한다.

플랫폼 저장 위치와 제한은 [Unity PlayerPrefs 공식 문서](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/PlayerPrefs.html)를 기준으로 한다.

`PlayerPrefs`는 암호화되거나 위변조가 방지된 저장소가 아니다. 따라서 로컬 랭킹은 편의 기능으로만 취급해야 하며 경쟁성이 있는 글로벌 랭킹의 신뢰 근거로 사용하면 안 된다.

---

## 10. 외부 SDK 확장성

### 10.1 현재 구조에서 교체 가능한 범위

외부 SDK가 다음 형태의 기능을 제공한다면 기존 UI와 `RankingService`를 변경하지 않고 어댑터만 추가할 수 있다.

- 보드 ID로 상위 N개 기록 조회
- 플레이어 표시 이름과 정수 점수 등록
- 비동기 완료 및 실패 결과 제공
- 취소 또는 객체 파괴 이후의 콜백 무시 가능

어댑터는 `IRankingRepository`를 구현하는 `MonoBehaviour`로 작성한다.

```csharp
public sealed class ExternalRankingRepository : MonoBehaviour, IRankingRepository
{
    public Task<RankingSnapshot> GetEntriesAsync(
        string boardId,
        int maxEntries,
        CancellationToken cancellationToken)
    {
        // SDK 조회 결과를 RankingEntry와 RankingSnapshot으로 변환한다.
    }

    public Task<RankingSubmissionResult> SubmitAsync(
        string boardId,
        string playerName,
        int distance,
        string guestPrefix,
        int maxEntries,
        CancellationToken cancellationToken)
    {
        // SDK 등록 결과를 RankingSubmissionResult로 변환한다.
    }
}
```

교체 절차는 다음과 같다.

1. 외부 SDK 초기화와 인증 수명주기를 결정한다.
2. SDK 전용 저장소 어댑터에서 `IRankingRepository`를 구현한다.
3. SDK DTO를 Core의 `RankingEntry`, `RankingSnapshot`, `RankingSubmissionResult`로 변환한다.
4. `RankingSystem.prefab`에 어댑터 컴포넌트를 배치한다.
5. `RankingService._repositoryComponent` 참조를 `LocalRankingRepository`에서 새 어댑터로 교체한다.
6. 등록, 조회, 타임아웃, 인증 실패, 오프라인, 중복 요청을 검증한다.

UI 프리팹, 이벤트 채널, 타이틀 씬, 결과 팝업은 그대로 유지할 수 있다.

### 10.2 SDK 의존성 격리 권장안

SDK 패키지를 도입할 때 `Game.Core`나 `Game.UI`가 SDK 어셈블리를 참조하게 만들면 안 된다. 가능하면 SDK 어댑터를 별도 어셈블리에 격리한다.

```text
Game.Core
   ↑
Game.App.RankingProvider.<SDK> ──> External SDK

Game.App.RankingService ──> Game.Core.IRankingRepository
```

이 구조에서는 `RankingService`가 외부 SDK 어셈블리를 직접 참조하지 않는다. 프리팹에 연결된 `MonoBehaviour`를 Core 인터페이스로만 다룬다.

프로젝트 규칙상 패키지 설치와 asmdef 참조 변경은 자동으로 진행하지 않는다. 실제 SDK 선정 시 다음 항목을 먼저 팀에 보고하고 승인받아야 한다.

- 추가할 패키지 ID와 버전
- 지원 플랫폼과 Web 빌드 지원 여부
- 필요한 초기화 오브젝트와 씬 수명
- 새 어셈블리와 참조 방향
- 인증 및 개인정보 처리 방식

### 10.3 계약 변경이 필요한 외부 기능

현재 인터페이스는 단순한 상위 기록 조회와 점수 등록에 맞춰져 있다. 다음 기능이 필요하면 SDK 어댑터 안에서 억지로 숨기지 말고 Core 계약을 별도 버전으로 확장해야 한다.

| 요구 기능 | 현재 부족한 정보 |
|---|---|
| 사용자 계정 | 플레이어 ID, 인증 상태, 표시 이름과 계정의 구분 |
| 페이지네이션 | 페이지 토큰 또는 범위 요청 |
| 내 주변 순위 | 현재 플레이어 식별자와 기준 순위 |
| 시즌 랭킹 | 시즌 ID와 기간 |
| 친구 랭킹 | 관계 범위 또는 필터 |
| 기록 갱신 방식 | 최고 기록만 유지할지 모든 시도를 보관할지에 대한 정책 |
| 서버 권위 검증 | 서버가 검증할 플레이 세션 또는 서명된 결과 데이터 |

이 경우에도 UI에 SDK DTO를 직접 노출하지 않는다. Core에 공급자 독립적인 요청·응답 타입을 추가하고, 어댑터가 SDK 타입과 변환하도록 한다.

### 10.4 동작 차이를 결정해야 하는 항목

외부 서비스로 교체하기 전에 다음 정책을 명시해야 한다.

- 같은 플레이어가 여러 기록을 가질 수 있는가, 최고 기록 하나만 유지하는가
- 동점일 때 먼저 등록한 기록이 우선인지 SDK 자체 규칙을 따를지
- 빈 이름의 게스트 번호를 클라이언트가 만들지 서버가 만들지
- 등록 성공 후 전체 목록을 다시 조회할지 SDK 응답으로 즉시 갱신할지
- 오프라인 등록을 실패 처리할지 로컬 큐에 보관할지
- 로컬 랭킹과 온라인 랭킹을 합칠지 별도 보드로 보여줄지

글로벌 랭킹에서는 이름 정규화, 금칙어 처리, 점수 범위 검증을 서버에서도 다시 수행해야 한다. 클라이언트 검증만으로 기록을 신뢰해서는 안 된다.

### 10.5 로컬 기록 마이그레이션

현재는 외부 SDK 도입 시 로컬 데이터를 자동 업로드하지 않는다. 자동 이전이 필요하다면 다음 위험을 먼저 해결해야 한다.

- PlayerPrefs는 사용자가 수정할 수 있으므로 기존 점수를 신뢰할 수 없다.
- 로컬 기록에는 외부 서비스의 플레이어 ID가 없다.
- 여러 기기에서 같은 게스트 이름이 중복될 수 있다.
- 재시도 시 같은 기록이 중복 업로드될 수 있다.

경쟁성이 있는 온라인 보드라면 기존 로컬 기록은 이전하지 않고, SDK 전환 이후 새 기록만 받는 방식이 기본 권장안이다. 이전이 꼭 필요하면 별도 비경쟁 보드, 일회성 마이그레이션 버전 키, 멱등성 ID를 설계해야 한다.

---

## 11. 실패 처리

- 연결된 설정이나 저장소가 없으면 서비스가 실패 결과를 발신한다.
- 저장소가 실패 `RankingSnapshot` 또는 `RankingSubmissionResult`를 반환하면 UI가 오류 문구를 표시한다.
- 저장소 호출에서 예외가 발생하면 서비스가 로그를 남기고 사용자용 실패 결과로 변환한다.
- 서비스가 파괴되면 CancellationToken을 취소한다.
- 로컬 JSON이 손상되었거나 지원하지 않는 버전이면 원문을 백업 키에 보관하고 빈 목록으로 복구한다.
- 팝업 등록 실패 시 입력 내용은 유지하고 확인 버튼을 다시 활성화한다.

외부 SDK 어댑터는 SDK 예외나 오류 코드를 UI에 직접 전달하지 말고, 로그용 상세 정보와 사용자용 메시지를 구분해야 한다.

---

## 12. 검증 체크리스트

### 공통

- [ ] 유한 스테이지 종료 시 등록 팝업이 열리지 않는다.
- [ ] 무한 스테이지 종료 시 거리와 빈 이름 입력란이 표시된다.
- [ ] 빈 이름 등록 시 `GUEST_XXX`가 생성된다.
- [ ] 최대 길이를 넘는 이름과 제어 문자가 정규화된다.
- [ ] 거리가 내림차순으로 정렬된다.
- [ ] 동점 기록이 등록 순서대로 정렬된다.
- [ ] 최대 보관 개수를 넘으면 하위 기록이 제거된다.
- [ ] 타이틀 재진입 후 기록이 다시 표시된다.
- [ ] 등록 실패 시 팝업이 닫히지 않고 재시도할 수 있다.

### Windows

- [ ] 게임 재시작 후 기록이 유지된다.
- [ ] 다른 Windows 사용자 계정의 저장 데이터와 분리된다.
- [ ] 저장 데이터가 손상되었을 때 빈 목록으로 복구된다.

### Web

- [ ] 같은 사이트에서 새로고침 후 기록이 유지된다.
- [ ] 브라우저 저장소를 삭제하면 기록도 초기화되는 동작을 안내한다.
- [ ] 비공개 모드와 저장소 차단 환경에서 실패 처리를 확인한다.
- [ ] 배포 URL 또는 사이트 저장 영역이 바뀌었을 때 데이터가 분리될 수 있음을 확인한다.

### 외부 SDK 도입 시 추가

- [ ] SDK 초기화 전 요청의 처리 방식이 정의되어 있다.
- [ ] 인증 성공·실패·만료 흐름을 검증한다.
- [ ] 오프라인, 타임아웃, 재시도, 중복 등록을 검증한다.
- [ ] 씬 전환이나 종료 후 늦게 도착한 콜백이 UI를 갱신하지 않는다.
- [ ] SDK의 동점 및 최고 점수 정책이 게임 정책과 일치한다.
- [ ] Web 빌드에서 SDK와 브라우저 보안 정책을 검증한다.
- [ ] 클라이언트 입력을 서버에서 다시 검증한다.

---

## 13. 관련 파일

```text
Assets/01.Scripts/Core/Ranking/
Assets/01.Scripts/Core/Events/Ranking*EventChannelSO.cs
Assets/01.Scripts/Data/Ranking/RankingSettingsSO.cs
Assets/01.Scripts/App/Ranking/
Assets/01.Scripts/UI/Ranking/
Assets/01.Scripts/UI/Title/TitleScreenUI.cs

Assets/02.Prefabs/App/Ranking/RankingSystem.prefab
Assets/02.Prefabs/UI/Ranking/

Assets/06.SO/Ranking/RankingSettings.asset
Assets/06.SO/Events/Ranking*.asset

Assets/00.Scenes/SystemScene.unity
Assets/00.Scenes/Title.unity
Assets/00.Scenes/UI_Scene.unity
```
