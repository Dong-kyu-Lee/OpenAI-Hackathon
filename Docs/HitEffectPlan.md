# 히트 이펙트 구현 계획

`Docs/WeaponSystemArchitecture.md`의 무기 시스템에 **피격 지점 이펙트**를 추가하기 위한 계획서다.
아직 코드는 작성하지 않았다. 승인 후 4장의 파일만 수정한다.

프로젝트 규칙은 `AGENTS.md`가 최우선이다. 특히 `.prefab`은 텍스트로 편집하지 않고, 에디터 작업은 6장으로 분리했다.

---

## 1. 목표

| 무기 | 발사 방식 | 이펙트 요구사항 |
|---|---|---|
| 머신건 | Projectile (Direct) | 탄이 맞은 지점에서 1회 재생 후 사라짐 |
| 로켓런처 | Projectile (Explosion) | 폭발 지점에서 1회 재생 후 사라짐 |
| 아이스건 | ContinuousRay | 레이가 닿는 지점을 **따라다니며** 계속 재생 |
| 파이어건 | ContinuousRay | 레이가 닿는 지점을 **따라다니며** 계속 재생 |

공통 요구사항: `Instantiate`/`Destroy`를 직접 호출하지 않고 기존 `ObjectPoolManager`를 경유한다 (AGENTS 3.7).

---

## 2. 사용할 이펙트 프리팹 (실측)

경로는 **`Assets/02.Prefabs/Gameplay/Bullet/Hit Effect`** 다.

| 프리팹 | 루트 ParticleSystem `loop` | 루트 `duration` | 하위 PS 수 | `CFXR_Effect.clearBehavior` |
|---|---|---|---:|---|
| `CFXR3 Machine Gun Hit Effect` | **false** | 1.0s | 2 | Destroy |
| `CFXR Rocket Launcher Hit Effect` | **false** | 2.0s | 4 | Destroy |
| `CFXR Ice Hit Effect` | **true** | 1.0s | 3 | Destroy |
| `CFXR Fire Hit Effect` | **true** | 1.0s | 3 | Destroy |

요청하신 대로 총알류(비루프) / 레이저류(루프) 구분이 프리팹에 이미 되어 있다. 모든 ParticleSystem의 `Stop Action`은 `None`이라 파티클 자체가 오브젝트를 지우지는 않는다.

### 2.1 반드시 먼저 고쳐야 하는 것 — `clearBehavior`

네 프리팹 모두 `CFXR_Effect.clearBehavior`가 **`Destroy`** 다. `CFXR_Effect.Update()`는 20프레임마다 `rootParticleSystem.IsAlive(true)`를 검사해 파티클이 끝나면 `GameObject.Destroy(this.gameObject)`를 호출한다.

이 상태로 풀링하면 **풀이 빌려준 인스턴스를 서드파티 스크립트가 파괴**해서, `ObjectPoolManager._prefabByInstance`에 죽은 참조만 남고 인스턴스는 큐로 돌아오지 못한다. 매 히트마다 새 `Instantiate`가 발생하는, 풀링을 넣기 전보다 나쁜 상태가 된다.

→ **네 프리팹의 `Clear Behavior`를 `None`으로 바꾼다** (6장 에디터 작업 1번).
코드로 `CFXR_Effect`를 끄는 방법은 `Game.Gameplay`가 `CFXR Runtime` asmdef를 참조해야 해서 AGENTS 3.1에 걸린다. 프리팹 필드 변경이 정답이다.

---

## 3. 설계

### 3.1 새 클래스 1개 — `PooledHitEffect`

```
Assets/01.Scripts/Gameplay/Weapon/PooledHitEffect.cs   (신규, Game.Gameplay)
```

풀에서 빌려온 파티클 오브젝트의 **재생 / 추적 / 반환 시점**만 책임진다. 데미지, 판정, 조준은 관여하지 않는다.

