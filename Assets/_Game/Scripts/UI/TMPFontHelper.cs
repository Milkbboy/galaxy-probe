using UnityEngine;
using TMPro;

namespace DrillCorp.UI
{
    /// <summary>
    /// TextMeshPro 폰트 헬퍼 - D2Coding 폰트 적용
    /// </summary>
    public static class TMPFontHelper
    {
        private static TMP_FontAsset _cachedFont;
        private static TMP_FontAsset _cachedFontBold;
        private static bool _initialized;

        /// <summary>
        /// 폰트 초기화 (FontHolder에서 호출)
        /// </summary>
        public static void Initialize(TMP_FontAsset defaultFont, TMP_FontAsset boldFont = null)
        {
            _cachedFont = defaultFont;
            _cachedFontBold = boldFont ?? defaultFont;
            _initialized = true;
        }

        /// <summary>
        /// 기본 폰트 (D2Coding)
        /// </summary>
        public static TMP_FontAsset DefaultFont
        {
            get
            {
                if (!_initialized || _cachedFont == null)
                {
                    // TMP 기본 폰트 사용
                    return TMP_Settings.defaultFontAsset;
                }
                return _cachedFont;
            }
        }

        /// <summary>
        /// 볼드 폰트 (D2CodingBold)
        /// </summary>
        public static TMP_FontAsset BoldFont
        {
            get
            {
                if (!_initialized || _cachedFontBold == null)
                {
                    return DefaultFont;
                }
                return _cachedFontBold;
            }
        }

        /// <summary>
        /// TextMeshPro에 기본 폰트 적용
        /// </summary>
        public static void ApplyDefaultFont(TMP_Text tmp)
        {
            if (tmp == null) return;

            var font = DefaultFont;
            if (font != null)
            {
                tmp.font = font;
            }
        }

        /// <summary>
        /// TextMeshPro에 볼드 폰트 적용
        /// </summary>
        public static void ApplyBoldFont(TMP_Text tmp)
        {
            if (tmp == null) return;

            var font = BoldFont;
            if (font != null)
            {
                tmp.font = font;
            }
        }
    }
}
