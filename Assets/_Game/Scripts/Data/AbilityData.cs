using System;
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

        [Tooltip("HUD 슬롯 강조색 (테두리·라벨·쿨다운바). v2 drawItemUI item.color 포팅값.")]
        [SerializeField] private Color _themeColor = new Color(1f, 0.42f, 0.21f, 1f); // #ff6b35 기본

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

        [Tooltip(
            "VFX 크기 배율. 기본 1 = 판정 반경(Range)에 맞춘 자동 스케일.\n" +
            "키우면 VFX가 커지고 줄이면 작아짐. 데칼·판정 반경은 영향 없음 (VFX만 조절).\n" +
            "프리펩별 '한 유닛 반경' 은 각 Runner 내부 상수로 관리."
        )]
        [Min(0f)]
        [SerializeField] private float _vfxScale = 1f;

        // ─── Properties ───
        public string AbilityId => _abilityId;
        public string CharacterId => _characterId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public string IconEmoji => _iconEmoji;
        public Sprite Icon => _icon;
        public Color ThemeColor => _themeColor;
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
        public float VfxScale => _vfxScale;

#if UNITY_EDITOR
        /// <summary>
        /// 에디터에서 SO 값이 변경될 때 발생 (OnValidate 훅).
        /// 빌드엔 포함되지 않음 — 개발 중 라이브 튜닝 전용.
        /// Runner가 구독해 VFX 스케일/데칼 크기/판정 반경을 즉시 갱신할 수 있게 해줌.
        /// </summary>
        public event Action OnValidated;

        private void OnValidate()
        {
            OnValidated?.Invoke();
        }
#endif
    }
}
