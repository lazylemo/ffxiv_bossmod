using Dalamud.Game.ClientState.JobGauge.Types;
using System;
using System.Linq;

namespace BossMod.DRK
{
    class Actions : TankActions
    {
        public const int AutoActionST = AutoActionFirstCustom + 0;
        public const int AutoActionAOE = AutoActionFirstCustom + 1;

        private DRKConfig _config;
        private bool _aoe;
        private Rotation.State _state;
        private Rotation.Strategy _strategy;

        public Actions(Autorotation autorot, Actor player)
            : base(autorot, player, Definitions.UnlockQuests, Definitions.SupportedActions)
        {
            _config = Service.Config.Get<DRKConfig>();
            _state = new(autorot.Cooldowns);
            _strategy = new();

            // upgrades
            SupportedSpell(AID.Grit).TransformAction = SupportedSpell(AID.ReleaseGrit).TransformAction = () => ActionID.MakeSpell(_state.HaveTankStance ? AID.ReleaseGrit : AID.Grit);
            SupportedSpell(AID.FloodOfDarkness).TransformAction = SupportedSpell(AID.FloodOfShadow).TransformAction = () => ActionID.MakeSpell(_state.BestFlood);
            SupportedSpell(AID.EdgeOfDarkness).TransformAction = SupportedSpell(AID.EdgeOfShadow).TransformAction = () => ActionID.MakeSpell(_state.BestEdge);

            SupportedSpell(AID.Reprisal).Condition = _ => Autorot.Hints.PotentialTargets.Any(e => e.Actor.Position.InCircle(Player.Position, 5 + e.Actor.HitboxRadius)); // TODO: consider checking only target?..
            SupportedSpell(AID.Interject).Condition = target => target?.CastInfo?.Interruptible ?? false;
            SupportedSpell(AID.Unmend).Condition = _ => !_config.ForbidEarlyUnmend || _strategy.CombatTimer == float.MinValue || _strategy.CombatTimer >= -0.7f;
            // TODO: SIO - check that raid is in range?..
            // TODO: Provoke - check that not already MT?
            // TODO: Shirk - check that hate is close to MT?..

            _config.Modified += OnConfigModified;
            OnConfigModified(null, EventArgs.Empty);
        }

        public override void Dispose()
        {
            _config.Modified -= OnConfigModified;
        }

        public override CommonRotation.PlayerState GetState() => _state;
        public override CommonRotation.Strategy GetStrategy() => _strategy;

        protected override void UpdateInternalState(int autoAction)
        {
            base.UpdateInternalState(autoAction);
            _aoe = autoAction switch
            {
                AutoActionST => false,
                AutoActionAOE => true, // TODO: consider making AI-like check
                AutoActionAIFight => NumTargetsHitByAOE() >= 3,
                _ => false, // irrelevant...
            };
            UpdatePlayerState();
            FillCommonStrategy(_strategy, CommonDefinitions.IDPotionStr);
            _strategy.ApplyStrategyOverrides(Autorot.Bossmods.ActiveModule?.PlanExecution?.ActiveStrategyOverrides(Autorot.Bossmods.ActiveModule.StateMachine) ?? new uint[0]);
        }

        protected override void QueueAIActions()
        {
            if (_state.Unlocked(AID.Interject))
            {
                var interruptibleEnemy = Autorot.Hints.PotentialTargets.Find(e => e.ShouldBeInterrupted && (e.Actor.CastInfo?.Interruptible ?? false) && e.Actor.Position.InCircle(Player.Position, 3 + e.Actor.HitboxRadius + Player.HitboxRadius));
                SimulateManualActionForAI(ActionID.MakeSpell(AID.Interject), interruptibleEnemy?.Actor, interruptibleEnemy != null);
            }
            if (_state.Unlocked(AID.Grit))
                SimulateManualActionForAI(ActionID.MakeSpell(AID.Grit), Player, ShouldSwapStance());
            if (_state.Unlocked(AID.Provoke))
            {
                var provokeEnemy = Autorot.Hints.PotentialTargets.Find(e => e.ShouldBeTanked && e.PreferProvoking && e.Actor.TargetID != Player.InstanceID && e.Actor.Position.InCircle(Player.Position, 25 + e.Actor.HitboxRadius + Player.HitboxRadius));
                SimulateManualActionForAI(ActionID.MakeSpell(AID.Provoke), provokeEnemy?.Actor, provokeEnemy != null);
            }
        }

