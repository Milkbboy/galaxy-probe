using UnityEngine;
using DrillCorp.Core;

namespace DrillCorp.Audio
{
    /// <summary>
    /// SFX 재생 전담. GameEvents 구독으로 머신 피격·벌레 사망음 자동 재생,
    /// 무기·투사체는 공개 API(PlayBombLaunch 등) 직접 호출.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Weapon SFX")]
        [SerializeField] private AudioClip _sfxMachineGunFire;
        [SerializeField] private AudioClip _sfxSniperFire;
        [SerializeField] private AudioClip _sfxBombLaunch;
        [SerializeField] private AudioClip _sfxBombExplosion;
        [SerializeField] private AudioClip _sfxLaserBeam;

        [Header("Bug SFX")]
        [SerializeField] private AudioClip _sfxBugDeath;
        [SerializeField] private AudioClip _sfxBugHit;

        [Header("Machine SFX")]
        [SerializeField] private AudioClip _sfxMachineDamaged;

        [Header("Mixer")]
        [Range(0f, 1f)] [SerializeField] private float _masterVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float _sfxVolume = 1f;

        [Header("Pool")]
        [SerializeField] private int _poolSize = 8;

        [Header("Debug — 개별 SFX 토글 (Play 중에도 동작, 하나씩 켜며 테스트용)")]
        [SerializeField] private bool _enableMachineGunFire = true;
        [SerializeField] private bool _enableSniperFire = true;
        [SerializeField] private bool _enableBombLaunch = true;
        [SerializeField] private bool _enableBombExplosion = true;
        [SerializeField] private bool _enableLaserBeam = true;
        [SerializeField] private bool _enableBugHit = true;
        [SerializeField] private bool _enableBugDeath = true;
        [SerializeField] private bool _enableMachineDamaged = true;

        [Header("Per-SFX Volume (0~2, 1=기본. 클립별 음량 편차 보정용)")]
        [Range(0f, 2f)] [SerializeField] private float _volMachineGunFire = 0.4f;
        [Range(0f, 2f)] [SerializeField] private float _volSniperFire = 1f;
        [Range(0f, 2f)] [SerializeField] private float _volBombLaunch = 1f;
        [Range(0f, 2f)] [SerializeField] private float _volBombExplosion = 1.2f;
        [Range(0f, 2f)] [SerializeField] private float _volLaserBeam = 1f;
        [Range(0f, 2f)] [SerializeField] private float _volBugHit = 0.6f;
        [Range(0f, 2f)] [SerializeField] private float _volBugDeath = 1f;
        [Range(0f, 2f)] [SerializeField] private float _volMachineDamaged = 1f;

        private AudioSource[] _sfxPool;
        private int _poolIndex;

        // 기관총 전용 AudioSource — 연사 겹침 방지 (매 발 Stop→Play로 "탕-탕-탕" 보장)
        private AudioSource _machineGunSource;

        // 레이저 전용 AudioSource — 빔 생존 동안 loop 재생, 빔 파괴 시 Stop
        private AudioSource _laserSource;

        // 같은 클립이 한 프레임에 여러 번 겹치는 경우 최소 간격
        private const float SameClipMinInterval = 0.03f;
        private AudioClip _lastClip;
        private float _lastClipTime;

        // 머신 피격 연속음 쓰로틀 — 프로토 sndHitThrottled 동일 간격(150ms)
        // 벌레 여러 마리가 머신에 붙으면 매 프레임 피격 이벤트 → 귀 아픈 소음 방지
        private const float MachineDamagedMinInterval = 0.15f;
        private float _lastMachineDamagedTime = -1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                DestroyImmediate(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _sfxPool = new AudioSource[_poolSize];
            for (int i = 0; i < _poolSize; i++)
            {
                var go = new GameObject($"SfxSource_{i}");
                go.transform.SetParent(transform);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                _sfxPool[i] = src;
            }

            // 기관총 전용 — Pool 밖에 별도 Source 하나
            var mgGo = new GameObject("MachineGunSource");
            mgGo.transform.SetParent(transform);
            _machineGunSource = mgGo.AddComponent<AudioSource>();
            _machineGunSource.playOnAwake = false;
            _machineGunSource.spatialBlend = 0f;

            // 레이저 전용 — loop 재생용
            var laserGo = new GameObject("LaserSource");
            laserGo.transform.SetParent(transform);
            _laserSource = laserGo.AddComponent<AudioSource>();
            _laserSource.playOnAwake = false;
            _laserSource.spatialBlend = 0f;
            _laserSource.loop = true;
        }

