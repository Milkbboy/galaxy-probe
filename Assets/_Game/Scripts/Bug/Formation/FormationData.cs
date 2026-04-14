using System.Collections.Generic;
using UnityEngine;
using DrillCorp.Data;

namespace DrillCorp.Bug.Formation
{
    public enum FormationType
    {
        Cluster,    // 원형 뭉텅이
        Line,       // 일렬 종대 (돌파형)
        Swarm,      // 느슨한 군집
    }

    public enum FormationSize
    {
        Small,      // 15~35
        Medium,     // 40~90
        Large,      // 120~200
    }

    [CreateAssetMenu(fileName = "Formation_New", menuName = "Drill-Corp/Formation Data", order = 11)]
    public class FormationData : ScriptableObject
    {
        [System.Serializable]
        public class MemberEntry
        {
            [Tooltip("멤버 BugData")]
            public BugData BugData;

            [Tooltip("전체 멤버 중 비율 (0~1)")]
            [Range(0f, 1f)]
            public float Ratio = 1f;
        }

        [Header("기본")]
        [SerializeField] private string _displayName = "Formation";
        [SerializeField] private FormationType _formationType = FormationType.Cluster;
        [SerializeField] private FormationSize _formationSize = FormationSize.Medium;

        [Header("멤버 수량")]
        [Tooltip("최소 멤버 수 (리더 제외)")]
        [Range(5, 200)]
        [SerializeField] private int _minMembers = 20;

        [Tooltip("최대 멤버 수 (리더 제외)")]
        [Range(5, 200)]
        [SerializeField] private int _maxMembers = 40;

        [Header("진형 형태")]
        [Tooltip("멤버 간 간격")]
        [Range(0.3f, 3f)]
        [SerializeField] private float _spacing = 0.8f;

        [Tooltip("진형 반경/길이 (Cluster=반경, Line=길이, Swarm=반경)")]
        [Range(1f, 20f)]
        [SerializeField] private float _formationRadius = 4f;

        [Tooltip("랜덤 흔들림 세기 (Swarm용)")]
        [Range(0f, 2f)]
        [SerializeField] private float _jitter = 0.3f;

        [Header("리더")]
        [Tooltip("리더로 사용할 BugData (비우면 첫 멤버 자동 선정)")]
        [SerializeField] private BugData _leaderBugData;

        [Header("멤버 구성")]
        [Tooltip("멤버 BugData 풀 + 비율")]
        [SerializeField] private List<MemberEntry> _members = new List<MemberEntry>();

        [Header("이동")]
        [Tooltip("Formation 전체 이동 속도 배율")]
        [Range(0.3f, 2f)]
        [SerializeField] private float _speedMultiplier = 1f;

        public string DisplayName => _displayName;
        public FormationType FormationType => _formationType;
        public FormationSize FormationSize => _formationSize;
        public int MinMembers => _minMembers;
        public int MaxMembers => _maxMembers;
        public float Spacing => _spacing;
        public float FormationRadius => _formationRadius;
        public float Jitter => _jitter;
        public BugData LeaderBugData => _leaderBugData;
        public IReadOnlyList<MemberEntry> Members => _members;
        public float SpeedMultiplier => _speedMultiplier;

        /// <summary>
        /// 랜덤 멤버 수량 반환
        /// </summary>
        public int GetRandomMemberCount()
        {
            return Random.Range(_minMembers, _maxMembers + 1);
        }

        /// <summary>
        /// 비율 기반 랜덤 멤버 BugData 반환
        /// </summary>
        public BugData PickRandomMember()
        {
            if (_members == null || _members.Count == 0)
                return null;

            float totalRatio = 0f;
            for (int i = 0; i < _members.Count; i++)
            {
                if (_members[i].BugData != null)
                    totalRatio += _members[i].Ratio;
            }

            if (totalRatio <= 0f)
                return null;

            float roll = Random.Range(0f, totalRatio);
            float acc = 0f;
            for (int i = 0; i < _members.Count; i++)
            {
                if (_members[i].BugData == null)
                    continue;

                acc += _members[i].Ratio;
                if (roll <= acc)
                    return _members[i].BugData;
            }

            return _members[0].BugData;
        }
    }
}