        protected override NextAction CalculateAutomaticGCD()
        {
            if (Autorot.PrimaryTarget == null || AutoAction < AutoActionAIFight)
                return new();
            if (AutoAction == AutoActionAIFight && !Autorot.PrimaryTarget.Position.InCircle(Player.Position, 3 + Autorot.PrimaryTarget.HitboxRadius + Player.HitboxRadius) && _state.Unlocked(AID.Unmend))
                return MakeResult(AID.Unmend, Autorot.PrimaryTarget); // TODO: reconsider...
            var aid = Rotation.GetNextBestGCD(_state, _strategy, _aoe);
            return MakeResult(aid, Autorot.PrimaryTarget);
        }

        protected override NextAction CalculateAutomaticOGCD(float deadline)
        {
            if (AutoAction < AutoActionAIFight)
                return new();

            ActionID res = new();
            if (_state.CanWeave(deadline - _state.OGCDSlotLength)) // first ogcd slot
                res = Rotation.GetNextBestOGCD(_state, _strategy, deadline - _state.OGCDSlotLength, _aoe);
            if (!res && _state.CanWeave(deadline)) // second/only ogcd slot
                res = Rotation.GetNextBestOGCD(_state, _strategy, deadline, _aoe);
            return MakeResult(res, Autorot.PrimaryTarget);
        }

        private void UpdatePlayerState()
        {
            FillCommonPlayerState(_state);
            _state.HaveTankStance = Player.FindStatus(SID.Grit) != null;

            _state.Gauge = Service.JobGauges.Get<DRKGauge>();


            _state.BloodWeaponLeft = _state.DeliriumLeft = _state.SaltedEarthLeft = 0;
            _state.BloodWeaponStacks = _state.DeliriumStacks = 0;
            foreach (var status in Player.Statuses)
            {
                switch ((SID)status.ID)
                {
                    case SID.SaltedEarth:
                        _state.SaltedEarthLeft = StatusDuration(status.ExpireAt);
                        break;
                    case SID.BloodWeapon:
                        _state.BloodWeaponLeft = StatusDuration(status.ExpireAt);
                        _state.BloodWeaponStacks = status.Extra & 0xFF;
                        break;
                    case SID.Delirium:
                        _state.DeliriumLeft = StatusDuration(status.ExpireAt);
                        _state.DeliriumStacks = status.Extra & 0xFF;
                        break;
                }
            }
        }

        private void OnConfigModified(object? sender, EventArgs args)
        {
            // placeholders
            SupportedSpell(AID.HardSlash).PlaceholderForAuto = _config.FullRotation ? AutoActionST : AutoActionNone;
            SupportedSpell(AID.Unleash).PlaceholderForAuto = _config.FullRotation ? AutoActionAOE : AutoActionNone;

            // combo replacement
            SupportedSpell(AID.SyphonStrike).TransformAction = _config.STCombos ? () => ActionID.MakeSpell(Rotation.GetNextSTComboAction(ComboLastMove)) : null;
            SupportedSpell(AID.Souleater).TransformAction = _config.STCombos ? () => ActionID.MakeSpell(Rotation.GetNextSTComboAction(ComboLastMove)) : null;
            SupportedSpell(AID.StalwartSoul).TransformAction = _config.AOECombos ? () => ActionID.MakeSpell(Rotation.GetNextAOEComboAction(ComboLastMove)) : null;

            // smart targets
            SupportedSpell(AID.Shirk).TransformTarget = _config.SmartNascentFlashShirkTarget ? SmartTargetCoTank : null;
            SupportedSpell(AID.Provoke).TransformTarget = _config.ProvokeMouseover ? SmartTargetHostile : null; // TODO: also interject/low-blow
        }

        private AID ComboLastMove => (AID)ActionManagerEx.Instance!.ComboLastMove;

        private int NumTargetsHitByAOE() => Autorot.Hints.NumPriorityTargetsInAOECircle(Player.Position, 5);
    }
}
