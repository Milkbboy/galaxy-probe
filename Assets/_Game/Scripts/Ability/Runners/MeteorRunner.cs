using UnityEngine;
using DrillCorp.Data;

namespace DrillCorp.Ability.Runners
{
    /// <summary>
    /// 사라 3번 어빌리티 — 반중력 메테오.
    /// AutoInterval 타입 — AutoIntervalSec (기본 10s) 마다 랜덤 위치에 운석 낙하.
    /// 사용자 입력(TryUse) 무시.
    ///
    /// v2.html:1146~1184 포팅. 낙하/착지/화염지대 생성 로직은 <see cref="MeteorInstance"/> 에.
    ///
    /// 단위 해석:
    ///   · AbilityData.AutoIntervalSec = 자동 발동 주기 (10s)
    ///   · AbilityData.Range           = 화염 지대 반경 (5.5)
    ///   · AbilityData.DurationSec     = 화염 지대 지속 (15s)
    ///   · AbilityData.Damage          = 0.1s 틱당 데미지 (0.5)
    ///   · AbilityData.VfxPrefab       = 낙하체 프리펩 (MeteorInstance 컴포넌트 포함) — 필수.
    ///                                   에디터 메뉴 MeteorPrefabCreator 로 자동 생성.
    ///
    /// 랜덤 위치: 머신 중심 기준 SpawnRadiusMin ~ SpawnRadiusMax 링 안 임의 좌표.
    /// CooldownNormalized: 다음 발동까지 남은 비율 = (1 - autoTimer/autoIntervalSec).
    ///
    /// 에디터 라이브 튜닝:
    ///   AbilityData.OnValidated 를 구독해 SO 값 변경 시 다음 스폰부터 반영.
    ///   이미 낙하 중/지대 생성된 인스턴스는 스폰 시점 값 그대로(안 건드림).
    /// </summary>
    public class MeteorRunner : IAbilityRunner
    {
        public AbilityType Type => AbilityType.Meteor;

        // ─── 튜닝 상수 ───
        // v2 12/frame × 60 = 720/sec (픽셀) → Unity 스케일(÷10) = 72.
        // 체감 7.2 유닛/초는 2초에 유닛 14 — 높이에서 내려오는 느낌 연출에 너무 빠름 → 12 로 약간 상향.
        private const float FallSpeed = 12f;
        // 낙하 시작 높이 (targetPos.y + FallHeight 에서 떨어짐).
        private const float FallHeight = 18f;
        // 낙하 시작점의 XZ 오프셋 — 탑뷰에서 "비스듬히 떨어지는" 궤적을 위함.
        // targetPos 에서 뒤쪽(반대 방향) 으로 이 거리만큼 떨어진 곳에서 시작 → 대각선 궤적.
        // FallHeight 와 비슷한 크기면 약 45°, 작으면 더 수직, 크면 더 수평.
        private const float FallHorizontalOffset = 14f;
        // 머신 기준 스폰 반경 링 — MinRadius 는 머신 위를 피하고, MaxRadius 는 카메라 뷰 안쪽.
        private const float SpawnRadiusMin = 3f;
        private const float SpawnRadiusMax = 15f;
        // 화염 지대 틱 주기 (v2 6frame = 0.1s)
        private const float FireZoneTickInterval = 0.1f;

        private AbilityData _data;
        private AbilityContext _ctx;
        private float _autoTimer;

        public float CooldownNormalized =>
            (_data == null || _data.AutoIntervalSec <= 0f)
                ? 0f
                : Mathf.Clamp01(1f - _autoTimer / _data.AutoIntervalSec);

        public void Initialize(AbilityData data, AbilityContext ctx)
        {
            _data = data;
            _ctx = ctx;
            _autoTimer = 0f; // v2 원본 유지 — 게임 시작 후 AutoIntervalSec 대기 후 첫 발

#if UNITY_EDITOR
            if (_data != null)
            {
                _data.OnValidated -= OnDataValidated;
                _data.OnValidated += OnDataValidated;
            }
#endif
        }

        public void Tick(float dt)
        {
            if (_data == null || _data.AutoIntervalSec <= 0f) return;

            _autoTimer += dt;
            if (_autoTimer >= _data.AutoIntervalSec)
            {
                _autoTimer = 0f;
                SpawnMeteor();
            }
        }

        /// <summary>AutoInterval 타입 — 수동 발동 무시.</summary>
        public bool TryUse(Vector3 aimPoint) => false;

#if UNITY_EDITOR
        private void OnDataValidated()
        {
            // 활성 인스턴스는 spawn 시점 값을 유지 — 다음 스폰부터 새 값 반영.
            // Runner 자체는 SO 값 직접 읽으므로 별도 갱신 없음.
        }
#endif

        private void SpawnMeteor()
        {
            if (_ctx == null || _ctx.MachineTransform == null) return;
            if (_data == null || _data.VfxPrefab == null)
            {
                Debug.LogWarning("[MeteorRunner] AbilityData.VfxPrefab(낙하체 프리펩) 이 비어있습니다. " +
                                 "Ability_Sara_Meteor.asset 의 _vfxPrefab 슬롯에 MeteorInstance 프리펩 바인딩 필요.");
                return;
            }

            Vector3 machinePos = _ctx.MachineTransform.position;
            machinePos.y = 0f;

            // 머신 중심 링 랜덤 좌표 (XZ 평면).
            float angle = Random.value * Mathf.PI * 2f;
            float radius = Random.Range(SpawnRadiusMin, SpawnRadiusMax);
            Vector3 targetPos = machinePos + new Vector3(
                Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);

            // 비스듬한 낙하 궤적 — XZ 랜덤 방향으로 FallHorizontalOffset 만큼 밀린 위치에서 시작.
            // 방향은 각 메테오마다 독립적으로 랜덤화해 시각적 변주.
            float horizAngle = Random.value * Mathf.PI * 2f;
            Vector3 horizOffset = new Vector3(
                Mathf.Sin(horizAngle) * FallHorizontalOffset, 0f,
                Mathf.Cos(horizAngle) * FallHorizontalOffset);
            Vector3 spawnPos = targetPos + horizOffset + Vector3.up * FallHeight;

            var go = Object.Instantiate(_data.VfxPrefab, spawnPos, Quaternion.identity, _ctx.VfxParent);
            var meteor = go.GetComponent<MeteorInstance>();
            if (meteor == null)
            {
                Debug.LogError("[MeteorRunner] VfxPrefab 에 MeteorInstance 컴포넌트가 없습니다. " +
                               "MeteorPrefabCreator 로 낙하체 프리펩 생성이 필요합니다.");
                Object.Destroy(go);
                return;
            }

            meteor.Initialize(
                startPos: spawnPos,
                targetPos: targetPos,
                fallSpeed: FallSpeed,
                fireZoneRadius: _data.Range,
                fireZoneDuration: _data.DurationSec,
                fireZoneTickDamage: _data.Damage,
                fireZoneTickInterval: FireZoneTickInterval,
                bugLayer: _ctx.BugLayer,
                vfxParent: _ctx.VfxParent);
        }
    }
}
