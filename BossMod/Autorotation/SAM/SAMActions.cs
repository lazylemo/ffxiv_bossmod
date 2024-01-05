﻿using Dalamud.Game.ClientState.JobGauge.Enums;
using Dalamud.Game.ClientState.JobGauge.Types;
using System;
using System.Linq;

namespace BossMod.SAM
{
    class Actions : CommonActions
    {
        public const int AutoActionST = AutoActionFirstCustom + 0;
        public const int AutoActionAOE = AutoActionFirstCustom + 1;

        private SAMConfig _config;
        private bool _aoe;
        private Rotation.State _state;
        private Rotation.Strategy _strategy;
        private (Positional pos, bool imm) _positional;

        public Actions(Autorotation autorot, Actor player)
            : base(autorot, player, Definitions.UnlockQuests, Definitions.SupportedActions)
        {
            _config = Service.Config.Get<SAMConfig>();
            _state = new(autorot.Cooldowns);
            _strategy = new();
            
            // upgrades
            SupportedSpell(AID.Iaijutsu).TransformAction = SupportedSpell(AID.Higanbana).TransformAction = SupportedSpell(AID.TenkaGoken).TransformAction = SupportedSpell
                (AID.MidareSetsugekka).TransformAction = () => ActionID.MakeSpell(_state.BestIaijutsu);
            SupportedSpell(AID.TsubameGaeshi).TransformAction = SupportedSpell(AID.KaeshiHiganbana).TransformAction = SupportedSpell(AID.KaeshiGoken).TransformAction = SupportedSpell
                (AID.KaeshiSetsugekka).TransformAction = () => ActionID.MakeSpell(_state.BestTsubame);
            SupportedSpell(AID.Fuga).TransformAction = SupportedSpell(AID.Fuko).TransformAction = () => ActionID.MakeSpell(_state.BestFuga);
            SupportedSpell(AID.LegSweep).Condition = target => target?.CastInfo?.Interruptible ?? false;

            _config.Modified += OnConfigModified;
            OnConfigModified(null, EventArgs.Empty);
        }

        public override void Dispose()
        {
            _config.Modified -= OnConfigModified;
        }

        public override CommonRotation.PlayerState GetState() => _state;
        public override CommonRotation.Strategy GetStrategy() => _strategy;

        public override Targeting SelectBetterTarget(AIHints.Enemy initial)
        {
            // targeting for aoe
            if (_state.Unlocked(AID.Fuga))
            {
                var bestAOETarget = initial;
                var bestAOECount = NumTargetsHitByAOEGCD();
                foreach (var candidate in Autorot.Hints.PriorityTargets.Where(e => e != initial && e.Actor.Position.InCircle(Player.Position, 10)))
                {
                    var candidateAOECount = NumTargetsHitByAOEGCD();
                    if (candidateAOECount > bestAOECount)
                    {
                        bestAOETarget = candidate;
                        bestAOECount = candidateAOECount;
                    }
                }

                if (bestAOECount >= 3)
                    return new(bestAOETarget, 3);
            }
            // targeting for multidot
            var adjTarget = initial;
            if (_state.Unlocked(AID.Higanbana) && !WithoutDOT(initial.Actor))
            {
                var multidotTarget = Autorot.Hints.PriorityTargets.FirstOrDefault(e => e != initial && !e.ForbidDOTs && e.Actor.Position.InCircle(Player.Position, 5) && WithoutDOT(e.Actor));
                if (multidotTarget != null)
                    adjTarget = multidotTarget;
            }

            var pos = _strategy.NextPositionalImminent ? _strategy.NextPositional : Positional.Any; // TODO: move to common code
            return new(adjTarget, 3, pos);
        }

        protected override void UpdateInternalState(int autoAction)
        {
            _aoe = autoAction switch
            {
                AutoActionST => false,
                AutoActionAOE => true, // TODO: consider making AI-like check
                AutoActionAIFight => NumTargetsHitByAOEGCD() >= 3,
                _ => false, // irrelevant...
            };
            UpdatePlayerState();
            FillCommonStrategy(_strategy, CommonDefinitions.IDPotionStr);
            _strategy.ApplyStrategyOverrides(Autorot.Bossmods.ActiveModule?.PlanExecution?.ActiveStrategyOverrides(Autorot.Bossmods.ActiveModule.StateMachine) ?? new uint[0]);
            FillStrategyPositionals(_strategy, Rotation.GetNextPositional(_state, _strategy, _positional, _aoe), _state.TrueNorthLeft > _state.GCD);
        }

