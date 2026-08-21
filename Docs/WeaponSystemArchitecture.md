# 무기 시스템 아키텍처 및 구현 인수인계

이 문서는 `Gun.md` 명세를 바탕으로 구현한 플레이어 무기 시스템을 다른 Codex 세션이나 AI 에이전트가 빠르게 이해하고 이어서 작업할 수 있도록 정리한 인수인계 문서다.

프로젝트 전체 작업 규칙은 루트의 `AGENTS.md`가 최우선이다. 특히 `.unity`, `.prefab`, `.asset`, `.controller`, `.anim` 파일은 텍스트로 편집하지 않고 Unity Editor를 통해서만 변경한다.

## 1. 구현 범위

현재 코드에 포함된 범위는 다음과 같다.

- 마우스 화면 좌표 기반 조준
- 마우스 왼쪽 버튼 클릭/홀드 공격
- Q/W/E/R을 이용한 무기 직접 선택
- 무기 전환 시간 적용
- 무기마다 독립적으로 유지되는 쿨타임
- 머신건 형태의 점사 투사체 공격
- 로켓 형태의 단발/범위 피해 투사체 공격
- 아이스/파이어 형태의 지속 레이 공격
- 투사체 오브젝트 풀링
- 피격 지점 히트 이펙트(파티클)와 이펙트 오브젝트 풀링
- 일반 피해, 동결, 연소 인터페이스 호출
- Player 사망 시 공격 중단

다음 항목은 이 시스템에서 직접 구현하지 않았다.

- 적 체력의 구체 구현
- 물 지형을 얼음 발판으로 바꾸는 구체 구현
- 덩굴을 파괴하는 구체 구현
- 사운드
- UI에 현재 무기나 쿨타임을 표시하는 기능

스테이지 담당 코드는 아래 인터페이스만 구현하면 무기 시스템과 연결된다.

```csharp
IDamageable.TakeDamage(float amount)
IFreezable.Freeze()
IBurnable.ApplyBurn(float duration)
```

## 2. 어셈블리와 의존성

무기 시스템은 기존 asmdef 방향을 변경하지 않는다.

```text
Game.Gameplay ───────> Game.Core
       │
       └─────────────> Game.Data ─────> Game.Core
```

- `Game.Core`: 도메인 간 계약과 공용 오브젝트 풀
- `Game.Data`: 무기 수치 ScriptableObject
- `Game.Gameplay`: 입력 전달, 무기 전환, 발사, 투사체, 레이 판정
- asmdef 파일은 수정하지 않았다.
- Gameplay가 App을 참조하지 않도록 `ObjectPoolManager`는 Core에 배치했다.

## 3. 전체 실행 흐름

```text
InputSystem_Actions
        │
        ▼
PlayerInputReader
        │  공격 홀드 / 마우스 화면 좌표 / 무기 인덱스
        ▼
PlayerWeaponController
        │  현재 무기 선택, 전환 대기, 화면→월드 조준 변환
        ▼
WeaponBase
   ├─ ProjectileWeapon ──> ObjectPoolManager ──> PooledProjectile
   │                                               ├─ IDamageable
   │                                               ├─ PooledHitEffect (1회 재생)
   │                                               └─ 풀 반환
   └─ ContinuousRayWeapon
                                                   ├─ IDamageable
                                                   ├─ IFreezable
                                                   ├─ IBurnable
                                                   └─ PooledHitEffect (루프, 히트 지점 추적)
```

## 4. 생성한 코드

### 4.1 Core/Combat

| 파일 | 역할 |
|---|---|
| `Assets/01.Scripts/Core/Combat/IDamageable.cs` | `TakeDamage(float)` 피해 계약. 지속형 무기의 소수 DPS를 위해 `float` 사용 |
| `Assets/01.Scripts/Core/Combat/IFreezable.cs` | 아이스 레이에 맞은 오브젝트의 `Freeze()` 계약 |
| `Assets/01.Scripts/Core/Combat/IBurnable.cs` | 파이어 레이 접촉 시간을 전달하는 `ApplyBurn(float duration)` 계약 |

