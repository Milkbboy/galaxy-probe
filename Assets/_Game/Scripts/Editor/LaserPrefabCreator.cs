#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using DrillCorp.Weapon.Laser;

namespace DrillCorp.EditorTools
{
    /// <summary>
    /// 레이저(Phase 4) 자산 일괄 생성 — 구현 단계와 쌍으로 점진적 작성.
    ///
    /// 단계 1: Weapon_LaserBeam.asset (LaserWeaponData SO)
    /// 단계 2 (현재): LaserGlow.png + Laser_LifeArc.mat + LaserBeam.prefab
    ///   - 4겹 SR 자식 (OuterGlow / RingStroke / Core / Center)
    ///   - LifeArc LineRenderer 자식 (useWorldSpace=false, 탑뷰 XZ 평면 정렬)
    ///   - SO의 _beamPrefab 참조 자동 바인딩
    ///
    /// 레거시 공존: Weapon_Laser.asset(LaserBeamData) / LaserBeamField.prefab 은 건드리지 않음.
    /// </summary>
    public static class LaserPrefabCreator
    {
        // === 경로 ===
        private const string PrefabFolder = "Assets/_Game/Prefabs/Weapons";
        private const string DataFolder = "Assets/_Game/Data/Weapons";

        private const string BeamSpritePath = PrefabFolder + "/LaserGlow.png";
        private const string LifeArcMaterialPath = PrefabFolder + "/Laser_LifeArc.mat";
        private const string BeamPrefabPath = PrefabFolder + "/LaserBeam.prefab";
        private const string DataAssetPath = DataFolder + "/Weapon_LaserBeam.asset";

        // === 색 (프로토타입 _.html L307) ===
        private static readonly Color LaserRed = new Color(1f, 0.09f, 0.267f, 1f);      // #ff1744
        private static readonly Color LaserPink = new Color(1f, 0.376f, 0.565f, 1f);    // #ff6090
        private static readonly Color LaserCenter = new Color(1f, 0.784f, 0.823f, 1f);  // #ffc8d2

        // 기본 레이어 알파 (LaserBeam.Update의 BaseAlpha* 상수와 일치)
        private const float BaseAlphaOuter = 0.12f;
        private const float BaseAlphaRing = 0.60f;
        private const float BaseAlphaCore = 0.35f;
        private const float BaseAlphaCoreStroke = 0.90f;
        private const float BaseAlphaCenter = 0.70f;
        private const float BaseAlphaCrosshair = 0.70f;

        // 기본 반경 — 프로토 환산치(0.48)에서 시각/플레이감 위해 키움
        private const float DefaultRadius = 1.0f;
        private const float LifeArcRadiusOffset = 0.2f;
        private const float CrosshairOffset = 0.15f;    // 빔 반경 비례 (프로토 +4px 비율)

        [MenuItem("Tools/Drill-Corp/3. 게임 초기 설정/8. 레이저 자산 일괄 생성")]
        public static void CreateAll()
        {
            EnsureFolders();

            // === 단계 2 — 스프라이트 ===
            bool spriteCreated = EnsureSprite(BeamSpritePath, CreateGlowPng);
            if (spriteCreated)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            var glowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BeamSpritePath);
            if (glowSprite == null)
            {
                Debug.LogError("[LaserPrefabCreator] LaserGlow 스프라이트 로드 실패. 메뉴 재실행 바람.");
                return;
            }

            // === 단계 2 — LifeArc 머티리얼 ===
            var lifeArcMat = EnsureLifeArcMaterial();

            // === 단계 2 — LaserBeam 프리펩 ===
            var beamPrefab = CreateBeamPrefab(glowSprite, lifeArcMat);
            if (beamPrefab == null) return;

