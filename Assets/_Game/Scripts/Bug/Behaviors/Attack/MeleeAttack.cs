using UnityEngine;

namespace DrillCorp.Bug.Behaviors.Attack
{
    /// <summary>
    /// 근접 공격
    /// </summary>
    public class MeleeAttack : AttackBehaviorBase
    {
        protected override void PerformAttack(Transform target)
        {
            // 직접 데미지
            DealDamage(target, GetDamage());

            // 공격 이펙트 재생 (타겟 콜라이더 표면에 정확히 표시)
            // 탑다운 뷰: XZ 평면에서 방향 계산
            Vector3 toTarget = _bug.transform.position - target.position;
            Vector3 direction = new Vector3(toTarget.x, 0f, toTarget.z).normalized;
            float hitOffset = GetColliderRadius(target); // 콜라이더 경계에 정확히 붙임
            Vector3 hitPos = target.position + direction * hitOffset;
            hitPos.y = 0.5f; // 콜라이더 중간 높이
            PlayHitVfx(hitPos);
        }

        private float GetColliderRadius(Transform t)
        {
            var collider = t.GetComponent<Collider>();
            if (collider == null) return 0.5f;
            Vector3 size = collider.bounds.size;
            return Mathf.Max(size.x, size.z) * 0.5f;
        }
    }
}
