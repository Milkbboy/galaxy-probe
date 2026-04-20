using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using DrillCorp.Machine;

namespace DrillCorp.Aim
{
    /// <summary>
    /// 마우스 조준 + 크로스헤어 UI + 에임 데이터 제공.
    /// v2 포팅: 무기는 각자 자체 Update에서 발동 — AimController는 장착 개념 없이
    /// 에임 위치·범위·머신 참조만 제공 (AimPosition, AimRadius, BugsInRange, MachineTransform).
    /// </summary>
    public class AimController : MonoBehaviour
    {
        [Header("Aim Settings")]
        [SerializeField] private float _aimRadius = 0.5f;
        [SerializeField] private bool _autoCalculateRadius = true;
        [SerializeField] private LayerMask _bugLayer;

        [Tooltip("크로스헤어·링·라벨이 지면(Y=0)보다 얼마나 위에 떠있을지. " +
                 "벌레 스프라이트에 가려지지 않도록 벌레 높이보다 크게 둘 것.")]
        [Range(0.1f, 10f)]
        [SerializeField] private float _crosshairHeight = 2f;

        [Header("References")]
        [Tooltip("머신 Transform (비우면 'Machine' 태그 자동 탐색)")]
        [SerializeField] private Transform _machineTransform;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer _crosshairRenderer;
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _readyColor = Color.red;

        [Header("Info UI")]
        [Tooltip("크로스헤어 아래 표시되는 텍스트 (비우면 자동 생성)")]
        [SerializeField] private TextMeshPro _infoLabel;

        [Tooltip("자동 생성 시 폰트 크기")]
        [Range(10f, 200f)]
        [SerializeField] private float _infoLabelFontSize = 80f;

        [Tooltip("자동 생성 시 색상")]
        [SerializeField] private Color _infoLabelColor = Color.yellow;

        [Tooltip("크로스헤어 중심에서 화면 위쪽 거리 (Aim 부모 로컬 Y+, 탑뷰 월드 Z+)")]
        [Range(0f, 10f)]
        [SerializeField] private float _infoLabelDistanceAbove = 3.3f;

        [Tooltip("라벨을 지면에서 얼마나 띄울지 (Y축)")]
        [Range(0f, 5f)]
        [SerializeField] private float _infoLabelYOffset = 0.1f;

        [Tooltip("자동 생성 시 Scale (월드 크기 배율)")]
        [Range(0.1f, 5f)]
        [SerializeField] private float _infoLabelScale = 1.5f;

        [Tooltip("아웃라인 두께 (0=없음)")]
        [Range(0f, 1f)]
        [SerializeField] private float _infoLabelOutlineWidth = 0.2f;

        [Tooltip("아웃라인 색상")]
        [SerializeField] private Color _infoLabelOutlineColor = Color.black;


        private Camera _mainCamera;
        private Vector3 _aimPosition;
        private bool _hasBugInRange;

        // v2 저격총 range 업그레이드 → 에임 반경 배율 (1.0 = 기본).
        // SniperWeapon 또는 AimRangeUpgradeBinder가 외부에서 설정.
        private float _rangeMultiplier = 1f;
        private float _baseAimRadius;          // 스프라이트 자동 계산된 원본 반경
        private Vector3 _baseCrosshairScale;   // 크로스헤어 원본 스케일

        private readonly List<Collider> _cachedBugs = new List<Collider>();
        private readonly Collider[] _overlapBuffer = new Collider[128];

        public bool HasBugInRange => _hasBugInRange;
        public Vector3 AimPosition => _aimPosition;
        public float AimRadius => _aimRadius;        // 이미 배율 적용된 현재 반경
        public float BaseAimRadius => _baseAimRadius; // 배율 미적용 기본 반경
        public float RangeMultiplier => _rangeMultiplier;
        public LayerMask BugLayer => _bugLayer;
        public Transform MachineTransform => _machineTransform;
        public TextMeshPro InfoLabel => _infoLabel;