```csharp
public sealed class PooledHitEffect : MonoBehaviour, IPoolable
{
    [SerializeField] private ParticleSystem _rootParticle;      // 비우면 Awake에서 GetComponent
    [SerializeField] private bool _alignToSurfaceNormal = true; // 표면 법선에 맞춰 회전
    [SerializeField] private float _extraLingerTime;            // 반환 직전 추가 대기(기본 0)

    public void Play(Vector3 position, Vector3 normal);   // 위치·회전 세팅 후 재생 시작
    public void Follow(Vector3 position, Vector3 normal); // 루프 이펙트의 위치 갱신
    public void SetEmitting(bool isEmitting);             // 레이가 허공을 쏘는 동안 방출만 정지
    public void Release();                                // 방출 정지 후 잔여 파티클 수명만큼 대기 → 풀 반환
    public void OnDespawned();                            // IPoolable: Stop + Clear + 상태 초기화
}
```

동작 규칙:

1. `Awake()`에서 `GetComponentsInChildren<ParticleSystem>(true)`를 캐싱하고 두 값을 **계산**한다.
   - `_oneShotDuration` = 모든 PS의 `main.duration + main.startLifetime.constantMax` 중 최댓값
   - `_maxParticleLifetime` = 모든 PS의 `main.startLifetime.constantMax` 중 최댓값
   - 이렇게 하면 프리팹마다 다른 재생 시간을 코드에 숫자로 박지 않는다 (AGENTS 3.5).
2. `_isLooping = _rootParticle.main.loop` 으로 비루프/루프 동작을 자동 분기한다. 호출부가 무기 종류를 신경 쓸 필요가 없다.
3. **비루프(총알류)**: `Play()` 시 `_remaining = _oneShotDuration + _extraLingerTime`을 걸고 `Update()`에서 감소, 0이 되면 스스로 풀에 반환한다.
4. **루프(레이저류)**: `Play()` 후에는 타이머가 돌지 않는다. `Release()`가 호출되면 `Stop(true, StopEmitting)` 후 `_maxParticleLifetime`만큼만 더 살아서, 남은 파티클이 뚝 끊기지 않고 자연 소멸한 뒤 반환된다.
5. 재사용 시 `Awake`는 다시 호출되지 않으므로 상태 초기화는 `OnEnable`/`OnDespawned`에서 한다 (AGENTS 3.7).
6. 반환은 `ObjectPoolManager.Instance.Return(this)` 만 사용한다. 인스턴스가 없으면 `Debug.LogError` 후 `SetActive(false)` — 기존 `PooledProjectile.ReturnToPool()`과 동일한 패턴을 따른다.

### 3.2 이펙트 프리팹 참조를 어디에 둘 것인가

`WeaponDefinitionSO`(Game.Data)에 넣으면 `Game.Data → Game.Gameplay` 역참조가 생겨 AGENTS 3.1을 위반한다. `PooledHitEffect`를 `Core`로 내리는 방법도 있지만, 파티클 재생은 도메인 로직이라 `Core`(계약·유틸) 성격과 맞지 않는다.

→ **참조는 프리팹 단위로 붙인다.**

| 참조를 가진 컴포넌트 | 붙는 프리팹 | 연결할 이펙트 |
|---|---|---|
| `PooledProjectile._hitEffectPrefab` | `Bullet/Normal.prefab` | `CFXR3 Machine Gun Hit Effect` |
| `PooledProjectile._hitEffectPrefab` | `Bullet/Rocket.prefab` | `CFXR Rocket Launcher Hit Effect` |
| `ContinuousRayWeapon._hitEffectPrefab` | `Gun/icegun.prefab` | `CFXR Ice Hit Effect` |
| `ContinuousRayWeapon._hitEffectPrefab` | `Gun/firegun.prefab` | `CFXR Fire Hit Effect` |

`WeaponDefinitionSO`와 asmdef는 **수정하지 않는다.** 탄이 자기 히트 이펙트를 아는 구조라 기존 public 메서드 시그니처도 바뀌지 않는다 (AGENTS 1.7).

### 3.3 총알류 흐름

```
PooledProjectile.OnCollisionEnter2D / OnTriggerEnter2D
        │  (충돌 지점 + 법선을 함께 구함)
        ▼
ResolveImpact(collider, impactPoint, impactNormal)
        ├─ 기존 피해 처리 (Direct / Explosion)
        ├─ ObjectPoolManager.Spawn(_hitEffectPrefab) → PooledHitEffect.Play(point, normal)
        └─ 자신은 풀로 반환
```