        protected override void QueueAIActions()
        {
            if (_state.Unlocked(AID.LegSweep))
            {
                var interruptibleEnemy = Autorot.Hints.PotentialTargets.Find(e => e.ShouldBeInterrupted && (e.Actor.CastInfo?.Interruptible ?? false) && e.Actor.Position.InCircle(Player.Position, 25 + e.Actor.HitboxRadius + Player.HitboxRadius));
                SimulateManualActionForAI(ActionID.MakeSpell(AID.LegSweep), interruptibleEnemy?.Actor, interruptibleEnemy != null);
            }
            if (_state.Unlocked(AID.SecondWind))
                SimulateManualActionForAI(ActionID.MakeSpell(AID.SecondWind), Player, Player.InCombat && Player.HP.Cur < Player.HP.Max * 0.5f);
            if (_state.Unlocked(AID.Bloodbath))
                SimulateManualActionForAI(ActionID.MakeSpell(AID.Bloodbath), Player, Player.InCombat && Player.HP.Cur < Player.HP.Max * 0.8f);
            // TODO: true north...
        }

        protected override NextAction CalculateAutomaticGCD()
        {
            if (Autorot.PrimaryTarget == null || AutoAction < AutoActionAIFight)
                return new();
            var aid = Rotation.GetNextBestGCD(_state, _strategy, _positional, _aoe);
            return MakeResult(aid, Autorot.PrimaryTarget);
        }

        protected override NextAction CalculateAutomaticOGCD(float deadline)
        {
            if (Autorot.PrimaryTarget == null || AutoAction < AutoActionAIFight)
                return new();

            ActionID res = new();
            if (_state.CanWeave(deadline - _state.OGCDSlotLength)) // first ogcd slot
                res = Rotation.GetNextBestOGCD(_state, _strategy, deadline - _state.OGCDSlotLength, _aoe, _positional);
            if (!res && _state.CanWeave(deadline)) // second/only ogcd slot
                res = Rotation.GetNextBestOGCD(_state, _strategy, deadline, _aoe, _positional);
            return MakeResult(res, Autorot.PrimaryTarget);
        }

        private void UpdatePlayerState()
        {
            FillCommonPlayerState(_state);
            _state.Filler = (int)(_strategy.CombatTimer / 60) % 2 == 1 && _state.Gauge.Sen == Sen.SETSU && _state.MeikyoShisuiLeft < _state.AnimationLock && _state.TargetHiganbanaLeft >= 45;
            var gauge = Service.JobGauges.Get<SAMGauge>();
            _state.Oneseal = (gauge.HasKa && !gauge.HasGetsu && !gauge.HasSetsu) 
                || (!gauge.HasKa && gauge.HasGetsu && !gauge.HasSetsu)
                || (!gauge.HasKa && !gauge.HasGetsu && gauge.HasSetsu);
            _state.Twoseal = (gauge.HasKa && gauge.HasGetsu && !gauge.HasSetsu)
                || (!gauge.HasKa && gauge.HasGetsu && gauge.HasSetsu)
                || (gauge.HasKa && !gauge.HasGetsu && gauge.HasSetsu);
            _state.Threeseal = gauge.HasSetsu && gauge.HasGetsu && gauge.HasKa;
            _state.Gauge = gauge;
            if (_state.ComboLastMove == AID.Gekko || _state.ComboLastMove == AID.Kasha || _state.ComboLastMove == AID.Yukikaze || _state.ComboLastMove == AID.Mangetsu || _state.ComboLastMove == AID.Oka)
                _state.ComboTimeLeft = 0;

            _state.HasFugetsu = Player.FindStatus(SID.Fugetsu) != null;
            _state.HasFuka = Player.FindStatus(SID.Fuka) != null;
            _state.HasMeikyoShisui = Player.FindStatus(SID.MeikyoShisui) != null;
            _state.FugetsuLeft = StatusDetails(Player, SID.Fugetsu, Player.InstanceID).Left;
            _state.FukaLeft = StatusDetails(Player, SID.Fuka, Player.InstanceID).Left;
            _state.OgiNamikiriReady = StatusDetails(Player, SID.OgiNamikiriReady, Player.InstanceID).Left;
            _state.MeikyoShisuiLeft = StatusDetails(Player, SID.MeikyoShisui, Player.InstanceID).Left;
            _state.TrueNorthLeft = StatusDetails(Player, SID.TrueNorth, Player.InstanceID).Left;

            _state.TargetHiganbanaLeft = StatusDetails(Autorot.PrimaryTarget, _state.ExpectedHiganbana, Player.InstanceID).Left;
        }

        private void OnConfigModified(object? sender, EventArgs args)
        {
            // placeholders
            SupportedSpell(AID.Hakaze).PlaceholderForAuto = _config.FullRotation ? AutoActionST : AutoActionNone;
            SupportedSpell(AID.Fuga).PlaceholderForAuto = SupportedSpell(AID.Fuko).PlaceholderForAuto = _config.FullRotation ? AutoActionAOE : AutoActionNone;

            // combo replacement
        }

        private bool WithoutDOT(Actor a) => Rotation.RefreshDOT(_state, StatusDetails(a, SID.Higanbana, Player.InstanceID).Left);
        private int NumTargetsHitByAOEGCD() => Autorot.Hints.NumPriorityTargetsInAOECircle(Player.Position, 5);
    }
}
