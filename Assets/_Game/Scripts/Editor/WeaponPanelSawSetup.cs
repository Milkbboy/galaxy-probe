// WeaponPanel에 Saw 슬롯 자동 추가 + SawWeapon 씬 인스턴스 생성/바인딩
// ─────────────────────────────────────────────────────────────
// 한 번 클릭으로:
//   1) WeaponSlot_Sniper.prefab 을 템플릿으로 복제 → "WeaponSlot_Saw"
//   2) Weapons 컨테이너 아래에 SawWeapon GameObject 생성
//   3) Weapon_Saw.asset 자동 바인딩
//   4) WeaponPanelUI._slots / ._weapons 배열 확장
//
// 이미 Saw 슬롯이 있으면 스킵 (중복 실행 안전).
// Game 씬이 활성 씬이어야 함.
//
// 참고: docs/Sys-Weapon.md §6
// ─────────────────────────────────────────────────────────────

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DrillCorp.UI.Weapon;
using DrillCorp.Weapon.Saw;

namespace DrillCorp.Editor
{
    public static class WeaponPanelSawSetup
    {
        const string SLOT_PREFAB_PATH = "Assets/_Game/Prefabs/UI/WeaponSlot_Sniper.prefab";
        const string SAW_DATA_PATH    = "Assets/_Game/Data/Weapons/Weapon_Saw.asset";

        // ═════════════════════════════════════════════════════
        // 통합 원클릭 — 에셋 → 프리펩 → 씬 바인딩 전체 진행
        // ═════════════════════════════════════════════════════
        [MenuItem("Tools/Drill-Corp/Weapons/★ Saw 풀셋업 (에셋 + 프리펩 + 씬)")]
        public static void FullSetup()
        {
            Debug.Log("[SawFullSetup] 1/3 — Weapon_Saw.asset 생성/확인");
            V2DataSetupEditor.CreateSawWeaponDataMenu();

            Debug.Log("[SawFullSetup] 2/3 — SawBlade.prefab 생성 + Weapon_Saw 바인딩");
            SawBladePrefabBuilder.Build();

            Debug.Log("[SawFullSetup] 3/3 — WeaponPanel 슬롯 추가 + SawWeapon 씬 인스턴스");
            Setup();

            Debug.Log("[SawFullSetup] 완료 — Game 씬 저장 (Ctrl+S) 후 플레이하세요.");
        }

        [MenuItem("Tools/Drill-Corp/Weapons/WeaponPanel에 Saw 슬롯 추가 (활성 씬)")]
        public static void Setup()
        {
            var panelUI = Object.FindAnyObjectByType<WeaponPanelUI>();
            if (panelUI == null)
            {
                EditorUtility.DisplayDialog("Saw 슬롯 추가 실패",
                    "활성 씬에서 WeaponPanelUI 를 찾을 수 없습니다.\nGame 씬을 열어두고 다시 실행하세요.",
                    "확인");
                return;
            }

            // 1) 이미 Saw 바인딩돼 있는지 검사 (_weapons 배열에서 SawWeapon 찾기)
            var panelSo = new SerializedObject(panelUI);
            var slotsProp = panelSo.FindProperty("_slots");
            var weaponsProp = panelSo.FindProperty("_weapons");

            for (int i = 0; i < weaponsProp.arraySize; i++)
            {
                var w = weaponsProp.GetArrayElementAtIndex(i).objectReferenceValue;
                if (w is SawWeapon)
                {
                    Debug.Log("[SawSetup] Saw 이미 WeaponPanel 에 바인딩됨 — 스킵");
                    EditorGUIUtility.PingObject(w);
                    return;
                }
            }

            // 2) 슬롯 프리펩 로드 + 인스턴스화 + 언팩 + 리네임
            var slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SLOT_PREFAB_PATH);
            if (slotPrefab == null)
            {
                EditorUtility.DisplayDialog("Saw 슬롯 추가 실패",
                    $"{SLOT_PREFAB_PATH} 를 찾을 수 없습니다.", "확인");
                return;
            }

            var slotGo = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab, panelUI.transform);
            PrefabUtility.UnpackPrefabInstance(slotGo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            slotGo.name = "WeaponSlot_Saw";
            Undo.RegisterCreatedObjectUndo(slotGo, "Create Saw Slot");

            var newSlot = slotGo.GetComponent<WeaponSlotUI>();
            if (newSlot == null)
            {
                Debug.LogError("[SawSetup] 복제된 GameObject 에 WeaponSlotUI 컴포넌트가 없습니다.");
                Object.DestroyImmediate(slotGo);
                return;
            }

            // 슬롯 프리펩이 보유하던 이전 _weapon 레퍼런스 정리 (Sniper 스크립트 참조)
            var slotSo = new SerializedObject(newSlot);
            var weaponField = slotSo.FindProperty("_weapon");
            if (weaponField != null)
            {
                weaponField.objectReferenceValue = null;
                slotSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // 3) SawWeapon 씬 인스턴스 찾기 or 생성
            var sawWeapon = Object.FindAnyObjectByType<SawWeapon>();
            if (sawWeapon == null)
            {
                var weaponsContainer = GameObject.Find("Weapons");
                if (weaponsContainer == null)
                {
                    weaponsContainer = new GameObject("Weapons");
                    Undo.RegisterCreatedObjectUndo(weaponsContainer, "Create Weapons Container");
                    Debug.LogWarning("[SawSetup] 'Weapons' 컨테이너 없음 — 씬 루트에 신규 생성했습니다.");
                }

                var sawGo = new GameObject("SawWeapon");
                Undo.RegisterCreatedObjectUndo(sawGo, "Create SawWeapon");
                sawGo.transform.SetParent(weaponsContainer.transform, false);
                sawWeapon = sawGo.AddComponent<SawWeapon>();

                // Weapon_Saw.asset 자동 바인딩
                var sawData = AssetDatabase.LoadAssetAtPath<SawWeaponData>(SAW_DATA_PATH);
                if (sawData != null)
                {
                    var sawSo = new SerializedObject(sawWeapon);
                    var dataProp = sawSo.FindProperty("_data");
                    if (dataProp != null)
                    {
                        dataProp.objectReferenceValue = sawData;
                        sawSo.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
                else
                {
                    Debug.LogWarning($"[SawSetup] {SAW_DATA_PATH} 없음.\n" +
                        "Tools/Drill-Corp/3. 게임 초기 설정/Title/4b. Weapon_Saw 에셋만 생성 먼저 실행 후, " +
                        "SawWeapon 의 _data 슬롯에 수동 바인딩하세요.");
                }
            }

            // 4) _slots, _weapons 배열 확장
            int idx = slotsProp.arraySize;
            slotsProp.arraySize = idx + 1;
            weaponsProp.arraySize = idx + 1;
            slotsProp.GetArrayElementAtIndex(idx).objectReferenceValue = newSlot;
            weaponsProp.GetArrayElementAtIndex(idx).objectReferenceValue = sawWeapon;
            panelSo.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(panelUI);
            EditorUtility.SetDirty(newSlot);
            EditorUtility.SetDirty(sawWeapon);
            EditorSceneManager.MarkSceneDirty(panelUI.gameObject.scene);

            Debug.Log($"[SawSetup] 완료:\n" +
                      $"  • WeaponPanel 아래에 '{slotGo.name}' 추가\n" +
                      $"  • Weapons/{sawWeapon.name} 생성\n" +
                      $"  • WeaponPanelUI._slots[{idx}] / ._weapons[{idx}] 바인딩");

            Selection.activeObject = slotGo;
            EditorGUIUtility.PingObject(slotGo);
        }
    }
}
