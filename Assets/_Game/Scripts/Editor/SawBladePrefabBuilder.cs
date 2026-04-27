// 회전톱날 블레이드 비주얼 프리펩 자동 생성
// ─────────────────────────────────────────────────────────────
// v2.html drawSawPipe() 시각 디자인 포팅:
//   • 10개 톱니(#77ee77) — 밑변 반경 = BladeR × 0.68, 외측 반폭 = BladeR × 0.2
//   • 허브 원반(#666666) — 반경 = BladeR × 0.72
//   • 중앙 볼트(#bbbbbb) — 반경 = BladeR × 0.2
//
// 탑다운 카메라(-Y 방향) 기준:
//   • 메시는 XZ 평면에 누워 있음 (+Y 노멀)
//   • 부모의 Y축 회전 = 블레이드 "화면상 자전" (SawWeapon.UpdateBladeTransform 참조)
//
// 산출물:
//   • Assets/_Game/Prefabs/Weapons/SawBlade.prefab
//   • Assets/_Game/Prefabs/Weapons/Meshes/SawBlade_*.asset  (3종)
//   • Assets/_Game/Materials/SawBlade_*.mat                  (3종)
//   • Weapon_Saw.asset 가 있으면 _bladeVisualPrefab 자동 바인딩
//
// 참고: docs/Sys-Weapon.md §7, docs/v2.html 1530~1546줄
// ─────────────────────────────────────────────────────────────

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using DrillCorp.Weapon.Saw;

namespace DrillCorp.Editor
{
    public static class SawBladePrefabBuilder
    {
        const string DATA_ROOT  = "Assets/_Game/Data";
        const string PREFAB_DIR = "Assets/_Game/Prefabs/Weapons";
        const string MESH_DIR   = "Assets/_Game/Prefabs/Weapons/Meshes";
        const string MAT_DIR    = "Assets/_Game/Materials";

        // v2 기준: BASE.saw.bladeR = 18 canvas-unit → 1.8 world-unit
        const float BLADE_RADIUS            = 1.8f;
        const int   TEETH_COUNT             = 10;
        const float TOOTH_BASE_RATIO        = 0.68f;  // 톱니 내측 apex 반경 / BladeR
        const float TOOTH_HALF_WIDTH_RATIO  = 0.20f;  // 톱니 외측 ±Z 반폭 / BladeR
        const float HUB_RATIO               = 0.72f;
        const float BOLT_RATIO              = 0.20f;
        const int   HUB_SEGMENTS            = 48;
        const int   BOLT_SEGMENTS           = 24;

        // v2 색상 (drawSawPipe 기준)
        static readonly Color COLOR_TEETH = new Color32(0x77, 0xEE, 0x77, 0xFF); // #77ee77
        static readonly Color COLOR_HUB   = new Color32(0x66, 0x66, 0x66, 0xFF); // #666
        static readonly Color COLOR_BOLT  = new Color32(0xBB, 0xBB, 0xBB, 0xFF); // #bbb

        // 레이어 쌓기 (카메라 -Y, 즉 Y+가 위). 값이 클수록 카메라에 가까움.
        const float Y_TEETH = 0.000f;
        const float Y_HUB   = 0.020f;
        const float Y_BOLT  = 0.040f;

