using UnityEngine;
using DrillCorp.Aim;

namespace DrillCorp.Machine
{
    /// <summary>
    /// 머신 상단 포탑 — 에임 방향으로 배럴 회전.
    /// 현재 배럴: 기관총(Barrel_Gun). 추후 다른 무기 배럴 추가 예정.
    /// </summary>
    [ExecuteAlways]
    public class TurretController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("조준 위치를 제공하는 AimController.\n비우면 Awake에서 씬 전역 자동 탐색(FindAnyObjectByType).")]
        [SerializeField] private AimController _aim;

        [Tooltip("에임 방향으로 회전할 피벗 Transform.\n비우면 이 오브젝트의 'Pivot'이라는 자식을 자동 탐색.\nBuild Default Hierarchy 실행 시 자동 생성·할당됨.")]
        [SerializeField] private Transform _pivot;

        [Header("Rotation")]
        [Tooltip("배럴 회전 속도 (deg/s)\n0 = 즉시 스냅 (프레임당 바로 타겟 각도로)\n양수 = RotateTowards 보간 — 720이면 반 바퀴에 0.25초")]
        [Min(0f)]
        [SerializeField] private float _rotationSpeed = 720f;

        [Header("Build Settings")]
        [Tooltip("받침대(Base) 큐브의 월드 크기 (X=좌우 너비, Y=높이, Z=앞뒤 깊이).\n머신 상단에 올라갈 고정 부품. Build Default Hierarchy로 재빌드 시 반영.")]
        [SerializeField] private Vector3 _baseSize = new Vector3(1.2f, 0.4f, 1.2f);

        [Tooltip("배럴(총열) 큐브의 월드 크기 (X=두께, Y=두께, Z=길이).\nZ축이 조준 방향. 길게 만들수록 멀리 뻗어나옴. Build Default Hierarchy로 재빌드 시 반영.")]
        [SerializeField] private Vector3 _barrelSize = new Vector3(0.3f, 0.3f, 2.0f);

        [Tooltip("피벗(회전축) 로컬 위치 — 머신 상단 높이를 Y로 지정.\n배럴은 이 점을 중심으로 회전하며, Base 큐브는 이 Y의 절반 높이에 깔림.\nBuild Default Hierarchy로 재빌드 시 반영.")]
        [SerializeField] private Vector3 _pivotLocalPosition = new Vector3(0f, 0.7f, 0f);

        private void Awake()
        {
            EnsureReferences();
        }

        private void EnsureReferences()
        {
            if (_aim == null)
                _aim = FindAnyObjectByType<AimController>();

            if (_pivot == null)
            {
                var t = transform.Find("Pivot");
                if (t != null) _pivot = t;
            }
        }

        private void Update()
        {
            if (_pivot == null || _aim == null) return;

            Vector3 aimPos = _aim.AimPosition;
            Vector3 dir = aimPos - _pivot.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;

            Quaternion target = Quaternion.LookRotation(dir.normalized, Vector3.up);

            if (_rotationSpeed <= 0f || !Application.isPlaying)
            {
                _pivot.rotation = target;
            }
            else
            {
                _pivot.rotation = Quaternion.RotateTowards(
                    _pivot.rotation, target, _rotationSpeed * Time.deltaTime);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Inspector 값 변경 즉시 기존 자식 Transform에 반영 (뼈대 재생성 없이).
            // 자식이 아직 없으면(= Build 전이면) 아무 것도 안 함.
            UnityEditor.EditorApplication.delayCall += ApplyBuildSettingsSafe;
        }

        private void ApplyBuildSettingsSafe()
        {
            // delayCall 큐 처리 시점에 오브젝트가 파괴/재컴파일됐을 수 있음
            if (this == null) return;
            ApplyBuildSettings();
        }

        private void ApplyBuildSettings()
        {
            var baseT = transform.Find("Base");
            if (baseT != null)
            {
                baseT.localPosition = new Vector3(0f, _pivotLocalPosition.y * 0.5f, 0f);
                baseT.localScale = _baseSize;
            }

            var pivotT = transform.Find("Pivot");
            if (pivotT != null)
            {
                pivotT.localPosition = _pivotLocalPosition;
                _pivot = pivotT;

                var barrelT = pivotT.Find("Barrel_Gun");
                if (barrelT != null)
                {
                    barrelT.localPosition = new Vector3(0f, 0f, _barrelSize.z * 0.5f);
                    barrelT.localScale = _barrelSize;
                }
            }
        }

        [ContextMenu("Build Default Hierarchy")]
        private void BuildDefaultHierarchy()
        {
            UnityEditor.Undo.RegisterFullObjectHierarchyUndo(gameObject, "Build Turret");

            // 기존 자식 제거
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                UnityEditor.Undo.DestroyObjectImmediate(transform.GetChild(i).gameObject);
            }

            // Base (고정)
            var baseGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseGo.name = "Base";
            UnityEditor.Undo.RegisterCreatedObjectUndo(baseGo, "Create Base");
            baseGo.transform.SetParent(transform, false);
            baseGo.transform.localPosition = new Vector3(0f, _pivotLocalPosition.y * 0.5f, 0f);
            baseGo.transform.localScale = _baseSize;

            // Pivot (회전)
            var pivotGo = new GameObject("Pivot");
            UnityEditor.Undo.RegisterCreatedObjectUndo(pivotGo, "Create Pivot");
            pivotGo.transform.SetParent(transform, false);
            pivotGo.transform.localPosition = _pivotLocalPosition;
            pivotGo.transform.localRotation = Quaternion.identity;
            _pivot = pivotGo.transform;

            // Barrel (긴 큐브) — 피벗 앞쪽 +Z로 뻗음
            var barrelGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrelGo.name = "Barrel_Gun";
            UnityEditor.Undo.RegisterCreatedObjectUndo(barrelGo, "Create Barrel");
            barrelGo.transform.SetParent(pivotGo.transform, false);
            barrelGo.transform.localPosition = new Vector3(0f, 0f, _barrelSize.z * 0.5f);
            barrelGo.transform.localScale = _barrelSize;

            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log($"[TurretController] Built hierarchy under '{gameObject.name}'.", this);
        }
#endif
    }
}