        private void OnEnable()
        {
            GameEvents.OnMachineDamaged += HandleMachineDamaged;
            GameEvents.OnBugKilled += HandleBugKilled;
        }

        private void OnDisable()
        {
            GameEvents.OnMachineDamaged -= HandleMachineDamaged;
            GameEvents.OnBugKilled -= HandleBugKilled;
        }

        // 기관총: 전용 Source + 매 발 Stop→Play로 "1발 1소리" 보장.
        public void PlayMachineGunFire()
        {
            if (!_enableMachineGunFire || _machineGunSource == null || _sfxMachineGunFire == null) return;

            _machineGunSource.Stop();
            _machineGunSource.clip = _sfxMachineGunFire;
            _machineGunSource.pitch = 1f + Random.Range(-0.08f, 0.08f);   // 단조로움 완화
            _machineGunSource.volume = _masterVolume * _sfxVolume * _volMachineGunFire;
            _machineGunSource.Play();
        }
        public void PlaySniperFire() { if (_enableSniperFire) PlayOneShot(_sfxSniperFire, _volSniperFire); }
        public void PlayBombLaunch() { if (_enableBombLaunch) PlayOneShot(_sfxBombLaunch, _volBombLaunch); }
        public void PlayBombExplosion() { if (_enableBombExplosion) PlayOneShot(_sfxBombExplosion, _volBombExplosion); }
        /// <summary>
        /// 레이저 빔 발사 시작 — 빔 생존 동안 loop 재생. 반드시 StopLaserBeam과 쌍으로 호출.
        /// </summary>
        public void StartLaserBeamLoop()
        {
            if (!_enableLaserBeam || _laserSource == null || _sfxLaserBeam == null) return;
            _laserSource.clip = _sfxLaserBeam;
            _laserSource.volume = _masterVolume * _sfxVolume * _volLaserBeam;
            _laserSource.Play();
        }

        public void StopLaserBeam()
        {
            if (_laserSource != null) _laserSource.Stop();
        }
        // 벌레 피격: 여러 마리 동시 피격 시 pitch 변주로 다양한 톤
        public void PlayBugHit() { if (_enableBugHit) PlayOneShot(_sfxBugHit, _volBugHit, 0.10f); }

        /// <summary>
        /// SFX 재생. pitchVariance > 0 이면 매 재생마다 pitch를 ±variance 랜덤 변주 → 연사/반복 시 단조로움 완화.
        /// </summary>
        public void PlayOneShot(AudioClip clip, float volumeScale = 1f, float pitchVariance = 0f)
        {
            if (clip == null || _sfxPool == null) return;

            if (clip == _lastClip && Time.unscaledTime - _lastClipTime < SameClipMinInterval)
                return;
            _lastClip = clip;
            _lastClipTime = Time.unscaledTime;

            var src = _sfxPool[_poolIndex];
            _poolIndex = (_poolIndex + 1) % _sfxPool.Length;

            // pitch 변주 (±variance). variance=0이면 1.0 고정.
            src.pitch = pitchVariance > 0f
                ? 1f + Random.Range(-pitchVariance, pitchVariance)
                : 1f;

            src.PlayOneShot(clip, _masterVolume * _sfxVolume * volumeScale);
        }

        public void SetMasterVolume(float v) => _masterVolume = Mathf.Clamp01(v);
        public void SetSfxVolume(float v) => _sfxVolume = Mathf.Clamp01(v);

        private void HandleMachineDamaged(float damage)
        {
            if (!_enableMachineDamaged) return;

            float now = Time.unscaledTime;
            if (now - _lastMachineDamagedTime < MachineDamagedMinInterval) return;
            _lastMachineDamagedTime = now;
            PlayOneShot(_sfxMachineDamaged, _volMachineDamaged);
        }

        private void HandleBugKilled(int bugId)
        {
            if (!_enableBugDeath) return;
            PlayOneShot(_sfxBugDeath, _volBugDeath);
        }
    }
}
