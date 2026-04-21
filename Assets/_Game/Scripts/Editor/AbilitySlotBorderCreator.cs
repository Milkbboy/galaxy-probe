#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DrillCorp.EditorTools
{
    /// <summary>
    /// 어빌리티 슬롯 UI 의 테두리(_border) Image 가 쓸 9-slice 테두리 Sprite 를 생성한다.
    ///
    /// 출력:
    ///   · Assets/_Game/Prefabs/UI/AbilitySlotBorder.png (16×16, 가장자리 1px 흰색 + 내부 투명)
    ///   · TextureImporter 설정: Sprite (Single), Pixels Per Unit=100, FilterMode=Point,
    ///     Border=(4,4,4,4) — 9-slice 가장자리 고정
    ///
    /// 사용:
    ///   · 메뉴 실행 후 Game.unity 의 AbilitySlotUI 3개의 _border Image 에 해당 Sprite 할당
    ///   · Image.Type = Sliced 로 변경 (스크립트가 매 프레임 체크하지 않으므로 씬에서 직접 세팅)
    ///   · Image.color 는 AbilitySlotUI.Bind 에서 테마색 × α=0.9 로 자동 설정
    ///
    /// v2.html drawItemUI 의 `ctx.stroke(roundRect)` 1px 테마색 외곽선 포팅.
    ///
    /// 메뉴: Tools/Drill-Corp/3. 게임 초기 설정/UI/어빌리티 슬롯 테두리 스프라이트 생성
    /// </summary>
    public static class AbilitySlotBorderCreator
    {
        private const string OutputFolder = "Assets/_Game/Prefabs/UI";
        private const string OutputPath = OutputFolder + "/AbilitySlotBorder.png";

        // 텍스처 크기 — 9-slice 로 확대되므로 작게. 16×16 충분.
        private const int TextureSize = 16;
        // 실제 테두리 두께(픽셀). 1이면 얇은 1px 선.
        private const int BorderThicknessPx = 1;
        // 9-slice 가장자리 고정 영역 — 두께보다 크게 잡으면 안전.
        private const int SliceMarginPx = 4;

        [MenuItem("Tools/Drill-Corp/3. 게임 초기 설정/UI/어빌리티 슬롯 테두리 스프라이트 생성")]
        public static void CreateBorderSprite()
        {
            EnsureFolders();

            // ── 1. Texture2D 그리기 ──
            int size = TextureSize;
            int t = BorderThicknessPx;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false);
            var transparent = new Color(0f, 0f, 0f, 0f);
            var white = Color.white;
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool onBorder = x < t || x >= size - t || y < t || y >= size - t;
                    pixels[y * size + x] = onBorder ? white : transparent;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            // ── 2. PNG 로 저장 ──
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            File.WriteAllBytes(OutputPath, png);
            AssetDatabase.ImportAsset(OutputPath, ImportAssetOptions.ForceUpdate);

            // ── 3. TextureImporter 설정 (Sprite + 9-slice border) ──
            var importer = AssetImporter.GetAtPath(OutputPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[AbilitySlotBorderCreator] TextureImporter 얻지 못함: {OutputPath}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Point; // 9-slice 가장자리 선명
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            // 9-slice Border — 16×16 texture 안에서 left/bottom/right/top 각 4px 고정.
            // 다른 해상도의 Image에 할당해도 모서리 4px가 고정되고 가운데만 늘어나 테두리 두께 유지.
            // Single 모드는 TextureImporter.spriteBorder 만으로 충분 (spritesheet 배열 불필요).
            importer.spriteBorder = new Vector4(SliceMarginPx, SliceMarginPx, SliceMarginPx, SliceMarginPx);

            importer.SaveAndReimport();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(OutputPath);
            if (sprite != null)
            {
                Debug.Log($"[AbilitySlotBorderCreator] ✓ Sprite 생성 완료: {OutputPath}\n" +
                          "다음 단계: Game.unity 의 AbilitySlotUI 3개 (Victor 슬롯 1/2/3) 각각 " +
                          "_border Image 컴포넌트에 이 Sprite 를 할당하고 Image Type 을 Sliced 로 변경하세요.");
                Selection.activeObject = sprite;
                EditorGUIUtility.PingObject(sprite);
            }
            else
            {
                Debug.LogError($"[AbilitySlotBorderCreator] Sprite 로드 실패: {OutputPath}");
            }
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Prefabs"))
                AssetDatabase.CreateFolder("Assets/_Game", "Prefabs");
            if (!AssetDatabase.IsValidFolder(OutputFolder))
                AssetDatabase.CreateFolder("Assets/_Game/Prefabs", "UI");
        }
    }
}
#endif
