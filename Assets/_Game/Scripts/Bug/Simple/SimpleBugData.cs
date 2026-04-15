using UnityEngine;

namespace DrillCorp.Bug.Simple
{
    [CreateAssetMenu(fileName = "SimpleBug_", menuName = "Drill-Corp/Simple Bug Data")]
    public class SimpleBugData : ScriptableObject
    {
        public enum BugKind { Normal, Elite, Swift }

        [Header("Identity")]
        public string BugName = "Normal";
        public BugKind Kind = BugKind.Normal;
        public GameObject Prefab;

        [Header("Stats (base, wave 1)")]
        public float BaseHp = 2f;
        public float BaseSpeed = 0.5f;
        public float Size = 0.4f;
        public float Score = 1f;

        [Header("Wave Scaling")]
        public float HpPerWave = 0.5f;
        public float SpeedPerWave = 0.06f;
        public float SpeedRandom = 0.15f;

        [Header("Visual")]
        public Color Tint = Color.white;

        public float GetHp(int wave) => BaseHp + Mathf.Floor(wave * HpPerWave);
        public float GetSpeed(int wave) => BaseSpeed + wave * SpeedPerWave + Random.Range(0f, SpeedRandom);
    }
}