무기 코드는 충돌한 Collider의 부모 방향으로 인터페이스를 검색한다. 따라서 Collider와 실제 체력/지형 상태 컴포넌트가 서로 다른 계층에 있어도 부모에 인터페이스 구현체가 있으면 동작한다.

### 4.2 Core/Pooling

| 파일 | 역할 |
|---|---|
| `Assets/01.Scripts/Core/Pooling/IPoolable.cs` | 풀 반환 시 상태 정리 계약 |
| `Assets/01.Scripts/Core/Pooling/ObjectPoolManager.cs` | 투사체 생성, 재사용, 반환을 담당하는 허용된 전역 싱글톤 |

`ObjectPoolManager` 내부 자료구조는 다음과 같다.

- `_availableByPrefab`: 원본 Component별 재사용 가능 인스턴스 Queue
- `_prefabByInstance`: 생성된 인스턴스가 어느 원본에서 왔는지 기록
- `_pooledInstances`: 같은 인스턴스의 중복 반환 방지

`Spawn<T>()`은 Queue에 인스턴스가 있으면 재사용하고, 없으면 그때 최초 생성한다. 현재 사전 생성(prewarm)은 없으며 지연 생성 방식이다.

`Return()`은 다음 순서로 처리한다.

1. `IPoolable.OnDespawned()` 호출
2. GameObject 비활성화
3. Pool Root 아래로 이동
4. 원본 프리팹의 Queue에 삽입

Pool Root가 비어 있으면 `ObjectPoolManager` 자신의 Transform을 사용한다. 별도 자식 `PoolRoot`를 연결하는 것이 권장되며, Transform은 위치 0, 회전 0, 스케일 1을 유지해야 한다. 활성 투사체도 이 Transform의 자식이므로 Pool Root를 움직이거나 스케일하지 않는다.

### 4.3 Data

| 파일 | 역할 |
|---|---|
| `Assets/01.Scripts/Data/WeaponDefinitionSO.cs` | 무기별 공격 방식과 밸런스 수치 정의 |
| `Assets/01.Scripts/Data/WeaponLoadoutSO.cs` | 공통 무기 전환 시간 정의 |

`WeaponDefinitionSO` 주요 필드:

| 필드 | 의미 |
|---|---|
| `FireMode` | `Projectile` 또는 `ContinuousRay` |
| `ImpactMode` | 투사체 직접 피해 `Direct` 또는 범위 피해 `Explosion` |
| `Element` | `None`, `Ice`, `Fire` |
| `Damage` | 투사체는 발당 피해, 레이는 초당 피해량(DPS) |
| `Cooldown` | 공격 종료 후 다음 공격까지의 시간 |
| `BurstCount` | 한 점사의 투사체 수. 단발은 1 |
| `BurstInterval` | 점사 내부 발사 간격 |
| `MaxContinuousDuration` | 지속 레이 최대 유지 시간 |
| `Range` | 지속 레이 사거리 |
| `ProjectileSpeed` | 투사체 초당 이동 거리 |
| `ProjectileLifetime` | 미충돌 투사체의 자동 반환 시간 |
| `ExplosionRadius` | 범위 피해 반경 |
| `HitLayers` | 투사체/레이가 충돌 대상으로 인정할 레이어 |

모든 SerializeField에는 Inspector용 한국어 Tooltip이 추가되어 있다.

현재 관련 SO 파일은 다음 경로에 존재한다.

```text
Assets/06.SO/Weapon/MachineGun.asset
Assets/06.SO/Weapon/RocketLauncher.asset
Assets/06.SO/Weapon/IceGun.asset
Assets/06.SO/Weapon/FireGun.asset
Assets/06.SO/Weapon/WeaponLoadout.asset
```

SO 값은 테스트 결과에 따라 변경하는 것이 전제다. 수치를 코드로 옮기지 않는다.

### 4.4 Gameplay/Weapon

