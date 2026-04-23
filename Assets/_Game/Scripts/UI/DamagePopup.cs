using UnityEngine;
using TMPro;

namespace DrillCorp.UI
{
    /// <summary>
    /// 데미지 팝업 텍스트. <see cref="DamagePopupPool"/> 로 풀링 재사용.
    /// 공개 API (Create/CreateText) 는 기존 시그니처 그대로 — 내부 구현만 풀 경로로 전환.
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
            Vector3 spawnPos = position + Vector3.forward * 0.5f + Vector3.up * 0.1f;
            var popup = Spawn(spawnPos);
            popup.Initialize(damage, type);
            return popup;
        }

        /// <summary>
        /// 콜라이더 크기를 고려한 팝업 생성 (머신 등 큰 오브젝트용)
        /// </summary>
        public static DamagePopup Create(Transform target, float damage, PopupType type = PopupType.Normal)
        {
            var popup = Spawn(ResolvePositionForTarget(target));
            popup.Initialize(damage, type);
            return popup;
        }

        public static DamagePopup CreateText(Vector3 position, string text, Color color)
        {
            Vector3 spawnPos = position + Vector3.forward * 0.5f + Vector3.up * 0.1f;
            var popup = Spawn(spawnPos);
            popup.InitializeText(text, color);
            return popup;
        }

        /// <summary>
        /// 콜라이더 크기를 고려한 텍스트 팝업 생성
        /// </summary>
        public static DamagePopup CreateText(Transform target, string text, Color color)
        {
            var popup = Spawn(ResolvePositionForTarget(target));
            popup.InitializeText(text, color);
            return popup;
        }

        // ─── 내부 공통 헬퍼 ───

        private static Vector3 ResolvePositionForTarget(Transform target)
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
            return position;
        }

        // 풀에서 꺼내거나 첫 생성 — 위치/활성만 여기서 처리, 텍스트 설정은 호출자가 Initialize*.
        private static DamagePopup Spawn(Vector3 position)
        {
            var popup = DamagePopupPool.Acquire();
            popup.transform.position = position;
            popup.gameObject.SetActive(true);
            popup._timer = 0f; // 재활용 시 수명 초기화 필수
            return popup;
        }

        private void Initialize(float damage, PopupType type)
        {
            EnsureTextComponent();

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
            EnsureTextComponent();
            // CreateText 계열은 fontSize 를 건드리지 않아 마지막 상태가 남을 수 있음 → 기본값으로 복구
            _text.fontSize = 4f;
            _text.text = text;
            _color = color;
            _text.color = _color;

            _moveDirection = new Vector3(Random.Range(-0.3f, 0.3f), 0f, 1f).normalized;

            if (Camera.main != null)
            {
                transform.rotation = Camera.main.transform.rotation;
            }
        }

        // 풀에서 꺼낸 재활용 인스턴스는 이미 TextMeshPro 가 있으므로 첫 생성 때만 붙임.
        private void EnsureTextComponent()
        {
            if (_text == null)
            {
                _text = GetComponent<TextMeshPro>();
                if (_text == null)
                {
                    _text = gameObject.AddComponent<TextMeshPro>();
                    _text.alignment = TextAlignmentOptions.Center;
                    _text.fontSize = 4f;
                    _text.sortingOrder = 200;
                    TMPFontHelper.ApplyDefaultFont(_text);
                }
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

            // 수명 종료 — Destroy 대신 풀로 반환
            if (_timer >= _lifetime)
            {
                DamagePopupPool.Release(this);
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
