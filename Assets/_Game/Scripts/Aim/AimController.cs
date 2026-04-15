using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using DrillCorp.Machine;
using DrillCorp.Weapon;

namespace DrillCorp.Aim
{
    /// <summary>
    /// 마우스 조준 + 크로스헤어 UI + 장착 무기에 발사 위임
    /// 무기들은 AimController를 통해 에임 위치, 범위, 머신 참조를 얻음
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

        [Header("Weapon")]
        [Tooltip("시작 시 장착할 기본 무기")]
        [SerializeField] private WeaponBase _initialWeapon;

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
        private WeaponBase _currentWeapon;

        private readonly List<Collider> _cachedBugs = new List<Collider>();
        private readonly Collider[] _overlapBuffer = new Collider[128];

        public bool HasBugInRange => _hasBugInRange;
        public Vector3 AimPosition => _aimPosition;
        public float AimRadius => _aimRadius;
        public LayerMask BugLayer => _bugLayer;
        public Transform MachineTransform => _machineTransform;
        public WeaponBase CurrentWeapon => _currentWeapon;
        public TextMeshPro InfoLabel => _infoLabel;

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
        public float CooldownProgress => _currentWeapon != null ? _currentWeapon.CooldownProgress : 1f;
        public bool IsReady => _currentWeapon == null || _currentWeapon.CanFire;

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
            EnsureInfoLabel();
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
            if (_initialWeapon != null)
                EquipWeapon(_initialWeapon);
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
        }

        private void Update()
        {
            UpdateAimPosition();
            UpdateCrosshairPosition();

            bool suppress = _currentWeapon != null && _currentWeapon.SuppressAimBugDetection;
            if (suppress)
            {
                _cachedBugs.Clear();
                _hasBugInRange = false;
            }
            else
            {
                CollectBugsInRange();
            }

            TryFireWeapon();
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

        private void TryFireWeapon()
        {
            _currentWeapon?.TryFire(this);
        }

        private void UpdateCrosshairColor()
        {
            if (_crosshairRenderer == null) return;
            bool hitting = _currentWeapon != null
                ? _currentWeapon.IsHittingTarget(this)
                : _hasBugInRange;
            _crosshairRenderer.color = hitting ? _readyColor : _normalColor;
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

        public void EquipWeapon(WeaponBase weapon)
        {
            if (_currentWeapon == weapon)
                return;

            if (_currentWeapon != null)
            {
                _currentWeapon.OnUnequip();
                _currentWeapon.gameObject.SetActive(false);
            }

            _currentWeapon = weapon;
            SetInfoText(null);

            if (_currentWeapon != null)
            {
                _currentWeapon.gameObject.SetActive(true);
                _currentWeapon.OnEquip(this);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _aimRadius);
        }
    }
}