        /// <summary>
        /// v2 — 저격총 range 업그레이드 등으로 에임 반경을 동적으로 확장.
        /// multiplier=1.0이 기본, 1.2면 +20% 확장. 스프라이트도 함께 스케일.
        /// 모든 AimWeaponRing이 AimRadius를 기준으로 하므로 호들도 자동 따라감.
        /// </summary>
        public void SetRangeMultiplier(float multiplier)
        {
            multiplier = Mathf.Max(0.1f, multiplier);
            if (Mathf.Approximately(_rangeMultiplier, multiplier)) return;

            _rangeMultiplier = multiplier;
            ApplyRangeMultiplier();
        }

        private void ApplyRangeMultiplier()
        {
            // 판정 반경 갱신
            _aimRadius = _baseAimRadius * _rangeMultiplier;

            // 시각 — 크로스헤어 스프라이트도 비례 스케일
            if (_crosshairRenderer != null)
            {
                var t = _crosshairRenderer.transform;
                t.localScale = _baseCrosshairScale * _rangeMultiplier;
            }
        }

        public void SetInfoText(string text)
        {
            if (_infoLabel == null) return;
            if (string.IsNullOrEmpty(text))
            {
                if (_infoLabel.gameObject.activeSelf)
                    _infoLabel.gameObject.SetActive(false);
                return;
            }

            if (!_infoLabel.gameObject.activeSelf)
                _infoLabel.gameObject.SetActive(true);

            _infoLabel.text = text;
        }

        /// <summary>
        /// 현재 에임 범위 내의 Bug Collider 리스트 (매 프레임 갱신됨)
        /// </summary>
        public IReadOnlyList<Collider> BugsInRange => _cachedBugs;

        private void Awake()
        {
            _mainCamera = Camera.main;
            CalculateAimRadius();
            EnsureBugLayer();
            EnsureMachineTransform();
            // EnsureInfoLabel은 Start로 이동 — TMPFontHolder.Awake가 먼저 실행돼야
            // TMPFontHelper가 D2Coding을 반환. Awake끼리는 순서 보장 없음.
        }

        private void EnsureInfoLabel()
        {
            if (_infoLabel != null) return;

            var labelObj = new GameObject("InfoLabel");
            labelObj.transform.SetParent(transform, false);

            // Aim GameObject 자체가 이미 X축 90도 회전된 상태로 배치되어 있음 (Crosshair처럼)
            // → 자식도 부모 로컬 기준을 따른다.
            //   부모 로컬 Y+  == 월드 Z+ (탑뷰 화면 위쪽)
            //   부모 로컬 Z-  == 월드 Y+ (지면에서 위로)
            // → localRotation은 Identity (부모 회전 그대로, 텍스트가 카메라를 바라봄)
            labelObj.transform.localPosition = new Vector3(0f, _infoLabelDistanceAbove, -_infoLabelYOffset);
            labelObj.transform.localRotation = Quaternion.identity;
            labelObj.transform.localScale = Vector3.one * _infoLabelScale;

            _infoLabel = labelObj.AddComponent<TextMeshPro>();

            // 1. 폰트 먼저 적용 (폰트 교체가 설정 값을 초기화할 수 있어서)
            DrillCorp.UI.TMPFontHelper.ApplyDefaultFont(_infoLabel);

            // 2. RectTransform (글자 잘림 방지)
            var rect = _infoLabel.rectTransform;
            rect.sizeDelta = new Vector2(10f, 3f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            // 3. 텍스트 속성 적용 (폰트 교체 이후)
            _infoLabel.alignment = TextAlignmentOptions.Center;
            _infoLabel.fontSize = _infoLabelFontSize;
            _infoLabel.color = _infoLabelColor;
            _infoLabel.text = "0/0";
            _infoLabel.fontStyle = FontStyles.Bold;
            _infoLabel.textWrappingMode = TextWrappingModes.NoWrap;
            _infoLabel.overflowMode = TextOverflowModes.Overflow;
            _infoLabel.enableAutoSizing = false;

            // 4. 아웃라인 (가독성)
            if (_infoLabelOutlineWidth > 0f)
            {
                _infoLabel.outlineWidth = _infoLabelOutlineWidth;
                _infoLabel.outlineColor = _infoLabelOutlineColor;
            }

            // 5. 렌더 우선순위 (다른 오브젝트에 가려지지 않도록)
            var meshRenderer = _infoLabel.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.sortingOrder = 1000;
            }

            // 6. 즉시 메시 갱신 (폰트/사이즈 반영)
            _infoLabel.ForceMeshUpdate();

            labelObj.SetActive(false);
        }