| 파일 | 역할 |
|---|---|
| `WeaponBase.cs` | 무기 공통 Definition, muzzle, 조준 회전, Sprite 뒤집기, 독립 쿨타임 |
| `ProjectileWeapon.cs` | 점사/단발 상태 진행과 풀을 통한 투사체 발사 |
| `ContinuousRayWeapon.cs` | 지속 시간, Raycast, LineRenderer, DPS 및 속성 인터페이스 호출 |
| `PooledProjectile.cs` | 이동, 수명, 직접/폭발 피해, 히트 이펙트 생성, 풀 상태 초기화 및 반환 |
| `PooledHitEffect.cs` | 히트 이펙트 파티클의 재생, 히트 지점 추적, 자동 풀 반환 |
| `PlayerWeaponController.cs` | 현재 무기, 전환, 조준 좌표 변환, 공격 상태 총괄 |

## 5. 수정한 코드와 입력 에셋

### 5.1 PlayerInputReader

수정 파일:

```text
Assets/01.Scripts/Gameplay/Player/PlayerInputReader.cs
```

기존 Jump/Crouch 입력을 유지하면서 다음 필드를 추가했다.

```text
_attackAction
_aimPositionAction
_weaponOneAction
_weaponTwoAction
_weaponThreeAction
_weaponFourAction
_playerWeaponController
```

콜백은 `PlayerWeaponController`에 상태만 전달한다.

- Attack performed → `SetAttackHeld(true)`
- Attack canceled → `SetAttackHeld(false)`
- Aim Position performed → `SetAimScreenPosition(Vector2)`
- Weapon1~4 performed → `RequestWeaponSelection(0~3)`

OnEnable에서 구독/Enable하고 OnDisable에서 반드시 구독 해제/Disable한다.

### 5.2 Input Actions

현재 `Assets/InputSystem_Actions.inputactions`의 Player Action Map에 다음 액션이 저장되어 있다.

| 액션 | 바인딩 | 무기 배열 인덱스 |
|---|---|---:|
| `Weapon1` | Q | 0 |
| `Weapon2` | W | 1 |
| `Weapon3` | E | 2 |
| `Weapon4` | R | 3 |

`Assets/01.Scripts/Core/Generated/InputSystem_Actions.cs`는 Input Actions 에셋에서 자동 생성되는 파일이다. 직접 편집하지 않는다.

주의: W는 기존 Move/Up, E는 기존 Interact와 중복될 수 있다. 해당 기능을 동시에 사용하게 되면 입력 충돌을 정리해야 한다.

## 6. 클래스별 동작 상세

### 6.1 PlayerWeaponController

초기화:

1. 같은 Player에서 `PlayerHealth`를 캐싱한다.
2. Aim Camera가 비어 있으면 `Camera.main`을 한 번 캐싱한다.
3. Weapons 배열에서 첫 번째 유효한 무기만 활성화한다.

매 프레임:

1. Player가 죽었으면 현재 공격을 취소한다.
2. 전환 중이면 전환 완료 시간만 확인하고 공격하지 않는다.
3. 마우스 화면 좌표를 muzzle과 같은 Z 평면의 월드 좌표로 변환한다.
4. `muzzle → 마우스 월드 좌표` 방향을 현재 무기에 전달한다.
5. Attack 홀드 상태를 현재 무기의 `TickAttack()`에 전달한다.

무기 전환:

1. 현재 공격을 즉시 취소한다.
2. 현재 무기 GameObject를 비활성화한다.
3. `WeaponLoadoutSO.SwitchDuration` 동안 대기한다.
4. 선택된 무기를 활성화한다.
5. 전환을 시작할 때 Attack을 누르고 있었다면 버튼을 놓기 전까지 새 무기가 자동 발사되지 않도록 차단한다.

Weapons 배열 순서는 Q/W/E/R 인덱스와 일치해야 한다.

```text
0: Machine Gun
1: Rocket Launcher
2: Ice Gun
3: Fire Gun
```

### 6.2 WeaponBase와 쿨타임 보존

각 무기 Component 인스턴스가 `_nextReadyTime`을 따로 보관한다.

```csharp
_nextReadyTime = Time.time + Definition.Cooldown;
```

