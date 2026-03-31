using UnityEngine;
using TMPro;

namespace DrillCorp.UI
{
    /// <summary>
    /// TMP 폰트 에셋 보관 및 초기화
    /// 씬에 배치하여 D2Coding 폰트를 드래그로 연결
    /// </summary>
    public class TMPFontHolder : MonoBehaviour
    {
        [Header("Fonts")]
        [SerializeField] private TMP_FontAsset _defaultFont;
        [SerializeField] private TMP_FontAsset _boldFont;

        private static TMPFontHolder _instance;
        public static TMPFontHolder Instance => _instance;

        public TMP_FontAsset DefaultFont => _defaultFont;
        public TMP_FontAsset BoldFont => _boldFont;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeFonts();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeFonts()
        {
            TMPFontHelper.Initialize(_defaultFont, _boldFont);
            Debug.Log("[TMPFontHolder] Fonts initialized");
        }
    }
}
