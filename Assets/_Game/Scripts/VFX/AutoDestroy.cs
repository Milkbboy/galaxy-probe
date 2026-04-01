using UnityEngine;

namespace DrillCorp.VFX
{
    /// <summary>
    /// 일정 시간 후 자동으로 오브젝트를 삭제하는 컴포넌트
    /// </summary>
    public class AutoDestroy : MonoBehaviour
    {
        [SerializeField] private float _lifetime = 1f;

        private void Start()
        {
            Destroy(gameObject, _lifetime);
        }
    }
}
