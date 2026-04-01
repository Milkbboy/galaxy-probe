using UnityEngine;

namespace DrillCorp.VFX
{
    /// <summary>
    /// 일정 시간 후 자동으로 오브젝트를 삭제하는 컴포넌트
    /// </summary>
    public class AutoDestroy : MonoBehaviour
    {
        [SerializeField] private float _lifetime = 1f;
        private bool _initialized;

        private void Start()
        {
            if (!_initialized)
            {
                Destroy(gameObject, _lifetime);
            }
        }

        /// <summary>
        /// 런타임에서 lifetime 설정 (Start 전에 호출)
        /// </summary>
        public void SetLifetime(float lifetime)
        {
            _lifetime = lifetime;
            _initialized = true;
            Destroy(gameObject, _lifetime);
        }
    }
}
