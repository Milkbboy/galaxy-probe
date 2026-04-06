using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Networking;
using DrillCorp.Data;
using DrillCorp.Bug.Behaviors.Data;

namespace DrillCorp.Editor
{
    public class GoogleSheetsImporter : EditorWindow
    {
        private const string CREDENTIALS_PATH = "Assets/_Game/Data/Credentials/google-credentials.json";
        private const string SETTINGS_KEY = "GoogleSheetsImporter_SpreadsheetUrl";
        private const string DEFAULT_SPREADSHEET_URL = "https://docs.google.com/spreadsheets/d/1hwgQ4IF-gQqVSX4xS_uqeKIPWUDy2NR4bC-OWmZQO_E/edit";

        private string _spreadsheetUrl = "";
        private string _spreadsheetId = "";
        private string _accessToken = "";
        private bool _isAuthenticated = false;
        private Vector2 _scrollPosition;
        private string _statusMessage = "";
        private MessageType _statusType = MessageType.None;

        // 미리보기 데이터
        private int _previewTab = 0;
        private readonly string[] _previewTabNames = { "BugData", "WaveData", "SpawnGroups", "MachineData", "UpgradeData" };

        // BugBehavior 폴더 경로
        private const string BEHAVIOR_DATA_PATH = "Assets/_Game/Data/BugBehaviors";
        private Dictionary<string, List<List<string>>> _previewData = new Dictionary<string, List<List<string>>>();
        private Vector2 _previewScrollPosition;
        private bool _isLoading = false;

        // 시트 이름
        private const string SHEET_BUG_DATA = "BugData";
        private const string SHEET_WAVE_DATA = "WaveData";
        private const string SHEET_WAVE_SPAWN_GROUPS = "WaveSpawnGroups";
        private const string SHEET_MACHINE_DATA = "MachineData";
        private const string SHEET_UPGRADE_DATA = "UpgradeData";

        [MenuItem("Tools/Drill-Corp/4. 데이터 Import/Google Sheets Importer")]
        public static void ShowWindow()
        {
            var window = GetWindow<GoogleSheetsImporter>("Google Sheets Importer");
            window.minSize = new Vector2(400, 500);
        }

        private void OnEnable()
        {
            _spreadsheetUrl = EditorPrefs.GetString(SETTINGS_KEY, "");

            // URL이 없으면 기본값 사용
            if (string.IsNullOrEmpty(_spreadsheetUrl))
            {
                _spreadsheetUrl = DEFAULT_SPREADSHEET_URL;
                EditorPrefs.SetString(SETTINGS_KEY, _spreadsheetUrl);
            }

            ExtractSpreadsheetId();

            // 자동 인증
            if (!_isAuthenticated && File.Exists(CREDENTIALS_PATH) && !string.IsNullOrEmpty(_spreadsheetId))
            {
                Authenticate();
            }
        }

        private void ExtractSpreadsheetId()
        {
            // URL에서 Spreadsheet ID 추출
            // 형식: https://docs.google.com/spreadsheets/d/[SPREADSHEET_ID]/edit...
            if (string.IsNullOrEmpty(_spreadsheetUrl))
            {
                _spreadsheetId = "";
                return;
            }

            // 이미 ID만 입력한 경우
            if (!_spreadsheetUrl.Contains("/"))
            {
                _spreadsheetId = _spreadsheetUrl;
                return;
            }

            // URL에서 ID 추출
            try
            {
                string marker = "/d/";
                int startIndex = _spreadsheetUrl.IndexOf(marker);
                if (startIndex >= 0)
                {
                    startIndex += marker.Length;
                    int endIndex = _spreadsheetUrl.IndexOf("/", startIndex);
                    if (endIndex < 0) endIndex = _spreadsheetUrl.Length;
                    _spreadsheetId = _spreadsheetUrl.Substring(startIndex, endIndex - startIndex);
                }
                else
                {
                    _spreadsheetId = _spreadsheetUrl;
                }
            }
            catch
            {
                _spreadsheetId = _spreadsheetUrl;
            }
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeader();
            DrawSettings();
            DrawAuthentication();
            DrawPreviewSection();
            DrawImportButtons();
            DrawStatus();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Google Sheets Data Importer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Google Sheets에서 게임 데이터를 가져와 ScriptableObject로 변환합니다.\n" +
                "Service Account 인증을 사용합니다.",
                MessageType.Info
            );
            EditorGUILayout.Space(10);
        }