무기를 비활성화해도 `Time.time`과 이 필드는 유지된다. 따라서 로켓 발사 후 다른 총으로 전환했다가 돌아와도 로켓의 남은 쿨타임이 초기화되지 않는다.

조준 시 무기 Transform을 월드 Z축으로 회전하며, 왼쪽을 조준하면 연결된 SpriteRenderer의 `flipY`를 활성화한다.

### 6.3 ProjectileWeapon

- Trigger가 눌려 있고 쿨타임이 끝났으면 점사를 시작한다.
- `BurstCount`만큼 발사하며 각 발 사이에 `BurstInterval`을 적용한다.
- 마지막 탄을 발사한 시점부터 쿨타임을 시작한다.
- 전환 중 점사가 취소되면 즉시 쿨타임을 시작한다.
- 투사체는 `ObjectPoolManager.Instance.Spawn()`으로만 얻는다.
- 머신건과 로켓은 같은 클래스와 `PooledProjectile`을 사용하고 SO의 수치/ImpactMode로 동작을 나눈다.

### 6.4 PooledProjectile

필수 Component:

```text
Rigidbody2D
Collider2D
PooledProjectile
선택: TrailRenderer
```

동작:

1. 활성화될 때 `OnEnable()`에서 이전 속도, Definition, 수명, Trail 상태를 초기화한다.
2. `Launch()`가 Definition과 방향을 받아 Rigidbody2D 속도를 설정한다.
3. `FixedUpdate()`에서 수명을 차감한다.
4. Collision 또는 Trigger가 HitLayers에 속하면 첫 충돌을 처리한다.
5. Direct는 충돌 대상의 `IDamageable`에 한 번 피해를 준다.
6. Explosion은 `OverlapCircleNonAlloc`로 범위 내 Collider를 모은다.
7. 한 대상이 여러 Collider를 가져도 HashSet으로 한 번만 피해를 준다.
8. `_hitEffectPrefab`이 연결되어 있으면 충돌 지점에 히트 이펙트를 풀에서 꺼내 재생한다.
9. 충돌 또는 수명 종료 후 풀로 반환한다. 수명이 다해 반환되는 탄은 이펙트를 만들지 않는다.

히트 이펙트의 회전에 쓰는 법선은 `OnCollisionEnter2D`에서 `collision.GetContact(0).normal`을 사용한다. `OnTriggerEnter2D`는 접촉 법선을 주지 않으므로 진행 방향의 역벡터를 대신 쓴다.

폭발 검색 버퍼 크기는 32다. 한 폭발에 32개보다 많은 Collider가 들어오는 상황이 생기면 용량 또는 판정 구조를 재검토한다.

### 6.5 ContinuousRayWeapon

- Trigger를 홀드하면 최대 `MaxContinuousDuration` 동안 발사한다.
- muzzle에서 조준 방향으로 `Physics2D.Raycast`를 한 번 수행한다.
- HitLayers에 속한 첫 Collider까지만 LineRenderer를 표시한다.
- 피해량은 `Definition.Damage * deltaTime`이므로 Definition의 Damage는 DPS다.
- Ice는 피해와 함께 `IFreezable.Freeze()`를 호출한다.
- Fire는 피해와 함께 `IBurnable.ApplyBurn(deltaTime)`을 호출한다.
- 버튼 해제, 최대 지속 시간 도달, 무기 전환, GameObject 비활성화 시 레이를 종료한다.
- 레이 종료 시 쿨타임을 시작한다.
- `_hitEffectPrefab`이 연결되어 있으면 첫 히트에서 이펙트를 풀에서 꺼내고, 이후 매 프레임 `Follow()`로 히트 지점을 따라가게 한다.
- 레이가 허공을 향하는 동안에는 인스턴스를 반환하지 않고 방출만 멈춘다. 매 프레임 Spawn/Return이 반복되는 것을 막기 위해서다.
- 레이 종료 시 `Release()`로 방출을 멈추고, 남은 파티클이 자연 소멸한 뒤 이펙트가 스스로 풀로 돌아간다.

