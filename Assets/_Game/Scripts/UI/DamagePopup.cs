using UnityEngine;
using TMPro;

namespace DrillCorp.UI
{
    /// <summary>
    /// 데미지 팝업 텍스트
    /// </summary>
    public class DamagePopup : MonoBehaviour
    {
        [SerializeField] private TextMeshPro _text;
        [SerializeField] private float _floatSpeed = 1f;
        [SerializeField] private float _lifetime = 1f;

        private float _timer;
        private Color _color;
        private Vector3 _moveDirection;

        public static DamagePopup Create(Vector3 position, float damage, PopupType type = PopupType.Normal)
        {
            // 탑다운 뷰: Z축이 화면 위쪽
            GameObject popupObj = new GameObject("DamagePopup");
            popupObj.transform.position = position + Vector3.forward * 0.5f + Vector3.up * 0.1f;

            DamagePopup popup = popupObj.AddComponent<DamagePopup>();
            popup.Initialize(damage, type);

            return popup;
        }

        /// <summary>
        /// 콜라이더 크기를 고려한 팝업 생성 (머신 등 큰 오브젝트용)
        /// </summary>
        public static DamagePopup Create(Transform target, float damage, PopupType type = PopupType.Normal)
        {
            Vector3 position = target.position;

            // 콜라이더가 있으면 경계 위쪽에 표시
            var collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                float radius = Mathf.Max(collider.bounds.extents.x, collider.bounds.extents.z);
                position += Vector3.forward * (radius + 0.3f);
            }
            else
            {
                position += Vector3.forward * 0.5f;
            }

            position += Vector3.up * 0.1f;

            GameObject popupObj = new GameObject("DamagePopup");
            popupObj.transform.position = position;

            DamagePopup popup = popupObj.AddComponent<DamagePopup>();
            popup.Initialize(damage, type);

            return popup;
        }

        public static DamagePopup CreateText(Vector3 position, string text, Color color)
        {
            // 탑다운 뷰: Z축이 화면 위쪽
            GameObject popupObj = new GameObject("DamagePopup");
            popupObj.transform.position = position + Vector3.forward * 0.5f + Vector3.up * 0.1f;

            DamagePopup popup = popupObj.AddComponent<DamagePopup>();
            popup.InitializeText(text, color);

            return popup;
        }

        /// <summary>
        /// 콜라이더 크기를 고려한 텍스트 팝업 생성
        /// </summary>
        public static DamagePopup CreateText(Transform target, string text, Color color)
        {
            Vector3 position = target.position;

            var collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                float radius = Mathf.Max(collider.bounds.extents.x, collider.bounds.extents.z);
                position += Vector3.forward * (radius + 0.3f);
            }
            else
            {
                position += Vector3.forward * 0.5f;
            }

            position += Vector3.up * 0.1f;

            GameObject popupObj = new GameObject("DamagePopup");
            popupObj.transform.position = position;

            DamagePopup popup = popupObj.AddComponent<DamagePopup>();
            popup.InitializeText(text, color);

            return popup;
        }

        private void Initialize(float damage, PopupType type)
        {
            // TextMeshPro 컴포넌트 생성
            if (_text == null)
            {
                _text = gameObject.AddComponent<TextMeshPro>();
                _text.alignment = TextAlignmentOptions.Center;
                _text.fontSize = 4f;
                _text.sortingOrder = 200;
                TMPFontHelper.ApplyDefaultFont(_text);
            }

            // 타입별 설정
            switch (type)
            {
                case PopupType.Normal:
                    _text.text = Mathf.RoundToInt(damage).ToString();
                    _color = Color.white;
                    _text.fontSize = 4f;
                    break;

                case PopupType.Critical:
                    _text.text = Mathf.RoundToInt(damage).ToString() + "!";
                    _color = Color.yellow;
                    _text.fontSize = 5f;
                    break;

                case PopupType.Dodge:
                    _text.text = "DODGE";
                    _color = Color.cyan;
                    _text.fontSize = 3.5f;
                    break;

                case PopupType.Armor:
                    _text.text = Mathf.RoundToInt(damage).ToString();
                    _color = Color.gray;
                    _text.fontSize = 3f;
                    break;

                case PopupType.Heal:
                    _text.text = "+" + Mathf.RoundToInt(damage).ToString();
                    _color = Color.green;
                    _text.fontSize = 4f;
                    break;
            }

            _text.color = _color;

            // 랜덤 방향으로 떠오름 (탑다운 뷰: Z축이 위)
            _moveDirection = new Vector3(Random.Range(-0.3f, 0.3f), 0f, 1f).normalized;

            // 카메라를 향하도록 회전
            if (Camera.main != null)
            {
                transform.rotation = Camera.main.transform.rotation;
            }
        }

        private void InitializeText(string text, Color color)
        {
            if (_text == null)
            {
                _text = gameObject.AddComponent<TextMeshPro>();
                _text.alignment = TextAlignmentOptions.Center;
                _text.fontSize = 4f;
                _text.sortingOrder = 200;
                TMPFontHelper.ApplyDefaultFont(_text);
            }

            _text.text = text;
            _color = color;
            _text.color = _color;

            _moveDirection = new Vector3(Random.Range(-0.3f, 0.3f), 0f, 1f).normalized;

            if (Camera.main != null)
            {
                transform.rotation = Camera.main.transform.rotation;
            }
        }

        private void Update()
        {
            _timer += Time.deltaTime;

            // 위로 떠오름
            transform.position += _moveDirection * _floatSpeed * Time.deltaTime;

            // 페이드 아웃
            float alpha = 1f - (_timer / _lifetime);
            _text.color = new Color(_color.r, _color.g, _color.b, alpha);

            // 수명 종료
            if (_timer >= _lifetime)
            {
                Destroy(gameObject);
            }
        }
    }

    public enum PopupType
    {
        Normal,
        Critical,
        Dodge,
        Armor,
        Heal
    }
}
