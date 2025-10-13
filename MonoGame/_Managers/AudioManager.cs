using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace MonoGame
{
    public static class AudioManager
    {
        // Loaded assets (null-safe; code won’t crash if files haven’t been added yet)
        private static SoundEffect _sfxClick, _sfxPickup, _sfxA, _sfxB;
        private static Song _bgm;

        // The SFX instance we’re actively demoing (pause/volume/pitch)
        private static SoundEffectInstance _active;

        // Chain state: play B immediately after A finishes
        private static SoundEffectInstance _chainA, _chainB;
        private static bool _chainRequested, _chainBStarted;

        // Public for HUD/debug
        public static float SfxVolume => _active?.Volume ?? 0f;    // 0..1
        public static float SfxPitch  => _active?.Pitch  ?? 0f;    // -1..1
        public static bool  SfxPlaying => _active?.State == SoundState.Playing;
        public static bool  ChainActive => _chainRequested && !_chainBStarted;
        public static bool  BgmPlaying  => MediaPlayer.State == MediaState.Playing;

        public static void Init(ContentManager content)
        {
            _sfxClick  = TryLoad<SoundEffect>(content, "sfx_click");
            _sfxPickup = TryLoad<SoundEffect>(content, "sfx_pickup");
            _sfxA      = TryLoad<SoundEffect>(content, "sfx_a");
            _sfxB      = TryLoad<SoundEffect>(content, "sfx_b");
            _bgm       = TryLoad<Song>(content, "BGMToeJam");

            // Reasonable defaults
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Volume = 0.65f;
        }

        private static T TryLoad<T>(ContentManager c, string name) where T : class
        {
            try { return c.Load<T>(name); }
            catch { return null; } 
        }

        // ---- Required demos ----
        // 1) Play a sound off an event (e.g., button/menu)
        public static void PlayClick()
        {
            if (_sfxClick == null) return;
            _active = _sfxClick.CreateInstance();
            _active.Volume = 0.9f;
            _active.Pitch  = 0f;
            _active.Play();
        }

        // e.g., on item pickup
        public static void PlayPickup()
        {
            if (_sfxPickup == null) return;
            var inst = _sfxPickup.CreateInstance();
            inst.Volume = 0.9f;
            inst.Play();
            // keep _active for the “control” demo tied to click/chain
        }

        // 2) Play B as soon as A finishes
        public static void StartChain()
        {
            if (_sfxA == null || _sfxB == null) return;
            _chainA = _sfxA.CreateInstance();
            _chainB = _sfxB.CreateInstance();
            _chainRequested = true;
            _chainBStarted = false;

            // also set this active so you can pause/volume/pitch it while it plays
            _active = _chainA;
            _active.Volume = 0.9f;
            _active.Pitch = 0f;
            _chainA.Play();
        }

        public static void Update()
        {
            // Chain polling (SoundEffectInstance doesn’t raise events)
            if (_chainRequested && !_chainBStarted && _chainA != null &&
                _chainA.State == SoundState.Stopped)
            {
                _chainBStarted = true;
                _active = _chainB;           // take over “active” for controls if you want
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

        // 3) Pause/Resume the currently playing SFX
        public static void TogglePauseSfx()
        {
            if (_active == null) return;
            if (_active.State == SoundState.Playing) _active.Pause();
            else if (_active.State == SoundState.Paused) _active.Resume();
        }

        // 4) Modify volume/pitch while it’s playing
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

        // 5) Background music
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
    }
}