아이스의 `Freeze()`는 매 프레임 호출될 수 있으므로 스테이지 구현체가 이미 얼었는지 확인하는 멱등 구조여야 한다. 파이어의 `ApplyBurn(duration)`은 접촉 시간을 누적하는 방식으로 구현하면 된다.

### 6.6 PooledHitEffect

히트 이펙트 프리팹 루트에 붙는 컴포넌트다. 재생, 히트 지점 추적, 풀 반환 시점만 담당하고 피해나 판정에는 관여하지 않는다.

| 메서드 | 역할 |
|---|---|
| `Play(position, normal)` | 위치·회전을 잡고 파티클을 처음부터 재생 |
| `Follow(position, normal)` | 루프 이펙트가 히트 지점을 따라가도록 갱신 |
| `SetEmitting(bool)` | 인스턴스를 유지한 채 방출만 켜고 끔 |
| `Release()` | 방출을 멈추고 잔여 파티클 소멸 후 풀에 반환 |
| `OnDespawned()` | 풀 반환 시 Stop + Clear로 상태 초기화 |

동작 규칙:

- 루트 ParticleSystem의 `main.loop` 값으로 총알용(비루프)/레이저용(루프) 동작이 **자동으로 갈린다.** 호출부가 무기 종류를 알 필요가 없다.
- 비루프는 `Play()` 시점에 자동 반환 타이머를 걸고, 시간이 지나면 스스로 풀로 돌아간다.
- 루프는 `Release()`를 받기 전까지 스스로 반환하지 않는다.
- 자동 반환 시간은 코드 상수가 아니라 **프리팹의 파티클 설정에서 계산한다.** 하위 ParticleSystem 전체를 순회하며 `startDelay + duration + startLifetime`의 최댓값을 쓴다. 프리팹 재생 시간을 바꿔도 코드를 고칠 필요가 없다.
- `_extraLingerTime`은 계산값으로 부족할 때만 쓰는 추가 대기 시간이다. 기본 0.

CFXR 이펙트 프리팹을 쓸 때 `CFXR_Effect.clearBehavior`는 반드시 **`None`** 이어야 한다. `Destroy`나 `Disable`이면 파티클이 끝나는 순간 서드파티 스크립트가 풀 인스턴스를 파괴/비활성화해서 풀로 돌아오지 못한다.

## 7. 현재 관련 에디터 에셋

현재 프로젝트에 다음 에셋이 존재한다.

```text
Assets/00.Scenes/SystemScene.unity
Assets/02.Prefabs/Core/ObjectPoolManager.prefab

Assets/02.Prefabs/Gameplay/Gun/machinegun.prefab
Assets/02.Prefabs/Gameplay/Gun/rocketlauncher.prefab
Assets/02.Prefabs/Gameplay/Gun/icegun.prefab
Assets/02.Prefabs/Gameplay/Gun/firegun.prefab

Assets/02.Prefabs/Gameplay/Bullet/Normal.prefab
Assets/02.Prefabs/Gameplay/Bullet/Rocket.prefab
Assets/02.Prefabs/Gameplay/Bullet/FireLaser.prefab
Assets/02.Prefabs/Gameplay/Bullet/IceLaser.prefab

Assets/02.Prefabs/Gameplay/Bullet/Hit Effect/CFXR3 Machine Gun Hit Effect.prefab
Assets/02.Prefabs/Gameplay/Bullet/Hit Effect/CFXR Rocket Launcher Hit Effect.prefab
Assets/02.Prefabs/Gameplay/Bullet/Hit Effect/CFXR Ice Hit Effect.prefab
Assets/02.Prefabs/Gameplay/Bullet/Hit Effect/CFXR Fire Hit Effect.prefab
```

씬/프리팹/asset의 실제 직렬화 연결 상태를 확인하거나 수정해야 할 때는 텍스트로 열지 말고 Unity MCP 또는 Unity CLI를 사용한다.

## 8. 필수 Inspector 연결 체크리스트

### Player 프리팹

