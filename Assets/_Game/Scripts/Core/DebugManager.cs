using UnityEngine;
using UnityEngine.InputSystem;
using DrillCorp.Machine;

namespace DrillCorp.Core
{
    /// <summary>
    /// 디버그/테스트용 단축키 관리
    /// </summary>
    public class DebugManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool _enableDebugKeys = true;
        [SerializeField] private bool _showDebugUI = true;

        private MachineController _machine;
        private bool _infiniteFuel;

        private void Start()
        {
            _machine = FindAnyObjectByType<MachineController>();
        }

        private void Update()
        {
            if (!_enableDebugKeys) return;

            // I 키: 머신 무적 토글
            if (Keyboard.current.iKey.wasPressedThisFrame)
            {
                ToggleMachineInvincible();
            }

            // F 키: 무한 연료 토글
            if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                ToggleInfiniteFuel();
            }

            // H 키: 머신 체력 회복
            if (Keyboard.current.hKey.wasPressedThisFrame)
            {
                HealMachine();
            }

            // K 키: 모든 Bug 즉사
            if (Keyboard.current.kKey.wasPressedThisFrame)
            {
                KillAllBugs();
            }

            // 무한 연료 적용
            if (_infiniteFuel && _machine != null)
            {
                _machine.AddFuel(10f);
            }
        }

        private void ToggleMachineInvincible()
        {
            if (_machine != null)
            {
                _machine.ToggleInvincible();
            }
        }

        private void ToggleInfiniteFuel()
        {
            _infiniteFuel = !_infiniteFuel;
            Debug.Log($"[Debug] Infinite Fuel: {_infiniteFuel}");
        }

        private void HealMachine()
        {
            if (_machine != null)
            {
                _machine.Heal(_machine.MaxHealth);
                Debug.Log("[Debug] Machine healed to full");
            }
        }

        private void KillAllBugs()
        {
            var bugs = FindObjectsByType<Bug.BugController>(FindObjectsInactive.Exclude);
            foreach (var bug in bugs)
            {
                bug.TakeDamage(99999f);
            }
            Debug.Log($"[Debug] Killed {bugs.Length} bugs");
        }

        private void OnGUI()
        {
            if (!_showDebugUI) return;

            const float width = 250f;
            const float height = 150f;
            GUILayout.BeginArea(new Rect(Screen.width - width - 10f, 10f, width, height));
            GUILayout.BeginVertical("box");

            GUILayout.Label("<b>디버그 단축키</b>");
            GUILayout.Label($"[I] 머신 무적: {(_machine != null && _machine.IsInvincible ? "<color=cyan>ON</color>" : "OFF")}");
            GUILayout.Label($"[F] 무한 연료: {(_infiniteFuel ? "<color=cyan>ON</color>" : "OFF")}");
            GUILayout.Label("[H] 머신 체력 회복");
            GUILayout.Label("[K] 모든 벌레 즉사");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
