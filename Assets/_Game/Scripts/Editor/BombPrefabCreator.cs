#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using DrillCorp.Weapon.Bomb;

namespace DrillCorp.EditorTools
{
    /// <summary>
    /// 폭탄 무기 자산 일괄 생성:
    /// 1) BombProjectile 스프라이트 (PNG, 둥근 핵 모양)
    /// 2) BombLandingMarker 스프라이트 (PNG, 부드러운 채움 원)
    /// 3) BombProjectile.prefab (SpriteRenderer + BombProjectile)
    /// 4) BombLandingMarker.prefab (SpriteRenderer + BombLandingMarker)
    /// 5) Weapon_Bomb.asset (BombData SO — 위 두 프리펩 자동 참조)
    ///
    /// 메뉴 한 번 실행으로 폭탄 동작에 필요한 자산이 전부 만들어진다.
    /// </summary>
    public static class BombPrefabCreator
    {
        // === 경로 ===
        private const string PrefabFolder = "Assets/_Game/Prefabs/Weapons";
        private const string DataFolder = "Assets/_Game/Data/Weapons";

        private const string ProjectileSpritePath = PrefabFolder + "/BombProjectileSprite.png";
        private const string MarkerSpritePath = PrefabFolder + "/BombLandingMarkerCircle.png";
        private const string ExplosionSpritePath = PrefabFolder + "/BombExplosionBurst.png";

        private const string ProjectilePrefabPath = PrefabFolder + "/BombProjectile.prefab";
        private const string MarkerPrefabPath = PrefabFolder + "/BombLandingMarker.prefab";
        private const string ExplosionFxPrefabPath = PrefabFolder + "/BombExplosionFx.prefab";

        private const string TrailMaterialPath = PrefabFolder + "/BombTrail_Mat.mat";

        private const string DataAssetPath = DataFolder + "/Weapon_Bomb.asset";

        // === 색 (프로토타입 #f4a423 주황) ===
        private static readonly Color BombOrange = new Color(0.957f, 0.643f, 0.137f, 1f);

