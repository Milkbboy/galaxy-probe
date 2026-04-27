using System.Collections;
using UnityEngine;
using TMPro;
using DrillCorp.Core;

namespace DrillCorp.UI.HUD
{
    /// <summary>
    /// v2 거미 보스 등장 경고 — 화면 중앙에 짧게 표시 후 페이드 아웃.
    /// 근거: V2-prototype.html line 829 warnings.push({text:'⚠ 보스 등장!',...,life:200})
    ///
    /// 구조:
    ///   BossWarning (이 컴포넌트)
    ///     └── Container (CanvasGroup — fade in/out)
    ///         ├── TitleText      ("⚠ 보스 등장!")
    ///         └── SubtitleText   ("거미 보스 — 착지 시 새끼 소환")
    ///
    /// GameEvents.OnBossSpawned 구독 → ShowWarning() 자동 실행.
    /// </summary>
    public class BossWarningUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CanvasGroup _container;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _subtitleText;

        [Header("Text")]
        // 주의: D2Coding 폰트는 코딩 폰트라 ⚠(U+26A0) 같은 이모지 글리프가 없을 수 있음.
        // 글자만으로도 시각적으로 강조되므로 이모지 빼고 굵은 텍스트 + 색상으로 대체.
        [SerializeField] private string _title    = "보스 등장!";
        [SerializeField] private string _subtitle = "거미 보스 — 착지 시 새끼 소환";

        [Header("Timing (초)")]
        [Tooltip("페이드 인 시간")]
        [SerializeField] private float _fadeInDuration = 0.3f;
        [Tooltip("화면에 또렷하게 머무는 시간")]
        [SerializeField] private float _holdDuration = 2.5f;
        [Tooltip("페이드 아웃 시간")]
        [SerializeField] private float _fadeOutDuration = 0.5f;

        private Coroutine _showRoutine;

        private void Awake()
        {
            // 시작 시 비활성 (alpha=0)
            if (_container != null) _container.alpha = 0f;
            if (_titleText != null) _titleText.text = _title;
            if (_subtitleText != null) _subtitleText.text = _subtitle;
        }

        private void OnEnable()
        {
            GameEvents.OnBossSpawned += OnBossSpawned;
        }

        private void OnDisable()
        {
            GameEvents.OnBossSpawned -= OnBossSpawned;
        }

        private void OnBossSpawned(Vector3 _)
        {
            ShowWarning();
        }

        public void ShowWarning()
        {
            if (_container == null) return;
            if (_showRoutine != null) StopCoroutine(_showRoutine);
            _showRoutine = StartCoroutine(ShowRoutine());
        }

        private IEnumerator ShowRoutine()
        {
            // Fade In
            float t = 0f;
            while (t < _fadeInDuration)
            {
                t += Time.deltaTime;
                _container.alpha = Mathf.Clamp01(t / _fadeInDuration);
                yield return null;
            }
            _container.alpha = 1f;

            // Hold
            yield return new WaitForSeconds(_holdDuration);

            // Fade Out
            t = 0f;
            while (t < _fadeOutDuration)
            {
                t += Time.deltaTime;
                _container.alpha = 1f - Mathf.Clamp01(t / _fadeOutDuration);
                yield return null;
            }
            _container.alpha = 0f;
            _showRoutine = null;
        }
    }
}
