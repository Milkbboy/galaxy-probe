using UnityEngine;

namespace DrillCorp.Data
{
    /// <summary>
    /// 거미 보스 튜닝 데이터 (시트 'BossData' 1행 ↔ Boss_Spider.asset).
    /// SpiderBoss 인스펙터의 [SerializeField] 들이 옮겨와 기획자가 시트에서 조정 가능.
    /// 근거: docs/Sys-Boss.md, docs/V2-prototype.html line 715~880
    /// </summary>
    [CreateAssetMenu(fileName = "Boss_", menuName = "Drill-Corp/Boss Data")]
    public class BossData : ScriptableObject
    {
        [Header("Identity")]
        public string BossId = "spider";
        public string DisplayName = "거미 보스";

        [Header("Stats")]
        [Tooltip("최대 HP. v2: BOSS_HP_BASE=500")]
        public float MaxHp = 500f;
        [Tooltip("머신 접촉 시 초당 피해. v2: bug.atk * 60")]
        public float ContactDamagePerSecond = 30f;
        [Tooltip("접촉 피해 적용 반경. v2: sz+8")]
        public float ContactRange = 1.2f;

        [Header("Spawn Trigger")]
        [Tooltip("세션 누적 처치 점수가 이 값 이상이면 보스 등장. v2: BOSS_KILL_THRESHOLD=700")]
        public float KillThreshold = 250f;

        [Header("Movement — 자연스러운 행동 사이클")]
        [Tooltip("perch 도달 후 주위를 어슬렁거리는 시간(초). 0 이면 바로 정지.")]
        [Min(0f)] public float WalkDuration = 1.5f;
        [Tooltip("perch 중심에서 walk 가능한 최대 반경.")]
        [Min(0f)] public float WalkRadius = 2.5f;
        [Tooltip("walk 속도 (유닛/초).")]
        [Min(0f)] public float WalkSpeed = 2.0f;
        [Tooltip("walk 끝난 후 다음 점프까지 대기 시간(초).")]
        [Min(0f)] public float IdleDuration = 2.0f;
        [Tooltip("perch 도착 위치에 추가되는 랜덤 jitter 반경.")]
        [Min(0f)] public float PerchJitter = 1.5f;
        [Tooltip("점프 한 번의 최소 비행 시간(초). v2 기본 0.667초.")]
        [Min(0.1f)] public float JumpDurationMin = 0.5f;
        [Tooltip("점프 한 번의 최대 비행 시간(초). Min ≤ Max 보장.")]
        [Min(0.1f)] public float JumpDurationMax = 1.0f;

        [Header("Attack — 착지 후 새끼 소환")]
        [Tooltip("공격 모션 전체 시간(초). Animator Attack state default speed=1 기준.")]
        [Min(0.1f)] public float AttackDuration = 2.0f;
        [Tooltip("공격 모션 중 어느 시점에 새끼를 소환할지 (0~1).")]
        [Range(0f, 1f)] public float AttackSpawnFraction = 0.5f;

        [Header("Boss Children")]
        [Tooltip("착지마다 소환되는 새끼 거미 수.")]
        [Min(0)] public int ChildCountPerLanding = 3;
        [Tooltip("새끼 거미 스폰 위치 jitter 반경.")]
        [Min(0f)] public float ChildSpawnJitter = 1.5f;

        [Header("Telegraph — 인터럽트 가능 압박 패턴")]
        [Tooltip("정상 사이클 N번 완료마다 텔레그래프 발동 (0=비활성).")]
        [Min(0)] public int TelegraphCooldownCycles = 2;
        [Tooltip("텔레그래프 지속 시간 — 인터럽트 못 채우면 Pounce 발동.")]
        [Min(0.1f)] public float TelegraphDuration = 2f;
        [Tooltip("텔레그래프 인터럽트에 필요한 명중 수.")]
        [Min(1)] public int InterruptHitsRequired = 8;
        [Tooltip("Pounce 시 가까운 perch 반경 비율 (0.5 = 머신에서 절반 거리).")]
        [Range(0.1f, 1f)] public float PounceRadiusMultiplier = 0.5f;
        [Tooltip("Pounce 착지 시 머신에 가하는 임팩트 데미지.")]
        [Min(0f)] public float PounceImpactDamage = 50f;
        [Tooltip("인터럽트 성공 시 거미가 잠깐 흠칫하는 시간.")]
        [Min(0f)] public float FlinchDuration = 0.6f;
        [Tooltip("텔레그래프 시 스케일 펄스 진폭 (0.1 = 10%).")]
        [Min(0f)] public float TelegraphScalePulse = 0.1f;
        [Tooltip("스케일 펄스 주기 (Hz).")]
        [Min(0.1f)] public float TelegraphPulseFreq = 4f;

        [Header("HP Bar Visual")]
        [Tooltip("HP 바 25% 임계 — 이하에서 빨강으로 전환.")]
        [Range(0f, 1f)] public float HpBarLowThreshold = 0.25f;
    }
}
