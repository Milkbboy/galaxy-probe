using UnityEngine;

namespace DrillCorp.Ability
{
    /// <summary>
    /// 어빌리티 바닥 데칼용 Mesh 생성기 — XZ 평면 위에 눕힌 사각형/부채꼴을 만든다.
    ///
    /// 모든 Mesh는 **로컬 공간에서 XZ 평면 (Y=0)**에 배치된다.
    ///   · 직사각형: 로컬 +Z 방향으로 length, 좌우 halfWidth
    ///     → 게임오브젝트 rotation = LookRotation(dir, up) 만 주면 끝.
    ///   · 부채꼴: 로컬 +Z 중심, ±halfAngle 벌어짐, 반지름 range
    ///     → 동일한 회전 규칙.
    ///
    /// 회전은 `Quaternion.Euler(90,0,0)` 불필요 — 메시 자체가 이미 XZ 평면이기 때문.
    /// (SpriteRenderer 프리펩은 원래 XY 평면이라 90°X 회전이 필요했지만, Mesh는 처음부터 XZ로 만든다.)
    /// </summary>
    public static class AbilityDecalMeshBuilder
    {
        /// <summary>
        /// 원점에서 +Z 방향으로 뻗는 직사각형. 길이 `length`, 좌우 반폭 `halfWidth`.
        /// 정점 4개 / 삼각형 2개. normals = +Y (탑뷰).
        /// </summary>
        public static Mesh BuildRectangle(float halfWidth, float length)
        {
            var mesh = new Mesh { name = "AbilityDecal_Rect" };

            var verts = new Vector3[4]
            {
                new Vector3(-halfWidth, 0f, 0f),
                new Vector3(+halfWidth, 0f, 0f),
                new Vector3(+halfWidth, 0f, length),
                new Vector3(-halfWidth, 0f, length),
            };
            var uvs = new Vector2[4]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
            };
            var normals = new Vector3[4] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            // Unity 기본 앞면 윈딩 = CW (법선 방향에서 봤을 때). 법선이 +Y이므로
            // +Y(위)에서 내려다본 탑뷰 카메라 시점에서 CW: 좌하(0) → 좌상(3) → 우상(2), 좌하(0) → 우상(2) → 우하(1).
            var tris = new int[6] { 0, 3, 2, 0, 2, 1 };

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// 원점을 중심으로 반지름 `radius` 의 속이 찬 원판. XZ 평면. segments = 둘레 분할 수.
        /// </summary>
        public static Mesh BuildCircle(float radius, int segments = 32)
        {
            segments = Mathf.Max(6, segments);

            var mesh = new Mesh { name = "AbilityDecal_Circle" };

            int vertCount = segments + 1; // 중심 1 + 외곽 segments
            var verts = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];
            var normals = new Vector3[vertCount];

            verts[0] = Vector3.zero;
            uvs[0] = new Vector2(0.5f, 0.5f);
            normals[0] = Vector3.up;

            for (int i = 0; i < segments; i++)
            {
                float a = (float)i / segments * Mathf.PI * 2f;
                float x = Mathf.Sin(a) * radius;
                float z = Mathf.Cos(a) * radius;
                verts[i + 1] = new Vector3(x, 0f, z);
                uvs[i + 1] = new Vector2(0.5f + Mathf.Sin(a) * 0.5f, 0.5f + Mathf.Cos(a) * 0.5f);
                normals[i + 1] = Vector3.up;
            }

            // CW from +Y — 부채꼴과 동일한 규칙: (center → i+1 → next).
            // i+1이 스크린 좌측이고 next가 우측이 되도록 인덱스 배열.
            var tris = new int[segments * 3];
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                tris[i * 3 + 0] = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = next + 1;
            }

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// 원점을 중심으로 내반지름 `innerRadius` ~ 외반지름 `outerRadius` 의 속이 빈 원형 링.
        /// XZ 평면 위 평면 링. segments = 둘레 분할 수.
        /// </summary>
        public static Mesh BuildRing(float innerRadius, float outerRadius, int segments = 48)
        {
            segments = Mathf.Max(8, segments);

            var mesh = new Mesh { name = "AbilityDecal_Ring" };

            int vertCount = segments * 2;
            var verts = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];
            var normals = new Vector3[vertCount];

