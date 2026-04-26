using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Networking;
using DrillCorp.Data;
using DrillCorp.Bug.Simple;
using DrillCorp.Weapon;

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
        private readonly string[] _previewTabNames = { "SimpleBugData", "WaveData", "MachineData", "UpgradeData", "WeaponData", "WeaponUpgradeData" };

        private Dictionary<string, List<List<string>>> _previewData = new Dictionary<string, List<List<string>>>();
        private Vector2 _previewScrollPosition;
        private bool _isLoading = false;

        // 시트 이름
        private const string SHEET_SIMPLE_BUG_DATA = "SimpleBugData";
        private const string SHEET_WAVE_DATA = "WaveData";
        private const string SHEET_MACHINE_DATA = "MachineData";
        private const string SHEET_UPGRADE_DATA = "UpgradeData";
        private const string SHEET_WEAPON_DATA = "WeaponData";
        private const string SHEET_WEAPON_UPGRADE_DATA = "WeaponUpgradeData";

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
                string[] sheetNames = { SHEET_SIMPLE_BUG_DATA, SHEET_WAVE_DATA, SHEET_MACHINE_DATA, SHEET_UPGRADE_DATA, SHEET_WEAPON_DATA, SHEET_WEAPON_UPGRADE_DATA };

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
            if (GUILayout.Button("SimpleBugData"))
            {
                ImportSimpleBugData();
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

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("WeaponData"))
            {
                ImportWeaponData();
            }
            if (GUILayout.Button("WeaponUpgradeData"))
            {
                ImportWeaponUpgradeData();
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
                await ImportSimpleBugDataAsync();
                await ImportMachineDataAsync();
                await ImportUpgradeDataAsync();
                await ImportWaveDataAsync();
                await ImportWeaponDataAsync();
                await ImportWeaponUpgradeDataAsync();

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

        private async void ImportSimpleBugData()
        {
            SetStatus("SimpleBugData 가져오는 중...", MessageType.Info);
            try
            {
                await ImportSimpleBugDataAsync();
                SetStatus("SimpleBugData 가져오기 완료!", MessageType.Info);
            }
            catch (Exception e)
            {
                SetStatus($"SimpleBugData 오류: {e.Message}", MessageType.Error);
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

        private async void ImportWeaponData()
        {
            SetStatus("WeaponData 가져오는 중...", MessageType.Info);
            try
            {
                await ImportWeaponDataAsync();
                SetStatus("WeaponData 가져오기 완료!", MessageType.Info);
            }
            catch (Exception e)
            {
                SetStatus($"WeaponData 오류: {e.Message}", MessageType.Error);
            }
        }

        private async void ImportWeaponUpgradeData()
        {
            SetStatus("WeaponUpgradeData 가져오는 중...", MessageType.Info);
            try
            {
                await ImportWeaponUpgradeDataAsync();
                SetStatus("WeaponUpgradeData 가져오기 완료!", MessageType.Info);
            }
            catch (Exception e)
            {
                SetStatus($"WeaponUpgradeData 오류: {e.Message}", MessageType.Error);
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

        // ─────────────────────────────────────────────
        // SimpleBugData import
        // 매칭 기준: SO 내부 BugName 필드 (파일명 오타 SimpleBug_Elit.asset 수용)
        // 빈 셀 → 기존 SO 값 보존. Prefab 필드는 절대 덮어쓰지 않음.
        // ─────────────────────────────────────────────
        private async Task ImportSimpleBugDataAsync()
        {
            var rows = await ReadSheetAsync(SHEET_SIMPLE_BUG_DATA);
            if (rows.Count < 2) return;

            var headers = rows[0];
            string savePath = "Assets/_Game/Data/Bugs";
            if (!AssetDatabase.IsValidFolder(savePath))
                AssetDatabase.CreateFolder("Assets/_Game/Data", "Bugs");

            // 기존 SimpleBugData SO 전부 로드 후 BugName으로 인덱싱
            var cache = new Dictionary<string, SimpleBugData>(StringComparer.OrdinalIgnoreCase);
            foreach (var guid in AssetDatabase.FindAssets("t:SimpleBugData"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<SimpleBugData>(path);
                if (so != null && !string.IsNullOrEmpty(so.BugName))
                    cache[so.BugName] = so;
            }

            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Count == 0 || string.IsNullOrEmpty(row[0])) continue;

                string bugName = GetValue(row, headers, "BugName", "");
                if (string.IsNullOrEmpty(bugName))
                {
                    Debug.LogWarning($"[GoogleSheetsImporter] SimpleBugData row {i}: BugName 비어있음, 스킵");
                    continue;
                }

                SimpleBugData data;
                if (!cache.TryGetValue(bugName, out data))
                {
                    data = ScriptableObject.CreateInstance<SimpleBugData>();
                    data.BugName = bugName;
                    string newPath = $"{savePath}/SimpleBug_{bugName}.asset";
                    AssetDatabase.CreateAsset(data, newPath);
                    cache[bugName] = data;
                    Debug.Log($"[GoogleSheetsImporter] 신규 생성: {newPath}");
                }

                // Kind enum
                string kindStr = GetValue(row, headers, "Kind", "");
                if (!string.IsNullOrEmpty(kindStr) && Enum.TryParse<SimpleBugData.BugKind>(kindStr, true, out var kind))
                {
                    data.Kind = kind;
                }

                // 숫자 필드 — 빈 셀은 기존값 보존
                data.BaseHp = GetFloatOrKeep(row, headers, "BaseHp", data.BaseHp);
                data.HpPerWave = GetFloatOrKeep(row, headers, "HpPerWave", data.HpPerWave);
                data.BaseSpeed = GetFloatOrKeep(row, headers, "BaseSpeed", data.BaseSpeed);
                data.SpeedPerWave = GetFloatOrKeep(row, headers, "SpeedPerWave", data.SpeedPerWave);
                data.SpeedRandom = GetFloatOrKeep(row, headers, "SpeedRandom", data.SpeedRandom);
                data.Size = GetFloatOrKeep(row, headers, "Size", data.Size);
                data.Score = GetFloatOrKeep(row, headers, "Score", data.Score);

                // TintHex — 빈 셀은 기존 Tint 보존
                string tintHex = GetValue(row, headers, "TintHex", "");
                if (!string.IsNullOrEmpty(tintHex) && TryParseHexColor(tintHex, out var tint))
                {
                    data.Tint = tint;
                }

                EditorUtility.SetDirty(data);
                Debug.Log($"[GoogleSheetsImporter] Imported SimpleBugData: {bugName}");
            }

            AssetDatabase.SaveAssets();
        }

        // ─────────────────────────────────────────────
        // WaveData import (새 스키마)
        // SimpleWaveData SO에 오버라이드 값 주입. -1/빈셀 → -1로 기록(런타임 Resolve에서 폴백), 0 → 0 기록.
        // 기존 Wave_NN.asset을 WaveNumber 기준으로 매칭, 없으면 신규 생성.
        // ─────────────────────────────────────────────
        private async Task ImportWaveDataAsync()
        {
            var rows = await ReadSheetAsync(SHEET_WAVE_DATA);
            if (rows.Count < 2) return;

            var headers = rows[0];
            string savePath = "Assets/_Game/Data/Waves";
            if (!AssetDatabase.IsValidFolder(savePath))
                AssetDatabase.CreateFolder("Assets/_Game/Data", "Waves");

            // 기존 SimpleWaveData SO 전부 로드 후 WaveNumber로 인덱싱
            var cache = new Dictionary<int, SimpleWaveData>();
            foreach (var guid in AssetDatabase.FindAssets("t:SimpleWaveData"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<SimpleWaveData>(path);
                if (so != null) cache[so.WaveNumber] = so;
            }

            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Count == 0 || string.IsNullOrEmpty(row[0])) continue;

                int waveNumber = GetIntValue(row, headers, "WaveNumber", -1);
                if (waveNumber < 1)
                {
                    Debug.LogWarning($"[GoogleSheetsImporter] WaveData row {i}: WaveNumber 이상({waveNumber}), 스킵");
                    continue;
                }

                SimpleWaveData wave;
                if (!cache.TryGetValue(waveNumber, out wave))
                {
                    wave = ScriptableObject.CreateInstance<SimpleWaveData>();
                    wave.WaveNumber = waveNumber;
                    string newPath = $"{savePath}/Wave_{waveNumber:D2}.asset";
                    AssetDatabase.CreateAsset(wave, newPath);
                    cache[waveNumber] = wave;
                    Debug.Log($"[GoogleSheetsImporter] 신규 생성: {newPath}");
                }

                wave.WaveNumber = waveNumber;
                wave.WaveName = GetValue(row, headers, "WaveName", wave.WaveName);
                wave.KillTarget = GetFloatOrKeep(row, headers, "KillTarget", wave.KillTarget);

                // 오버라이드 — 빈 셀은 기존 값 유지. 시트에 "-1"을 명시하면 -1로 기록되어 런타임에서 폴백 동작.
                wave.NormalSpawnInterval = GetFloatOrKeep(row, headers, "NormalSpawnInterval", wave.NormalSpawnInterval);
                wave.EliteSpawnInterval = GetFloatOrKeep(row, headers, "EliteSpawnInterval", wave.EliteSpawnInterval);
                wave.MaxBugs = GetIntOrKeep(row, headers, "MaxBugs", wave.MaxBugs);
                wave.TunnelEnabled = GetBoolValue(row, headers, "TunnelEnabled", wave.TunnelEnabled);
                wave.TunnelEventInterval = GetFloatOrKeep(row, headers, "TunnelEventInterval", wave.TunnelEventInterval);
                wave.SwiftPerTunnel = GetIntOrKeep(row, headers, "SwiftPerTunnel", wave.SwiftPerTunnel);

                EditorUtility.SetDirty(wave);
                Debug.Log($"[GoogleSheetsImporter] Imported Wave_{waveNumber:D2} ({wave.WaveName})");
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
                SetSerializedField(so, "_miningRate", GetFloatValue(row, headers, "MiningRate", 5f));
                SetSerializedField(so, "_miningBonus", GetFloatValue(row, headers, "MiningBonus", 0f));
                SetSerializedField(so, "_baseMiningTarget", GetFloatValue(row, headers, "BaseMiningTarget", 100f));
                SetSerializedField(so, "_baseGemDropRate", GetFloatValue(row, headers, "BaseGemDropRate", 0.05f));

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

            // 기존 UpgradeData SO 전부 로드 후 UpgradeId 로 인덱싱.
            // 파일명이 아니라 _upgradeId 필드 기준으로 매칭해야 씬 바인딩(GUID) 보존됨.
            var cache = new Dictionary<string, UpgradeData>(StringComparer.OrdinalIgnoreCase);
            foreach (var guid in AssetDatabase.FindAssets("t:UpgradeData"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var so0 = AssetDatabase.LoadAssetAtPath<UpgradeData>(path);
                if (so0 != null && !string.IsNullOrEmpty(so0.UpgradeId))
                    cache[so0.UpgradeId] = so0;
            }

            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Count == 0 || string.IsNullOrEmpty(row[0])) continue;

                string upgradeId = GetValue(row, headers, "UpgradeId", $"upgrade_{i}");

                UpgradeData upgradeData;
                if (!cache.TryGetValue(upgradeId, out upgradeData))
                {
                    upgradeData = ScriptableObject.CreateInstance<UpgradeData>();
                    string newPath = $"{savePath}/Upgrade_{upgradeId}.asset";
                    AssetDatabase.CreateAsset(upgradeData, newPath);
                    cache[upgradeId] = upgradeData;
                    Debug.Log($"[GoogleSheetsImporter] 신규 생성: {newPath}");
                }

                var so = new SerializedObject(upgradeData);
                SetSerializedField(so, "_upgradeId", upgradeId);
                SetSerializedField(so, "_displayName", GetValue(row, headers, "DisplayName", ""));
                SetSerializedField(so, "_description", GetValue(row, headers, "Description", ""));
                SetSerializedField(so, "_maxLevel", GetIntValue(row, headers, "MaxLevel", 10));
                SetSerializedField(so, "_baseValue", GetFloatValue(row, headers, "BaseValue", 0f));
                SetSerializedField(so, "_valuePerLevel", GetFloatValue(row, headers, "ValuePerLevel", 1f));
                SetSerializedField(so, "_isPercentage", GetBoolValue(row, headers, "IsPercentage", false));

                // BaseCostOre (v2 네이밍) 우선, fallback BaseCost (레거시)
                int baseCostOre = GetIntValue(row, headers, "BaseCostOre", int.MinValue);
                if (baseCostOre == int.MinValue) baseCostOre = GetIntValue(row, headers, "BaseCost", 100);
                SetSerializedField(so, "_baseCost", baseCostOre);

                SetSerializedField(so, "_baseCostGem", GetIntValue(row, headers, "BaseCostGem", 0));
                SetSerializedField(so, "_costMultiplier", GetFloatValue(row, headers, "CostMultiplier", 1.5f));

                // 배열 스케줄 (파이프 구분: "60|130|230|370|540")
                SetSerializedIntArray(so, "_oreCostSchedule", GetValue(row, headers, "OreCostSchedule", ""));
                SetSerializedIntArray(so, "_gemCostSchedule", GetValue(row, headers, "GemCostSchedule", ""));

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

                // CurrencyType enum (Ore/Gem/Both, default Ore)
                string currencyStr = GetValue(row, headers, "CurrencyType", "Ore");
                if (Enum.TryParse<UpgradeCurrencyType>(currencyStr, true, out var currencyType))
                {
                    var currencyProp = so.FindProperty("_currencyType");
                    if (currencyProp != null)
                    {
                        currencyProp.enumValueIndex = (int)currencyType;
                    }
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(upgradeData);

                Debug.Log($"[GoogleSheetsImporter] Imported: {upgradeId} ({currencyStr})");
            }

            AssetDatabase.SaveAssets();
        }

        // 무기별 ExtraStats 허용 키 — 코드 = 명세 (살아있는 문서).
        // SO 클래스(서브타입)에 새 필드 추가 시 여기에도 추가해야 임포트가 통과됨.
        private static readonly Dictionary<string, HashSet<string>> _allowedExtraKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            ["sniper"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                "useAimRadius", "customRange" },
            ["bomb"]   = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                "explosionRadius", "instant", "projectileSpeed", "projectileLifetime", "explosionVfxLifetime" },
            ["gun"]    = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                "maxAmmo", "reloadDuration", "lowAmmoThreshold", "bulletSpeed", "bulletLifetime", "bulletHitRadius", "spreadAngle" },
            ["laser"]  = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                "cooldown", "beamDuration", "beamSpeed", "stopDistance", "beamRadius", "tickInterval",
                "scorchScaleMultiplier", "scorchStopAfter", "scorchTotalLifetime" },
            ["saw"]    = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                "orbitRadius", "bladeRadius", "spinSpeed", "damageTickInterval", "slowFactor", "slowDuration" },
        };

        private async Task ImportWeaponDataAsync()
        {
            var rows = await ReadSheetAsync(SHEET_WEAPON_DATA);
            if (rows.Count < 2) return;

            var headers = rows[0];

            // 기존 WeaponData SO 전부 로드 후 WeaponId 기준 인덱싱.
            // 무기는 5종이고 각자 다른 서브클래스라 신규 생성은 지원 안 함 — 기존만 update-in-place.
            var cache = new Dictionary<string, DrillCorp.Weapon.WeaponData>(StringComparer.OrdinalIgnoreCase);
            foreach (var guid in AssetDatabase.FindAssets("t:WeaponData"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var so0 = AssetDatabase.LoadAssetAtPath<DrillCorp.Weapon.WeaponData>(path);
                if (so0 != null && !string.IsNullOrEmpty(so0.WeaponId))
                    cache[so0.WeaponId] = so0;
            }

            // Pass 1 — 공통 필드 + ExtraStats
            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Count == 0 || string.IsNullOrEmpty(row[0])) continue;

                string weaponId = GetValue(row, headers, "WeaponId", "");
                if (string.IsNullOrEmpty(weaponId)) continue;

                if (!cache.TryGetValue(weaponId, out var weapon))
                {
                    Debug.LogError($"[WeaponData] 시트의 WeaponId='{weaponId}' 에 해당하는 SO가 없음. " +
                                   "신규 무기는 Unity 에서 SO 생성(서브클래스 선택) 후 import 하세요.");
                    continue;
                }

                var so = new SerializedObject(weapon);

                SetSerializedField(so, "_displayName", GetValue(row, headers, "DisplayName", ""));
                SetSerializedField(so, "_description", GetValue(row, headers, "Description", ""));

                // ThemeColorHex
                string colorHex = GetValue(row, headers, "ThemeColorHex", "");
                if (!string.IsNullOrEmpty(colorHex) && TryParseHexColor(colorHex, out var color))
                {
                    var prop = so.FindProperty("_themeColor");
                    if (prop != null) prop.colorValue = color;
                }

                SetSerializedField(so, "_unlockedByDefault", GetBoolValue(row, headers, "UnlockedByDefault", false));
                SetSerializedField(so, "_unlockGemCost",     GetIntValue(row, headers, "UnlockGemCost", 0));
                SetSerializedField(so, "_fireDelay",         GetFloatValue(row, headers, "FireDelay", 0.5f));
                SetSerializedField(so, "_damage",            GetFloatValue(row, headers, "Damage", 1f));
                SetSerializedField(so, "_hitVfxLifetime",    GetFloatValue(row, headers, "HitVfxLifetime", 1.5f));

                // ExtraStats — 무기별 고유 필드 (key:value|key:value)
                ApplyExtraStats(so, weaponId, GetValue(row, headers, "ExtraStats", ""));

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(weapon);
            }

            // Pass 2 — RequiredWeaponId → SO 참조 재해석 (Pass 1 후 캐시 완성 보장)
            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Count == 0 || string.IsNullOrEmpty(row[0])) continue;

                string weaponId = GetValue(row, headers, "WeaponId", "");
                if (!cache.TryGetValue(weaponId, out var weapon)) continue;

                var so = new SerializedObject(weapon);
                var reqProp = so.FindProperty("_requiredWeapon");
                if (reqProp == null) continue;

                string reqId = GetValue(row, headers, "RequiredWeaponId", "");
                if (string.IsNullOrEmpty(reqId))
                {
                    reqProp.objectReferenceValue = null;
                }
                else if (cache.TryGetValue(reqId, out var reqWeapon))
                {
                    reqProp.objectReferenceValue = reqWeapon;
                }
                else
                {
                    Debug.LogWarning($"[WeaponData] {weaponId}: RequiredWeaponId='{reqId}' 인 SO 없음 — null 처리");
                    reqProp.objectReferenceValue = null;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(weapon);

                Debug.Log($"[GoogleSheetsImporter] Imported Weapon: {weaponId}");
            }

            AssetDatabase.SaveAssets();
        }

        // ExtraStats 셀 ("explosionRadius:3|instant:false|...") 파싱 + 화이트리스트 검증 후 SO 필드에 반영.
        private void ApplyExtraStats(SerializedObject so, string weaponId, string extraStats)
        {
            if (string.IsNullOrWhiteSpace(extraStats)) return;

            if (!_allowedExtraKeys.TryGetValue(weaponId, out var allowed))
            {
                Debug.LogError($"[WeaponData] '{weaponId}' 등록되지 않은 무기 ID — _allowedExtraKeys 누락");
                return;
            }

            foreach (var pair in extraStats.Split('|'))
            {
                if (string.IsNullOrWhiteSpace(pair)) continue;

                int idx = pair.IndexOf(':');
                if (idx < 0)
                {
                    Debug.LogWarning($"[WeaponData] {weaponId}: ':' 없는 항목 '{pair}' 무시");
                    continue;
                }

                string key = pair.Substring(0, idx).Trim();
                string val = pair.Substring(idx + 1).Trim();

                if (!allowed.Contains(key))
                {
                    string ownerHint = FindOwnerWeapon(key);
                    Debug.LogError(string.IsNullOrEmpty(ownerHint)
                        ? $"[WeaponData] {weaponId}: 허용되지 않은 키 '{key}'"
                        : $"[WeaponData] {weaponId}: 허용되지 않은 키 '{key}' ('{ownerHint}' 무기 전용 — 행 잘못 입력?)");
                    continue;
                }

                var prop = so.FindProperty("_" + key);
                if (prop == null)
                {
                    Debug.LogError($"[WeaponData] {weaponId}: 키 '{key}' 가 화이트리스트에 있지만 SO 필드 '_{key}' 없음 — 클래스/매핑 불일치");
                    continue;
                }

                switch (prop.propertyType)
                {
                    case SerializedPropertyType.Float:
                        if (float.TryParse(val, out var f)) prop.floatValue = f;
                        else Debug.LogWarning($"[WeaponData] {weaponId}.{key}: '{val}' 를 float 으로 파싱 실패");
                        break;
                    case SerializedPropertyType.Integer:
                        if (int.TryParse(val, out var iv)) prop.intValue = iv;
                        else Debug.LogWarning($"[WeaponData] {weaponId}.{key}: '{val}' 를 int 으로 파싱 실패");
                        break;
                    case SerializedPropertyType.Boolean:
                        if (bool.TryParse(val, out var b)) prop.boolValue = b;
                        else Debug.LogWarning($"[WeaponData] {weaponId}.{key}: '{val}' 를 bool 로 파싱 실패");
                        break;
                    default:
                        Debug.LogWarning($"[WeaponData] {weaponId}.{key}: 미지원 타입 {prop.propertyType}");
                        break;
                }
            }
        }

        private string FindOwnerWeapon(string key)
        {
            foreach (var kvp in _allowedExtraKeys)
                if (kvp.Value.Contains(key)) return kvp.Key;
            return null;
        }

        private async Task ImportWeaponUpgradeDataAsync()
        {
            var rows = await ReadSheetAsync(SHEET_WEAPON_UPGRADE_DATA);
            if (rows.Count < 2) return;

            var headers = rows[0];
            string savePath = "Assets/_Game/Data/WeaponUpgrades";
            if (!AssetDatabase.IsValidFolder(savePath))
                AssetDatabase.CreateFolder("Assets/_Game/Data", "WeaponUpgrades");

            // 기존 WeaponUpgradeData SO 전부 로드 후 UpgradeId 기준 인덱싱.
            var cache = new Dictionary<string, WeaponUpgradeData>(StringComparer.OrdinalIgnoreCase);
            foreach (var guid in AssetDatabase.FindAssets("t:WeaponUpgradeData"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var so0 = AssetDatabase.LoadAssetAtPath<WeaponUpgradeData>(path);
                if (so0 != null && !string.IsNullOrEmpty(so0.UpgradeId))
                    cache[so0.UpgradeId] = so0;
            }

            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Count == 0 || string.IsNullOrEmpty(row[0])) continue;

                string upgradeId = GetValue(row, headers, "UpgradeId", "");
                if (string.IsNullOrEmpty(upgradeId)) continue;

                WeaponUpgradeData upgrade;
                if (!cache.TryGetValue(upgradeId, out upgrade))
                {
                    upgrade = ScriptableObject.CreateInstance<WeaponUpgradeData>();
                    string newPath = $"{savePath}/WeaponUpgrade_{upgradeId}.asset";
                    AssetDatabase.CreateAsset(upgrade, newPath);
                    cache[upgradeId] = upgrade;
                    Debug.Log($"[GoogleSheetsImporter] 신규 생성: {newPath}");
                }

                var so = new SerializedObject(upgrade);
                SetSerializedField(so, "_upgradeId",         upgradeId);
                SetSerializedField(so, "_weaponId",          GetValue(row, headers, "WeaponId", ""));
                SetSerializedField(so, "_displayName",       GetValue(row, headers, "DisplayName", ""));
                SetSerializedField(so, "_maxLevel",          GetIntValue(row, headers, "MaxLevel", 5));
                SetSerializedField(so, "_valuePerLevel",     GetFloatValue(row, headers, "ValuePerLevel", 0.25f));
                SetSerializedField(so, "_isPercentage",      GetBoolValue(row, headers, "IsPercentage", true));
                SetSerializedField(so, "_baseCostOre",       GetIntValue(row, headers, "BaseCostOre", 40));
                SetSerializedField(so, "_baseCostGem",       GetIntValue(row, headers, "BaseCostGem", 2));
                SetSerializedField(so, "_oreCostMultiplier", GetFloatValue(row, headers, "OreCostMultiplier", 2f));
                SetSerializedField(so, "_gemCostMultiplier", GetFloatValue(row, headers, "GemCostMultiplier", 2f));

                // TargetStat enum
                string targetStr = GetValue(row, headers, "TargetStat", "Damage");
                if (Enum.TryParse<WeaponUpgradeStat>(targetStr, true, out var stat))
                {
                    var p = so.FindProperty("_targetStat");
                    if (p != null) p.enumValueIndex = (int)stat;
                }
                else Debug.LogWarning($"[WeaponUpgradeData] {upgradeId}: TargetStat='{targetStr}' 파싱 실패");

                // Operation enum
                string opStr = GetValue(row, headers, "Operation", "Multiply");
                if (Enum.TryParse<WeaponUpgradeOp>(opStr, true, out var op))
                {
                    var p = so.FindProperty("_operation");
                    if (p != null) p.enumValueIndex = (int)op;
                }
                else Debug.LogWarning($"[WeaponUpgradeData] {upgradeId}: Operation='{opStr}' 파싱 실패");

                // ManualCosts — 평행 배열 (둘 길이 같아야)
                SetManualCosts(so,
                    GetValue(row, headers, "ManualCostsOre", ""),
                    GetValue(row, headers, "ManualCostsGem", ""));

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(upgrade);

                Debug.Log($"[GoogleSheetsImporter] Imported: {upgradeId} ({stat}, {op})");
            }

            AssetDatabase.SaveAssets();
        }

        // ManualCostsOre/Gem 파이프 배열 → List<WeaponUpgradeCostTuple>.
        // 둘 다 비면 list clear (BaseCost × Multiplier^level 공식 사용).
        // 길이 다르면 짧은 쪽 기준 + 경고.
        private void SetManualCosts(SerializedObject so, string oreCsv, string gemCsv)
        {
            var prop = so.FindProperty("_manualCosts");
            if (prop == null || !prop.isArray) return;

            bool oreEmpty = string.IsNullOrWhiteSpace(oreCsv);
            bool gemEmpty = string.IsNullOrWhiteSpace(gemCsv);
            if (oreEmpty && gemEmpty)
            {
                prop.arraySize = 0;
                return;
            }

            var oreParts = oreEmpty ? new string[0] : oreCsv.Split('|');
            var gemParts = gemEmpty ? new string[0] : gemCsv.Split('|');
            int len = Mathf.Min(oreParts.Length, gemParts.Length);
            if (oreParts.Length != gemParts.Length)
                Debug.LogWarning($"[WeaponUpgradeData] ManualCostsOre/Gem 길이 불일치 ({oreParts.Length} vs {gemParts.Length}) — 짧은 쪽({len}) 기준");

            prop.arraySize = len;
            for (int i = 0; i < len; i++)
            {
                var elem = prop.GetArrayElementAtIndex(i);
                var oreField = elem.FindPropertyRelative("Ore");
                var gemField = elem.FindPropertyRelative("Gem");
                if (oreField != null && int.TryParse(oreParts[i].Trim(), out var oVal)) oreField.intValue = oVal;
                if (gemField != null && int.TryParse(gemParts[i].Trim(), out var gVal)) gemField.intValue = gVal;
            }
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

        // 셀이 비어있으면 기존값(keep) 유지, 채워져 있으면 파싱. "-1" 명시는 -1로 기록됨.
        private float GetFloatOrKeep(List<string> row, List<string> headers, string column, float keep)
        {
            int index = headers.IndexOf(column);
            if (index < 0 || index >= row.Count) return keep;
            string value = row[index];
            if (string.IsNullOrEmpty(value)) return keep;
            return float.TryParse(value, out float result) ? result : keep;
        }

        private int GetIntOrKeep(List<string> row, List<string> headers, string column, int keep)
        {
            int index = headers.IndexOf(column);
            if (index < 0 || index >= row.Count) return keep;
            string value = row[index];
            if (string.IsNullOrEmpty(value)) return keep;
            return int.TryParse(value, out int result) ? result : keep;
        }

        // "#RRGGBB" 또는 "#RRGGBBAA" 파싱. ColorUtility.TryParseHtmlString 위임.
        private bool TryParseHexColor(string hex, out Color color)
        {
            return ColorUtility.TryParseHtmlString(hex.Trim(), out color);
        }

        private void SetSerializedField(SerializedObject so, string fieldName, int value)
        {
            var prop = so.FindProperty(fieldName);
            if (prop != null) prop.intValue = value;
        }

        // 파이프 구분 문자열("60|130|230|370|540") → int[] 로 SerializedProperty 배열에 주입.
        // 빈 문자열이면 length=0 (기존 내용 클리어).
        private void SetSerializedIntArray(SerializedObject so, string fieldName, string piped)
        {
            var prop = so.FindProperty(fieldName);
            if (prop == null || !prop.isArray) return;

            if (string.IsNullOrWhiteSpace(piped))
            {
                prop.arraySize = 0;
                return;
            }

            var parts = piped.Split('|');
            prop.arraySize = parts.Length;
            for (int i = 0; i < parts.Length; i++)
            {
                int v = 0;
                int.TryParse(parts[i].Trim(), out v);
                prop.GetArrayElementAtIndex(i).intValue = v;
            }
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

        #endregion
    }
}
