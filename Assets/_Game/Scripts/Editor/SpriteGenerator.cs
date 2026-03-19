using UnityEngine;
using UnityEditor;
using System.IO;

namespace DrillCorp.Editor
{
    public class SpriteGenerator : UnityEditor.Editor
    {
        [MenuItem("Tools/Drill-Corp/Generate UI Sprites")]
        public static void GenerateUISprites()
        {
            string folderPath = "Assets/_Game/Sprites/UI";

            // 폴더 생성
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // 단순 사각형 스프라이트 생성 (4x4 흰색)
            CreateSquareSprite(folderPath, "Square_White", 4, Color.white);

            AssetDatabase.Refresh();
            Debug.Log("[SpriteGenerator] UI 스프라이트 생성 완료!");
        }

        private static void CreateSquareSprite(string folderPath, string name, int size, Color color)
        {
            Texture2D texture = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();

            // PNG로 저장
            byte[] pngData = texture.EncodeToPNG();
            string filePath = $"{folderPath}/{name}.png";
            File.WriteAllBytes(filePath, pngData);

            // 메모리 정리
            DestroyImmediate(texture);
        }
    }
}
