using UnityEngine;

namespace DrillCorp.Data
{
    /// <summary>
    /// 플레이어 캐릭터 정의.
    /// 3캐릭터 시스템 — 각자 테마 컬러 + 기본 머신 + 고유 어빌리티 3종.
    /// v2 통합 가이드: docs/Sys-Character.md
    /// </summary>
    [CreateAssetMenu(fileName = "Character_New", menuName = "Drill-Corp/Character Data", order = 5)]
    public class CharacterData : ScriptableObject
    {
        [Header("Identification")]
        [Tooltip("고유 ID (소문자 영문). 예: 'victor' / 'sara' / 'jinus'")]
        [SerializeField] private string _characterId;

        [SerializeField] private string _displayName;

        [Tooltip("칭호 (예: '중장비 전문가')")]
        [SerializeField] private string _title;

        [TextArea(2, 4)]
        [SerializeField] private string _description;

        [Header("Theme")]
        [Tooltip("캐릭터 테마 컬러 — UI 강조, HUD 이름표 등에 사용")]
        [SerializeField] private Color _themeColor = Color.white;

        [SerializeField] private Sprite _portrait;

        [Header("Machine")]
        [Tooltip("이 캐릭터가 사용하는 기본 채굴 머신")]
        [SerializeField] private MachineData _defaultMachine;

        [Header("Abilities")]
        [Tooltip("슬롯 1/2/3에 배치되는 어빌리티 3개. 길이 3 고정.")]
        [SerializeField] private AbilityData[] _abilities = new AbilityData[3];

        // ─── Properties ───
        public string CharacterId => _characterId;
        public string DisplayName => _displayName;
        public string Title => _title;
        public string Description => _description;
        public Color ThemeColor => _themeColor;
        public Sprite Portrait => _portrait;
        public MachineData DefaultMachine => _defaultMachine;

        /// <summary>
        /// 슬롯 번호(1/2/3)로 어빌리티 조회. 범위 밖이면 null.
        /// </summary>
        public AbilityData GetAbility(int slotKey)
        {
            int idx = slotKey - 1;
            if (_abilities == null || idx < 0 || idx >= _abilities.Length) return null;
            return _abilities[idx];
        }

        public AbilityData[] Abilities => _abilities;
    }
}
