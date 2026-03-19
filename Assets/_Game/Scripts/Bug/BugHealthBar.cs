using UnityEngine;

namespace DrillCorp.Bug
{
    public class BugHealthBar : MonoBehaviour
    {
        [Header("Sprites")]
        [SerializeField] private SpriteRenderer _backgroundRenderer;
        [SerializeField] private SpriteRenderer _fillRenderer;

        [Header("Settings")]
        [SerializeField] private Vector3 _offset = new Vector3(0f, 0.1f, 0.8f);
        [SerializeField] private Vector2 _barSize = new Vector2(0.5f, 0.08f);
        [SerializeField] private Color _fullColor = Color.green;
        [SerializeField] private Color _lowColor = Color.red;
        [SerializeField] private float _lowHealthThreshold = 0.3f;

        private Transform _target;
        private float _currentRatio = 1f;

        public void Initialize(Transform target)
        {
            _target = target;
            UpdatePosition();
            SetHealth(1f);
        }

        private void LateUpdate()
        {
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            if (_target != null)
            {
                transform.position = _target.position + _offset;
            }
        }

        public void SetHealth(float ratio)
        {
            _currentRatio = Mathf.Clamp01(ratio);

            if (_fillRenderer != null)
            {
                // Scale X로 HP 표현
                Vector3 scale = _fillRenderer.transform.localScale;
                scale.x = _barSize.x * _currentRatio;
                _fillRenderer.transform.localScale = scale;

                // 위치 조정 (왼쪽 정렬)
                Vector3 pos = _fillRenderer.transform.localPosition;
                pos.x = -(_barSize.x - scale.x) / 2f;
                _fillRenderer.transform.localPosition = pos;

                // 색상 변경
                _fillRenderer.color = _currentRatio <= _lowHealthThreshold ? _lowColor : _fullColor;
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 코드로 HP바 생성 (프리팹 없이)
        /// </summary>
        public static BugHealthBar Create(Transform bugTransform)
        {
            GameObject hpBarObj = new GameObject("HealthBar");
            BugHealthBar healthBar = hpBarObj.AddComponent<BugHealthBar>();

            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(hpBarObj.transform);
            bgObj.transform.localPosition = Vector3.zero;
            bgObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            SpriteRenderer bgRenderer = bgObj.AddComponent<SpriteRenderer>();
            bgRenderer.sprite = CreateSquareSprite();
            bgRenderer.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            bgRenderer.sortingOrder = 100;
            bgObj.transform.localScale = new Vector3(0.5f, 0.08f, 1f);

            // Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(hpBarObj.transform);
            fillObj.transform.localPosition = Vector3.zero;
            fillObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            SpriteRenderer fillRenderer = fillObj.AddComponent<SpriteRenderer>();
            fillRenderer.sprite = CreateSquareSprite();
            fillRenderer.color = Color.green;
            fillRenderer.sortingOrder = 101;
            fillObj.transform.localScale = new Vector3(0.5f, 0.08f, 1f);

            healthBar._backgroundRenderer = bgRenderer;
            healthBar._fillRenderer = fillRenderer;
            healthBar.Initialize(bugTransform);

            return healthBar;
        }

        private static Sprite CreateSquareSprite()
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
