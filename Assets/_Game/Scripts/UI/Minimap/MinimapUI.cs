using UnityEngine;
using UnityEngine.UI;

namespace DrillCorp.UI.Minimap
{
    /// <summary>
    /// RenderTexture를 RawImage로 표시. 좌상단에 배치.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class MinimapUI : MonoBehaviour
    {
        [SerializeField] private MinimapCamera _minimapCamera;
        [SerializeField] private RawImage _rawImage;

        private void Awake()
        {
            if (_rawImage == null) _rawImage = GetComponent<RawImage>();
            if (_minimapCamera == null) _minimapCamera = FindAnyObjectByType<MinimapCamera>();
        }

        private void Start()
        {
            if (_minimapCamera != null && _minimapCamera.RenderTexture != null)
                _rawImage.texture = _minimapCamera.RenderTexture;
        }
    }
}
