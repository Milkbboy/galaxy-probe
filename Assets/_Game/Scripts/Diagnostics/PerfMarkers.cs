using Unity.Profiling;

namespace DrillCorp.Diagnostics
{
    /// <summary>
    /// 프레임 드랍 조사용 커스텀 ProfilerMarker 모음.
    ///
    /// 사용 패턴:
    ///   private void Update()
    ///   {
    ///       using var _ = PerfMarkers.BugController_Update.Auto();
    ///       // ... 기존 코드
    ///   }
    ///
    /// 오버헤드: 프로파일 꺼진 빌드에서 ~25ns/호출. 무시 가능.
    /// PerfRecorder 가 이 마커들을 자동으로 CSV 에 포함한다.
    ///
    /// 마커 이름 규칙: "DrillCorp.<Class>.<Method>" — 알파벳 순 정렬 시 가까운 것끼리 묶이도록.
    /// </summary>
    public static class PerfMarkers
    {
        // ─── Bug ───────────────────────────────────────────────────
        public static readonly ProfilerMarker BugController_Update =
            new(ProfilerCategory.Scripts, "DrillCorp.BugController.Update");

        public static readonly ProfilerMarker BugLabel_LateUpdate =
            new(ProfilerCategory.Scripts, "DrillCorp.BugLabel.LateUpdate");

        // ─── Drone / Spider ────────────────────────────────────────
        public static readonly ProfilerMarker Drone_Update =
            new(ProfilerCategory.Scripts, "DrillCorp.Drone.Update");

        public static readonly ProfilerMarker Drone_OverlapSphere =
            new(ProfilerCategory.Scripts, "DrillCorp.Drone.OverlapSphere");

        public static readonly ProfilerMarker Spider_Update =
            new(ProfilerCategory.Scripts, "DrillCorp.Spider.Update");

        public static readonly ProfilerMarker Spider_OverlapSphere =
            new(ProfilerCategory.Scripts, "DrillCorp.Spider.OverlapSphere");

        // ─── UI / HUD ──────────────────────────────────────────────
        public static readonly ProfilerMarker TopBarHud_Update =
            new(ProfilerCategory.Scripts, "DrillCorp.TopBarHud.Update");

        public static readonly ProfilerMarker MachineStatusUI_Update =
            new(ProfilerCategory.Scripts, "DrillCorp.MachineStatusUI.Update");

        public static readonly ProfilerMarker MiningUI_Update =
            new(ProfilerCategory.Scripts, "DrillCorp.MiningUI.Update");

        public static readonly ProfilerMarker AbilityHud_Update =
            new(ProfilerCategory.Scripts, "DrillCorp.AbilityHud.Update");

        public static readonly ProfilerMarker Hp3DBar_LateUpdate =
            new(ProfilerCategory.Scripts, "DrillCorp.Hp3DBar.LateUpdate");
    }
}
