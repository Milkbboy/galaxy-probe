using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DrillCorp.Core
{
    [Serializable]
    public class PlayerData
    {
        // === 기존 필드 (하위 호환용 — 신규 코드는 Ore를 쓸 것) ===
        public int Currency;                // 레거시. Load 시 Ore로 마이그레이션.
        public int MachineLevel;
        public int TotalSessionsPlayed;
        public int TotalBugsKilled;

        // === v2 — 이중 재화 ===
        public int Ore;                     // 굴착기 강화, 무기 강화(혼합)
        public int Gems;                    // 무기 해금, 어빌리티 해금, 보석 채집 스킬

        // === v2 — 선택·해금 상태 ===
        public string SelectedCharacterId = "victor";
        public List<string> UnlockedWeapons = new List<string> { "sniper" };
        public List<string> UnlockedAbilities = new List<string>();

        // === v2 — 직전 세션 결과 (Title 진입 시 ResultOverlay 표시용) ===
        public SessionResult LastSessionResult;

        /// <summary>
        /// Load 직후 호출. 기본값 누락 보정 + Currency → Ore 마이그레이션.
        /// </summary>
        public void NormalizeAfterLoad()
        {
            // 레거시 Currency → Ore 이전 (한 번만)
            if (Currency > 0 && Ore == 0)
            {
                Ore = Currency;
                Currency = 0;
            }

            // List 필드 null 방지 (구 세이브 파일 호환)
            if (UnlockedWeapons == null) UnlockedWeapons = new List<string> { "sniper" };
            if (UnlockedAbilities == null) UnlockedAbilities = new List<string>();
            if (string.IsNullOrEmpty(SelectedCharacterId)) SelectedCharacterId = "victor";

            // 기본 무기(저격총)는 항상 해금된 상태 보장
            if (!UnlockedWeapons.Contains("sniper")) UnlockedWeapons.Add("sniper");
        }

        public bool HasWeapon(string weaponId) =>
            !string.IsNullOrEmpty(weaponId) && UnlockedWeapons.Contains(weaponId);

        public bool HasAbility(string abilityId) =>
            !string.IsNullOrEmpty(abilityId) && UnlockedAbilities.Contains(abilityId);
    }

    /// <summary>
    /// 세션 종료 후 Title 씬으로 전달되는 결과. LastSessionResult에 저장.
    /// TimestampTicks는 표시 후 0으로 리셋 (중복 노출 방지).
    /// </summary>
    [Serializable]
    public class SessionResult
    {
        public bool IsWin;
        public int OreGained;
        public int GemGained;
        public int KillCount;
        public long TimestampTicks;

        public bool HasValue => TimestampTicks > 0;

        public void Clear()
        {
            IsWin = false;
            OreGained = 0;
            GemGained = 0;
            KillCount = 0;
            TimestampTicks = 0;
        }
    }

    public class DataManager : MonoBehaviour
    {
        public static DataManager Instance { get; private set; }

        public PlayerData Data { get; private set; }

        // === 기존 (하위 호환 — Ore를 반환) ===
        public int Currency => Data?.Ore ?? 0;

        // === v2 ===
        public int Ore => Data?.Ore ?? 0;
        public int Gems => Data?.Gems ?? 0;

        private string _savePath;
        private const string SaveFileName = "playerdata.json";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                DestroyImmediate(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _savePath = Path.Combine(Application.persistentDataPath, SaveFileName);
            Load();
        }

        public void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(Data, true);
                File.WriteAllText(_savePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[DataManager] Save failed: {e.Message}");
            }
        }

        public void Load()
        {
            try
            {
                if (File.Exists(_savePath))
                {
                    string json = File.ReadAllText(_savePath);
                    Data = JsonUtility.FromJson<PlayerData>(json) ?? new PlayerData();
                }
                else
                {
                    Data = new PlayerData();
                    Save();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[DataManager] Load failed: {e.Message}");
                Data = new PlayerData();
            }

            Data.NormalizeAfterLoad();
        }

        // ═══════════════════════════════════════════════════
        // 재화 — 광석 (Ore)
        // ═══════════════════════════════════════════════════
        public void AddOre(int amount)
        {
            if (amount <= 0) return;
            Data.Ore += amount;
            GameEvents.OnOreChanged?.Invoke(Data.Ore);
            GameEvents.OnCurrencyChanged?.Invoke(Data.Ore);  // 레거시 호환
            Save();
        }

        public bool SpendOre(int amount)
        {
            if (amount <= 0) return true;
            if (Data.Ore < amount) return false;

            Data.Ore -= amount;
            GameEvents.OnOreChanged?.Invoke(Data.Ore);
            GameEvents.OnCurrencyChanged?.Invoke(Data.Ore);
            Save();
            return true;
        }

        // ═══════════════════════════════════════════════════
        // 재화 — 보석 (Gems)
        // ═══════════════════════════════════════════════════
        public void AddGems(int amount)
        {
            if (amount <= 0) return;
            Data.Gems += amount;
            GameEvents.OnGemsChanged?.Invoke(Data.Gems);
            Save();
        }

        public bool SpendGems(int amount)
        {
            if (amount <= 0) return true;
            if (Data.Gems < amount) return false;

            Data.Gems -= amount;
            GameEvents.OnGemsChanged?.Invoke(Data.Gems);
            Save();
            return true;
        }

        // ═══════════════════════════════════════════════════
        // 레거시 API (기존 호출부 유지) — Ore로 리다이렉트
        // ═══════════════════════════════════════════════════
        [Obsolete("Use AddOre instead.")]
        public void AddCurrency(int amount) => AddOre(amount);

        [Obsolete("Use SpendOre instead.")]
        public bool SpendCurrency(int amount) => SpendOre(amount);

        // ═══════════════════════════════════════════════════
        // 해금 상태
        // ═══════════════════════════════════════════════════
        public bool TryUnlockWeapon(string weaponId, int gemCost)
        {
            if (Data.HasWeapon(weaponId)) return false;
            if (!SpendGems(gemCost)) return false;
            Data.UnlockedWeapons.Add(weaponId);
            Save();
            GameEvents.OnWeaponUnlocked?.Invoke(weaponId);
            return true;
        }

        public bool TryUnlockAbility(string abilityId, int gemCost)
        {
            if (Data.HasAbility(abilityId)) return false;
            if (!SpendGems(gemCost)) return false;
            Data.UnlockedAbilities.Add(abilityId);
            Save();
            GameEvents.OnAbilityUnlocked?.Invoke(abilityId);
            return true;
        }

        // ═══════════════════════════════════════════════════
        // 캐릭터 선택
        // ═══════════════════════════════════════════════════
        public void SelectCharacter(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return;
            if (Data.SelectedCharacterId == characterId) return;
            Data.SelectedCharacterId = characterId;
            Save();
            GameEvents.OnCharacterSelected?.Invoke(characterId);
        }

        // ═══════════════════════════════════════════════════
        // 세션 결과
        // ═══════════════════════════════════════════════════
        public void StoreSessionResult(bool isWin, int oreGained, int gemGained, int kills)
        {
            if (Data.LastSessionResult == null)
                Data.LastSessionResult = new SessionResult();

            Data.LastSessionResult.IsWin = isWin;
            Data.LastSessionResult.OreGained = oreGained;
            Data.LastSessionResult.GemGained = gemGained;
            Data.LastSessionResult.KillCount = kills;
            Data.LastSessionResult.TimestampTicks = DateTime.UtcNow.Ticks;

            // 재화 적립 책임 분리:
            //  - 광석: MachineController가 성공 시 AddOre(_totalMined) 호출 (패배 시 0).
            //  - 보석: Gem.cs가 채집 순간 즉시 AddGems(1) 호출 (세션 중간에 사망해도 유지).
            // 여기서 중복 AddOre/AddGems를 하면 이중 적립이 되므로 기록과 통계만 담당.
            IncrementSessionsPlayed();  // 내부에서 Save 호출 — LastSessionResult 함께 저장됨
            AddBugsKilled(kills);
        }

        public SessionResult ConsumeLastSessionResult()
        {
            var r = Data?.LastSessionResult;
            if (r == null || !r.HasValue) return null;

            // 복사본 반환 후 원본 비우기 (다음 씬 진입 시 중복 표시 방지)
            var copy = new SessionResult
            {
                IsWin = r.IsWin,
                OreGained = r.OreGained,
                GemGained = r.GemGained,
                KillCount = r.KillCount,
                TimestampTicks = r.TimestampTicks,
            };
            r.Clear();
            Save();
            return copy;
        }

        // ═══════════════════════════════════════════════════
        // 통계
        // ═══════════════════════════════════════════════════
        public void IncrementSessionsPlayed()
        {
            Data.TotalSessionsPlayed++;
            Save();
        }

        public void AddBugsKilled(int count)
        {
            if (count <= 0) return;
            Data.TotalBugsKilled += count;
            Save();
        }

        public void ResetData()
        {
            Data = new PlayerData();
            Data.NormalizeAfterLoad();
            Save();
            GameEvents.OnOreChanged?.Invoke(Data.Ore);
            GameEvents.OnGemsChanged?.Invoke(Data.Gems);
            GameEvents.OnCurrencyChanged?.Invoke(Data.Ore);
        }
    }
}
