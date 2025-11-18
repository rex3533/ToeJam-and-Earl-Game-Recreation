using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace MonoGame
{
    public enum ActionModeKind
    {
        Default,    // Z/Shift = Sneak (TOGGLE)
        PressFire,  // Z = Fire/Use on press
        HoldCharge  // Z = Charge while held, release triggers action
    }

    public class PlayerActions
    {
        public ActionModeKind Mode { get; private set; } = ActionModeKind.Default;

        // Default mode (toggle)
        public bool IsSneaking { get; private set; }
        public bool SneakToggledThisFrame { get; private set; }

        // Press/Fire mode
        public bool FireJustPressed { get; private set; }
        public bool FireHeld       { get; private set; }

        // Hold/Charge mode
        public bool ChargeHeld     { get; private set; }
        public float Charge01      { get; private set; }    // 0..1
        public bool ChargeReleased { get; private set; }

        // Key config
        public Keys SneakKey    = Keys.Z;          // also LeftShift in Default
        public Keys AltSneakKey = Keys.LeftShift;  // also toggles
        public Keys PrimaryKey  = Keys.Z;          // remapped by mode
        public float SpringMaxHoldSeconds = 1.25f; // max charge time

        // Internal
        private KeyboardState _prev;
        private float _chargeTimer;

        // Optional auto-revert timer for temporary modes (presents)
        private bool _hasAutoRevert;
        private double _autoRevertSeconds;

        public void SetMode(ActionModeKind mode, double autoRevertSeconds = 0)
        {
            if (Mode == mode) return;

            // leaving Hold/Charge? clear charge
            if (Mode == ActionModeKind.HoldCharge) ResetSpringCharge();

            Mode = mode;

            // optional auto-revert
            _hasAutoRevert = autoRevertSeconds > 0;
            _autoRevertSeconds = autoRevertSeconds;

            // clear one-frame flags
            FireJustPressed = false;
            ChargeReleased  = false;
            SneakToggledThisFrame = false;

            // in non-default modes, make sure sneak is off
            if (Mode != ActionModeKind.Default) IsSneaking = false;
        }

        public void RevertToDefault() => SetMode(ActionModeKind.Default, 0);

        public void Update(GameTime time) => Update((float)time.ElapsedGameTime.TotalSeconds);

        public void Update(float dt)
        {
            var k = Keyboard.GetState();

            // auto-revert countdown
            if (_hasAutoRevert)
            {
                _autoRevertSeconds -= dt;
                if (_autoRevertSeconds <= 0)
                {
                    _hasAutoRevert = false;
                    SetMode(ActionModeKind.Default, 0);
                }
            }

            // reset one-frame pulses
            FireJustPressed = false;
            ChargeReleased  = false;
            SneakToggledThisFrame = false;

            switch (Mode)
            {
                case ActionModeKind.Default:
                {
                    // TOGGLE on press (Z or LeftShift)
                    bool zNow  = k.IsKeyDown(SneakKey);
                    bool zPrev = _prev.IsKeyDown(SneakKey);
                    bool sNow  = k.IsKeyDown(AltSneakKey);
                    bool sPrev = _prev.IsKeyDown(AltSneakKey);

                    if ((zNow && !zPrev) || (sNow && !sPrev))
                    {
                        IsSneaking = !IsSneaking;
                        SneakToggledThisFrame = true;
                    }

                    FireHeld = false;
                    if (ChargeHeld) ResetSpringCharge();
                    break;
                }

                case ActionModeKind.PressFire:
                {
                    // Z becomes Fire/Use
                    bool now  = k.IsKeyDown(PrimaryKey);
                    bool prev = _prev.IsKeyDown(PrimaryKey);
                    FireHeld = now;
                    if (now && !prev) FireJustPressed = true;

                    IsSneaking = false;
                    if (ChargeHeld) ResetSpringCharge();
                    break;
                }

                case ActionModeKind.HoldCharge:
                {
                    // Z becomes chargeable action
                    bool now  = k.IsKeyDown(PrimaryKey);
                    bool prev = _prev.IsKeyDown(PrimaryKey);

                    if (now)
                    {
                        if (!prev) { ChargeHeld = true; _chargeTimer = 0f; }
                        _chargeTimer += dt;
                        Charge01 = MathHelper.Clamp(_chargeTimer / SpringMaxHoldSeconds, 0f, 1f);
                    }
                    else
                    {
                        if (prev && ChargeHeld) ChargeReleased = true;
                        ChargeHeld = false;
                        // keep Charge01 available this frame
                    }

                    IsSneaking = false;
                    FireHeld = false;
                    break;
                }
            }

            // decay charge UI when idle
            if (Mode == ActionModeKind.HoldCharge && !ChargeHeld && !ChargeReleased)
                Charge01 = System.Math.Max(0f, Charge01 - dt * 2.5f);

            _prev = k;
        }

        private void ResetSpringCharge()
        {
            ChargeHeld = false;
            Charge01 = 0f;
            _chargeTimer = 0f;
            ChargeReleased = false;
        }
    }
}
