using UnityEngine;

namespace DrillCorp.Bug.Pool
{
    /// <summary>
    /// 풀링된 Bug에 부착되는 마커 컴포넌트
    /// 풀 소속 여부와 BugId를 기억하고 Return 처리
    /// </summary>
    public class PooledBug : MonoBehaviour
    {
        private BugPool _pool;
        private int _bugId;
        private bool _isPooled;

        public int BugId => _bugId;
        public bool IsPooled => _isPooled;

        public void Setup(BugPool pool, int bugId)
        {
            _pool = pool;
            _bugId = bugId;
            _isPooled = true;
        }

        public void ReturnToPool()
        {
            if (!_isPooled || _pool == null)
            {
                Destroy(gameObject);
                return;
            }

            _pool.Return(gameObject, _bugId);
        }
    }
}
