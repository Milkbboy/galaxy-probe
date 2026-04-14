using System.Collections.Generic;
using UnityEngine;

namespace DrillCorp.Bug
{
    /// <summary>
    /// 다수 Bug의 Update를 여러 프레임에 분산 처리
    /// BugController가 등록되면 BugsPerFrame 만큼씩 순차적으로 Tick 호출
    /// </summary>
    public class BugManager : MonoBehaviour
    {
        public static BugManager Instance { get; private set; }

        [Header("Distribution")]
        [Tooltip("한 프레임에 Tick 처리할 Bug 수")]
        [Range(10, 500)]
        [SerializeField] private int _bugsPerFrame = 80;

        [Header("Culling")]
        [Tooltip("카메라 밖 Bug는 Tick 빈도를 낮춤")]
        [SerializeField] private bool _reduceOffscreenTick = true;

        [Tooltip("카메라 밖 Bug의 Tick 스킵 비율 (0.5 = 절반만 처리)")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _offscreenTickRatio = 0.5f;

        private readonly List<IBugTick> _tickables = new List<IBugTick>();
        private int _nextIndex;
        private int _offscreenCounter;

        public int RegisteredCount => _tickables.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Register(IBugTick tickable)
        {
            if (tickable == null || _tickables.Contains(tickable))
                return;
            _tickables.Add(tickable);
        }

        public void Unregister(IBugTick tickable)
        {
            if (tickable == null)
                return;
            _tickables.Remove(tickable);
        }

        private void Update()
        {
            int count = _tickables.Count;
            if (count == 0)
                return;

            int processed = 0;
            int limit = Mathf.Min(_bugsPerFrame, count);
            float deltaTime = Time.deltaTime * Mathf.Max(1, count / Mathf.Max(1, _bugsPerFrame));

            while (processed < limit)
            {
                if (_nextIndex >= _tickables.Count)
                    _nextIndex = 0;

                var t = _tickables[_nextIndex];

                if (t == null)
                {
                    _tickables.RemoveAt(_nextIndex);
                    continue;
                }

                if (_reduceOffscreenTick && !t.IsVisibleToCamera)
                {
                    _offscreenCounter++;
                    if (_offscreenCounter < Mathf.RoundToInt(1f / _offscreenTickRatio))
                    {
                        _nextIndex++;
                        processed++;
                        continue;
                    }
                    _offscreenCounter = 0;
                }

                t.ManagedTick(deltaTime);

                _nextIndex++;
                processed++;
            }
        }
    }

    /// <summary>
    /// BugManager에 등록 가능한 Tick 인터페이스
    /// </summary>
    public interface IBugTick
    {
        bool IsVisibleToCamera { get; }
        void ManagedTick(float deltaTime);
    }
}
