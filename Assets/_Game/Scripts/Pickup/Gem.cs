using UnityEngine;
using DrillCorp.Aim;
using DrillCorp.Core;
using DrillCorp.Data;
using DrillCorp.OutGame;
using DrillCorp.UI;

namespace DrillCorp.Pickup
{
    /// <summary>
    /// 월드 보석 오브젝트. 프리펩 없이 Create() 팩토리로 프로그램 스폰.
    /// 마우스(AimController.AimPosition)가 픽업 반경 내에 2초(gem_speed 보정) 머물면 채집.
    /// 채집 시 DataManager.AddGems(1) + 팝업 + 자파괴.
    ///
    /// v2 가이드: docs/v2.html — 기본 2초 호버, gem_speed 레벨당 +20% 채집 속도.
    /// </summary>
    public class Gem : MonoBehaviour
    {
        private const float BasePickupDuration = 2f;
        private const float PickupRadius = 0.6f;
        private const float HoverDecayMul = 0.5f;   // 호버 이탈 시 진행도 감소 배율

        private const int RingSegments = 48;
        private const float RingRadius = 0.45f;

        private static readonly Color GemColor      = new Color(0.53f, 0.87f, 1f, 1f);   // #88ddff v2 보석 색
        private static readonly Color RingColorIdle = new Color(0.53f, 0.87f, 1f, 0.25f);
        private static readonly Color RingColorHot  = new Color(0.53f, 0.87f, 1f, 1f);

        private const float SpriteSize = 0.7f;   // 월드 유닛 기준 지름

        private AimController _aim;
        private LineRenderer _progressRing;
        private float _hoverTime;
        private bool _collected;

        /// <summary>벌레 사망 위치에서 스폰. sprite가 null이면 단색 Quad로 대체.</summary>
        public static Gem Create(Vector3 pos, Sprite sprite = null)
        {
            var root = new GameObject("Gem");
            root.transform.position = new Vector3(pos.x, 0.1f, pos.z);
            // 탑다운: 스프라이트는 X=90° 회전해 지면에 누움 (CLAUDE.md)
            root.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var visualGo = new GameObject("Visual");
            visualGo.transform.SetParent(root.transform, false);
            var sr = visualGo.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = sprite != null ? Color.white : GemColor;
            sr.sortingOrder = 50;
            // 스프라이트가 지정된 스케일로 보이도록 localScale 조절
            float scale = SpriteSize;
            if (sprite != null && sprite.rect.width > 0f)
            {
                // PPU 100 기준 sprite.rect.width / 100 이 월드 단위. SpriteSize에 맞춰 비율 조절
                float unit = sprite.rect.width / sprite.pixelsPerUnit;
                scale = unit > 0f ? SpriteSize / unit : SpriteSize;
            }
            visualGo.transform.localScale = Vector3.one * scale;

            var gem = root.AddComponent<Gem>();
            gem.BuildProgressRing();
            return gem;
        }

        private void Start()
        {
            _aim = FindAnyObjectByType<AimController>();
        }

        private void Update()
        {
            if (_collected) return;
            if (_aim == null) return;

            Vector3 aimPos = _aim.AimPosition;
            aimPos.y = 0f;
            Vector3 gemPos = transform.position;
            gemPos.y = 0f;

            float dist = Vector3.Distance(aimPos, gemPos);
            bool hovering = dist <= PickupRadius;

            if (hovering)
            {
                // gem_speed 업그레이드 — 레벨당 +20% 채집 속도 (GemCollectSpeed Add)
                float speedMul = 1f;
                var um = UpgradeManager.Instance;
                if (um != null)
                    speedMul += um.GetTotalBonus(UpgradeType.GemCollectSpeed);

                _hoverTime += Time.deltaTime * speedMul;
                if (_hoverTime >= BasePickupDuration)
                {
                    Collect();
                    return;
                }
            }
            else
            {
                // 이탈 시 진행도 부드럽게 감소 (완전 리셋은 답답)
                _hoverTime = Mathf.Max(0f, _hoverTime - Time.deltaTime * HoverDecayMul);
            }

            UpdateRing(_hoverTime / BasePickupDuration, hovering);
        }

        private void Collect()
        {
            if (_collected) return;
            _collected = true;

            DataManager.Instance?.AddGems(1);
            GameEvents.OnGemCollected?.Invoke(1);

            DamagePopup.CreateText(transform.position, "+1 보석", GemColor);
            Destroy(gameObject);
        }

        // ═══════════════════════════════════════════════════
        // 진행 링 시각화
        // ═══════════════════════════════════════════════════

        private void BuildProgressRing()
        {
            // 링은 루트 밑에 별도 GO — useWorldSpace=true로 월드 XZ 평면에 직접 그린다
            var ringObj = new GameObject("ProgressRing");
            ringObj.transform.SetParent(transform, false);
            ringObj.transform.localPosition = Vector3.zero;
            ringObj.transform.localRotation = Quaternion.identity;

            _progressRing = ringObj.AddComponent<LineRenderer>();
            _progressRing.useWorldSpace = true;
            _progressRing.positionCount = 0;
            _progressRing.widthMultiplier = 0.05f;
            _progressRing.material = new Material(Shader.Find("Sprites/Default"));
            _progressRing.startColor = RingColorIdle;
            _progressRing.endColor = RingColorIdle;
            _progressRing.loop = false;
            _progressRing.sortingOrder = 60;
            _progressRing.numCornerVertices = 2;
        }

        private void UpdateRing(float ratio, bool hovering)
        {
            if (_progressRing == null) return;

            ratio = Mathf.Clamp01(ratio);

            if (ratio <= 0f)
            {
                _progressRing.positionCount = 0;
                return;
            }

            int count = Mathf.Max(2, Mathf.RoundToInt(RingSegments * ratio) + 1);
            if (_progressRing.positionCount != count)
                _progressRing.positionCount = count;

            // 월드 XZ 평면에 링 — 탑다운 카메라가 내려다보므로 Y=보석 높이에서 원호
            Vector3 center = transform.position;
            float ringY = center.y + 0.05f;
            float startAngle = Mathf.PI * 0.5f; // 12시
            float totalAngle = Mathf.PI * 2f * ratio;

            for (int i = 0; i < count; i++)
            {
                float t = count > 1 ? (float)i / (count - 1) : 0f;
                float a = startAngle - totalAngle * t;
                _progressRing.SetPosition(i, new Vector3(
                    center.x + Mathf.Cos(a) * RingRadius,
                    ringY,
                    center.z + Mathf.Sin(a) * RingRadius));
            }

            var col = hovering ? RingColorHot : RingColorIdle;
            _progressRing.startColor = col;
            _progressRing.endColor = col;
        }
    }
}
