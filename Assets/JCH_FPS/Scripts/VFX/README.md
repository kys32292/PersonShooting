# 피격 VFX 사용법

<!-- 2026-11-09: 신규 VFX 구성과 기존 코드 수정 지점을 기록합니다. -->

- `VfxService`: 하이어라키에 붙이지 않는 정적 호출 창구입니다.
- `VfxId`: 상황 식별자입니다. 현재 `EnemyHit`만 등록되어 있습니다.
- `VfxConfig`: 상황별 프리팹, 배율, 제거 시간, 표면 오프셋, 회전 보정을 보관합니다.
- `Resources/VFX/VfxConfig.asset`: 최초 호출 때 자동 로드되는 실제 설정입니다.
  기존 `Assets/Art/VFX/HitImpactVFX/00_Prefab/0_Classic/VFX_Classic_01.prefab`을 연결했습니다.
  원본 에셋은 수정하지 않았습니다.

## 현재 연결

`Bullet.OnTriggerEnter`에서 살아 있는 `EnemyBase`에 양수 피해를 적용하기 직전에 호출합니다.
죽이는 공격에도 한 번 재생하며 플레이어, 벽, 이미 죽은 적에는 재생하지 않습니다.
기존 피해 처리와 적 판별 범위는 유지합니다. 근접 공격이나 직접 `TakeDamage` 호출에는
자동 연결되지 않습니다. 자식 콜라이더 피격이 기존에 피해를 주지 않았다면 이 기능도 동일합니다.

트리거 콜라이더는 정확한 접촉 정보를 제공하지 않으므로 `ClosestPoint`와 총알 반대 방향을
사용합니다. 총알이 콜라이더 안으로 들어온 경우 위치는 표면보다 안쪽일 수 있습니다.

## 연출 변경

1. Project 창에서 이 폴더의 `Resources/VFX/VfxConfig.asset`을 선택합니다.
2. Entries의 Enemy Hit에서 Prefab, Scale, Lifetime, Surface Offset, Rotation Offset을 조정합니다.
3. 총알로 살아 있는 적을 맞혀 크기와 방향, 재생 종료를 확인합니다.

Config는 플레이 시작 전에 수정합니다. 자동 로드용 Config를 같은 Resources 경로로 중복 생성하지 않습니다.
프리팹은 활성 상태로 저장하고, 전용 연출 스크립트는 해당 프리팹에 붙입니다.
파티클은 자동으로 재생하며, 커스텀 스크립트는 Awake/OnEnable에서 시작하도록 구성합니다.
Lifetime은 가장 긴 하위 파티클의 지연 + 방출 시간 + 입자 수명보다 길게 설정합니다.
반복/비파티클 연출도 Lifetime 뒤에 제거됩니다. 제거 예약은 게임 시간에 따릅니다.

## 다른 곳에서 호출

```csharp
using PesonShooting.VFX;

// 지정 위치에서 재생합니다. rotation의 +Z가 표면 바깥을 향하도록 전달합니다.
VfxService.Play(VfxId.EnemyHit, hitPosition, hitRotation);
```

새 상황은 VfxId 뒤에 값을 추가하고 Config의 Entries에도 같은 ID와 프리팹을 등록합니다.
씬에 매니저를 미리 넣을 필요는 없지만, 재생 중인 VFX 오브젝트는 런타임 하이어라키에 생성됩니다.
현재는 매번 생성/제거하는 방식이며 풀링, 사운드, 카메라 흔들림은 추가하지 않았습니다.