- 법선은 `OnCollisionEnter2D`에서 `collision.GetContact(0).normal`을 쓴다.
- `OnTriggerEnter2D`는 법선이 없으므로 **속도를 0으로 만들기 전에** 진행 방향의 역벡터를 법선 대용으로 저장해 쓴다.
- 수명이 다해 반환되는 탄(미충돌)은 이펙트를 만들지 않는다.
- `ResolveImpact`는 private이라 인자 추가에 제약이 없다.

### 3.4 레이저류 흐름

```
ContinuousRayWeapon.ProcessRay(appliedDuration)
        ├─ hit 있음 → 인스턴스 없으면 Spawn + Play, 있으면 Follow(hit.point, hit.normal) + SetEmitting(true)
        └─ hit 없음 → 인스턴스 유지, SetEmitting(false)   ← 풀 스래싱 방지
StopAttack(...)  (버튼 해제 / 최대 지속시간 / 무기 전환 / OnDisable)
        └─ _hitEffectInstance.Release(); _hitEffectInstance = null;
```

- 인스턴스는 `ContinuousRayWeapon`이 필드 하나로 들고 있다. 레이가 벽을 스쳐 매 프레임 붙었다 떨어졌다 해도 Spawn/Return이 반복되지 않도록, **발사 중에는 인스턴스를 유지하고 방출만 껐다 켠다.**
- 종료 경로는 이미 `StopAttack()` 한 곳으로 모여 있다. `OnDisable → CancelAttack → StopAttack` 경로가 살아 있어 무기 전환과 플레이어 사망 시에도 이펙트가 남지 않는다.
- 반환 대기 중인 이펙트는 `PoolRoot`(System_Scene) 밑에 있으므로, 총이 비활성화된 뒤에도 잔여 파티클이 정상적으로 소멸하고 스스로 반환된다.

---

## 4. 변경 파일

```
(신규) Assets/01.Scripts/Gameplay/Weapon/PooledHitEffect.cs        [Game.Gameplay]
(수정) Assets/01.Scripts/Gameplay/Weapon/PooledProjectile.cs       [Game.Gameplay]
(수정) Assets/01.Scripts/Gameplay/Weapon/ContinuousRayWeapon.cs    [Game.Gameplay]
(수정) Docs/WeaponSystemArchitecture.md                            [인수인계 문서 갱신]
```

- `Core`, `Data`, asmdef, `WeaponDefinitionSO`, 입력 에셋은 건드리지 않는다.
- 기존 `[SerializeField]` 이름과 public 시그니처 변경 없음 → 인스펙터 연결이 끊기지 않는다.

---

## 5. 왜 이 구조인가 (검토했지만 버린 안)

| 대안 | 버린 이유 |
|---|---|
| `WeaponDefinitionSO`에 이펙트 프리팹 필드 추가 | `Game.Data → Game.Gameplay` 역참조 발생 (AGENTS 3.1) |
| `PooledHitEffect`를 `Core`에 배치 | `Core`는 계약·유틸 영역. 파티클 재생은 Gameplay 로직 |
| 이펙트 전용 `HitEffectManager` 싱글톤 | 싱글톤 화이트리스트 3개 위반 (AGENTS 3.4) |
| 루프/비루프용 클래스를 각각 만들기 | `main.loop` 한 줄로 분기 가능. 클래스 2개는 과설계 |
| 코드에서 `CFXR_Effect`를 비활성화 | Gameplay가 CFXR asmdef를 참조해야 함 |

---

## 6. 에디터에서 필요한 작업

**모두 사용자가 직접 하거나, 별도 지시가 있을 때만 MCP로 수행한다** (AGENTS 2.3).

1. **[필수]** `Assets/02.Prefabs/Gameplay/Bullet/Hit Effect` 의 네 프리팹 루트에서 `CFXR_Effect > Clear Behavior`를 **`Destroy` → `None`** 으로 변경 (2.1 참조. 이걸 빼면 풀링이 무너진다)
2. **[필수]** 같은 네 프리팹 루트에 `PooledHitEffect` 컴포넌트 추가
   - `Root Particle`은 비워두면 루트에서 자동으로 찾는다
   - `Align To Surface Normal` 기본값 ON