        [MenuItem("Tools/Drill-Corp/Weapons/SawBlade 프리펩 생성")]
        public static void Build()
        {
            EnsureFolders();

            // 1) 머티리얼 3종
            var matTeeth = CreateOrUpdateMaterial("SawBlade_Teeth", COLOR_TEETH);
            var matHub   = CreateOrUpdateMaterial("SawBlade_Hub",   COLOR_HUB);
            var matBolt  = CreateOrUpdateMaterial("SawBlade_Bolt",  COLOR_BOLT);

            // 2) 메시 3종
            var teethMesh = SaveOrReplaceMesh(BuildTeethMesh(),
                "SawBlade_Teeth");
            var hubMesh   = SaveOrReplaceMesh(
                BuildDiscMesh(BLADE_RADIUS * HUB_RATIO,  HUB_SEGMENTS),
                "SawBlade_Hub");
            var boltMesh  = SaveOrReplaceMesh(
                BuildDiscMesh(BLADE_RADIUS * BOLT_RATIO, BOLT_SEGMENTS),
                "SawBlade_Bolt");

            // 3) GameObject 조립
            var root = new GameObject("SawBlade");
            AttachLayer(root, "Teeth", teethMesh, matTeeth, Y_TEETH);
            AttachLayer(root, "Hub",   hubMesh,   matHub,   Y_HUB);
            AttachLayer(root, "Bolt",  boltMesh,  matBolt,  Y_BOLT);

            // 4) 프리펩 저장 (기존 덮어쓰기)
            string prefabPath = $"{PREFAB_DIR}/SawBlade.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            // 5) Weapon_Saw.asset 에 자동 바인딩 (있으면)
            TryBindToSawWeaponData(prefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[SawBlade] 프리펩 생성 완료: {prefabPath}");

            if (prefab != null) EditorGUIUtility.PingObject(prefab);
        }

        // ═════════════════════════════════════════════════════
        // 폴더
        // ═════════════════════════════════════════════════════
        static void EnsureFolders()
        {
            EnsureFolder("Assets/_Game", "Prefabs");
            EnsureFolder("Assets/_Game/Prefabs", "Weapons");
            EnsureFolder("Assets/_Game/Prefabs/Weapons", "Meshes");
            EnsureFolder("Assets/_Game", "Materials");
        }

        static void EnsureFolder(string parent, string name)
        {
            string full = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(full))
                AssetDatabase.CreateFolder(parent, name);
        }

        // ═════════════════════════════════════════════════════
        // 머티리얼 — URP Lit (없으면 Standard 폴백)
        // ═════════════════════════════════════════════════════
        static Material CreateOrUpdateMaterial(string name, Color color)
        {
            string path = $"{MAT_DIR}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            if (existing != null)
            {
                if (existing.shader != shader) existing.shader = shader;
                ApplyColor(existing, color);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var mat = new Material(shader) { name = name };
            ApplyColor(mat, color);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static void ApplyColor(Material mat, Color color)
        {
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))     mat.SetColor("_Color", color);
            // URP Lit 기본 — Smoothness 낮춰 무광 느낌
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.25f);
            if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic", 0f);
        }

