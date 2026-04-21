using System.Collections.Generic;
using UnityEngine;
using DrillCorp.Data;

namespace DrillCorp.Ability.Runners
{
    /// <summary>
    /// 지누스 1번 어빌리티 — 드론 포탑.
    /// 마우스 위치에 <see cref="DroneInstance"/> 프리펩을 배치. 최대 <see cref="AbilityData.MaxInstances"/> 기 동시 존재.
    ///
    /// v2.html:1057~1060 (useItem 'drone') 포팅. 타겟팅·발사·HP·파괴 로직은 DroneInstance 안에 있음.
    ///
    /// 쿨다운: TryUse 성공 시에만 시작 (배치 풀 초과로 실패 시 쿨다운 소모 X — v2 와 동일).
    /// </summary>
    public class DroneRunner : IAbilityRunner
    {
        public AbilityType Type => AbilityType.Drone;

        private AbilityData _data;
        private AbilityContext _ctx;
        private float _cooldown;

        private readonly List<DroneInstance> _drones = new List<DroneInstance>();

        public float CooldownNormalized =>
            (_data == null || _data.CooldownSec <= 0f)
                ? 0f
                : Mathf.Clamp01(_cooldown / _data.CooldownSec);

        public void Initialize(AbilityData data, AbilityContext ctx)
        {
            _data = data;
            _ctx = ctx;
            _cooldown = 0f;
        }

        public void Tick(float dt)
        {
            if (_cooldown > 0f) _cooldown = Mathf.Max(0f, _cooldown - dt);

            // 파괴된 드론(Destroy 후 null 로 바뀜) 정리
            for (int i = _drones.Count - 1; i >= 0; i--)
            {
                if (_drones[i] == null) _drones.RemoveAt(i);
            }
        }

        public bool TryUse(Vector3 aimPoint)
        {
            if (_cooldown > 0f) return false;
            if (_data == null || _ctx == null) return false;
            if (_data.VfxPrefab == null)
            {
                Debug.LogWarning("[DroneRunner] AbilityData.VfxPrefab(드론 프리펩) 이 비어있습니다. " +
                                 "Ability_Jinus_Drone.asset 의 _vfxPrefab 슬롯에 DroneInstance 프리펩 바인딩 필요. " +
                                 "Tools/Drill-Corp/3. 게임 초기 설정/10. 지누스 드론 프리펩 생성 메뉴 실행 권장.");
                return false;
            }
            if (_drones.Count >= Mathf.Max(1, _data.MaxInstances)) return false;

            Vector3 pos = aimPoint;
            pos.y = 0f;

            var go = Object.Instantiate(_data.VfxPrefab, pos, Quaternion.identity, _ctx.VfxParent);
            var drone = go.GetComponent<DroneInstance>();
            if (drone == null)
            {
                Debug.LogError("[DroneRunner] VfxPrefab 에 DroneInstance 컴포넌트가 없습니다. 드론 프리펩 바인딩 확인.");
                Object.Destroy(go);
                return false;
            }

            drone.Initialize(_data, _ctx.BugLayer);
            drone.OnDestroyed += () => _drones.Remove(drone);
            _drones.Add(drone);

            _cooldown = _data.CooldownSec;
            return true;
        }
    }
}
