using System.Collections.Generic;
using UnityEngine;
using DrillCorp.Data;

namespace DrillCorp.Ability.Runners
{
    /// <summary>
    /// 지누스 3번 어빌리티 — 드론 거미 (AutoInterval).
    /// 키 입력이 아니라 Tick 에서 <see cref="AbilityData.AutoIntervalSec"/> 주기마다 자동 발동.
    /// 머신 주변 ±SpawnSpread 에 <see cref="SpiderDroneInstance"/> 1기 스폰 (최대 MaxInstances 기).
    ///
    /// v2.html:1156~1164 (자동 소환) 포팅. 거미 Instance 의 tick 로직은 SpiderDroneInstance 안.
    ///
    /// TryUse 는 항상 false — 1/2/3 키 입력에 반응하지 않음 (AutoInterval 의 의미).
    /// 쿨다운 표시는 CooldownNormalized 로 AutoIntervalSec 대비 남은 시간 비율을 돌려준다.
    /// </summary>
    public class SpiderDroneRunner : IAbilityRunner
    {
        public AbilityType Type => AbilityType.SpiderDrone;

        // 머신 주위 스폰 오프셋 (v2 원본 ±20pix ÷ 10 × 2 = ±2 유닛 → 너비 4).
        private const float SpawnSpread = 4f;

        private AbilityData _data;
        private AbilityContext _ctx;
        private float _autoTimer;

        private readonly List<SpiderDroneInstance> _spiders = new List<SpiderDroneInstance>();

        /// <summary>
        /// AutoInterval 남은 시간 대비 비율 (0 = 방금 발동 직후 / 1 = 다음 발동 직전).
        /// UI 슬롯 "쿨다운" 표시에 그대로 쓸 수 있도록 v2 drawItemUI 의 `max - autoTimer` 패턴 포팅.
        /// </summary>
        public float CooldownNormalized
        {
            get
            {
                if (_data == null || _data.AutoIntervalSec <= 0f) return 0f;
                float remain = _data.AutoIntervalSec - _autoTimer;
                return Mathf.Clamp01(remain / _data.AutoIntervalSec);
            }
        }

        public void Initialize(AbilityData data, AbilityContext ctx)
        {
            _data = data;
            _ctx = ctx;
            _autoTimer = 0f;
        }

        public void Tick(float dt)
        {
            // null 된 항목 정리 (HP 0 또는 씬 종료)
            for (int i = _spiders.Count - 1; i >= 0; i--)
            {
                if (_spiders[i] == null) _spiders.RemoveAt(i);
            }

            if (_data == null || _ctx == null) return;
            if (_data.AutoIntervalSec <= 0f) return;

            _autoTimer += dt;
            if (_autoTimer < _data.AutoIntervalSec) return;

            int max = Mathf.Max(1, _data.MaxInstances);
            if (_spiders.Count >= max)
            {
                // 상한 도달 — 타이머를 꽉 차게 유지해서 상한이 풀리자마자 바로 소환.
                _autoTimer = _data.AutoIntervalSec;
                return;
            }

            if (TrySpawn())
                _autoTimer = 0f;
            else
                _autoTimer = _data.AutoIntervalSec; // VfxPrefab 없는 등의 실패 — 재시도 대기
        }

        /// <summary>AutoInterval 이므로 키 입력은 무시.</summary>
        public bool TryUse(Vector3 aimPoint) => false;

        private bool TrySpawn()
        {
            if (_data.VfxPrefab == null)
            {
                Debug.LogWarning("[SpiderDroneRunner] AbilityData.VfxPrefab 이 비어있습니다. " +
                                 "Ability_Jinus_SpiderDrone.asset 의 _vfxPrefab 슬롯에 SpiderDroneInstance 프리펩 바인딩 필요. " +
                                 "Tools/Drill-Corp/3. 게임 초기 설정/10. 지누스 드론 프리펩 생성 메뉴 실행 권장.");
                return false;
            }

            // 머신 위치 기준 ±SpawnSpread/2 랜덤 오프셋 (v2 `CX ± 20, CY ± 20`).
            Transform machineT = _ctx.MachineTransform;
            Vector3 center = machineT != null ? machineT.position : Vector3.zero;
            center.y = 0f;

            float half = SpawnSpread * 0.5f;
            Vector3 pos = new Vector3(
                center.x + Random.Range(-half, half),
                0f,
                center.z + Random.Range(-half, half));

            var go = Object.Instantiate(_data.VfxPrefab, pos, Quaternion.identity, _ctx.VfxParent);
            var spider = go.GetComponent<SpiderDroneInstance>();
            if (spider == null)
            {
                Debug.LogError("[SpiderDroneRunner] VfxPrefab 에 SpiderDroneInstance 컴포넌트가 없습니다.");
                Object.Destroy(go);
                return false;
            }

            spider.Initialize(_data, machineT, _ctx.BugLayer);
            spider.OnDestroyed += () => _spiders.Remove(spider);
            _spiders.Add(spider);
            return true;
        }
    }
}