            // === 단계 1 — SO (프리펩 참조 바인딩) ===
            var data = CreateOrUpdateData(beamPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = data;
            EditorGUIUtility.PingObject(data);

            Debug.Log(
                "[LaserPrefabCreator] ✅ 단계 2 완료\n" +
                $"  • {BeamSpritePath}\n" +
                $"  • {LifeArcMaterialPath}\n" +
                $"  • {BeamPrefabPath}\n" +
                $"  • {DataAssetPath} (BeamPrefab 자동 바인딩)\n" +
                "다음 단계(3): LaserWeapon.cs — 자체 구동 상태머신 + UI 오버라이드"
            );
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

        /// <summary>PNG 보장 + TextureImporter 설정 (PPU=128 → 지름 1 유닛). 신규 생성 여부 반환.</summary>
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
            // PPU=128, PNG 128×128 → 지름 1 유닛. 런타임에 localScale로 최종 지름 지정.
            if (Mathf.Abs(importer.spritePixelsPerUnit - 128f) > 0.01f) { importer.spritePixelsPerUnit = 128f; changed = true; }
            if (importer.filterMode != FilterMode.Bilinear) { importer.filterMode = FilterMode.Bilinear; changed = true; }
            if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }

            if (changed || !existed) importer.SaveAndReimport();
            return !existed;
        }