            for (int i = 0; i < segments; i++)
            {
                float a = (float)i / segments * Mathf.PI * 2f;
                float cos = Mathf.Cos(a);
                float sin = Mathf.Sin(a);
                // 짝수 인덱스 = inner, 홀수 = outer
                verts[i * 2 + 0] = new Vector3(sin * innerRadius, 0f, cos * innerRadius);
                verts[i * 2 + 1] = new Vector3(sin * outerRadius, 0f, cos * outerRadius);
                uvs[i * 2 + 0] = new Vector2((float)i / segments, 0f);
                uvs[i * 2 + 1] = new Vector2((float)i / segments, 1f);
                normals[i * 2 + 0] = Vector3.up;
                normals[i * 2 + 1] = Vector3.up;
            }

            // 각 세그먼트마다 쿼드 2개 삼각형 — (inner_i, outer_i, outer_next), (inner_i, outer_next, inner_next)
            var tris = new int[segments * 6];
            for (int i = 0; i < segments; i++)
            {
                int iNext = (i + 1) % segments;
                int i0 = i * 2;
                int i1 = i * 2 + 1;
                int i2 = iNext * 2 + 1;
                int i3 = iNext * 2;

                tris[i * 6 + 0] = i0;
                tris[i * 6 + 1] = i1;
                tris[i * 6 + 2] = i2;
                tris[i * 6 + 3] = i0;
                tris[i * 6 + 4] = i2;
                tris[i * 6 + 5] = i3;
            }

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// 원점에서 +Z 중심으로 ±halfAngleRad 벌어지는 부채꼴. 반지름 `range`.
        /// segments = 외곽 호를 쪼갤 분할 수 (클수록 부드러움).
        /// </summary>
        public static Mesh BuildSector(float halfAngleRad, float range, int segments = 24)
        {
            segments = Mathf.Max(2, segments);

            var mesh = new Mesh { name = "AbilityDecal_Sector" };

            int vertCount = segments + 2; // 중심 1 + 호 segments+1
            var verts = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];
            var normals = new Vector3[vertCount];

            verts[0] = Vector3.zero;
            uvs[0] = new Vector2(0.5f, 0f);
            normals[0] = Vector3.up;

            float fullAngle = halfAngleRad * 2f;
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;                    // 0~1
                float a = -halfAngleRad + fullAngle * t;          // -half ~ +half (Y축 회전각)
                // XZ 평면: 부채꼴 중심선은 +Z. +X로 벌어지는 각도 a.
                float x = Mathf.Sin(a) * range;
                float z = Mathf.Cos(a) * range;
                verts[i + 1] = new Vector3(x, 0f, z);
                uvs[i + 1] = new Vector2(t, 1f);
                normals[i + 1] = Vector3.up;
            }

            // 탑뷰 카메라(+Y 위치 → -Y forward)에서 봤을 때 CW = 앞면.
            // 스크린 매핑: 스크린X=월드X, 스크린Y(위)=월드Z.
            // i 증가 → a 증가 → x 증가 (좌→우 이동), z는 halfAngle 근처에서 감소하지만 전반적으로 양수.
            // center(0,0) → v[i+1](스크린 좌측 상단쪽) → v[i+2](스크린 우측 상단쪽) 순서가
            // shoelace 기준 CW (음수). 아래 winding이 카메라에 보이는 앞면.
            var tris = new int[segments * 3];
            for (int i = 0; i < segments; i++)
            {
                tris[i * 3 + 0] = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = i + 2;
            }

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
