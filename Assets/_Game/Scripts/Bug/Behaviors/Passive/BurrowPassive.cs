using UnityEngine;

namespace DrillCorp.Bug.Behaviors.Passive
{
    /// <summary>
    /// Burrow 상태
    /// </summary>
    public enum BurrowState
    {
        Idle,           // 일반 상태
        Burrowing,      // 잠수 중 (애니메이션)
        Underground,    // 땅속
        Emerging        // 출현 중 (애니메이션)
    }

    /// <summary>
    /// 땅속 숨기 패시브 - 외부(Trigger)에서 호출하여 발동
    /// param1 = 숨어있는 시간 (초)
    /// param2 = 잠수/출현 애니메이션 시간 (초)
    /// </summary>
    public class BurrowPassive : PassiveBehaviorBase
    {
        private float _hideDuration;      // 땅속에 있는 시간
        private float _animDuration;      // 잠수/출현 애니메이션 시간
        private float _timer;

        private BurrowState _state = BurrowState.Idle;
        private GameObject _burrowVfxPrefab;
        private GameObject _emergeVfxPrefab;

        // 시각적 캐싱
        private Renderer[] _renderers;
        private Color[] _originalColors;
        private Vector3 _originalScale;
        private const float BURROWED_ALPHA = 0.3f;

        // 프로퍼티
        public BurrowState State => _state;
        public bool IsBurrowed => _state == BurrowState.Underground;
        public bool IsAnimating => _state == BurrowState.Burrowing || _state == BurrowState.Emerging;
        public bool CanBurrow => _state == BurrowState.Idle;

        public BurrowPassive(float hideDuration = 2f, float animDuration = 0.3f,
            GameObject burrowVfxPrefab = null, GameObject emergeVfxPrefab = null)
        {
            _hideDuration = hideDuration > 0f ? hideDuration : 2f;
            _animDuration = animDuration > 0f ? animDuration : 0.3f;
            _burrowVfxPrefab = burrowVfxPrefab;
            _emergeVfxPrefab = emergeVfxPrefab;
        }

        public override void Initialize(BugController bug)
        {
            base.Initialize(bug);
            CacheRenderers();
            _originalScale = bug.transform.localScale;
        }

        private void CacheRenderers()
        {
            _renderers = _bug.GetComponentsInChildren<Renderer>();
            _originalColors = new Color[_renderers.Length];

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i].material.HasProperty("_Color"))
                {
                    _originalColors[i] = _renderers[i].material.color;
                }
                else if (_renderers[i].material.HasProperty("_BaseColor"))
                {
                    _originalColors[i] = _renderers[i].material.GetColor("_BaseColor");
                }
                else
                {
                    _originalColors[i] = Color.white;
                }
            }
        }

        /// <summary>
        /// 외부에서 호출하여 Burrow 시작 (Trigger에서 사용)
        /// </summary>
        public bool TryBurrow()
        {
            if (!CanBurrow) return false;
            StartBurrow();
            return true;
        }

        private void StartBurrow()
        {
            _state = BurrowState.Burrowing;
            _timer = _animDuration;

            _bug.SetInvulnerable(true);
            PlayEffect(_burrowVfxPrefab, "숨음!");
        }

        public override void UpdatePassive(float deltaTime)
        {
            if (_state == BurrowState.Idle) return;

            _timer -= deltaTime;

            switch (_state)
            {
                case BurrowState.Burrowing:
                    UpdateBurrowingVisual();
                    if (_timer <= 0f) EnterUnderground();
                    break;

                case BurrowState.Underground:
                    if (_timer <= 0f) StartEmerge();
                    break;

                case BurrowState.Emerging:
                    UpdateEmergingVisual();
                    if (_timer <= 0f) FinishEmerge();
                    break;
            }
        }

        private void EnterUnderground()
        {
            _state = BurrowState.Underground;
            _timer = _hideDuration;

            // 완전히 납작 + 반투명
            SetScale(0.1f);
            SetAlpha(BURROWED_ALPHA);
        }

        private void StartEmerge()
        {
            _state = BurrowState.Emerging;
            _timer = _animDuration;

            PlayEffect(_emergeVfxPrefab, "등장!");
        }

        private void FinishEmerge()
        {
            _state = BurrowState.Idle;

            // 원래 상태 복원
            _bug.transform.localScale = _originalScale;
            RestoreColors();
            _bug.SetInvulnerable(false);
        }

        #region Visual

        private void UpdateBurrowingVisual()
        {
            // 진행률 (1 → 0)
            float progress = _animDuration > 0 ? _timer / _animDuration : 0f;

            // Y 스케일 축소 (1 → 0.1)
            float scaleY = Mathf.Lerp(0.1f, 1f, progress);
            SetScale(scaleY);

            // 반투명 (1 → 0.3)
            float alpha = Mathf.Lerp(BURROWED_ALPHA, 1f, progress);
            SetAlpha(alpha);
        }

        private void UpdateEmergingVisual()
        {
            // 진행률 (1 → 0)
            float progress = _animDuration > 0 ? _timer / _animDuration : 0f;

            // Y 스케일 복원 (0.1 → 1)
            float scaleY = Mathf.Lerp(1f, 0.1f, progress);
            SetScale(scaleY);

            // 불투명 복원 (0.3 → 1)
            float alpha = Mathf.Lerp(1f, BURROWED_ALPHA, progress);
            SetAlpha(alpha);
        }

        private void SetScale(float yScale)
        {
            Vector3 scale = _originalScale;
            scale.y = _originalScale.y * yScale;
            _bug.transform.localScale = scale;
        }

        private void SetAlpha(float alpha)
        {
            if (_renderers == null) return;

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;

                Material mat = _renderers[i].material;
                Color color = _originalColors[i];
                color.a = alpha;

                if (mat.HasProperty("_Color"))
                {
                    mat.color = color;
                }
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", color);
                }

                // 투명 모드 설정
                SetMaterialTransparent(mat, alpha < 1f);
            }
        }

        private void RestoreColors()
        {
            if (_renderers == null) return;

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;

                Material mat = _renderers[i].material;
                Color color = _originalColors[i];

                if (mat.HasProperty("_Color"))
                {
                    mat.color = color;
                }
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", color);
                }

                SetMaterialTransparent(mat, false);
            }
        }

        private void SetMaterialTransparent(Material mat, bool transparent)
        {
            if (transparent)
            {
                mat.SetFloat("_Surface", 1); // Transparent
                mat.SetFloat("_Blend", 0);   // Alpha
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = 3000;
            }
            else
            {
                mat.SetFloat("_Surface", 0); // Opaque
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                mat.SetInt("_ZWrite", 1);
                mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = 2000;
            }
        }

        #endregion

        #region VFX

        private void PlayEffect(GameObject prefab, string fallbackText)
        {
            Vector3 pos = _bug.transform.position;

            if (prefab != null)
            {
                Object.Instantiate(prefab, pos, Quaternion.identity);
            }
            else
            {
                // 폴백: SimpleVFX 텍스트 (구현 필요시)
                VFX.SimpleVFX.PlayText(pos, fallbackText, new Color(0.6f, 0.4f, 0.2f)); // 갈색
            }
        }

        #endregion

        public override void Cleanup()
        {
            // 상태 복원
            if (_bug != null && _state != BurrowState.Idle)
            {
                _bug.transform.localScale = _originalScale;
                RestoreColors();
                _bug.SetInvulnerable(false);
            }

            base.Cleanup();
        }
    }
}