        private void Start()
        {
            // Awake가 아닌 Start에서 — TMPFontHolder.Initialize가 먼저 호출되도록 보장
            EnsureInfoLabel();
        }

        private void EnsureBugLayer()
        {
            if (_bugLayer == 0)
            {
                int bugLayerIndex = LayerMask.NameToLayer("Bug");
                if (bugLayerIndex != -1)
                    _bugLayer = 1 << bugLayerIndex;
            }
        }

        private void EnsureMachineTransform()
        {
            if (_machineTransform == null)
            {
                var obj = GameObject.FindGameObjectWithTag("Machine");
                if (obj != null)
                    _machineTransform = obj.transform;
            }
        }

        private void CalculateAimRadius()
        {
            if (_autoCalculateRadius && _crosshairRenderer != null && _crosshairRenderer.sprite != null)
            {
                Vector3 spriteSize = _crosshairRenderer.bounds.size;
                _aimRadius = Mathf.Max(spriteSize.x, spriteSize.z) / 2f;
            }

            // 배율 적용을 위한 원본 값 보존
            _baseAimRadius = _aimRadius;
            _baseCrosshairScale = _crosshairRenderer != null
                ? _crosshairRenderer.transform.localScale
                : Vector3.one;

            ApplyRangeMultiplier();  // 현재 배율 즉시 반영 (기본 1.0이면 변화 없음)
        }

        private void Update()
        {
            // v2 포팅: 모든 무기가 자체 Update에서 발동 (AimController는 에임 데이터만 제공).
            // SuppressAimBugDetection은 더 이상 의미 없음 — 항상 범위 내 벌레를 수집한다.
            UpdateAimPosition();
            UpdateCrosshairPosition();
            CollectBugsInRange();
            UpdateCrosshairColor();
        }

        private void UpdateAimPosition()
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = _mainCamera.ScreenPointToRay(mousePos);

            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float distance))
                _aimPosition = ray.GetPoint(distance);
        }

        private void UpdateCrosshairPosition()
        {
            // _aimPosition 자체는 Y=0 (지면) 기준 — 무기 Overlap/Raycast는 이 값 사용.
            // 시각 요소만 _crosshairHeight 만큼 위로 띄워서 벌레에 안 가리게.
            transform.position = _aimPosition + Vector3.up * _crosshairHeight;
        }

        private void CollectBugsInRange()
        {
            _cachedBugs.Clear();
            int count = Physics.OverlapSphereNonAlloc(_aimPosition, _aimRadius, _overlapBuffer, _bugLayer);
            for (int i = 0; i < count; i++)
            {
                if (_overlapBuffer[i] != null)
                    _cachedBugs.Add(_overlapBuffer[i]);
            }
            _hasBugInRange = _cachedBugs.Count > 0;
        }

        private void UpdateCrosshairColor()
        {
            if (_crosshairRenderer == null) return;
            _crosshairRenderer.color = _hasBugInRange ? _readyColor : _normalColor;
        }

        /// <summary>
        /// 에임 중심에 가장 가까운 Bug Collider 반환
        /// </summary>
        public Collider GetClosestBugToAim()
        {
            Collider best = null;
            float bestDist = float.MaxValue;
            foreach (var c in _cachedBugs)
            {
                if (c == null) continue;
                float d = (c.transform.position - _aimPosition).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = c;
                }
            }
            return best;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _aimRadius);
        }
    }
}
