using UnityEngine;

namespace DrillCorp.Bug
{
    /// <summary>
    /// Renderer의 OnBecame Visible/Invisible 이벤트를 캐싱
    /// BugController에서 가시성 상태를 빠르게 조회하기 위함
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class OffscreenVisibilityTracker : MonoBehaviour
    {
        private bool _isVisible = true;

        public bool IsVisible => _isVisible;

        private void OnBecameVisible()
        {
            _isVisible = true;
        }

        private void OnBecameInvisible()
        {
            _isVisible = false;
        }

        private void OnEnable()
        {
            _isVisible = true;
        }
    }
}
