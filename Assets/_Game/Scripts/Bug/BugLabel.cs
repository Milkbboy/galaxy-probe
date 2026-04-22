using UnityEngine;
using TMPro;
using DrillCorp.Diagnostics;
using DrillCorp.UI;

namespace DrillCorp.Bug
{
    /// <summary>
    /// Bug 행동 라벨 표시 (테스트/디버그용)
    /// 탑다운 뷰: 월드 좌표 고정, 부모 회전 무시
    /// </summary>
    public class BugLabel : MonoBehaviour
    {
        [SerializeField] private TextMeshPro _text;
        [SerializeField] private Vector3 _offset = new Vector3(0f, 0.1f, 1.2f);

        private Transform _target;

        private void Start()
        {
            // 프리펩에서 로드된 경우 부모를 타겟으로 설정
            if (_target == null && transform.parent != null)
            {
                _target = transform.parent;
            }
        }

        public void Initialize(Transform target, Vector3? customOffset = null)
        {
            _target = target;
            if (customOffset.HasValue)
            {
                _offset = customOffset.Value;
            }
        }

        private void LateUpdate()
        {
            using var _perf = PerfMarkers.BugLabel_LateUpdate.Auto();

            if (_target == null) return;

            // 월드 좌표로 위치 설정 (부모 회전 무시)
            transform.position = _target.position + _offset;

            // 회전 고정 (탑다운 뷰용)
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        public void SetText(string text)
        {
            if (_text != null)
                _text.text = text;
        }

        public void SetColor(Color color)
        {
            if (_text != null)
                _text.color = color;
        }

        /// <summary>
        /// 코드로 라벨 생성
        /// </summary>
        public static BugLabel Create(Transform bugTransform, string labelText, Color color, Vector3? customOffset = null)
        {
            GameObject labelObj = new GameObject("BugLabel");
            BugLabel label = labelObj.AddComponent<BugLabel>();

            if (customOffset.HasValue)
            {
                label._offset = customOffset.Value;
            }

            label._target = bugTransform;

            // TextMeshPro 생성
            label._text = labelObj.AddComponent<TextMeshPro>();
            label._text.text = labelText;
            label._text.fontSize = 2f;
            label._text.alignment = TextAlignmentOptions.Center;
            label._text.color = color;
            label._text.sortingOrder = 150;
            TMPFontHelper.ApplyDefaultFont(label._text);

            return label;
        }
    }
}