        // ═════════════════════════════════════════════════════
        // 디스크 메시 (허브/볼트) — XZ 평면 + Y 노멀
        // 삼각팬: center + segments
        // Winding (center, p_next, p_curr) → +Y 노멀 (Unity 좌표계에서
        // (P2-P1)×(P3-P1) 가 +Y가 되도록)
        // ═════════════════════════════════════════════════════
        static Mesh BuildDiscMesh(float radius, int segments)
        {
            var mesh = new Mesh();
            var verts = new Vector3[segments + 1];
            var uvs   = new Vector2[segments + 1];
            var tris  = new int[segments * 3];

            verts[0] = Vector3.zero;
            uvs[0]   = new Vector2(0.5f, 0.5f);

            for (int i = 0; i < segments; i++)
            {
                float a = (float)i / segments * Mathf.PI * 2f;
                float cs = Mathf.Cos(a);
                float sn = Mathf.Sin(a);
                verts[i + 1] = new Vector3(cs * radius, 0f, sn * radius);
                uvs[i + 1]   = new Vector2(cs * 0.5f + 0.5f, sn * 0.5f + 0.5f);
            }

            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                tris[i * 3 + 0] = 0;
                tris[i * 3 + 1] = next + 1;
                tris[i * 3 + 2] = i + 1;
            }

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // ═════════════════════════════════════════════════════
        // 톱니 메시 (N개 삼각형) — v2 drawSawPipe 톱니 형상
        // 각 톱니 = 내측 apex 1점 + 외측 2점(±halfW)
        // Winding (A, C, B) → +Y 노멀
        // ═════════════════════════════════════════════════════
        static Mesh BuildTeethMesh()
        {
            var mesh = new Mesh();
            int n = TEETH_COUNT;

            var verts = new Vector3[n * 3];
            var uvs   = new Vector2[n * 3];
            var tris  = new int[n * 3];

            float baseR = BLADE_RADIUS * TOOTH_BASE_RATIO;
            float tipR  = BLADE_RADIUS;
            float halfW = BLADE_RADIUS * TOOTH_HALF_WIDTH_RATIO;

            for (int i = 0; i < n; i++)
            {
                float a = (float)i / n * Mathf.PI * 2f;
                float cs = Mathf.Cos(a);
                float sn = Mathf.Sin(a);

                // 로컬(톱니 기준, X=반경 방향, Z=접선 방향) → 월드 (Y축 회전)
                Vector3 A = Rotate(new Vector3(baseR, 0f,  0f),    cs, sn); // 내측 apex
                Vector3 B = Rotate(new Vector3(tipR,  0f, -halfW), cs, sn); // 외측 -Z
                Vector3 C = Rotate(new Vector3(tipR,  0f, +halfW), cs, sn); // 외측 +Z

                int b = i * 3;
                verts[b + 0] = A;
                verts[b + 1] = B;
                verts[b + 2] = C;

                uvs[b + 0] = new Vector2(0f, 0.5f);
                uvs[b + 1] = new Vector2(1f, 0f);
                uvs[b + 2] = new Vector2(1f, 1f);

                // +Y 노멀: (A, C, B) 순서
                tris[b + 0] = b + 0;
                tris[b + 1] = b + 2;
                tris[b + 2] = b + 1;
            }

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Vector3 Rotate(Vector3 local, float cs, float sn)
        {
            // Y축 회전: (x, y, z) → (x*cs - z*sn, y, x*sn + z*cs)
            return new Vector3(
                local.x * cs - local.z * sn,
                local.y,
                local.x * sn + local.z * cs);
        }

        // ═════════════════════════════════════════════════════
        // 메시 저장 — 기존 에셋 있으면 GUID 보존하며 덮어쓰기
        // ═════════════════════════════════════════════════════
        static Mesh SaveOrReplaceMesh(Mesh source, string fileName)
        {
            string path = $"{MESH_DIR}/{fileName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                existing.Clear();
                existing.vertices = source.vertices;
                existing.uv = source.uv;
                existing.triangles = source.triangles;
                existing.RecalculateNormals();
                existing.RecalculateBounds();
                EditorUtility.SetDirty(existing);
                return existing;
            }
            source.name = fileName;
            AssetDatabase.CreateAsset(source, path);
            return source;
        }

        static void AttachLayer(GameObject root, string name, Mesh mesh, Material mat, float yOffset)
        {
            var child = new GameObject(name);
            child.transform.SetParent(root.transform, false);
            child.transform.localPosition = new Vector3(0f, yOffset, 0f);

            var mf = child.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            var mr = child.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        // ═════════════════════════════════════════════════════
        // Weapon_Saw.asset 자동 바인딩
        // ═════════════════════════════════════════════════════
        static void TryBindToSawWeaponData(GameObject prefab)
        {
            if (prefab == null) return;

            string sawPath = $"{DATA_ROOT}/Weapons/Weapon_Saw.asset";
            var sawData = AssetDatabase.LoadAssetAtPath<SawWeaponData>(sawPath);
            if (sawData == null)
            {
                Debug.Log($"[SawBlade] Weapon_Saw.asset 없음 — V2DataSetup 메뉴로 먼저 생성 후 다시 실행하면 자동 바인딩됩니다.");
                return;
            }

            var so = new SerializedObject(sawData);
            var prop = so.FindProperty("_bladeVisualPrefab");
            if (prop == null)
            {
                Debug.LogWarning("[SawBlade] SawWeaponData._bladeVisualPrefab 프로퍼티를 찾지 못함");
                return;
            }
            prop.objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(sawData);
            Debug.Log($"[SawBlade] Weapon_Saw.asset._bladeVisualPrefab 자동 바인딩 완료");
        }
    }
}