        /// <summary>
        /// 레이저 글로우 PNG — 128×128 흰색 라디얼 그라디언트.
        /// 중앙 100% 알파 → 가장자리 0%. 색은 런타임에 SpriteRenderer.color로 덮어씀.
        /// (4겹 SR 전부 이 스프라이트 공유, 색/스케일/알파만 다름)
        /// </summary>
        private static void CreateGlowPng()
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];

            float c = (size - 1) * 0.5f;
            float maxR = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - c, dy = y - c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float t = Mathf.Clamp01(d / maxR);

                    // 중앙→가장자리 알파 페이드 (부드럽게 — 내부 80%까지 강한 알파, 바깥 20% 페이드)
                    float a;
                    if (t < 0.80f)
                        a = 1f - t * 0.3f;              // 1.0 → 0.76
                    else
                        a = (1f - (t - 0.80f) / 0.20f) * 0.76f;  // 0.76 → 0

                    a = Mathf.Clamp01(a);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(BeamSpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        // ═══════════ 머티리얼 (LifeArc LineRenderer용) ═══════════

        /// <summary>
        /// LineRenderer용 머티리얼 — Sprites/Default (vertex color alpha blend 지원).
        /// 에셋으로 저장해야 프리펩 참조 보존됨 (런타임 new Material() 사용 금지 — BombPrefabCreator 교훈).
        /// </summary>
        private static Material EnsureLifeArcMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(LifeArcMaterialPath);
            if (mat != null) return mat;

            Shader sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
            if (sh == null)
            {
                Debug.LogError("[LaserPrefabCreator] LifeArc 머티리얼용 셰이더를 찾을 수 없습니다.");
                return null;
            }

            mat = new Material(sh) { name = "Laser_LifeArc" };
            AssetDatabase.CreateAsset(mat, LifeArcMaterialPath);
            return mat;
        }

        // ═══════════ 프리펩 (LaserBeam) ═══════════

        private static GameObject CreateBeamPrefab(Sprite glowSprite, Material lifeArcMat)
        {
            var root = new GameObject("LaserBeam");
            try
            {
                // 탑뷰 (X=90) — local XY 평면이 월드 XZ 평면에 누움
                root.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                // Root scale 2 — 빔 전체 시각 크기 ×2 (자식 scale 곱연산). 데미지 OverlapSphere(_radius)는 영향 받지 않음.
                root.transform.localScale = Vector3.one * 2f;

                var beam = root.AddComponent<LaserBeam>();

                // 외곽 2겹 — LineRenderer 링(프로토 ctx.stroke 재현, 선명한 외곽선)
                //   OuterGlow: 굵고 희미한 빨간 글로우 링 (알파 0.12 × pulse)
                //   RingStroke: 얇고 또렷한 분홍 링 (알파 0.60) ← 데미지 범위 경계 UX
                var outerGlow = CreateRing(root, "OuterGlow", DefaultRadius + 0.13f, width: 0.10f,
                    LaserRed, BaseAlphaOuter, sortingOrder: 8, lifeArcMat);
                var ringStroke = CreateRing(root, "RingStroke", DefaultRadius + 0.05f, width: 0.03f,
                    LaserPink, BaseAlphaRing, sortingOrder: 9, lifeArcMat);

                // 내부 2겹 — SpriteRenderer 채움(프로토 ctx.fill 재현)
                //   Core: 빔 몸체 빨간 채움 (알파 0.35 × pulse)
                //   Center: 밝은 분홍 핵 (알파 0.70)
                var core = CreateFillLayer(root, "Core", glowSprite, LaserRed, BaseAlphaCore,
                    DefaultRadius * 2f, sortingOrder: 10);
                // CoreStroke — Core fill 외곽 진한 빨간 외곽선 (프로토 alpha 0.9, 빔 윤곽 선명도 핵심)
                var coreStroke = CreateRing(root, "CoreStroke", DefaultRadius, width: 0.025f,
                    LaserRed, BaseAlphaCoreStroke, sortingOrder: 11, lifeArcMat);
                var center = CreateFillLayer(root, "Center", glowSprite, LaserCenter, BaseAlphaCenter,
                    (DefaultRadius * 0.45f) * 2f, sortingOrder: 12);

                // 십자 (프로토 L307 cr=r+4 분홍 가로/세로 선)
                float cr = DefaultRadius + CrosshairOffset;
                var crosshairH = CreateCrosshairLine(root, "CrosshairH", true, cr, width: 0.018f,
                    LaserPink, BaseAlphaCrosshair, sortingOrder: 13, lifeArcMat);
                var crosshairV = CreateCrosshairLine(root, "CrosshairV", false, cr, width: 0.018f,
                    LaserPink, BaseAlphaCrosshair, sortingOrder: 13, lifeArcMat);

                // 빔 영역 분홍 스파클 fx (프로토 L294 매 프레임 2개 파티클)
                var centerParticles = CreateBeamParticles(root, DefaultRadius);

                // LifeArc LineRenderer 자식 (수명 호)
                var lifeArc = CreateLifeArc(root, lifeArcMat);

                // 컴포넌트 바인딩 (SerializedObject로 private 필드 세팅)
                var so = new SerializedObject(beam);
                BindObject(so, "_outerGlow", outerGlow);
                BindObject(so, "_ringStroke", ringStroke);
                BindObject(so, "_coreStroke", coreStroke);
                BindObject(so, "_core", core);
                BindObject(so, "_center", center);
                BindObject(so, "_crosshairH", crosshairH);
                BindObject(so, "_crosshairV", crosshairV);
                BindObject(so, "_centerParticles", centerParticles);
                SetFloat(so, "_crosshairOffset", CrosshairOffset);
                BindObject(so, "_lifeArc", lifeArc);
                SetInt(so, "_lifeArcSegments", 64);
                SetFloat(so, "_lifeArcRadiusOffset", LifeArcRadiusOffset);
                so.ApplyModifiedPropertiesWithoutUndo();

                return SaveAsPrefab(root, BeamPrefabPath, "LaserBeam");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// 채움(filled) 레이어 — Core/Center용 SpriteRenderer.
        /// 스프라이트 지름 1 유닛(PPU=128) 기준 → localScale 값이 그대로 목표 지름.
        /// </summary>
        private static SpriteRenderer CreateFillLayer(
            GameObject parent, string name, Sprite sprite, Color baseColor, float baseAlpha, float diameter, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, worldPositionStays: false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = new Vector3(diameter, diameter, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseAlpha);
            sr.sortingOrder = sortingOrder;
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sr.receiveShadows = false;
            return sr;
        }

        /// <summary>
        /// 링(stroke) 레이어 — OuterGlow/RingStroke용 LineRenderer.
        /// loop=true로 원을 닫음. positionCount=64로 원주를 근사.
        /// 반경은 런타임 _radius 업그레이드 시 LaserBeam.RebuildRing이 재구성.
        /// </summary>
        private static LineRenderer CreateRing(
            GameObject parent, string name, float radius, float width,
            Color baseColor, float baseAlpha, int sortingOrder, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, worldPositionStays: false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;                       // 원을 닫음
            lr.alignment = LineAlignment.TransformZ;  // 탑뷰(X=90°)에서 XZ 평면에 누움
            lr.textureMode = LineTextureMode.Stretch;

            lr.startWidth = width;
            lr.endWidth = width;
            lr.numCornerVertices = 2;
            lr.numCapVertices = 0;                // loop라 cap 불필요

            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.sortingOrder = sortingOrder;

            if (mat != null) lr.sharedMaterial = mat;

            const int segments = 64;
            lr.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float a = (float)i / segments * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));
            }

            var c = new Color(baseColor.r, baseColor.g, baseColor.b, baseAlpha);
            lr.startColor = c;
            lr.endColor = c;

            return lr;
        }

        /// <summary>
        /// 십자(crosshair) 직선 LineRenderer — 빔 local 좌표 (-cr~+cr) 양 끝 두 점.
        /// 빔 X+90 회전에 의해 local X→월드 X(가로), local Y→월드 Z(세로) 매핑.
        /// </summary>
        private static LineRenderer CreateCrosshairLine(
            GameObject parent, string name, bool horizontal, float cr, float width,
            Color baseColor, float baseAlpha, int sortingOrder, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, worldPositionStays: false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = false;
            lr.alignment = LineAlignment.TransformZ;
            lr.textureMode = LineTextureMode.Stretch;
            lr.startWidth = width;
            lr.endWidth = width;
            lr.numCapVertices = 0;
            lr.numCornerVertices = 0;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.sortingOrder = sortingOrder;
            if (mat != null) lr.sharedMaterial = mat;

            lr.positionCount = 2;
            if (horizontal)
            {
                lr.SetPosition(0, new Vector3(-cr, 0f, 0f));
                lr.SetPosition(1, new Vector3(+cr, 0f, 0f));
            }
            else
            {
                lr.SetPosition(0, new Vector3(0f, -cr, 0f));
                lr.SetPosition(1, new Vector3(0f, +cr, 0f));
            }

            var c = new Color(baseColor.r, baseColor.g, baseColor.b, baseAlpha);
            lr.startColor = c;
            lr.endColor = c;
            return lr;
        }

        /// <summary>
        /// 빔 영역 분홍 스파클 fx — 프로토 L294 매 프레임 2개 파티클(60fps × 2 = 120/s).
        /// Local space 시뮬레이션 → 빔 따라다님. Shape Circle radius는 LaserBeam.Initialize에서 _radius로 재세팅.
        /// </summary>
        private static ParticleSystem CreateBeamParticles(GameObject parent, float defaultRadius)
        {
            var go = new GameObject("CenterParticles");
            go.transform.SetParent(parent.transform, worldPositionStays: false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var ps = go.AddComponent<ParticleSystem>();
            // ParticleSystem AddComponent 시 자동 생성된 ParticleSystemRenderer를 가져옴 (안전 폴백)
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (renderer == null) renderer = go.AddComponent<ParticleSystemRenderer>();

            // Main module
            var main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);   // 더 길게 보이도록
            main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.22f);      // 시인성을 위해 크게 (프로토 환산보다 큼)
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);       // 살짝 더 활발
            main.startColor = new ParticleSystem.MinMaxGradient(LaserPink);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;          // 빔 따라다님
            main.maxParticles = 200;
            main.playOnAwake = true;

            // Emission — 풍성한 fx (프로토 120/s 보다 크게 — 빔 반경 키운 만큼)
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 200f;

            // Shape — 빔 반경 안 무작위 위치
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = defaultRadius;
            shape.radiusMode = ParticleSystemShapeMultiModeValue.Random;
            shape.arc = 360f;

            // Color over Lifetime — 분홍 → 투명 페이드
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(LaserPink, 0f), new GradientColorKey(LaserPink, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = new ParticleSystem.MinMaxGradient(grad);

            // Renderer
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.Facing;
            renderer.sortingOrder = 14;   // 빔 모든 레이어 위
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            // Default Particle 머티리얼 (Unity built-in) — 흰색 원형 텍스처 + alpha blend
            var defaultParticleMat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");
            if (defaultParticleMat != null) renderer.sharedMaterial = defaultParticleMat;

            return ps;
        }

        /// <summary>
        /// LifeArc LineRenderer — 빔 local 좌표계에서 원주를 그림.
        /// useWorldSpace=false → 빔 이동 시 함께 따라옴, 빔 회전(90,0,0)에 의해 XZ 평면에 평평히 누움.
        /// positionCount는 런타임에 life/maxLife로 재계산되므로 프리펩 초기값만 세팅.
        /// </summary>
        private static LineRenderer CreateLifeArc(GameObject parent, Material mat)
        {
            var go = new GameObject("LifeArc");
            go.transform.SetParent(parent.transform, worldPositionStays: false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = false;
            lr.alignment = LineAlignment.TransformZ;  // 탑뷰(X=90°)에서 XZ 평면에 누움
            lr.textureMode = LineTextureMode.Stretch;

            lr.startWidth = 0.04f;
            lr.endWidth = 0.04f;
            lr.numCornerVertices = 2;
            lr.numCapVertices = 2;

            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.sortingOrder = 12;   // 4겹 SR 위

            if (mat != null) lr.sharedMaterial = mat;

            // 초기 색 — 런타임에 알파를 매 프레임 재세팅
            var c = new Color(LaserRed.r, LaserRed.g, LaserRed.b, 0.8f);
            lr.startColor = c;
            lr.endColor = c;

            // 초기 positionCount — 런타임이 재구성
            lr.positionCount = 2;
            lr.SetPosition(0, Vector3.zero);
            lr.SetPosition(1, Vector3.zero);

            return lr;
        }

        private static GameObject SaveAsPrefab(GameObject root, string path, string label)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
            if (!success || prefab == null)
            {
                Debug.LogError($"[LaserPrefabCreator] {label} 프리펩 저장 실패: {path}");
                return null;
            }
            return prefab;
        }

        // ═══════════ 데이터 (LaserWeaponData SO) ═══════════

        private static LaserWeaponData CreateOrUpdateData(GameObject beamPrefab)
        {
            var data = AssetDatabase.LoadAssetAtPath<LaserWeaponData>(DataAssetPath);
            bool isNew = data == null;

            if (isNew)
            {
                data = ScriptableObject.CreateInstance<LaserWeaponData>();
                AssetDatabase.CreateAsset(data, DataAssetPath);
            }

            var so = new SerializedObject(data);

            if (isNew)
            {
                SetString(so, "_displayName", "레이저");
                SetString(so, "_description", "쿨다운 자동발사. 마우스 추적 6초 + 5초 쿨.");
                SetColor(so, "_themeColor", LaserRed);
                SetFloat(so, "_damage", 0.8f);
                SetFloat(so, "_hitVfxLifetime", 0.5f);

                SetFloat(so, "_cooldown", 5f);
                SetFloat(so, "_beamDuration", 6f);
                SetFloat(so, "_beamSpeed", 1.725f);
                SetFloat(so, "_stopDistance", 0.033f);
                SetFloat(so, "_beamRadius", 0.48f);
                SetFloat(so, "_tickInterval", 0.1f);
            }

            // === 매번 강제 ===
            SetFloat(so, "_fireDelay", 0f);
            if (beamPrefab != null)
                SetObject(so, "_beamPrefab", beamPrefab);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);

            return data;
        }

        // ═══════════ SerializedObject 유틸 ═══════════

        private static void SetString(SerializedObject so, string field, string v)
        {
            var p = so.FindProperty(field); if (p != null) p.stringValue = v;
        }

        private static void SetFloat(SerializedObject so, string field, float v)
        {
            var p = so.FindProperty(field); if (p != null) p.floatValue = v;
        }

        private static void SetInt(SerializedObject so, string field, int v)
        {
            var p = so.FindProperty(field); if (p != null) p.intValue = v;
        }

        private static void SetColor(SerializedObject so, string field, Color v)
        {
            var p = so.FindProperty(field); if (p != null) p.colorValue = v;
        }

        private static void SetObject(SerializedObject so, string field, Object v)
        {
            var p = so.FindProperty(field); if (p != null) p.objectReferenceValue = v;
        }

        private static void BindObject(SerializedObject so, string field, Object obj)
        {
            var p = so.FindProperty(field); if (p != null) p.objectReferenceValue = obj;
        }
    }
}
#endif
