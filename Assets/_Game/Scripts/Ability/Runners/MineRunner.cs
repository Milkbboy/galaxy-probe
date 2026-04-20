using System.Collections.Generic;
using UnityEngine;
using DrillCorp.Data;

namespace DrillCorp.Ability.Runners
{
    /// <summary>
    /// 빅터 3번 어빌리티 — 폭발 지뢰.
    /// 마우스 위치에 지뢰 프리펩(MineInstance)을 배치. 최대 <see cref="AbilityData.MaxInstances"/>개 동시 존재.
    ///
    /// v2.html:1092~1099(생성) + 1243~1263(탐지/폭발) 포팅. 실제 폭발 로직은 MineInstance.Update 안에.
    ///
    /// 지뢰 프리펩은 AbilityData.VfxPrefab 슬롯에 바인딩 — Runner는 그대로 Instantiate.
    ///
    /// 스폰 루트:
    ///   · 마우스 지면 위치 (aim.AimPosition, Y=0 보정)
    ///   · BombWeapon / BugLayer / AbilityData는 MineInstance.Initialize 로 전달 — 폭발 스탯 조회용
    ///
    /// 쿨다운: TryUse 성공 시에만 시작 (배치 풀 초과로 실패 시 쿨다운 소모 X — v2와 동일).
    /// </summary>
    public class MineRunner : IAbilityRunner
    {
        public AbilityType Type => AbilityType.Mine;

        private AbilityData _data;
        private AbilityContext _ctx;
        private float _cooldown;

        private readonly List<MineInstance> _mines = new List<MineInstance>();

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

            // 혹시 외부 요인(씬 종료 전)으로 null 이 섞였다면 정리.
            for (int i = _mines.Count - 1; i >= 0; i--)
            {
                if (_mines[i] == null) _mines.RemoveAt(i);
            }
        }

        public bool TryUse(Vector3 aimPoint)
        {
            if (_cooldown > 0f) return false;
            if (_data == null || _ctx == null) return false;
            if (_data.VfxPrefab == null)
            {
                Debug.LogWarning("[MineRunner] AbilityData.VfxPrefab(지뢰 프리펩) 이 비어있습니다. " +
                                 "Ability_Victor_Mine.asset 의 _vfxPrefab 슬롯에 MineInstance 프리펩 바인딩 필요.");
                return false;
            }
            if (_mines.Count >= Mathf.Max(1, _data.MaxInstances)) return false;

            Vector3 pos = aimPoint;
            pos.y = 0f;

            var go = Object.Instantiate(_data.VfxPrefab, pos, Quaternion.identity, _ctx.VfxParent);
            var mine = go.GetComponent<MineInstance>();
            if (mine == null)
            {
                Debug.LogError("[MineRunner] VfxPrefab 에 MineInstance 컴포넌트가 없습니다. 지뢰 프리펩이 잘못 바인딩됨.");
                Object.Destroy(go);
                return false;
            }

            mine.Initialize(_ctx.BombWeapon, _data, _ctx.BugLayer);
            mine.OnDestroyed += () => _mines.Remove(mine);
            _mines.Add(mine);

            _cooldown = _data.CooldownSec;
            return true;
        }
    }
}