- `PlayerWeaponController.Loadout` → `WeaponLoadout.asset`
- `PlayerWeaponController.Weapons` → Q/W/E/R 순서로 네 무기 Component
- `PlayerWeaponController.Aim Camera` → 비워두면 MainCamera 태그 카메라 사용
- `PlayerWeaponController.Player Health` → 같은 Player의 PlayerHealth
- `PlayerInputReader.Attack Action` → `Player/Attack`
- `PlayerInputReader.Aim Position Action` → `UI/Point`
- `PlayerInputReader.Weapon One~Four Action` → `Player/Weapon1~4`
- 네 총 프리팹/인스턴스는 Player 오른손 아래의 기존 `WeaponSlot`에 배치

### 총 프리팹

- 머신건/로켓 → `ProjectileWeapon`
- 아이스/파이어 → `ContinuousRayWeapon`
- WeaponBase의 Definition, muzzle, SpriteRenderer 연결
- ProjectileWeapon의 Projectile Prefab 연결
- ContinuousRayWeapon의 LineRenderer 연결
- 아이스/파이어의 `ContinuousRayWeapon.Hit Effect Prefab` → 각각 `CFXR Ice / Fire Hit Effect`

`WeaponDefinitionSO.FireMode`는 현재 어떤 MonoBehaviour를 자동으로 선택하지 않는다. 프리팹에 올바른 `ProjectileWeapon` 또는 `ContinuousRayWeapon` Component를 직접 붙여야 한다.

### 투사체 프리팹

- Rigidbody2D Gravity Scale 0
- 빠른 투사체는 Collision Detection을 Continuous로 설정
- Collider2D와 PooledProjectile 추가
- HitLayers가 Player 자신을 포함하지 않도록 설정
- TrailRenderer 사용 시 PooledProjectile 필드에 연결
- `PooledProjectile.Hit Effect Prefab` → Normal은 `CFXR3 Machine Gun Hit Effect`, Rocket은 `CFXR Rocket Launcher Hit Effect`

### 히트 이펙트 프리팹

- 루트에 `PooledHitEffect` 추가, `Root Particle`은 루트 ParticleSystem 연결(비워두면 자동 탐색)
- `CFXR_Effect.Clear Behavior`를 **`None`** 으로 설정 (6.6 참조)
- 총알용은 루트 ParticleSystem `Looping` 해제, 레이저용은 `Looping` 설정

### ObjectPoolManager

- SystemScene에 정확히 하나만 존재
- 자식 빈 GameObject `PoolRoot`를 만들고 Pool Root에 연결하는 것을 권장
- PoolRoot Transform은 움직이거나 스케일하지 않음
- SystemScene은 게임 동안 유지되어야 함

## 9. 알려진 제약과 후속 작업

- 풀은 지연 생성 방식이므로 첫 발사와 각 이펙트의 첫 히트 순간 Instantiate가 발생할 수 있다. 파티클 프리팹은 탄보다 무거우므로 프레임 튐이 보이면 ObjectPoolManager 내부에 프리워밍을 추가한다.
- 로켓 폭발 사운드는 아직 없다. 히트 이펙트만 재생된다.
- 히트 이펙트는 월드 좌표에 고정된다. `MapScrollController`가 맵을 왼쪽으로 밀기 때문에 재생 시간이 긴 이펙트(로켓 약 2초)는 맞은 벽에서 조금씩 뒤로 밀린다. 눈에 띄면 `PooledHitEffect`에 대상 Transform 추적 옵션을 추가한다. 맵 세그먼트에 `SetParent`로 붙이는 방식은 세그먼트가 풀로 반환될 때 이펙트까지 비활성화되어 풀로 돌아오지 못하므로 쓰지 않는다.
- 히트 이펙트의 ParticleSystemRenderer Sorting Layer는 현재 조정하지 않았다. 스프라이트에 가려 보이면 그때 조정한다.
- CFXR 이펙트의 `Point Light` 자식은 URP 2D Renderer에서 렌더되지 않는다. 보이지 않으면서 `CFXR_Effect`가 매 프레임 강도를 애니메이션하는 비용만 든다.
- 현재 무기 UI 이벤트 채널은 없다. UI가 필요하면 Core에 상태 변화 이벤트 채널을 새로 추가하고 Gameplay와 UI가 직접 참조하지 않도록 한다.
- 적/지형 구현체는 `float` 피해를 처리해야 한다. 정수 체력만 사용한다면 내부 누적 후 정수 변환 정책을 명시한다.
- Camera가 자동 탐색되려면 해당 카메라에 MainCamera 태그가 필요하다.
- HitLayers가 잘못 설정되면 레이/투사체가 적이나 지형을 통과한 것처럼 보인다.
- 무기 Sprite의 피벗과 기본 오른쪽 방향이 `transform.right` 기준과 맞지 않으면 조준 회전 또는 flip 설정을 조정해야 한다.
- W/E는 기존 Move/Interact 바인딩과 중복될 수 있다.

