using System.Collections.Generic;
using UnityEngine;

namespace DrillCorp.Bug.Formation
{
    /// <summary>
    /// 진형별 멤버 오프셋(리더 로컬 좌표) 계산기
    /// </summary>
    public static class FormationOffsetCalculator
    {
        public static List<Vector3> Calculate(FormationType type, int count, float spacing, float radius, float jitter)
        {
            switch (type)
            {
                case FormationType.Cluster:
                    return CalcCluster(count, spacing, radius);
                case FormationType.Line:
                    return CalcLine(count, spacing, radius);
                case FormationType.Swarm:
                    return CalcSwarm(count, spacing, radius, jitter);
                default:
                    return CalcCluster(count, spacing, radius);
            }
        }

        /// <summary>
        /// 원형 뭉텅이 - 중심 밀집, 바깥으로 퍼지는 링 배치
        /// </summary>
        private static List<Vector3> CalcCluster(int count, float spacing, float radius)
        {
            var result = new List<Vector3>(count);
            if (count <= 0)
                return result;

            result.Add(Vector3.zero);

            int placed = 1;
            int ring = 1;

            while (placed < count)
            {
                float ringRadius = Mathf.Min(ring * spacing, radius);
                int perRing = Mathf.Max(6, Mathf.RoundToInt(2f * Mathf.PI * ringRadius / spacing));
                int remaining = count - placed;
                int thisRing = Mathf.Min(perRing, remaining);

                for (int i = 0; i < thisRing; i++)
                {
                    float angle = (float)i / thisRing * Mathf.PI * 2f;
                    float jitterAngle = Random.Range(-0.1f, 0.1f);
                    float x = Mathf.Cos(angle + jitterAngle) * ringRadius;
                    float z = Mathf.Sin(angle + jitterAngle) * ringRadius;
                    result.Add(new Vector3(x, 0f, z));
                }

                placed += thisRing;
                ring++;

                if (ring > 30) break;
            }

            return result;
        }

        /// <summary>
        /// 일렬 종대 - 리더 뒤쪽(-Z)으로 줄줄이
        /// </summary>
        private static List<Vector3> CalcLine(int count, float spacing, float length)
        {
            var result = new List<Vector3>(count);
            if (count <= 0)
                return result;

            int columns = Mathf.Max(3, Mathf.RoundToInt(length / spacing / 3f));
            int rowsNeeded = Mathf.CeilToInt((float)count / columns);

            int index = 0;
            for (int row = 0; row < rowsNeeded && index < count; row++)
            {
                int thisRowCount = Mathf.Min(columns, count - index);
                float rowStartX = -(thisRowCount - 1) * spacing * 0.5f;
                for (int c = 0; c < thisRowCount; c++)
                {
                    float x = rowStartX + c * spacing;
                    float z = -(row + 1) * spacing;
                    result.Add(new Vector3(x, 0f, z));
                    index++;
                }
            }

            return result;
        }

        /// <summary>
        /// 느슨한 군집 - 반경 내 랜덤 분포 + 흔들림
        /// </summary>
        private static List<Vector3> CalcSwarm(int count, float spacing, float radius, float jitter)
        {
            var result = new List<Vector3>(count);
            if (count <= 0)
                return result;

            result.Add(Vector3.zero);

            for (int i = 1; i < count; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float r = Random.Range(spacing, radius);
                float x = Mathf.Cos(angle) * r + Random.Range(-jitter, jitter);
                float z = Mathf.Sin(angle) * r + Random.Range(-jitter, jitter);
                result.Add(new Vector3(x, 0f, z));
            }

            return result;
        }
    }
}
