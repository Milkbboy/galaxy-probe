using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DrillCorp.Aim;

namespace DrillCorp.Weapon.LockOn
{
    /// <summary>
    /// 락온 - 쿨다운 동안 에임 범위 내 Bug에 마커 표시 → 일괄 피해
    /// 발사 조건 (둘 중 하나):
    ///   1. FireDelay 경과
    ///   2. 마커 수가 MaxTargets 도달 (조기 발사)
    /// </summary>
    public class LockOnWeapon : WeaponBase
    {
        [Header("Data")]
        [SerializeField] private LockOnData _data;

        [Header("Marker")]
        [Tooltip("락온 마커 프리펩 (LockOnMarker 컴포넌트 필요)")]
        [SerializeField] private GameObject _markerPrefab;

        [Header("Imminent Fire")]
        [Tooltip("발사 직전 몇 초 동안 마커를 빠르게 깜빡일지")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _imminentDuration = 0.5f;

        private readonly Dictionary<Transform, LockOnMarker> _activeMarkers = new Dictionary<Transform, LockOnMarker>();
        private bool _isLocking;
        private bool _imminentSet;
        private float _nextMarkerTime;

        public int CurrentLockCount => _activeMarkers.Count;
        public int MaxLockCount => _data != null ? _data.MaxTargets : 0;

        private void Awake()
        {
            _baseData = _data;
        }

        public override void OnEquip(AimController aim)
        {
            base.OnEquip(aim);
            _isLocking = true;
            _imminentSet = false;
            _nextFireTime = Time.time + (_data != null ? _data.FireDelay : 0f);
            _nextMarkerTime = 0f;
        }

        public override void OnUnequip()
        {
            base.OnUnequip();
            ClearAllMarkers();
            _isLocking = false;
            if (_aim != null)
                _aim.SetInfoText(null);
        }

        private void UpdateInfoText(AimController aim)
        {
            int current = _activeMarkers.Count;
            int max = _data.MaxTargets;
            aim.SetInfoText($"{current}/{max}");
        }

        public override void TryFire(AimController aim)
        {
            _aim = aim;
            if (aim == null || _data == null) return;

            if (_isLocking)
            {
                UpdateLockingMarkers(aim);
                UpdateImminentState();
            }

            UpdateInfoText(aim);

            bool timeExpired = CanFire;
            bool maxReached = _activeMarkers.Count >= _data.MaxTargets;

            if (!timeExpired && !maxReached) return;

            if (_isLocking)
                ExecuteLockOn();

            StartNewLockCycle();
        }

        protected override void Fire(AimController aim) { /* TryFire에서 직접 처리 */ }

        private void StartNewLockCycle()
        {
            _nextFireTime = Time.time + _data.FireDelay;
            _isLocking = true;
            _imminentSet = false;
            _nextMarkerTime = 0f;
        }

        private void UpdateLockingMarkers(AimController aim)
        {
            RemoveDeadMarkers();

            if (_activeMarkers.Count >= _data.MaxTargets) return;

            // 간격 체크 (간격이 0이면 매 프레임 1개씩 제한 없이 생성)
            float interval = _data.MarkerSpawnInterval;
            if (interval > 0f && Time.time < _nextMarkerTime) return;

            Transform nextTarget = FindNextLockTarget(aim);
            if (nextTarget == null) return;

            SpawnMarker(nextTarget);
            _nextMarkerTime = Time.time + interval;
        }

        /// <summary>
        /// 아직 락온 안 된 Bug 중 에임 중심에 가장 가까운 타겟 반환
        /// </summary>
        private Transform FindNextLockTarget(AimController aim)
        {
            Vector3 aimPos = aim.AimPosition;
            Transform best = null;
            float bestDist = float.MaxValue;

            var bugs = aim.BugsInRange;
            for (int i = 0; i < bugs.Count; i++)
            {
                var c = bugs[i];
                if (c == null) continue;
                var t = c.transform;

                if (_activeMarkers.ContainsKey(t)) continue;

                Vector3 diff = t.position - aimPos;
                diff.y = 0f;
                float d = diff.sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = t;
                }
            }

            return best;
        }

        /// <summary>
        /// 발사 직전 {_imminentDuration}초 진입 시 마커 전부 깜빡임 모드
        /// </summary>
        private void UpdateImminentState()
        {
            float remaining = _nextFireTime - Time.time;
            bool imminent = remaining <= _imminentDuration;

            if (imminent && !_imminentSet)
            {
                SetAllMarkersImminent(true);
                _imminentSet = true;
            }
            else if (!imminent && _imminentSet)
            {
                SetAllMarkersImminent(false);
                _imminentSet = false;
            }
        }

        private void SetAllMarkersImminent(bool value)
        {
            foreach (var kv in _activeMarkers)
            {
                if (kv.Value != null)
                    kv.Value.SetImminent(value);
            }
        }

        private void SpawnMarker(Transform target)
        {
            if (_markerPrefab == null)
            {
                _activeMarkers[target] = null;
                return;
            }

            var markerObj = Instantiate(_markerPrefab, target.position, _markerPrefab.transform.rotation);
            var marker = markerObj.GetComponent<LockOnMarker>();
            if (marker == null) marker = markerObj.AddComponent<LockOnMarker>();
            marker.SetTarget(target);
            if (_imminentSet) marker.SetImminent(true);
            _activeMarkers[target] = marker;
        }

        private void RemoveDeadMarkers()
        {
            var toRemove = new List<Transform>();
            foreach (var kv in _activeMarkers)
            {
                if (kv.Key == null || !kv.Key.gameObject.activeInHierarchy)
                    toRemove.Add(kv.Key);
            }
            foreach (var t in toRemove)
            {
                if (_activeMarkers.TryGetValue(t, out var marker) && marker != null)
                    Destroy(marker.gameObject);
                _activeMarkers.Remove(t);
            }
        }

        private void ExecuteLockOn()
        {
            var targets = new List<Transform>();
            foreach (var kv in _activeMarkers)
            {
                if (kv.Key != null && kv.Key.gameObject.activeInHierarchy)
                    targets.Add(kv.Key);
            }

            StartCoroutine(HitSequence(targets));
            ClearAllMarkers();
        }

        private IEnumerator HitSequence(List<Transform> targets)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                var t = targets[i];
                if (t != null && t.gameObject.activeInHierarchy)
                {
                    DealDamage(t, _data.Damage);
                }

                if (_data.HitInterval > 0f)
                    yield return new WaitForSeconds(_data.HitInterval);
            }
        }

        private void ClearAllMarkers()
        {
            foreach (var kv in _activeMarkers)
            {
                if (kv.Value != null)
                    Destroy(kv.Value.gameObject);
            }
            _activeMarkers.Clear();
        }
    }
}
