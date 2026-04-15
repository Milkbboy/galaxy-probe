using System.Collections.Generic;
using UnityEngine;

namespace DrillCorp.Bug.Simple
{
    /// <summary>
    /// 프로토타입(_.html)의 startTunnelEvent + spawnSwiftFromTunnel 포팅.
    /// 게임 시작 후 일정 시간이 지나면 주기적으로 땅굴을 열어
    /// 스위프트 벌레 N마리를 빠른 간격으로 스폰한다.
    /// </summary>
    public class TunnelEventManager : MonoBehaviour
    {
        public struct Warning
        {
            public string Text;
            public string Subtext;
            public float Life;
            public float MaxLife;
            public Vector3 WorldPos;
        }

        private class Tunnel
        {
            public Vector3 Pos;
            public int Remaining;
            public float TickTimer;
        }

        [Header("Dependencies")]
        [SerializeField] private SimpleBugSpawner _spawner;
        [SerializeField] private SimpleBugData _swiftData;

        [Header("Timing (seconds)")]
        [SerializeField] private float _gameTimeStart = 30f;   // GAME_TIME_START 1800f / 60
        [SerializeField] private float _eventInterval = 15f;   // EVENT_INTERVAL 900f / 60
        [SerializeField] private float _tunnelSpawnInterval = 0.2f; // TUNNEL_SPAWN_INTERVAL 12f / 60

        [Header("Tunnel Settings")]
        [SerializeField] private int _swiftPerTunnel = 10;
        [SerializeField] private float _warningDuration = 2.5f;
        [SerializeField] private float _edgeMargin = 0.4f;     // 화면 가장자리로부터 안쪽 여유
        [SerializeField] private float _spawnJitter = 0.15f;   // 땅굴 지점 주변 랜덤 오프셋

        [Header("VFX")]
        [SerializeField] private GameObject _tunnelMarkerPrefab;  // 선택 (없으면 Gizmo만)

        [Header("Controls")]
        [SerializeField] private bool _autoRun = true;

        private float _gameTime;
        private float _eventTimer;
        private readonly List<Tunnel> _tunnels = new();
        private readonly List<Warning> _warnings = new();
        private readonly List<GameObject> _markerObjects = new();

        public IReadOnlyList<Warning> ActiveWarnings => _warnings;
        public float GameTime => _gameTime;
        public bool EventPhaseStarted => _gameTime >= _gameTimeStart;

        private void Update()
        {
            if (!_autoRun) return;

            _gameTime += Time.deltaTime;

            if (EventPhaseStarted)
            {
                _eventTimer -= Time.deltaTime;
                if (_eventTimer <= 0f)
                {
                    StartTunnelEvent();
                    _eventTimer = _eventInterval;
                }
            }

            TickTunnels(Time.deltaTime);
            TickWarnings(Time.deltaTime);
        }

        public void StartTunnelEvent()
        {
            Vector3 pos = PickEdgePosition();
            _tunnels.Add(new Tunnel { Pos = pos, Remaining = _swiftPerTunnel, TickTimer = 0f });
            _warnings.Add(new Warning
            {
                Text = "◈ 땅굴 침공 !",
                Subtext = $"극속 하얀 벌레 {_swiftPerTunnel}마리",
                Life = _warningDuration,
                MaxLife = _warningDuration,
                WorldPos = pos
            });

            if (_tunnelMarkerPrefab != null)
            {
                var marker = Instantiate(_tunnelMarkerPrefab, pos, Quaternion.Euler(90f, 0f, 0f));
                _markerObjects.Add(marker);
            }
        }

        private void TickTunnels(float dt)
        {
            for (int i = _tunnels.Count - 1; i >= 0; i--)
            {
                var t = _tunnels[i];
                t.TickTimer -= dt;
                if (t.TickTimer <= 0f)
                {
                    SpawnSwift(t.Pos);
                    t.Remaining--;
                    t.TickTimer = _tunnelSpawnInterval;
                    if (t.Remaining <= 0)
                    {
                        _tunnels.RemoveAt(i);
                        if (i < _markerObjects.Count && _markerObjects[i] != null)
                        {
                            Destroy(_markerObjects[i]);
                            _markerObjects.RemoveAt(i);
                        }
                    }
                }
            }
        }

        private void TickWarnings(float dt)
        {
            for (int i = _warnings.Count - 1; i >= 0; i--)
            {
                var w = _warnings[i];
                w.Life -= dt;
                if (w.Life <= 0f) _warnings.RemoveAt(i);
                else _warnings[i] = w;
            }
        }

        private void SpawnSwift(Vector3 tunnelPos)
        {
            if (_spawner == null || _swiftData == null || _swiftData.Prefab == null) return;
            Vector3 jitter = new Vector3(
                Random.Range(-_spawnJitter, _spawnJitter),
                0f,
                Random.Range(-_spawnJitter, _spawnJitter)
            );
            _spawner.SpawnAt(_swiftData, tunnelPos + jitter);
        }

        private Vector3 PickEdgePosition()
        {
            var cam = Camera.main;
            if (cam == null || !cam.orthographic)
            {
                return transform.position + Random.insideUnitSphere * 5f;
            }

            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            Vector3 center = cam.transform.position;
            int side = Random.Range(0, 4);
            float x, z;

            switch (side)
            {
                case 0: x = Random.Range(-halfW, halfW); z = halfH; break;       // 상
                case 1: x = halfW; z = Random.Range(-halfH, halfH); break;       // 우
                case 2: x = Random.Range(-halfW, halfW); z = -halfH; break;      // 하
                default: x = -halfW; z = Random.Range(-halfH, halfH); break;     // 좌
            }

            x = Mathf.Clamp(x, -halfW + _edgeMargin, halfW - _edgeMargin);
            z = Mathf.Clamp(z, -halfH + _edgeMargin, halfH - _edgeMargin);
            return new Vector3(center.x + x, 0f, center.z + z);
        }

        public void ResetTimer()
        {
            _gameTime = 0f;
            _eventTimer = 0f;
            foreach (var m in _markerObjects) if (m != null) Destroy(m);
            _markerObjects.Clear();
            _tunnels.Clear();
            _warnings.Clear();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.7f, 0.7f, 1f, 0.9f);
            foreach (var t in _tunnels)
            {
                Gizmos.DrawWireSphere(t.Pos, 0.5f);
            }
        }
#endif
    }
}
