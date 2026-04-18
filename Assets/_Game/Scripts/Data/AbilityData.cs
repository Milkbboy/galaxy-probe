using UnityEngine;

namespace DrillCorp.Data
{
    /// <summary>
    /// 캐릭터 고유 어빌리티 타입 (실행 로직 분기용).
    /// 실제 동작은 AbilityType별 IAbilityRunner 구현체가 담당.
    /// </summary>
    public enum AbilityType
    {
        // Victor
        Napalm,        // 부채꼴 화염 지대 (지속)
        Flame,         // 화염방사기 (부채꼴, 5초)
        Mine,          // 폭발 지뢰 (배치형, 최대 5)

        // Sara
        BlackHole,     // 중력 당기기 (지속)
        Shockwave,     // 확장 링 밀쳐내기 + 슬로우
        Meteor,        // 반중력 메테오 (자동 10초 주기)

        // Jinus
        Drone,         // 드론 포탑 (배치, HP 有)
        MiningDrone,   // 채굴 드론 (자원 생성형)
        SpiderDrone,   // 드론 거미 (자동 10초 주기 소환)
    }

    /// <summary>
    /// 발동 방식.
    /// Manual: 키 1/2/3 입력 시 발동
    /// AutoInterval: AutoIntervalSec마다 자동 발동
    /// </summary>
    public enum AbilityTrigger
    {
        Manual,
        AutoInterval,
    }

    [CreateAssetMenu(fileName = "Ability_New", menuName = "Drill-Corp/Ability Data", order = 6)]
    public class AbilityData : ScriptableObject
    {
        [Header("Identification")]
        [Tooltip("고유 ID (예: 'victor_napalm')")]
        [SerializeField] private string _abilityId;

        [Tooltip("소속 캐릭터 ID (CharacterData.CharacterId)")]
        [SerializeField] private string _characterId;

        [SerializeField] private string _displayName;

        [TextArea(2, 4)]
        [SerializeField] private string _description;

        [Tooltip("UI 이모지 (Sprite 없을 때 대체용)")]
        [SerializeField] private string _iconEmoji = "✦";

        [SerializeField] private Sprite _icon;

        [Header("Slot")]
        [Tooltip("인게임 입력 키 슬롯 (1 / 2 / 3)")]
        [Range(1, 3)]
        [SerializeField] private int _slotKey = 1;

        [SerializeField] private AbilityType _abilityType;
        [SerializeField] private AbilityTrigger _trigger = AbilityTrigger.Manual;

        [Header("Timing (초 단위)")]
        [Tooltip("쿨다운 (Manual 트리거만 의미 있음)")]
        [Min(0f)]
        [SerializeField] private float _cooldownSec = 20f;

        [Tooltip("지속 시간 (지속형만 사용. 0이면 즉발)")]
        [Min(0f)]
        [SerializeField] private float _durationSec = 0f;

        [Tooltip("자동 발동 주기 (AutoInterval 트리거만 사용)")]
        [Min(0f)]
        [SerializeField] private float _autoIntervalSec = 0f;

        [Header("Combat / Range")]
        [Tooltip("데미지 (틱 또는 1회). 해석은 AbilityType마다 다름")]
        [Min(0f)]
        [SerializeField] private float _damage = 0f;

        [Tooltip("범위 / 반경 / 사거리 (Unity 유닛)")]
        [Min(0f)]
        [SerializeField] private float _range = 0f;

        [Tooltip("부채꼴 반각 (라디안). 화염방사기 등")]
        [Min(0f)]
        [SerializeField] private float _angle = 0f;

        [Tooltip("동시 배치 최대 수 (지뢰·드론·거미드론 등)")]
        [Min(1)]
        [SerializeField] private int _maxInstances = 1;

        [Header("Unlock")]
        [Tooltip("해금 비용 (보석)")]
        [Min(0)]
        [SerializeField] private int _unlockGemCost = 30;

        [Tooltip("이 어빌리티를 해금하기 전 반드시 해금되어야 하는 선행 어빌리티")]
        [SerializeField] private AbilityData _requiredAbility;

        [Header("Visuals")]
        [SerializeField] private GameObject _vfxPrefab;
        [SerializeField] private AudioClip _useSfx;

        // ─── Properties ───
        public string AbilityId => _abilityId;
        public string CharacterId => _characterId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public string IconEmoji => _iconEmoji;
        public Sprite Icon => _icon;
        public int SlotKey => _slotKey;
        public AbilityType Type => _abilityType;
        public AbilityTrigger Trigger => _trigger;
        public float CooldownSec => _cooldownSec;
        public float DurationSec => _durationSec;
        public float AutoIntervalSec => _autoIntervalSec;
        public float Damage => _damage;
        public float Range => _range;
        public float Angle => _angle;
        public int MaxInstances => _maxInstances;
        public int UnlockGemCost => _unlockGemCost;
        public AbilityData RequiredAbility => _requiredAbility;
        public GameObject VfxPrefab => _vfxPrefab;
        public AudioClip UseSfx => _useSfx;
    }
}
