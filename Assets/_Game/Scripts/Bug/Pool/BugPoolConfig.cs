using System.Collections.Generic;
using UnityEngine;
using DrillCorp.Data;

namespace DrillCorp.Bug.Pool
{
    [CreateAssetMenu(fileName = "BugPoolConfig", menuName = "Drill-Corp/Bug Pool Config", order = 10)]
    public class BugPoolConfig : ScriptableObject
    {
        [System.Serializable]
        public class PoolEntry
        {
            [Tooltip("풀링할 BugData")]
            public BugData BugData;

            [Tooltip("초기 생성 수량")]
            [Range(10, 500)]
            public int InitialSize = 50;

            [Tooltip("필요 시 자동 확장 허용")]
            public bool AllowGrow = true;
        }

        [Header("Pool Entries")]
        [Tooltip("각 BugData별 풀 설정")]
        [SerializeField] private List<PoolEntry> _entries = new List<PoolEntry>();

        [Header("Global")]
        [Tooltip("전체 최대 동시 활성 수")]
        [Range(100, 2000)]
        [SerializeField] private int _maxActiveTotal = 1000;

        public IReadOnlyList<PoolEntry> Entries => _entries;
        public int MaxActiveTotal => _maxActiveTotal;
    }
}