        private void DrawSettings()
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Google Sheets URL");
            _spreadsheetUrl = EditorGUILayout.TextField(_spreadsheetUrl);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString(SETTINGS_KEY, _spreadsheetUrl);
                ExtractSpreadsheetId();
            }

            // 추출된 ID 표시
            if (!string.IsNullOrEmpty(_spreadsheetId))
            {
                EditorGUILayout.HelpBox($"✓ Spreadsheet ID: {_spreadsheetId}", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("Google Sheets URL을 입력하세요.", MessageType.Warning);
            }

            EditorGUILayout.Space(5);

            // Credentials 파일 확인
            bool hasCredentials = File.Exists(CREDENTIALS_PATH);
            if (hasCredentials)
            {
                EditorGUILayout.HelpBox("✓ Credentials 파일 확인됨", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "✗ Credentials 파일이 없습니다.\n" +
                    $"경로: {CREDENTIALS_PATH}",
                    MessageType.Error
                );
            }

            EditorGUILayout.Space(10);
        }

        private void DrawAuthentication()
        {
            EditorGUILayout.LabelField("Authentication", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (_isAuthenticated)
            {
                EditorGUILayout.HelpBox("✓ 인증됨", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("✗ 인증 필요", MessageType.Warning);
            }

            if (GUILayout.Button("Authenticate", GUILayout.Width(100)))
            {
                Authenticate();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);
        }

        private void DrawPreviewSection()
        {
            EditorGUILayout.LabelField("Sheet Preview", EditorStyles.boldLabel);

            GUI.enabled = _isAuthenticated && !string.IsNullOrEmpty(_spreadsheetId) && !_isLoading;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(_isLoading ? "Loading..." : "Load Preview", GUILayout.Height(25)))
            {
                LoadAllSheetPreview();
            }
            if (GUILayout.Button("Clear", GUILayout.Width(60), GUILayout.Height(25)))
            {
                _previewData.Clear();
            }
            EditorGUILayout.EndHorizontal();

            GUI.enabled = true;

            if (_previewData.Count > 0)
            {
                EditorGUILayout.Space(5);

                // 탭 선택
                _previewTab = GUILayout.Toolbar(_previewTab, _previewTabNames);

                string currentSheet = _previewTabNames[_previewTab];
                if (currentSheet == "SpawnGroups") currentSheet = SHEET_WAVE_SPAWN_GROUPS;

                if (_previewData.ContainsKey(currentSheet) && _previewData[currentSheet].Count > 0)
                {
                    DrawPreviewTable(_previewData[currentSheet]);
                }
                else
                {
                    EditorGUILayout.HelpBox($"'{currentSheet}' 시트에 데이터가 없습니다.", MessageType.Info);
                }
            }

            EditorGUILayout.Space(10);
        }

        private void DrawPreviewTable(List<List<string>> data)
        {
            if (data == null || data.Count == 0) return;

            var headers = data[0];
            int rowCount = Mathf.Min(data.Count, 20); // 최대 20행 표시

            // 테이블 스크롤뷰
            _previewScrollPosition = EditorGUILayout.BeginScrollView(
                _previewScrollPosition,
                GUILayout.MaxHeight(250)
            );

            // 테이블 스타일
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { background = MakeTexture(1, 1, new Color(0.3f, 0.3f, 0.3f)) }
            };

            var cellStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(4, 4, 2, 2)
            };

            var altRowStyle = new GUIStyle(cellStyle)
            {
                normal = { background = MakeTexture(1, 1, new Color(0.25f, 0.25f, 0.25f, 0.3f)) }
            };

            // 헤더 행
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("#", headerStyle, GUILayout.Width(30));
            foreach (var header in headers)
            {
                EditorGUILayout.LabelField(header, headerStyle, GUILayout.Width(100));
            }
            EditorGUILayout.EndHorizontal();

            // 데이터 행
            for (int i = 1; i < rowCount; i++)
            {
                var row = data[i];
                var style = (i % 2 == 0) ? cellStyle : altRowStyle;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(i.ToString(), style, GUILayout.Width(30));
                for (int j = 0; j < headers.Count; j++)
                {
                    string cellValue = j < row.Count ? row[j] : "";
                    EditorGUILayout.LabelField(cellValue, style, GUILayout.Width(100));
                }
                EditorGUILayout.EndHorizontal();
            }

            if (data.Count > 20)
            {
                EditorGUILayout.HelpBox($"... 외 {data.Count - 20}행 더 있음", MessageType.None);
            }

            EditorGUILayout.EndScrollView();
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = color;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private async void LoadAllSheetPreview()
        {
            if (_isLoading) return;
            _isLoading = true;
            _previewData.Clear();

            SetStatus("시트 데이터 로딩 중...", MessageType.Info);

            try
            {
                string[] sheetNames = { SHEET_BUG_DATA, SHEET_WAVE_DATA, SHEET_WAVE_SPAWN_GROUPS, SHEET_MACHINE_DATA, SHEET_UPGRADE_DATA };

                foreach (var sheetName in sheetNames)
                {
                    try
                    {
                        var data = await ReadSheetAsync(sheetName);
                        _previewData[sheetName] = data;
                        Debug.Log($"[GoogleSheetsImporter] Loaded preview: {sheetName} ({data.Count} rows)");
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[GoogleSheetsImporter] Failed to load {sheetName}: {e.Message}");
                        _previewData[sheetName] = new List<List<string>>();
                    }
                }

                SetStatus($"프리뷰 로드 완료! ({_previewData.Count}개 시트)", MessageType.Info);
            }
            catch (Exception e)
            {
                SetStatus($"프리뷰 로드 실패: {e.Message}", MessageType.Error);
            }
            finally
            {
                _isLoading = false;
                Repaint();
            }
        }

        private void DrawImportButtons()
        {
            EditorGUILayout.LabelField("Import Data", EditorStyles.boldLabel);

            GUI.enabled = _isAuthenticated && !string.IsNullOrEmpty(_spreadsheetId);

            if (GUILayout.Button("Import All Data", GUILayout.Height(30)))
            {
                ImportAllData();
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("BugData"))
            {
                ImportBugData();
            }
            if (GUILayout.Button("WaveData"))
            {
                ImportWaveData();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("MachineData"))
            {
                ImportMachineData();
            }
            if (GUILayout.Button("UpgradeData"))
            {
                ImportUpgradeData();
            }
            EditorGUILayout.EndHorizontal();

            GUI.enabled = true;
            EditorGUILayout.Space(10);
        }

        private void DrawStatus()
        {
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, _statusType);
            }
        }

        private void SetStatus(string message, MessageType type)
        {
            _statusMessage = message;
            _statusType = type;
            Repaint();
        }

        #region Authentication

        private async void Authenticate()
        {
            SetStatus("인증 중...", MessageType.Info);

            try
            {
                _accessToken = await GetAccessTokenAsync();
                _isAuthenticated = !string.IsNullOrEmpty(_accessToken);

                if (_isAuthenticated)
                {
                    SetStatus("인증 성공!", MessageType.Info);
                }
                else
                {
                    SetStatus("인증 실패: 토큰을 가져올 수 없습니다.", MessageType.Error);
                }
            }
            catch (Exception e)
            {
                SetStatus($"인증 오류: {e.Message}", MessageType.Error);
                Debug.LogError($"[GoogleSheetsImporter] Auth error: {e}");
            }
        }

        private async Task<string> GetAccessTokenAsync()
        {
            if (!File.Exists(CREDENTIALS_PATH))
            {
                throw new FileNotFoundException($"Credentials file not found: {CREDENTIALS_PATH}");
            }

            string json = File.ReadAllText(CREDENTIALS_PATH);
            var credentials = JsonUtility.FromJson<ServiceAccountCredentials>(json);

            // JWT 생성
            string jwt = CreateJwt(credentials);

            // 토큰 요청
            string tokenUrl = "https://oauth2.googleapis.com/token";
            string postData = $"grant_type=urn:ietf:params:oauth:grant-type:jwt-bearer&assertion={jwt}";

            using (var request = new UnityWebRequest(tokenUrl, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(postData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception($"Token request failed: {request.error}\n{request.downloadHandler.text}");
                }

                var response = JsonUtility.FromJson<TokenResponse>(request.downloadHandler.text);
                return response.access_token;
            }
        }

        private string CreateJwt(ServiceAccountCredentials credentials)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long exp = now + 3600; // 1시간

            // Header
            var header = new JwtHeader { alg = "RS256", typ = "JWT" };
            string headerJson = JsonUtility.ToJson(header);
            string headerBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));

            // Payload
            var payload = new JwtPayload
            {
                iss = credentials.client_email,
                scope = "https://www.googleapis.com/auth/spreadsheets.readonly",
                aud = "https://oauth2.googleapis.com/token",
                iat = now,
                exp = exp
            };
            string payloadJson = JsonUtility.ToJson(payload);
            string payloadBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));

            // Signature
            string message = $"{headerBase64}.{payloadBase64}";
            string signature = SignWithRSA(message, credentials.private_key);

            return $"{message}.{signature}";
        }

        private string SignWithRSA(string message, string privateKey)
        {
            // PEM에서 키 추출
            privateKey = privateKey
                .Replace("-----BEGIN PRIVATE KEY-----", "")
                .Replace("-----END PRIVATE KEY-----", "")
                .Replace("-----BEGIN RSA PRIVATE KEY-----", "")
                .Replace("-----END RSA PRIVATE KEY-----", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Replace(" ", "");

            byte[] keyBytes = Convert.FromBase64String(privateKey);
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);

            // PKCS#8 형식에서 RSA 파라미터 추출
            var rsaParams = DecodeRsaPrivateKey(keyBytes);

            using (var rsa = new System.Security.Cryptography.RSACryptoServiceProvider())
            {
                rsa.ImportParameters(rsaParams);
                byte[] signature = rsa.SignData(messageBytes, System.Security.Cryptography.CryptoConfig.MapNameToOID("SHA256"));
                return Base64UrlEncode(signature);
            }
        }

        private System.Security.Cryptography.RSAParameters DecodeRsaPrivateKey(byte[] pkcs8Bytes)
        {
            // PKCS#8 구조: SEQUENCE { INTEGER (version), SEQUENCE { OID, NULL }, OCTET STRING (RSA key) }
            // 간단한 파싱 - Google Service Account 키 형식에 맞춤

            using (var mem = new MemoryStream(pkcs8Bytes))
            using (var reader = new BinaryReader(mem))
            {
                // 외부 SEQUENCE 건너뛰기
                SkipAsn1Header(reader);

                // Version INTEGER 건너뛰기
                SkipAsn1Element(reader);

                // AlgorithmIdentifier SEQUENCE 건너뛰기
                SkipAsn1Element(reader);

                // OCTET STRING (실제 RSA 키)
                byte tag = reader.ReadByte();
                int length = ReadAsn1Length(reader);

                // RSA 키 SEQUENCE
                return DecodeRsaKeySequence(reader);
            }
        }

        private System.Security.Cryptography.RSAParameters DecodeRsaKeySequence(BinaryReader reader)
        {
            // RSA 키 SEQUENCE 헤더
            SkipAsn1Header(reader);

            // Version
            SkipAsn1Element(reader);

            // RSA 파라미터 읽기
            var rsaParams = new System.Security.Cryptography.RSAParameters
            {
                Modulus = ReadAsn1Integer(reader),
                Exponent = ReadAsn1Integer(reader),
                D = ReadAsn1Integer(reader),
                P = ReadAsn1Integer(reader),
                Q = ReadAsn1Integer(reader),
                DP = ReadAsn1Integer(reader),
                DQ = ReadAsn1Integer(reader),
                InverseQ = ReadAsn1Integer(reader)
            };

            return rsaParams;
        }

        private void SkipAsn1Header(BinaryReader reader)
        {
            reader.ReadByte(); // tag
            ReadAsn1Length(reader);
        }

        private void SkipAsn1Element(BinaryReader reader)
        {
            reader.ReadByte(); // tag
            int length = ReadAsn1Length(reader);
            reader.ReadBytes(length);
        }

        private int ReadAsn1Length(BinaryReader reader)
        {
            byte b = reader.ReadByte();
            if ((b & 0x80) == 0)
                return b;

            int numBytes = b & 0x7F;
            int length = 0;
            for (int i = 0; i < numBytes; i++)
            {
                length = (length << 8) | reader.ReadByte();
            }
            return length;
        }

        private byte[] ReadAsn1Integer(BinaryReader reader)
        {
            byte tag = reader.ReadByte();
            if (tag != 0x02)
                throw new Exception($"Expected INTEGER tag (0x02), got 0x{tag:X2}");

            int length = ReadAsn1Length(reader);
            byte[] data = reader.ReadBytes(length);

            // 앞의 0x00 패딩 제거 (부호 비트 때문에 추가된 경우)
            if (data.Length > 1 && data[0] == 0x00)
            {
                byte[] trimmed = new byte[data.Length - 1];
                Array.Copy(data, 1, trimmed, 0, trimmed.Length);
                return trimmed;
            }

            return data;
        }

        private string Base64UrlEncode(byte[] input)
        {
            string base64 = Convert.ToBase64String(input);
            return base64.Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        #endregion

        #region Import Methods

        private async void ImportAllData()
        {
            SetStatus("전체 데이터 가져오는 중...", MessageType.Info);

            try
            {
                await ImportBugDataAsync();
                await ImportWaveDataAsync();
                await ImportMachineDataAsync();
                await ImportUpgradeDataAsync();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                SetStatus("전체 데이터 가져오기 완료!", MessageType.Info);
            }
            catch (Exception e)
            {
                SetStatus($"오류: {e.Message}", MessageType.Error);
                Debug.LogError($"[GoogleSheetsImporter] Import error: {e}");
            }
        }

        private async void ImportBugData()
        {
            SetStatus("BugData 가져오는 중...", MessageType.Info);
            try
            {
                await ImportBugDataAsync();
                SetStatus("BugData 가져오기 완료!", MessageType.Info);
            }
            catch (Exception e)
            {
                SetStatus($"BugData 오류: {e.Message}", MessageType.Error);
            }
        }

        private async void ImportWaveData()
        {
            SetStatus("WaveData 가져오는 중...", MessageType.Info);
            try
            {
                await ImportWaveDataAsync();
                SetStatus("WaveData 가져오기 완료!", MessageType.Info);
            }
            catch (Exception e)
            {
                SetStatus($"WaveData 오류: {e.Message}", MessageType.Error);
            }
        }

        private async void ImportMachineData()
        {
            SetStatus("MachineData 가져오는 중...", MessageType.Info);
            try
            {
                await ImportMachineDataAsync();
                SetStatus("MachineData 가져오기 완료!", MessageType.Info);
            }
            catch (Exception e)
            {
                SetStatus($"MachineData 오류: {e.Message}", MessageType.Error);
            }
        }

        private async void ImportUpgradeData()
        {
            SetStatus("UpgradeData 가져오는 중...", MessageType.Info);
            try
            {
                await ImportUpgradeDataAsync();
                SetStatus("UpgradeData 가져오기 완료!", MessageType.Info);
            }
            catch (Exception e)
            {
                SetStatus($"UpgradeData 오류: {e.Message}", MessageType.Error);
            }
        }

        #endregion

        #region Sheet Reading

        private async Task<List<List<string>>> ReadSheetAsync(string sheetName)
        {
            string range = $"{sheetName}!A:Z";
            string url = $"https://sheets.googleapis.com/v4/spreadsheets/{_spreadsheetId}/values/{Uri.EscapeDataString(range)}";

            using (var request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", $"Bearer {_accessToken}");

                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception($"Sheet read failed: {request.error}\n{request.downloadHandler.text}");
                }

                string json = request.downloadHandler.text;
                return ParseSheetResponse(json);
            }
        }

        private List<List<string>> ParseSheetResponse(string json)
        {
            var result = new List<List<string>>();

            // "values" 배열 찾기
            string valuesKey = "\"values\"";
            int valuesIndex = json.IndexOf(valuesKey);
            if (valuesIndex < 0) return result;

            // "values": 다음의 [ 찾기
            int arrayStart = json.IndexOf('[', valuesIndex);
            if (arrayStart < 0) return result;

            // 해당 배열의 끝 ] 찾기
            int depth = 0;
            int arrayEnd = -1;
            for (int i = arrayStart; i < json.Length; i++)
            {
                if (json[i] == '[') depth++;
                else if (json[i] == ']')
                {
                    depth--;
                    if (depth == 0)
                    {
                        arrayEnd = i;
                        break;
                    }
                }
            }

            if (arrayEnd < 0) return result;

            string valuesJson = json.Substring(arrayStart, arrayEnd - arrayStart + 1);
            return ParseValues(valuesJson);
        }

        private List<List<string>> ParseValues(string valuesJson)
        {
            // Unity JsonUtility는 2차원 배열을 직접 파싱할 수 없으므로 수동 파싱
            var result = new List<List<string>>();

            if (string.IsNullOrEmpty(valuesJson)) return result;

            // 간단한 JSON 배열 파싱 (실제로는 더 robust한 파싱 필요)
            // values: [["a","b"],["c","d"]]
            valuesJson = valuesJson.Trim();
            if (!valuesJson.StartsWith("[")) return result;

            int depth = 0;
            int rowStart = -1;
            var currentRow = new List<string>();

            for (int i = 0; i < valuesJson.Length; i++)
            {
                char c = valuesJson[i];

                if (c == '[')
                {
                    depth++;
                    if (depth == 2)
                    {
                        rowStart = i + 1;
                        currentRow = new List<string>();
                    }
                }
                else if (c == ']')
                {
                    if (depth == 2 && rowStart >= 0)
                    {
                        string rowContent = valuesJson.Substring(rowStart, i - rowStart);
                        currentRow = ParseRow(rowContent);
                        result.Add(currentRow);
                    }
                    depth--;
                }
            }

            return result;
        }

        private List<string> ParseRow(string rowContent)
        {
            var cells = new List<string>();
            bool inQuotes = false;
            var current = new StringBuilder();

            for (int i = 0; i < rowContent.Length; i++)
            {
                char c = rowContent[i];

                if (c == '"' && (i == 0 || rowContent[i - 1] != '\\'))
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    cells.Add(current.ToString().Trim().Trim('"'));
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            if (current.Length > 0)
            {
                cells.Add(current.ToString().Trim().Trim('"'));
            }

            return cells;
        }

        #endregion

        #region Data Import Implementation

        private async Task ImportBugDataAsync()
        {
            var rows = await ReadSheetAsync(SHEET_BUG_DATA);
            if (rows.Count < 2) return; // 헤더 + 최소 1행

            var headers = rows[0];
            string savePath = "Assets/_Game/Data/Bugs";
            string behaviorSavePath = $"{BEHAVIOR_DATA_PATH}/Imported";

            // 폴더 생성
            if (!AssetDatabase.IsValidFolder(savePath))
            {
                AssetDatabase.CreateFolder("Assets/_Game/Data", "Bugs");
            }
            EnsureBehaviorFolders();

            // 기존 Movement/Attack SO 캐시 (참조용)
            var movementCache = LoadBehaviorCache<MovementBehaviorData>($"{BEHAVIOR_DATA_PATH}/Movement");
            var attackCache = LoadBehaviorCache<AttackBehaviorData>($"{BEHAVIOR_DATA_PATH}/Attack");
            var passiveCache = LoadBehaviorCache<PassiveBehaviorData>($"{BEHAVIOR_DATA_PATH}/Passive");
            var skillCache = LoadBehaviorCache<SkillBehaviorData>($"{BEHAVIOR_DATA_PATH}/Skill");
            var triggerCache = LoadBehaviorCache<TriggerBehaviorData>($"{BEHAVIOR_DATA_PATH}/Trigger");

            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Count == 0 || string.IsNullOrEmpty(row[0])) continue;

                string bugName = GetValue(row, headers, "BugName", $"Bug_{i}");
                string assetPath = $"{savePath}/Bug_{bugName}.asset";

                BugData bugData = AssetDatabase.LoadAssetAtPath<BugData>(assetPath);
                if (bugData == null)
                {
                    bugData = ScriptableObject.CreateInstance<BugData>();
                    AssetDatabase.CreateAsset(bugData, assetPath);
                }

                var so = new SerializedObject(bugData);
                SetSerializedField(so, "_bugId", GetIntValue(row, headers, "BugId", i));
                SetSerializedField(so, "_bugName", GetValue(row, headers, "BugName", ""));
                SetSerializedField(so, "_description", GetValue(row, headers, "Description", ""));
                SetSerializedField(so, "_maxHealth", GetFloatValue(row, headers, "MaxHealth", 10f));
                SetSerializedField(so, "_moveSpeed", GetFloatValue(row, headers, "MoveSpeed", 2f));
                SetSerializedField(so, "_attackDamage", GetFloatValue(row, headers, "AttackDamage", 5f));
                SetSerializedField(so, "_attackCooldown", GetFloatValue(row, headers, "AttackCooldown", 1f));
                SetSerializedField(so, "_attackRange", GetFloatValue(row, headers, "AttackRange", 1f));
                SetSerializedField(so, "_scale", GetFloatValue(row, headers, "Scale", 1f));
                SetSerializedField(so, "_currencyReward", GetIntValue(row, headers, "CurrencyReward", 1));
                SetSerializedField(so, "_dropChance", GetFloatValue(row, headers, "DropChance", 1f));

                // HpBarOffset
                float hpX = GetFloatValue(row, headers, "HpBarOffsetX", 0f);
                float hpY = GetFloatValue(row, headers, "HpBarOffsetY", 0.1f);
                float hpZ = GetFloatValue(row, headers, "HpBarOffsetZ", 0.8f);
                var offsetProp = so.FindProperty("_hpBarOffset");
                if (offsetProp != null)
                {
                    offsetProp.vector3Value = new Vector3(hpX, hpY, hpZ);
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(bugData);

                // === Behavior 데이터 처리 ===
                string movementType = GetValue(row, headers, "MovementType", "");
                string attackType = GetValue(row, headers, "AttackType", "");

                // 행동 컬럼이 있으면 BugBehaviorData 생성/갱신
                if (!string.IsNullOrEmpty(movementType) || !string.IsNullOrEmpty(attackType))
                {
                    var behaviorData = CreateOrUpdateBugBehaviorData(
                        bugName,
                        behaviorSavePath,
                        row,
                        headers,
                        movementCache,
                        attackCache,
                        passiveCache,
                        skillCache,
                        triggerCache
                    );

                    // BugData에 BehaviorData 참조 설정
                    so = new SerializedObject(bugData);
                    var behaviorProp = so.FindProperty("_behaviorData");
                    if (behaviorProp != null)
                    {
                        behaviorProp.objectReferenceValue = behaviorData;
                    }
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(bugData);

                    Debug.Log($"[GoogleSheetsImporter] Imported with Behavior: {bugName}");
                }
                else
                {
                    Debug.Log($"[GoogleSheetsImporter] Imported: {bugName}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Behavior 폴더 구조 확인 및 생성
        /// </summary>
        private void EnsureBehaviorFolders()
        {
            string[] folders = { "Imported", "Movement", "Attack", "Passive", "Skill", "Trigger" };

            if (!AssetDatabase.IsValidFolder(BEHAVIOR_DATA_PATH))
            {
                AssetDatabase.CreateFolder("Assets/_Game/Data", "BugBehaviors");
            }

            foreach (var folder in folders)
            {
                string path = $"{BEHAVIOR_DATA_PATH}/{folder}";
                if (!AssetDatabase.IsValidFolder(path))
                {
                    AssetDatabase.CreateFolder(BEHAVIOR_DATA_PATH, folder);
                }
            }
        }

        /// <summary>
        /// 특정 폴더의 Behavior SO들을 이름으로 캐시
        /// </summary>
        private Dictionary<string, T> LoadBehaviorCache<T>(string folderPath) where T : ScriptableObject
        {
            var cache = new Dictionary<string, T>(System.StringComparer.OrdinalIgnoreCase);

            if (!AssetDatabase.IsValidFolder(folderPath))
                return cache;

            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folderPath });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    // 파일명에서 키 추출 (Movement_Linear -> Linear)
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                    string[] parts = fileName.Split('_');
                    string key = parts.Length > 1 ? parts[1] : fileName;
                    cache[key] = asset;
                }
            }

            return cache;
        }

        /// <summary>
        /// BugBehaviorData SO 생성 또는 업데이트
        /// </summary>
        private BugBehaviorData CreateOrUpdateBugBehaviorData(
            string bugName,
            string savePath,
            List<string> row,
            List<string> headers,
            Dictionary<string, MovementBehaviorData> movementCache,
            Dictionary<string, AttackBehaviorData> attackCache,
            Dictionary<string, PassiveBehaviorData> passiveCache,
            Dictionary<string, SkillBehaviorData> skillCache,
            Dictionary<string, TriggerBehaviorData> triggerCache)
        {
            string assetPath = $"{savePath}/BugBehavior_{bugName}.asset";

            BugBehaviorData behaviorData = AssetDatabase.LoadAssetAtPath<BugBehaviorData>(assetPath);
            if (behaviorData == null)
            {
                behaviorData = ScriptableObject.CreateInstance<BugBehaviorData>();
                AssetDatabase.CreateAsset(behaviorData, assetPath);
                AssetDatabase.SaveAssets();
            }

            var so = new SerializedObject(behaviorData);

            // === Movement 설정 ===
            string movementTypeStr = GetValue(row, headers, "MovementType", "Linear");
            float movementParam1 = GetFloatValue(row, headers, "MovementParam1", 0f);
            float movementParam2 = GetFloatValue(row, headers, "MovementParam2", 0f);

            // 기존 SO 있으면 참조, 없으면 새로 생성
            MovementBehaviorData movementSO = FindOrCreateMovementSO(
                movementTypeStr, movementParam1, movementParam2, movementCache);

            var movementProp = so.FindProperty("_defaultMovement");
            if (movementProp != null)
            {
                movementProp.objectReferenceValue = movementSO;
            }

            // === Attack 설정 ===
            string attackTypeStr = GetValue(row, headers, "AttackType", "Melee");
            float attackRange = GetFloatValue(row, headers, "AttackRange", 1.5f);
            float attackParam1 = GetFloatValue(row, headers, "AttackParam1", 0f);
            float attackParam2 = GetFloatValue(row, headers, "AttackParam2", 0f);

            AttackBehaviorData attackSO = FindOrCreateAttackSO(
                attackTypeStr, attackRange, attackParam1, attackParam2, attackCache);

            var attackProp = so.FindProperty("_defaultAttack");
            if (attackProp != null)
            {
                attackProp.objectReferenceValue = attackSO;
            }

            // === Passives 설정 ===
            string passivesStr = GetValue(row, headers, "Passives", "");
            var passiveSOList = ParseAndCreatePassives(passivesStr, passiveCache);
            SetSOListProperty(so, "_passives", passiveSOList);

            // === Skills 설정 ===
            string skillsStr = GetValue(row, headers, "Skills", "");
            var skillSOList = ParseAndCreateSkills(skillsStr, skillCache);
            SetSOListProperty(so, "_skills", skillSOList);

            // === Triggers 설정 ===
            string triggersStr = GetValue(row, headers, "Triggers", "");
            var triggerSOList = ParseAndCreateTriggers(triggersStr, triggerCache);
            SetSOListProperty(so, "_triggers", triggerSOList);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(behaviorData);

            return behaviorData;
        }

        /// <summary>
        /// Movement SO 찾거나 새로 생성
        /// </summary>
        private MovementBehaviorData FindOrCreateMovementSO(
            string typeStr, float param1, float param2,
            Dictionary<string, MovementBehaviorData> cache)
        {
            // 기존 캐시에서 찾기 (파라미터 무시하고 타입만으로)
            if (cache.TryGetValue(typeStr, out var existing))
            {
                return existing;
            }

            // 없으면 Imported 폴더에 새로 생성
            if (!System.Enum.TryParse(typeStr, true, out MovementType movementType))
            {
                movementType = MovementType.Linear;
            }

            string assetPath = $"{BEHAVIOR_DATA_PATH}/Imported/Movement_{typeStr}_{param1}_{param2}.asset";

            // 이미 같은 파라미터로 생성된 것 있는지 확인
            var existingImported = AssetDatabase.LoadAssetAtPath<MovementBehaviorData>(assetPath);
            if (existingImported != null)
                return existingImported;

            var newSO = ScriptableObject.CreateInstance<MovementBehaviorData>();
            AssetDatabase.CreateAsset(newSO, assetPath);

            var so = new SerializedObject(newSO);
            SetSerializedEnumField(so, "_type", (int)movementType);
            SetSerializedField(so, "_displayName", $"{typeStr} (Imported)");
            SetSerializedField(so, "_param1", param1);
            SetSerializedField(so, "_param2", param2);
            so.ApplyModifiedPropertiesWithoutUndo();

            cache[typeStr] = newSO;
            return newSO;
        }

        /// <summary>
        /// Attack SO 찾거나 새로 생성
        /// </summary>
        private AttackBehaviorData FindOrCreateAttackSO(
            string typeStr, float range, float param1, float param2,
            Dictionary<string, AttackBehaviorData> cache)
        {
            if (cache.TryGetValue(typeStr, out var existing))
            {
                return existing;
            }

            if (!System.Enum.TryParse(typeStr, true, out AttackType attackType))
            {
                attackType = AttackType.Melee;
            }

            string assetPath = $"{BEHAVIOR_DATA_PATH}/Imported/Attack_{typeStr}_{range}.asset";

            var existingImported = AssetDatabase.LoadAssetAtPath<AttackBehaviorData>(assetPath);
            if (existingImported != null)
                return existingImported;

            var newSO = ScriptableObject.CreateInstance<AttackBehaviorData>();
            AssetDatabase.CreateAsset(newSO, assetPath);

            var so = new SerializedObject(newSO);
            SetSerializedEnumField(so, "_type", (int)attackType);
            SetSerializedField(so, "_displayName", $"{typeStr} (Imported)");
            SetSerializedField(so, "_range", range);
            SetSerializedField(so, "_param1", param1);
            SetSerializedField(so, "_param2", param2);
            so.ApplyModifiedPropertiesWithoutUndo();

            cache[typeStr] = newSO;
            return newSO;
        }

        /// <summary>
        /// Passives 문자열 파싱 및 SO 리스트 생성
        /// 형식: "Armor:5, Shield:20:2"
        /// </summary>
        private List<PassiveBehaviorData> ParseAndCreatePassives(
            string passivesStr,
            Dictionary<string, PassiveBehaviorData> cache)
        {
            var result = new List<PassiveBehaviorData>();
            if (string.IsNullOrEmpty(passivesStr)) return result;

            string[] entries = passivesStr.Split(',');
            foreach (var entry in entries)
            {
                string trimmed = entry.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                var (type, param1, param2) = PassiveBehaviorData.Parse(trimmed);
                string typeStr = type.ToString();

                // 캐시 확인
                if (cache.TryGetValue(typeStr, out var existing))
                {
                    result.Add(existing);
                    continue;
                }

                // 새로 생성
                string assetPath = $"{BEHAVIOR_DATA_PATH}/Imported/Passive_{typeStr}_{param1}_{param2}.asset";
                var existingImported = AssetDatabase.LoadAssetAtPath<PassiveBehaviorData>(assetPath);
                if (existingImported != null)
                {
                    result.Add(existingImported);
                    continue;
                }

                var newSO = ScriptableObject.CreateInstance<PassiveBehaviorData>();
                AssetDatabase.CreateAsset(newSO, assetPath);

                var so = new SerializedObject(newSO);
                SetSerializedEnumField(so, "_type", (int)type);
                SetSerializedField(so, "_displayName", $"{typeStr} (Imported)");
                SetSerializedField(so, "_param1", param1);
                SetSerializedField(so, "_param2", param2);
                so.ApplyModifiedPropertiesWithoutUndo();

                result.Add(newSO);
            }

            return result;
        }

        /// <summary>
        /// Skills 문자열 파싱 및 SO 리스트 생성
        /// 형식: "Nova:5:10:3" (Type:Cooldown:Param1:Param2)
        /// </summary>
        private List<SkillBehaviorData> ParseAndCreateSkills(
            string skillsStr,
            Dictionary<string, SkillBehaviorData> cache)
        {
            var result = new List<SkillBehaviorData>();
            if (string.IsNullOrEmpty(skillsStr)) return result;

            string[] entries = skillsStr.Split(',');
            foreach (var entry in entries)
            {
                string trimmed = entry.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                var (type, cooldown, param1, param2, stringParam) = SkillBehaviorData.Parse(trimmed);
                string typeStr = type.ToString();

                if (cache.TryGetValue(typeStr, out var existing))
                {
                    result.Add(existing);
                    continue;
                }

                string assetPath = $"{BEHAVIOR_DATA_PATH}/Imported/Skill_{typeStr}_{cooldown}.asset";
                var existingImported = AssetDatabase.LoadAssetAtPath<SkillBehaviorData>(assetPath);
                if (existingImported != null)
                {
                    result.Add(existingImported);
                    continue;
                }

                var newSO = ScriptableObject.CreateInstance<SkillBehaviorData>();
                AssetDatabase.CreateAsset(newSO, assetPath);

                var so = new SerializedObject(newSO);
                SetSerializedEnumField(so, "_type", (int)type);
                SetSerializedField(so, "_displayName", $"{typeStr} (Imported)");
                SetSerializedField(so, "_cooldown", cooldown);
                SetSerializedField(so, "_param1", param1);
                SetSerializedField(so, "_param2", param2);
                if (!string.IsNullOrEmpty(stringParam))
                {
                    SetSerializedField(so, "_stringParam", stringParam);
                }
                so.ApplyModifiedPropertiesWithoutUndo();

                result.Add(newSO);
            }

            return result;
        }

        /// <summary>
        /// Triggers 문자열 파싱 및 SO 리스트 생성
        /// 형식: "Enrage:30:50, ExplodeOnDeath:10:2"
        /// </summary>
        private List<TriggerBehaviorData> ParseAndCreateTriggers(
            string triggersStr,
            Dictionary<string, TriggerBehaviorData> cache)
        {
            var result = new List<TriggerBehaviorData>();
            if (string.IsNullOrEmpty(triggersStr)) return result;

            string[] entries = triggersStr.Split(',');
            foreach (var entry in entries)
            {
                string trimmed = entry.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                var (type, param1, param2, param3, stringParam) = TriggerBehaviorData.Parse(trimmed);
                string typeStr = type.ToString();

                if (cache.TryGetValue(typeStr, out var existing))
                {
                    result.Add(existing);
                    continue;
                }

                string assetPath = $"{BEHAVIOR_DATA_PATH}/Imported/Trigger_{typeStr}_{param1}_{param2}.asset";
                var existingImported = AssetDatabase.LoadAssetAtPath<TriggerBehaviorData>(assetPath);
                if (existingImported != null)
                {
                    result.Add(existingImported);
                    continue;
                }

                var newSO = ScriptableObject.CreateInstance<TriggerBehaviorData>();
                AssetDatabase.CreateAsset(newSO, assetPath);

                var so = new SerializedObject(newSO);
                SetSerializedEnumField(so, "_type", (int)type);
                SetSerializedField(so, "_displayName", $"{typeStr} (Imported)");
                SetSerializedField(so, "_param1", param1);
                SetSerializedField(so, "_param2", param2);
                SetSerializedField(so, "_param3", param3);
                if (!string.IsNullOrEmpty(stringParam))
                {
                    SetSerializedField(so, "_stringParam", stringParam);
                }
                so.ApplyModifiedPropertiesWithoutUndo();

                result.Add(newSO);
            }

            return result;
        }

        /// <summary>
        /// SO 리스트를 SerializedProperty에 설정
        /// </summary>
        private void SetSOListProperty<T>(SerializedObject so, string propertyName, List<T> list) where T : ScriptableObject
        {
            var prop = so.FindProperty(propertyName);
            if (prop == null) return;

            prop.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
            }
        }

        /// <summary>
        /// Enum 필드 설정
        /// </summary>
        private void SetSerializedEnumField(SerializedObject so, string fieldName, int value)
        {
            var prop = so.FindProperty(fieldName);
            if (prop != null) prop.enumValueIndex = value;
        }

        private async Task ImportWaveDataAsync()
        {
            // WaveData 기본 정보
            var waveRows = await ReadSheetAsync(SHEET_WAVE_DATA);
            // SpawnGroups
            var spawnRows = await ReadSheetAsync(SHEET_WAVE_SPAWN_GROUPS);

            if (waveRows.Count < 2) return;

            var waveHeaders = waveRows[0];
            var spawnHeaders = spawnRows.Count > 0 ? spawnRows[0] : new List<string>();

            string savePath = "Assets/_Game/Data/Waves";

            if (!AssetDatabase.IsValidFolder(savePath))
            {
                AssetDatabase.CreateFolder("Assets/_Game/Data", "Waves");
            }

            // BugData 캐시
            var bugDataCache = new Dictionary<int, BugData>();
            var bugGuids = AssetDatabase.FindAssets("t:BugData", new[] { "Assets/_Game/Data/Bugs" });
            foreach (var guid in bugGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var bugData = AssetDatabase.LoadAssetAtPath<BugData>(path);
                if (bugData != null)
                {
                    bugDataCache[bugData.BugId] = bugData;
                }
            }

            for (int i = 1; i < waveRows.Count; i++)
            {
                var row = waveRows[i];
                if (row.Count == 0 || string.IsNullOrEmpty(row[0])) continue;

                int waveNumber = GetIntValue(row, waveHeaders, "WaveNumber", i);
                string assetPath = $"{savePath}/Wave_{waveNumber:D2}.asset";

                WaveData waveData = AssetDatabase.LoadAssetAtPath<WaveData>(assetPath);
                if (waveData == null)
                {
                    waveData = ScriptableObject.CreateInstance<WaveData>();
                    AssetDatabase.CreateAsset(waveData, assetPath);
                }

                var so = new SerializedObject(waveData);
                SetSerializedField(so, "_waveNumber", waveNumber);
                SetSerializedField(so, "_waveName", GetValue(row, waveHeaders, "WaveName", $"Wave {waveNumber}"));
                SetSerializedField(so, "_waveDuration", GetFloatValue(row, waveHeaders, "WaveDuration", 60f));
                SetSerializedField(so, "_delayBeforeNextWave", GetFloatValue(row, waveHeaders, "DelayBeforeNextWave", 3f));
                SetSerializedField(so, "_healthMultiplier", GetFloatValue(row, waveHeaders, "HealthMultiplier", 1f));
                SetSerializedField(so, "_damageMultiplier", GetFloatValue(row, waveHeaders, "DamageMultiplier", 1f));
                SetSerializedField(so, "_speedMultiplier", GetFloatValue(row, waveHeaders, "SpeedMultiplier", 1f));

                // SpawnGroups 찾기
                var groups = new List<SpawnGroupData>();
                for (int j = 1; j < spawnRows.Count; j++)
                {
                    var spawnRow = spawnRows[j];
                    if (spawnRow.Count == 0) continue;

                    int spawnWaveNum = GetIntValue(spawnRow, spawnHeaders, "WaveNumber", -1);
                    if (spawnWaveNum != waveNumber) continue;

                    int bugId = GetIntValue(spawnRow, spawnHeaders, "BugId", 1);
                    BugData bugData = bugDataCache.ContainsKey(bugId) ? bugDataCache[bugId] : null;

                    groups.Add(new SpawnGroupData
                    {
                        BugData = bugData,
                        Count = GetIntValue(spawnRow, spawnHeaders, "Count", 5),
                        StartDelay = GetFloatValue(spawnRow, spawnHeaders, "StartDelay", 0f),
                        SpawnInterval = GetFloatValue(spawnRow, spawnHeaders, "SpawnInterval", 1f),
                        RandomPosition = GetBoolValue(spawnRow, spawnHeaders, "RandomPosition", true)
                    });
                }

                // SpawnGroups 배열 설정
                var spawnGroupsProp = so.FindProperty("_spawnGroups");
                if (spawnGroupsProp != null)
                {
                    spawnGroupsProp.arraySize = groups.Count;
                    for (int g = 0; g < groups.Count; g++)
                    {
                        var element = spawnGroupsProp.GetArrayElementAtIndex(g);
                        element.FindPropertyRelative("BugData").objectReferenceValue = groups[g].BugData;
                        element.FindPropertyRelative("Count").intValue = groups[g].Count;
                        element.FindPropertyRelative("StartDelay").floatValue = groups[g].StartDelay;
                        element.FindPropertyRelative("SpawnInterval").floatValue = groups[g].SpawnInterval;
                        element.FindPropertyRelative("RandomPosition").boolValue = groups[g].RandomPosition;
                    }
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(waveData);

                Debug.Log($"[GoogleSheetsImporter] Imported: Wave_{waveNumber:D2} with {groups.Count} spawn groups");
            }

            AssetDatabase.SaveAssets();
        }

        private async Task ImportMachineDataAsync()
        {
            var rows = await ReadSheetAsync(SHEET_MACHINE_DATA);
            if (rows.Count < 2) return;

            var headers = rows[0];
            string savePath = "Assets/_Game/Data/Machines";

            if (!AssetDatabase.IsValidFolder(savePath))
            {
                AssetDatabase.CreateFolder("Assets/_Game/Data", "Machines");
            }

            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Count == 0 || string.IsNullOrEmpty(row[0])) continue;

                string machineName = GetValue(row, headers, "MachineName", $"Machine_{i}");
                string assetPath = $"{savePath}/Machine_{machineName}.asset";

                MachineData machineData = AssetDatabase.LoadAssetAtPath<MachineData>(assetPath);
                if (machineData == null)
                {
                    machineData = ScriptableObject.CreateInstance<MachineData>();
                    AssetDatabase.CreateAsset(machineData, assetPath);
                }

                var so = new SerializedObject(machineData);
                SetSerializedField(so, "_machineId", GetIntValue(row, headers, "MachineId", i));
                SetSerializedField(so, "_machineName", machineName);
                SetSerializedField(so, "_description", GetValue(row, headers, "Description", ""));
                SetSerializedField(so, "_maxHealth", GetFloatValue(row, headers, "MaxHealth", 100f));
                SetSerializedField(so, "_healthRegen", GetFloatValue(row, headers, "HealthRegen", 0f));
                SetSerializedField(so, "_armor", GetFloatValue(row, headers, "Armor", 0f));
                SetSerializedField(so, "_maxFuel", GetFloatValue(row, headers, "MaxFuel", 60f));
                SetSerializedField(so, "_fuelConsumeRate", GetFloatValue(row, headers, "FuelConsumeRate", 1f));
                SetSerializedField(so, "_miningRate", GetFloatValue(row, headers, "MiningRate", 10f));
                SetSerializedField(so, "_miningBonus", GetFloatValue(row, headers, "MiningBonus", 0f));
                SetSerializedField(so, "_attackDamage", GetFloatValue(row, headers, "AttackDamage", 20f));
                SetSerializedField(so, "_attackCooldown", GetFloatValue(row, headers, "AttackCooldown", 0.5f));
                SetSerializedField(so, "_attackRange", GetFloatValue(row, headers, "AttackRange", 3f));
                SetSerializedField(so, "_critChance", GetFloatValue(row, headers, "CritChance", 0f));
                SetSerializedField(so, "_critMultiplier", GetFloatValue(row, headers, "CritMultiplier", 1.5f));

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(machineData);

                Debug.Log($"[GoogleSheetsImporter] Imported: {machineName}");
            }

            AssetDatabase.SaveAssets();
        }

        private async Task ImportUpgradeDataAsync()
        {
            var rows = await ReadSheetAsync(SHEET_UPGRADE_DATA);
            if (rows.Count < 2) return;

            var headers = rows[0];
            string savePath = "Assets/_Game/Data/Upgrades";

            if (!AssetDatabase.IsValidFolder(savePath))
            {
                AssetDatabase.CreateFolder("Assets/_Game/Data", "Upgrades");
            }

            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Count == 0 || string.IsNullOrEmpty(row[0])) continue;

                string upgradeId = GetValue(row, headers, "UpgradeId", $"upgrade_{i}");
                string assetPath = $"{savePath}/Upgrade_{upgradeId}.asset";

                UpgradeData upgradeData = AssetDatabase.LoadAssetAtPath<UpgradeData>(assetPath);
                if (upgradeData == null)
                {
                    upgradeData = ScriptableObject.CreateInstance<UpgradeData>();
                    AssetDatabase.CreateAsset(upgradeData, assetPath);
                }

                var so = new SerializedObject(upgradeData);
                SetSerializedField(so, "_upgradeId", upgradeId);
                SetSerializedField(so, "_displayName", GetValue(row, headers, "DisplayName", ""));
                SetSerializedField(so, "_description", GetValue(row, headers, "Description", ""));
                SetSerializedField(so, "_maxLevel", GetIntValue(row, headers, "MaxLevel", 10));
                SetSerializedField(so, "_baseValue", GetFloatValue(row, headers, "BaseValue", 0f));
                SetSerializedField(so, "_valuePerLevel", GetFloatValue(row, headers, "ValuePerLevel", 1f));
                SetSerializedField(so, "_isPercentage", GetBoolValue(row, headers, "IsPercentage", false));
                SetSerializedField(so, "_baseCost", GetIntValue(row, headers, "BaseCost", 100));
                SetSerializedField(so, "_costMultiplier", GetFloatValue(row, headers, "CostMultiplier", 1.5f));

                // UpgradeType enum
                string typeStr = GetValue(row, headers, "UpgradeType", "MaxHealth");
                if (Enum.TryParse<UpgradeType>(typeStr, out var upgradeType))
                {
                    var typeProp = so.FindProperty("_upgradeType");
                    if (typeProp != null)
                    {
                        typeProp.enumValueIndex = (int)upgradeType;
                    }
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(upgradeData);

                Debug.Log($"[GoogleSheetsImporter] Imported: {upgradeId}");
            }

            AssetDatabase.SaveAssets();
        }

        #endregion

        #region Helper Methods

        private string GetValue(List<string> row, List<string> headers, string column, string defaultValue)
        {
            int index = headers.IndexOf(column);
            if (index < 0 || index >= row.Count) return defaultValue;
            string value = row[index];
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }

        private int GetIntValue(List<string> row, List<string> headers, string column, int defaultValue)
        {
            string str = GetValue(row, headers, column, "");
            return int.TryParse(str, out int result) ? result : defaultValue;
        }

        private float GetFloatValue(List<string> row, List<string> headers, string column, float defaultValue)
        {
            string str = GetValue(row, headers, column, "");
            return float.TryParse(str, out float result) ? result : defaultValue;
        }

        private bool GetBoolValue(List<string> row, List<string> headers, string column, bool defaultValue)
        {
            string str = GetValue(row, headers, column, "").ToLower();
            if (str == "true" || str == "1" || str == "yes") return true;
            if (str == "false" || str == "0" || str == "no") return false;
            return defaultValue;
        }

        private void SetSerializedField(SerializedObject so, string fieldName, int value)
        {
            var prop = so.FindProperty(fieldName);
            if (prop != null) prop.intValue = value;
        }

        private void SetSerializedField(SerializedObject so, string fieldName, float value)
        {
            var prop = so.FindProperty(fieldName);
            if (prop != null) prop.floatValue = value;
        }

        private void SetSerializedField(SerializedObject so, string fieldName, string value)
        {
            var prop = so.FindProperty(fieldName);
            if (prop != null) prop.stringValue = value;
        }

        private void SetSerializedField(SerializedObject so, string fieldName, bool value)
        {
            var prop = so.FindProperty(fieldName);
            if (prop != null) prop.boolValue = value;
        }

        #endregion

        #region JSON Classes

        [Serializable]
        private class ServiceAccountCredentials
        {
            public string type;
            public string project_id;
            public string private_key_id;
            public string private_key;
            public string client_email;
            public string client_id;
        }

        [Serializable]
        private class TokenResponse
        {
            public string access_token;
            public int expires_in;
            public string token_type;
        }

        [Serializable]
        private class JwtHeader
        {
            public string alg;
            public string typ;
        }

        [Serializable]
        private class JwtPayload
        {
            public string iss;
            public string scope;
            public string aud;
            public long iat;
            public long exp;
        }

        [Serializable]
        private class SheetValuesResponse
        {
            public string range;
            public string majorDimension;
            public string values; // JSON 배열 문자열
        }

        private class SpawnGroupData
        {
            public BugData BugData;
            public int Count;
            public float StartDelay;
            public float SpawnInterval;
            public bool RandomPosition;
        }

        #endregion
    }
}