## 10. 검증 기록과 권장 테스트

최초 코드 구현 직후 Unity CLI 재컴파일 결과는 `completed`, 컴파일 오류 0건, 콘솔 경고/오류 0건이었다. 이후 에디터 에셋 연결 상태는 변경될 수 있으므로 다음 테스트를 다시 수행한다.

1. Q/W/E/R 선택 시 정확히 대응하는 총으로 1초 후 전환되는지 확인
2. 전환 중 공격이 실행되지 않는지 확인
3. 로켓 발사 → 다른 무기 → 로켓 복귀 시 쿨타임이 유지되는지 확인
4. 머신건이 세 발 점사 후 쿨타임에 들어가는지 확인
5. 총알이 첫 HitLayers 충돌에서 사라지고 풀로 반환되는지 확인
6. 로켓 폭발이 같은 대상의 복수 Collider에 중복 피해를 주지 않는지 확인
7. 아이스/파이어가 최대 3초 동안만 발사되고 이후 쿨타임에 들어가는지 확인
8. 지속 발사 도중 무기를 전환하면 LineRenderer가 즉시 사라지는지 확인
9. 물/덩굴 테스트 구현체에서 Freeze/ApplyBurn 호출을 확인
10. 반복 발사 후 Hierarchy의 PoolRoot 아래에서 인스턴스가 재사용되는지 확인
11. Profiler에서 발사마다 반복적인 Instantiate/Destroy가 발생하지 않는지 확인
12. 머신건/로켓 탄착 지점에 이펙트가 1회 재생되고 사라지는지 확인
13. 아이스/파이어 레이가 벽을 훑을 때 이펙트가 히트 지점을 끊김 없이 따라오는지 확인
14. 레이를 허공으로 돌렸다가 다시 벽에 맞췄을 때 이펙트 방출이 멈췄다 재개되는지 확인
15. 레이 발사 중 무기를 전환하면 이펙트가 남지 않는지 확인
16. 장시간 연사 후 PoolRoot 아래 이펙트 인스턴스 수가 더 늘지 않고 재사용되는지 확인

## 11. 작업 시 주의사항

- 기존 `[SerializeField]` 이름을 바꾸지 않는다. 프리팹 연결이 끊길 수 있다.
- 기존 public 메서드 시그니처를 바꾸기 전에 사용처를 검색한다.
- Core의 기존 파일은 수정하지 않는다. 새 계약이 필요하면 새 파일 추가를 우선한다.
- 총알, 로켓, 이펙트에 직접 `Instantiate/Destroy`를 추가하지 않는다.
- 이펙트 프리팹 참조는 `WeaponDefinitionSO`가 아니라 탄/총 프리팹의 컴포넌트에 둔다. SO에 두면 `Game.Data → Game.Gameplay` 역참조가 생긴다.
- 숫자를 무기 코드에 하드코딩하지 않고 WeaponDefinitionSO 또는 WeaponLoadoutSO에 둔다.
- `.unity`, `.prefab`, `.asset`, `.meta`를 텍스트 편집하지 않는다.
- `InputSystem_Actions.cs`는 자동 생성 파일이므로 직접 편집하지 않는다.
- 스테이지 코드가 준비되기 전까지 Weapon 쪽에서 구체 Stage 클래스를 참조하지 않는다. Core 인터페이스만 사용한다.