        [MenuItem("Tools/Drill-Corp/3. 게임 초기 설정/6. 폭탄 자산 일괄 생성")]
        public static void CreateAll()
        {
            EnsureFolders();

            bool projSpriteCreated = EnsureSprite(ProjectileSpritePath, CreateProjectilePng);
            bool markerSpriteCreated = EnsureSprite(MarkerSpritePath, CreateMarkerPng);
            bool explosionSpriteCreated = EnsureSprite(ExplosionSpritePath, CreateExplosionBurstPng);

            if (projSpriteCreated || markerSpriteCreated || explosionSpriteCreated)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            var projectileSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ProjectileSpritePath);
            var markerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MarkerSpritePath);
            var explosionSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ExplosionSpritePath);

            if (projectileSprite == null || markerSprite == null || explosionSprite == null)
            {
                Debug.LogError("[BombPrefabCreator] 스프라이트 로드 실패. 메뉴를 다시 한번 실행해보세요.");
                return;
            }

            // 트레일 머티리얼 (에셋으로 저장 — 프리펩 참조 보존됨)
            var trailMat = EnsureTrailMaterial();

            var projectilePrefab = CreateProjectilePrefab(projectileSprite, trailMat);
            var markerPrefab = CreateMarkerPrefab(markerSprite);
            var explosionPrefab = CreateExplosionFxPrefab(explosionSprite);

            if (projectilePrefab == null || markerPrefab == null || explosionPrefab == null) return;

            var data = CreateOrUpdateData(projectilePrefab, markerPrefab, explosionPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = data;
            EditorGUIUtility.PingObject(data);
            Debug.Log(
                "[BombPrefabCreator] ✅ 폭탄 자산 일괄 생성 완료\n" +
                $"  • {ProjectilePrefabPath}\n" +
                $"  • {MarkerPrefabPath}\n" +
                $"  • {ExplosionFxPrefabPath}\n" +
                $"  • {TrailMaterialPath}\n" +
                $"  • {DataAssetPath}\n" +
                "다음: 씬에 BombWeapon GameObject를 추가하고 Data 필드에 Weapon_Bomb 할당."
            );
        }

        // ═══════════ 트레일 머티리얼 ═══════════

        /// <summary>
        /// TrailRenderer용 머티리얼 — 에셋으로 저장해야 프리펩에 참조 유지됨
        /// (런타임 new Material()을 직접 sharedMaterial에 넣으면 프리펩 저장 시 null로 빠지고
        ///  런타임에 마젠타/핑크 'Missing Material' 색으로 렌더링됨)
        /// </summary>
        private static Material EnsureTrailMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(TrailMaterialPath);
            if (mat != null) return mat;

            // Sprites/Default는 vertex color 지원 → TrailRenderer.colorGradient가 그대로 적용됨
            Shader sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
            if (sh == null)
            {
                Debug.LogError("[BombPrefabCreator] Trail 머티리얼용 셰이더를 찾을 수 없습니다.");
                return null;
            }

            mat = new Material(sh) { name = "BombTrail_Mat" };
            AssetDatabase.CreateAsset(mat, TrailMaterialPath);
            return mat;
        }

        // ═══════════ 폴더 ═══════════

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/_Game", "Prefabs");
            EnsureFolder("Assets/_Game/Prefabs", "Weapons");
            EnsureFolder("Assets/_Game", "Data");
            EnsureFolder("Assets/_Game/Data", "Weapons");
        }

        private static void EnsureFolder(string parent, string newFolder)
        {
            string full = parent + "/" + newFolder;
            if (!AssetDatabase.IsValidFolder(full))
                AssetDatabase.CreateFolder(parent, newFolder);
        }

        // ═══════════ 스프라이트 ═══════════

        /// <summary>PNG 파일 보장 + TextureImporter 설정. 신규 생성 여부 반환.</summary>
        private static bool EnsureSprite(string path, System.Action createPng)
        {
            bool existed = File.Exists(path);
            if (!existed) createPng();

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return !existed;

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; changed = true; }
            if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
            if (Mathf.Abs(importer.spritePixelsPerUnit - 100f) > 0.01f) { importer.spritePixelsPerUnit = 100f; changed = true; }
            if (importer.filterMode != FilterMode.Bilinear) { importer.filterMode = FilterMode.Bilinear; changed = true; }
            if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }

            if (changed || !existed) importer.SaveAndReimport();
            return !existed;
        }

        /// <summary>
        /// 폭탄 투사체 PNG: 64×64, 흰 발광 코어 → 노랑 → 주황 → 외곽 글로우
        /// 탑뷰에서 작은 점이라도 또렷하게 보이도록 고대비 + 밝은 중심
        /// </summary>
        private static void CreateProjectilePng()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];

            float c = (size - 1) * 0.5f;
            float coreR = size * 0.18f;   // 흰 발광 중심
            float midR = size * 0.34f;    // 노랑→주황 중간
            float outerR = size * 0.46f;  // 주황 외곽
            float glowR = size * 0.50f;   // 페이드 끝

            Color hotCore = new Color(1.0f, 1.0f, 0.95f, 1f); // 흰색에 가까운 발광
            Color hotMid = new Color(1.0f, 0.85f, 0.30f, 1f); // 노랑
            Color edgeOrange = BombOrange;                     // 주황 #f4a423

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - c, dy = y - c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);

                    Color px = new Color(0, 0, 0, 0);

                    if (d <= coreR)
                    {
                        // 흰 발광 코어
                        px = hotCore;
                    }
                    else if (d <= midR)
                    {
                        // 흰→노랑
                        float t = Mathf.InverseLerp(coreR, midR, d);
                        px = Color.Lerp(hotCore, hotMid, t);
                    }
                    else if (d <= outerR)
                    {
                        // 노랑→주황
                        float t = Mathf.InverseLerp(midR, outerR, d);
                        px = Color.Lerp(hotMid, edgeOrange, t);
                    }
                    else if (d <= glowR)
                    {
                        // 주황 → 투명 (글로우 페이드)
                        float t = Mathf.InverseLerp(outerR, glowR, d);
                        px = new Color(edgeOrange.r, edgeOrange.g, edgeOrange.b, 1f - t);
                    }

                    pixels[y * size + x] = px;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(ProjectileSpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>
        /// 착탄 마커 PNG: 128×128 부드러운 채움 원 (가장자리 약간 진한 링)
        /// 흰색 단색으로 만들고 색상은 SpriteRenderer.color로 적용 → 동일 스프라이트로 색 변경 가능
        /// </summary>
        private static void CreateMarkerPng()
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];

            float c = (size - 1) * 0.5f;
            float outerR = size * 0.49f;       // 텍스처 거의 가득 채움 → scale=diameter 일치
            float ringInner = size * 0.42f;
            float ringOuter = size * 0.48f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - c, dy = y - c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);

                    Color px = new Color(0, 0, 0, 0);

                    if (d <= ringOuter)
                    {
                        // 안쪽 채움 (살짝 투명)
                        px = new Color(1f, 1f, 1f, 0.55f);
                    }

                    // 가장자리 진한 링 (강조)
                    if (d >= ringInner && d <= ringOuter)
                    {
                        float t = 1f - Mathf.Abs(d - (ringInner + ringOuter) * 0.5f) / ((ringOuter - ringInner) * 0.5f);
                        float ringAlpha = Mathf.Clamp01(t);
                        px = new Color(1f, 1f, 1f, Mathf.Max(px.a, ringAlpha));
                    }

                    // 바깥 안티앨리어스
                    if (d > ringOuter && d <= outerR + 1.5f)
                    {
                        float a = (1f - Mathf.Clamp01((d - ringOuter) / (outerR - ringOuter + 1.5f))) * 0.4f;
                        px = new Color(1f, 1f, 1f, a);
                    }

                    pixels[y * size + x] = px;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(MarkerSpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>
        /// 폭발 burst PNG: 128×128 라디얼 그라데이션
        /// 흰 코어 → 노랑 → 주황 → 빨강 → 페이드
        /// 폭발 순간 사방으로 퍼지는 느낌. BombExplosionFx가 스케일을 키우면서 페이드아웃.
        /// </summary>
        private static void CreateExplosionBurstPng()
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];

            float c = (size - 1) * 0.5f;
            float maxR = size * 0.5f;

            Color hotWhite = new Color(1f, 1f, 0.95f, 1f);
            Color yellow = new Color(1f, 0.85f, 0.30f, 1f);
            Color orange = BombOrange;
            Color red = new Color(0.95f, 0.25f, 0.15f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - c, dy = y - c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float t = Mathf.Clamp01(d / maxR);

                    Color px;
                    if (t < 0.15f)
                    {
                        // 흰 코어
                        px = Color.Lerp(hotWhite, yellow, t / 0.15f);
                    }
                    else if (t < 0.40f)
                    {
                        // 노랑 → 주황
                        px = Color.Lerp(yellow, orange, (t - 0.15f) / 0.25f);
                    }
                    else if (t < 0.70f)
                    {
                        // 주황 → 빨강
                        px = Color.Lerp(orange, red, (t - 0.40f) / 0.30f);
                    }
                    else
                    {
                        // 빨강 → 투명 페이드
                        px = red;
                        px.a = Mathf.Clamp01(1f - (t - 0.70f) / 0.30f);
                    }

                    // 외곽 컷오프 (매끄러운 안티앨리어스)
                    if (t > 0.95f) px.a = 0f;

                    pixels[y * size + x] = px;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(ExplosionSpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        // ═══════════ 프리펩 ═══════════

        private static GameObject CreateProjectilePrefab(Sprite sprite, Material trailMat)
        {
            var root = new GameObject("BombProjectile");
            try
            {
                // 탑뷰 (X=90) — 스프라이트가 XZ 평면에 누움
                root.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                // 64px / 100ppu × 5.0 = 3.2 유닛 지름 (탑뷰에서 또렷하게 보이도록 크게)
                root.transform.localScale = Vector3.one * 5f;

                var sr = root.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = Color.white; // 스프라이트 자체가 색 가지고 있음
                sr.sortingOrder = 8;

                // 비행 잔상 (프로토타입 _.html의 b.trail 효과)
                AddTrailRenderer(root, trailMat);

                root.AddComponent<BombProjectile>();

                return SaveAsPrefab(root, ProjectilePrefabPath, "BombProjectile");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// 폭탄 비행 잔상 — 0.3초 길이, 주황색 그라데이션, 끝으로 갈수록 얇아지고 투명해짐
        /// 머티리얼은 외부 에셋(BombTrail_Mat.mat)을 받음 — 프리펩에 참조 유지하기 위함
        /// </summary>
        private static void AddTrailRenderer(GameObject root, Material trailMat)
        {
            var trail = root.AddComponent<TrailRenderer>();
            trail.time = 0.3f;
            // 굵기는 transform.lossyScale에 곱해짐 → 본체 scale 5 환경에서
            // startWidth 1.0 = 실제 5유닛 (폭탄 지름 3.2보다 살짝 큰 잔상)
            trail.startWidth = 1.0f;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.05f;
            trail.numCornerVertices = 2;
            trail.numCapVertices = 2;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.sortingOrder = 7; // 본체(8) 바로 아래

            // 색 그라데이션: 주황 (alpha 0.9) → 투명
            var grad = new Gradient();
            grad.SetKeys(
                new[] {
                    new GradientColorKey(BombOrange, 0f),
                    new GradientColorKey(BombOrange, 1f)
                },
                new[] {
                    new GradientAlphaKey(0.9f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            trail.colorGradient = grad;

            // 머티리얼 에셋 참조 — 프리펩 저장 시 fileID로 보존됨
            // (런타임 new Material()을 sharedMaterial에 넣으면 프리펩 저장 시 null로 빠져
            //  Unity 기본 마젠타/핑크 'Missing Material' 색으로 렌더링됨 — 이전 버그 원인)
            if (trailMat != null) trail.sharedMaterial = trailMat;
        }

        /// <summary>
        /// 폭발 VFX 프리펩 — 스프라이트 + BombExplosionFx 컴포넌트 (스케일 확장 + 알파 페이드 + 자동 파괴)
        /// </summary>
        private static GameObject CreateExplosionFxPrefab(Sprite sprite)
        {
            var root = new GameObject("BombExplosionFx");
            try
            {
                // 탑뷰 (X=90) — 스프라이트가 XZ 평면에 누움
                root.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                // 시작 스케일은 BombExplosionFx가 매 프레임 덮어씀 (0.5 → 4.0)
                root.transform.localScale = Vector3.one * 0.5f;

                var sr = root.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                // 따뜻한 흰빛 (스프라이트 자체가 흰→노랑→주황→빨강 그라데이션을 가지고 있음)
                sr.color = new Color(1f, 1f, 1f, 1f);
                sr.sortingOrder = 9; // 폭탄(8)보다 위, 다른 모든 시각 효과 위로

                root.AddComponent<BombExplosionFx>();

                return SaveAsPrefab(root, ExplosionFxPrefabPath, "BombExplosionFx");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateMarkerPrefab(Sprite sprite)
        {
            var root = new GameObject("BombLandingMarker");
            try
            {
                // 탑뷰 (X=90) — XZ 평면에 누움
                root.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                // scale=1이 표준. BombProjectile.SpawnLandingMarker가 ExplosionRadius×2로 덮어씀
                // → 폭발 반경(1.8) × 2 = 3.6 유닛 지름으로 자동 표시
                root.transform.localScale = Vector3.one;

                // 살짝 띄워서 지면 z-fighting 방지 (BombProjectile에서 Y=0.02 위치로도 스폰)
                // 프리펩 자체는 Y=0 — 위치는 스폰 시 Initialize가 결정

                var sr = root.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                // 주황 + 알파 — 마커 컴포넌트가 알파를 펄스로 갱신
                sr.color = new Color(BombOrange.r, BombOrange.g, BombOrange.b, 0.25f);
                sr.sortingOrder = 4; // 폭탄(8)보다 아래, 지면 위

                root.AddComponent<BombLandingMarker>();

                return SaveAsPrefab(root, MarkerPrefabPath, "BombLandingMarker");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject SaveAsPrefab(GameObject root, string path, string label)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
            if (!success || prefab == null)
            {
                Debug.LogError($"[BombPrefabCreator] {label} 프리펩 저장 실패: {path}");
                return null;
            }
            return prefab;
        }

        // ═══════════ 데이터 (BombData SO) ═══════════

        private static BombData CreateOrUpdateData(GameObject projectilePrefab, GameObject markerPrefab, GameObject explosionPrefab)
        {
            var data = AssetDatabase.LoadAssetAtPath<BombData>(DataAssetPath);
            bool isNew = data == null;

            if (isNew)
            {
                data = ScriptableObject.CreateInstance<BombData>();
                AssetDatabase.CreateAsset(data, DataAssetPath);
            }

            // SerializedObject로 private 필드까지 안전하게 설정 (값 + 프리펩 참조)
            var so = new SerializedObject(data);

            // === WeaponData (베이스 필드) ===
            // 신규 생성 시에만 기본값 채움 — 사용자가 인스펙터에서 튜닝한 값 보존
            if (isNew)
            {
                SetString(so, "_displayName", "폭탄");
                SetString(so, "_description", "수동 클릭 발사. 도달 위치 폭발 AoE.");
                SetColor(so, "_themeColor", BombOrange);
                SetFloat(so, "_fireDelay", 6f);
                SetFloat(so, "_damage", 3f);
                SetFloat(so, "_explosionRadius", 1.8f);
                SetFloat(so, "_projectileSpeed", 5f);
                SetFloat(so, "_projectileLifetime", 5f);
                SetFloat(so, "_explosionVfxLifetime", 1.5f);
            }

            // === 프리펩 참조는 항상 갱신 (재생성된 프리펩으로 교체) ===
            SetObject(so, "_projectilePrefab", projectilePrefab);
            SetObject(so, "_landingMarkerPrefab", markerPrefab);
            SetObject(so, "_explosionVfxPrefab", explosionPrefab);
            // hitVfx는 사용자 선택 — 비워두거나 다른 VFX 할당 가능. 메뉴에선 건드리지 않음.

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);

            return data;
        }

        private static void SetString(SerializedObject so, string field, string v)
        {
            var p = so.FindProperty(field);
            if (p != null) p.stringValue = v;
        }
        private static void SetFloat(SerializedObject so, string field, float v)
        {
            var p = so.FindProperty(field);
            if (p != null) p.floatValue = v;
        }
        private static void SetColor(SerializedObject so, string field, Color v)
        {
            var p = so.FindProperty(field);
            if (p != null) p.colorValue = v;
        }
        private static void SetObject(SerializedObject so, string field, Object v)
        {
            var p = so.FindProperty(field);
            if (p != null) p.objectReferenceValue = v;
        }
    }
}
#endif
