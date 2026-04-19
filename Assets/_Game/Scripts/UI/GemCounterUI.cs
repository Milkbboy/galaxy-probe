using UnityEngine;
using TMPro;
using DrillCorp.Core;

namespace DrillCorp.UI
{
    /// <summary>
    /// 인게임 HUD의 세션 보석 카운터. MiningUI와 동일한 펀치 애니메이션 패턴.
    /// OnGemCollected 이벤트 발생마다 +amount, 세션 시작(OnSessionStarted 없으므로 OnEnable)에 리셋.
    /// </summary>
    public class GemCounterUI : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private TextMeshProUGUI _gemText;
        [SerializeField] private string _prefix = "보석: ";

        [Header("Animation")]
        [SerializeField] private float _punchScale = 1.2f;
        [SerializeField] private float _punchDuration = 0.1f;

        private Vector3 _originalScale;
        private float _punchTimer;
        private int _sessionGems;

        private void Awake()
        {
            if (_gemText != null)
                _originalScale = _gemText.transform.localScale;
        }

        private void OnEnable()
        {
            _sessionGems = 0;
            UpdateText();
            GameEvents.OnGemCollected += OnGemCollected;
        }

        private void OnDisable()
        {
            GameEvents.OnGemCollected -= OnGemCollected;
        }

        private void Update()
        {
            UpdatePunchAnimation();
        }

        private void OnGemCollected(int amount)
        {
            _sessionGems += amount;
            _punchTimer = _punchDuration;
            UpdateText();
        }

        private void UpdateText()
        {
            if (_gemText != null)
                _gemText.text = $"{_prefix}{_sessionGems}";
        }

        private void UpdatePunchAnimation()
        {
            if (_gemText == null) return;

            if (_punchTimer > 0f)
            {
                _punchTimer -= Time.deltaTime;
                float t = _punchTimer / _punchDuration;
                float scale = Mathf.Lerp(1f, _punchScale, t);
                _gemText.transform.localScale = _originalScale * scale;
            }
            else
            {
                _gemText.transform.localScale = _originalScale;
            }
        }
    }
}