3. **[필수]** `Bullet/Normal.prefab` → `PooledProjectile > Hit Effect Prefab` 에 `CFXR3 Machine Gun Hit Effect` 연결
4. **[필수]** `Bullet/Rocket.prefab` → `PooledProjectile > Hit Effect Prefab` 에 `CFXR Rocket Launcher Hit Effect` 연결
5. **[필수]** `Gun/icegun.prefab` → `ContinuousRayWeapon > Hit Effect Prefab` 에 `CFXR Ice Hit Effect` 연결
6. **[필수]** `Gun/firegun.prefab` → `ContinuousRayWeapon > Hit Effect Prefab` 에 `CFXR Fire Hit Effect` 연결
7. **[선택]** 네 이펙트의 `Point Light` 자식 비활성화 — URP **2D Renderer는 3D `Light` 컴포넌트를 렌더하지 않는다.** 지금은 보이지도 않으면서 `CFXR_Effect`가 매 프레임 강도를 애니메이션하는 비용만 낸다
8. **[선택]** `ParticleSystemRenderer`의 Sorting Layer / Order 조정 — 네 프리팹 모두 현재 `Default / 0`이라 플레이어·맵 스프라이트에 가려질 수 있다 (Sorting Layer **추가**가 필요하면 `ProjectSettings` 변경이므로 AGENTS 1장 5번 — 보고만 하고 사용자가 직접)

`ObjectPoolManager`는 `System_Scene`에 이미 있으므로 추가 배치는 없다.

---

## 7. 알려진 제약 / 후속 과제

- **맵 스크롤 드리프트**: `MapScrollController`가 맵을 왼쪽으로 이동시키는 구조라, 월드 좌표에 고정된 히트 이펙트는 맞은 벽에서 서서히 뒤로 밀린다. 머신건(1s)은 거의 눈에 띄지 않지만 **로켓(2s)** 은 보일 수 있다. 눈에 띄면 후속으로 `PooledHitEffect`에 "부모로 붙이지 않고 대상 Transform을 따라가는" 옵션을 추가한다. (맵 세그먼트에 `SetParent`로 붙이는 방식은 세그먼트가 풀로 반환될 때 이펙트가 같이 비활성화되어 풀에 영원히 안 돌아오므로 **쓰지 않는다.**)
- **지연 생성**: 풀이 lazy라 각 이펙트의 첫 히트에서 `Instantiate`가 한 번 발생한다. 파티클 프리팹은 탄보다 무거우므로, 프레임 튐이 보이면 `ObjectPoolManager`에 프리워밍을 추가한다 (기존 `WeaponSystemArchitecture.md` 9장과 동일한 과제).
- **폭발 반경과 이펙트 크기**: 로켓 이펙트의 시각적 크기와 `WeaponDefinitionSO.ExplosionRadius`는 서로 연동되지 않는다. 필요하면 이펙트 프리팹 스케일로 맞춘다.
- `CFXR_Effect`는 `OnDisable`에서 `ResetState()`로 라이트/카메라 셰이크를 되돌리므로, 풀 반환 후 재사용해도 상태가 누적되지 않는다. 별도 처리는 필요 없다.
- 사운드는 이 작업 범위 밖이다.

---

## 8. 검증 방법

1. 머신건으로 벽을 쏴서 탄착 지점마다 이펙트가 1회 재생되고 사라지는지 확인
2. 로켓을 쏴서 폭발 지점에 이펙트가 나오는지 확인
3. 아이스/파이어로 벽을 훑으며 이펙트가 레이 끝을 **끊김 없이 따라오는지** 확인
4. 레이를 허공으로 돌렸을 때 이펙트 방출이 멈추고, 다시 벽을 맞추면 재개되는지 확인
5. 레이 발사 중 Q/W/E/R로 무기를 바꿨을 때 이펙트가 남지 않는지 확인
6. 최대 지속시간(3초) 도달로 레이가 자동 종료될 때도 이펙트가 정리되는지 확인
7. 30초 이상 연사 후 Hierarchy의 `PoolRoot` 아래에서 이펙트 인스턴스 수가 **더 이상 늘지 않고 재사용**되는지 확인 (`clearBehavior` 수정 검증)
8. Profiler에서 히트마다 `Instantiate`/`Destroy`가 반복되지 않는지 확인
9. Console에 에러/경고 0건 확인
