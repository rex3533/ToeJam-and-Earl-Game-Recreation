using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace MonoGame
{
    public static class AudioManager
    {
        // ---- Loaded assets ----
        private static SoundEffect _sfxClick, _sfxPickup, _sfxA, _sfxB;
        private static SoundEffect _sfxHurt;         // ToeJam hurt SFX
        private static SoundEffect _sfxWakeup1;      // quiet "wakeup"
        private static SoundEffect _sfxWakeup2;      // loud  "WAKEUP!"
        private static Song _bgm;
        private static SoundEffect _sfxTomatoLaunch;
        private static SoundEffect _sfxSmoosh;

        // Active instance for instance-level controls (volume/pitch/pause)
        private static SoundEffectInstance _active;

        // Chain state: play B immediately after A finishes
        private static SoundEffectInstance _chainA, _chainB;
        private static bool _chainRequested, _chainBStarted;

        // ---- Public status for HUD/debug ----
        public static float SfxVolume    => _active?.Volume ?? 0f;             // per-instance 0..1
        public static float SfxPitch     => _active?.Pitch  ?? 0f;             // per-instance -1..1
        public static bool  SfxPlaying   => _active?.State == SoundState.Playing;
        public static bool  ChainActive  => _chainRequested && !_chainBStarted;
        public static bool  BgmPlaying   => MediaPlayer.State == MediaState.Playing;
        public static float MasterSfxVolume => SoundEffect.MasterVolume;       // global 0..1
        public static float BgmVolume       => MediaPlayer.Volume;             // global 0..1

        // Remember if BGM was playing when game pause began (so we only resume if appropriate)
        private static bool _bgmWasPlayingBeforePause = false;

        public static void Init(ContentManager content)
        {
            // Common SFX used elsewhere in project
            _sfxClick   = TryLoad<SoundEffect>(content, "sfx_click");
            _sfxPickup  = TryLoad<SoundEffect>(content, "sfx_pickup");
            _sfxA       = TryLoad<SoundEffect>(content, "sfx_a");
            _sfxB = TryLoad<SoundEffect>(content, "sfx_b");
            _sfxTomatoLaunch = content.Load<SoundEffect>("TomatoLaunch"); // TomatoLaunch.wav in the MGCB
            _sfxSmoosh       = content.Load<SoundEffect>("Smoosh");       // Smoosh.wav in the MGCB


            // New SFX for Assignment 4 demos
            _sfxHurt    = TryLoad<SoundEffect>(content, "Hurt_ToeJam") ?? TryLoad<SoundEffect>(content, "hurt_toejam");
            _sfxWakeup1 = TryLoad<SoundEffect>(content, "WakeUp");           // wakeup.wav
            _sfxWakeup2 = TryLoad<SoundEffect>(content, "WakeUp_Shout");     // WAKEUP!.wav -> asset name without '!'

            // BGM (accept either asset name)
            _bgm = TryLoad<Song>(content, "bgm_toejam") ?? TryLoad<Song>(content, "BGMToeJam");

            // Reasonable defaults
            SoundEffect.MasterVolume = 0.90f;   // affects all SoundEffects globally
            MediaPlayer.IsRepeating  = true;
            MediaPlayer.Volume       = 0.65f;
        }

        private static T TryLoad<T>(ContentManager c, string name) where T : class
        {
            try { return c.Load<T>(name); }
            catch { return null; } // allow running without assets so you can wire screenshots
        }

        // ----------------- Required demos -----------------

        //Event sound for “hurt” (ToeJam colliding with enemy)
        public static void PlayHurt()
        {
            if (_sfxHurt == null) return;
            var inst = _sfxHurt.CreateInstance();
            inst.Volume = 0.9f;
            inst.Play();
        }

        // Another simple event SFX (pickup) *not used yet*
        public static void PlayPickup()
        {
            if (_sfxPickup == null) return;
            var inst = _sfxPickup.CreateInstance();
            inst.Volume = 0.9f;
            inst.Play();
        }

        // #2: A sound plays as soon as another finishes (generic A->B)
        public static void StartChain()
        {
            if (_sfxA == null || _sfxB == null) return;
            _chainA = _sfxA.CreateInstance();
            _chainB = _sfxB.CreateInstance();
            _chainRequested = true;
            _chainBStarted  = false;

            _active = _chainA; // let + / - control instance volume live
            _active.Volume = 0.9f;
            _active.Pitch  = 0f;
            _chainA.Play();
        }

        // Specific chain for #2, idea: wakeup -> WAKEUP!
        public static void StartChainWakeup()
        {
            if (_sfxWakeup1 == null || _sfxWakeup2 == null) return;

            _chainA = _sfxWakeup1.CreateInstance();
            _chainB = _sfxWakeup2.CreateInstance();
            _chainRequested = true;
            _chainBStarted = false;

            _active = _chainA;
            _active.Volume = 0.9f;
            _active.Pitch = 0f;
            _chainA.Play();
        }
        
        public static void PlayTomatoLaunch(float volume = 1f, float pitch = 0f)
        {
            _sfxTomatoLaunch?.Play(
                MathHelper.Clamp(MasterSfxVolume * volume, 0f, 1f),
                pitch,
                0f
            );
        }

        public static void PlaySmoosh(float volume = 1f)
        {
            _sfxSmoosh?.Play(
                MathHelper.Clamp(MasterSfxVolume * volume, 0f, 1f),
                0f,
                0f
            );
        }


        public static void Update()
        {
            // Chain polling (SoundEffectInstance doesn’t raise events)
            if (_chainRequested && !_chainBStarted && _chainA != null && _chainA.State == SoundState.Stopped)
            {
                _chainBStarted = true;
                _active = _chainB; // instance controls now apply to B
                _active.Volume = 0.9f;
                _active.Pitch  = 0f;
                _chainB.Play();
            }

            if (_chainBStarted && _chainB != null && _chainB.State == SoundState.Stopped)
            {
                _chainRequested = false;
                _chainA = _chainB = null;
            }
        }

        // Pause/Resume the currently playing SFX instance (for the instance-control demo)
        public static void TogglePauseSfx()
        {
            if (_active == null) return;
            if (_active.State == SoundState.Playing) _active.Pause();
            else if (_active.State == SoundState.Paused) _active.Resume();
        }

        // Instance-level volume/pitch on the active SFX
        public static void NudgeVolume(float delta)
        {
            if (_active == null) return;
            _active.Volume = MathHelper.Clamp(_active.Volume + delta, 0f, 1f);
        }

        public static void NudgePitch(float delta)
        {
            if (_active == null) return;
            _active.Pitch = MathHelper.Clamp(_active.Pitch + delta, -1f, 1f);
        }

        // Global volume for ALL sounds (SFX master + BGM)
        public static void NudgeGlobalVolume(float delta)
        {
            SoundEffect.MasterVolume = MathHelper.Clamp(SoundEffect.MasterVolume + delta, 0f, 1f);
            MediaPlayer.Volume       = MathHelper.Clamp(MediaPlayer.Volume       + delta, 0f, 1f);
        }

        // ---- Background music (Song) ----
        public static void StartBgm()
        {
            if (_bgm == null) return;
            if (MediaPlayer.State != MediaState.Playing)
                MediaPlayer.Play(_bgm);
        }

        public static void ToggleBgmPause()
        {
            if (_bgm == null) return;
            if (MediaPlayer.State == MediaState.Playing) MediaPlayer.Pause();
            else if (MediaPlayer.State == MediaState.Paused) MediaPlayer.Resume();
            else MediaPlayer.Play(_bgm);
        }

        public static void PauseBgm()
        {
            _bgmWasPlayingBeforePause = (MediaPlayer.State == MediaState.Playing);
            if (_bgm != null && MediaPlayer.State == MediaState.Playing)
                MediaPlayer.Pause();
        }

        public static void ResumeBgm()
        {
            if (!_bgmWasPlayingBeforePause) return;
            if (_bgm != null && MediaPlayer.State == MediaState.Paused)
                MediaPlayer.Resume();
        }
    }
}
