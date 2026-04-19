using System.Collections.Generic;
using UnityEngine;
using DrillCorp.Data;

namespace DrillCorp.OutGame
{
    /// <summary>
    /// 모든 CharacterData SO 중앙 등록소.
    /// Title 씬의 GameObject에 부착, 인스펙터로 3 캐릭터 SO 할당.
    /// MachineController가 DataManager.SelectedCharacterId 로 DefaultMachine을 조회.
    /// UpgradeManager / WeaponUpgradeManager 와 동일한 싱글턴 + DontDestroyOnLoad 패턴.
    /// </summary>
    public class CharacterRegistry : MonoBehaviour
    {
        public static CharacterRegistry Instance { get; private set; }

        [Tooltip("등록된 모든 캐릭터 SO. v2 기본은 Victor / Sara / Jinus.")]
        [SerializeField] private CharacterData[] _characters;

        public IReadOnlyList<CharacterData> All => _characters;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                DestroyImmediate(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public CharacterData Find(string characterId)
        {
            if (string.IsNullOrEmpty(characterId) || _characters == null) return null;
            for (int i = 0; i < _characters.Length; i++)
            {
                var c = _characters[i];
                if (c != null && c.CharacterId == characterId) return c;
            }
            return null;
        }
    }
}
