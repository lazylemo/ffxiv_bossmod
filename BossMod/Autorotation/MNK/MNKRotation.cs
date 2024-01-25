﻿using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using BossMod.Components;
using BossMod.Endwalker.Criterion.C03AAI.C031Ketuduke;
using Dalamud.Game.ClientState.JobGauge.Enums;
using static BossMod.CommonRotation.Strategy;

namespace BossMod.MNK
{
    public static class Rotation
    {
        public enum Form
        {
            None,
            OpoOpo,
            Raptor,
            Coeurl
        }

        // full state needed for determining next action
        public class State : CommonRotation.PlayerState
        {
            public int Chakra; // 0-5
            public BeastChakra[] BeastChakra;
            public Nadi Nadi;
            public Form Form;
            public float FormLeft; // 0 if no form, 30 max
            public float DisciplinedFistLeft; // 15 max
            public float LeadenFistLeft; // 30 max
            public float TargetDemolishLeft; // TODO: this shouldn't be here...
            public float PerfectBalanceLeft; // 20 max
            public float FormShiftLeft; // 30 max
            public float FireLeft; // 20 max
            public float TrueNorthLeft; // 10 max
            public float TTK;

            public bool HasLunar => Nadi.HasFlag(Nadi.LUNAR);
            public bool HasSolar => Nadi.HasFlag(Nadi.SOLAR);

            public int BeastCount =>
                BeastChakra.Count(
                    x => x != Dalamud.Game.ClientState.JobGauge.Enums.BeastChakra.NONE
                );

            // upgrade paths
            public AID BestForbiddenChakra =>
                Unlocked(AID.ForbiddenChakra) ? AID.ForbiddenChakra : AID.SteelPeak;
            public AID BestEnlightenment =>
                Unlocked(AID.Enlightenment) ? AID.Enlightenment : AID.HowlingFist;
            public AID BestShadowOfTheDestroyer =>
                Unlocked(AID.ShadowOfTheDestroyer)
                    ? AID.ShadowOfTheDestroyer
                    : AID.ArmOfTheDestroyer;
            public AID BestRisingPhoenix =>
                Unlocked(AID.RisingPhoenix) ? AID.RisingPhoenix : AID.FlintStrike;
            public AID BestPhantomRush =>
                Unlocked(AID.PhantomRush) ? AID.PhantomRush : AID.TornadoKick;

            public AID BestBlitz
            {
                get
                {
                    if (BeastCount != 3)
                        return AID.MasterfulBlitz;

                    if (HasLunar && HasSolar)
                        return BestPhantomRush;

                    var bc = BeastChakra;

                    if (bc[0] == bc[1] && bc[1] == bc[2])
                        return AID.ElixirField;
                    if (bc[0] != bc[1] && bc[1] != bc[2] && bc[0] != bc[2])
                        return BestRisingPhoenix;
                    return AID.CelestialRevolution;
                }
            }

            public State(float[] cooldowns)
                : base(cooldowns) { }

            public bool Unlocked(AID aid) => Definitions.Unlocked(aid, Level, UnlockProgress);

            public bool Unlocked(TraitID tid) => Definitions.Unlocked(tid, Level, UnlockProgress);

            public override string ToString()
            {
                return $"RB={RaidBuffsLeft:f1}, N={Nadi}, BC={BeastChakra}, Chakra={Chakra}, Form={Form}/{FormLeft:f1}, DFist={DisciplinedFistLeft:f1}, LFist={LeadenFistLeft:f1}, Demo={TargetDemolishLeft:f1}, PotCD={PotionCD:f1}, GCD={GCD:f3}, ALock={AnimationLock:f3}+{AnimationLockDelay:f3}, lvl={Level}/{UnlockProgress}, TTK={TTK}";
            }
        }

        // strategy configuration
        public class Strategy : CommonRotation.Strategy
        {
            public int NumPointBlankAOETargets; // range 5 around self
            public int NumEnlightenmentTargets; // range 10 width 2/4 rect

            public bool PreCombatFormShift;

            public enum DashStrategy : uint
            {
                // only use in opener
                Automatic = 0,

                [PropertyDisplay("Forbid")]
                Forbid = 1,

                [PropertyDisplay("Use if outside melee range")]
                GapClose = 2
            }

            public DashStrategy DashUse;

            public enum NadiChoice : uint
            {
                Automatic = 0, // lunar -> solar

                [PropertyDisplay("Lunar", 0xFFDB8BCA)]
                Lunar = 1,

                [PropertyDisplay("Solar", 0xFF8EE6FA)]
                Solar = 2
            }

            public enum PerfectBalanceUse : uint
            {
                Automatic = 0, // lunar -> solar

                [PropertyDisplay("Delay", 0x800000ff)]
                Delay = 1,

                [PropertyDisplay("Force use", 0x8000ff00)]
                Force = 2,

                [PropertyDisplay("Lunar PB", 0xFFDB8BCA)]
                Lunar = 3,

                [PropertyDisplay("Solar PB", 0xFF8EE6FA)]
                Solar = 4
            }

            public NadiChoice NextNadi;

            public enum FireStrategy : uint
            {
                Automatic = 0, // use on cooldown-ish if something is targetable

                [PropertyDisplay("Don't use")]
                Delay = 1,

                [PropertyDisplay("Force use")]
                Force = 2,

                [PropertyDisplay("Delay until Brotherhood is off cooldown")]
                DelayUntilBrotherhood = 3
            }

            public enum PotionUse : uint
            {
                Manual = 0, // potion won't be used automatically

                [PropertyDisplay("Use ASAP, but delay slightly during opener", 0x8000ff00)]
                Immediate = 1,

                [PropertyDisplay("Delay until raidbuffs", 0x8000ffff)]
                DelayUntilRaidBuffs = 2,

                [PropertyDisplay("Use ASAP", 0x800000ff)]
                Force = 3,
            }

            public enum DragonKickStrat : uint
            {
                Manual = 0,

                [PropertyDisplay("Dragon Kick Spam before downtime", 0x8000ff00)]
                Force = 1,
            }

            public FireStrategy FireUse;
            public OffensiveAbilityUse WindUse;
            public OffensiveAbilityUse BrotherhoodUse;
            public PerfectBalanceUse PerfectBalanceStrategy;
            public OffensiveAbilityUse SSSUse;
            public OffensiveAbilityUse TrueNorthUse;
            public PotionUse PotionStrategy;
            public DragonKickStrat DragonKickSpam;

            public override string ToString()
            {
                return $"AOE={NumPointBlankAOETargets}/{NumEnlightenmentTargets}, no-dots={ForbidDOTs}, {CombatTimer}";
            }

            public void ApplyStrategyOverrides(uint[] overrides)
            {
                if (overrides.Length >= 10)
                {
                    DashUse = (DashStrategy)overrides[0];
                    TrueNorthUse = (OffensiveAbilityUse)overrides[1];
                    NextNadi = (NadiChoice)overrides[2];
                    FireUse = (FireStrategy)overrides[3];
                    WindUse = (OffensiveAbilityUse)overrides[4];
                    BrotherhoodUse = (OffensiveAbilityUse)overrides[5];
                    PerfectBalanceStrategy = (PerfectBalanceUse)overrides[6];
                    SSSUse = (OffensiveAbilityUse)overrides[7];
                    PotionStrategy = (PotionUse)overrides[8];
                    DragonKickSpam = (DragonKickStrat)overrides[9];
                }
                else
                {
                    DashUse = DashStrategy.Automatic;
                    TrueNorthUse = OffensiveAbilityUse.Automatic;
                    NextNadi = NadiChoice.Automatic;
                    FireUse = FireStrategy.Automatic;
                    WindUse = OffensiveAbilityUse.Automatic;
                    BrotherhoodUse = OffensiveAbilityUse.Automatic;
                    PerfectBalanceStrategy = PerfectBalanceUse.Automatic;
                    SSSUse = OffensiveAbilityUse.Automatic;
                    PotionStrategy = PotionUse.Manual;
                    DragonKickSpam = DragonKickStrat.Manual;
                }
            }
        }

        public static AID GetOpoOpoFormAction(State state, int numAOETargets)
        {
            // TODO: what should we use if form is not up?..
            if (state.Unlocked(AID.ArmOfTheDestroyer) && numAOETargets >= 3)
                return state.BestShadowOfTheDestroyer;

            if (state.Unlocked(AID.DragonKick) && state.LeadenFistLeft <= state.GCD)
                return AID.DragonKick;

            return AID.Bootshine;
        }

        public static AID GetRaptorFormAction(State state, Strategy strategy, int numAOETargets)
        {
            // TODO: low level - consider early restart...
            // TODO: better threshold for buff reapplication...
            if (state.Unlocked(AID.FourPointFury) && numAOETargets >= 3)
                return AID.FourPointFury;

            if (state.Unlocked(AID.TwinSnakes) && state.DisciplinedFistLeft < 5.0f || strategy.NextNadi == Strategy.NadiChoice.Lunar && state.DisciplinedFistLeft < state.GCD + 7)
                return AID.TwinSnakes;

            if (state.PerfectBalanceLeft > state.AnimationLock && state.DisciplinedFistLeft < (15 - (state.AttackGCDTime*2)))
                return AID.TwinSnakes;

            return AID.TrueStrike;
        }

        public static AID GetCoeurlFormAction(State state, Strategy strategy, int numAOETargets, bool forbidDOTs)
        {
            // TODO: multidot support...
            // TODO: low level - consider early restart...
            // TODO: better threshold for debuff reapplication...
            if (state.Unlocked(AID.Rockbreaker) && numAOETargets >= 3)
                return AID.Rockbreaker;

            if (
                !forbidDOTs
                && state.Unlocked(AID.Demolish)
                && (state.TargetDemolishLeft < 5.0f || strategy.NextNadi == Strategy.NadiChoice.Lunar && state.TargetDemolishLeft < state.GCD + 7)
            )
                return AID.Demolish;

            return AID.SnapPunch;
        }

        public static AID GetNextComboAction(
            State state,
            Strategy strategy,
            int numAOETargets,
            bool forbidDOTs,
            Strategy.NadiChoice nextNadi
        )
        {
            var form = GetEffectiveForm(state, nextNadi);
            if (form == Form.Coeurl)
                return GetCoeurlFormAction(state, strategy, numAOETargets, forbidDOTs);

            if (form == Form.Raptor)
                return GetRaptorFormAction(state, strategy, numAOETargets);

            if (Service.Config.Get<MNKConfig>().EarlyOpener && state.DisciplinedFistLeft < state.AnimationLock && state.FormShiftLeft > state.AnimationLock)
                return AID.TwinSnakes;

            return GetOpoOpoFormAction(state, numAOETargets);
        }

        private static Form GetEffectiveForm(State state, Strategy.NadiChoice nextNadi)
        {
            if (SolarTime(state, nextNadi))
            {
                switch (state.BeastCount)
                {
                    case 2:
                        if (state.BeastChakra[0] == BeastChakra.OPOOPO)
                        {
                            if (state.BeastChakra[1] == BeastChakra.COEURL)
                            {
                                return Form.Raptor;
                            }

                            if (state.BeastChakra[1] == BeastChakra.RAPTOR)
                            {
                                return Form.Coeurl;
                            }

                            if (state.BeastChakra[1] == BeastChakra.OPOOPO)
                            {
                                return Form.OpoOpo;
                            }
                        }

                        if (state.BeastChakra[0] == BeastChakra.RAPTOR)
                        {
                            if (state.BeastChakra[1] == BeastChakra.COEURL)
                            {
                                return Form.OpoOpo;
                            }

                            if (state.BeastChakra[1] == BeastChakra.OPOOPO)
                            {
                                return Form.Coeurl;
                            }
                        }

                        if (state.BeastChakra[0] == BeastChakra.COEURL)
                        {
                            if (state.BeastChakra[1] == BeastChakra.RAPTOR)
                            {
                                return Form.OpoOpo;
                            }

                            if (state.BeastChakra[1] == BeastChakra.OPOOPO)
                            {
                                return Form.Raptor;
                            }
                        }
                        break;

                    case 1:
                        if (state.BeastChakra[0] == BeastChakra.OPOOPO && state.TargetDemolishLeft < 2.1)
                            return Form.Coeurl;
                        if (state.BeastChakra[0] == BeastChakra.OPOOPO && state.TargetDemolishLeft > 2.1)
                            return Form.Raptor;
                        if (state.BeastChakra[0] == BeastChakra.RAPTOR && state.TargetDemolishLeft > 2.1)
                            return Form.OpoOpo;
                        if (state.BeastChakra[0] == BeastChakra.RAPTOR && state.TargetDemolishLeft < 2.1)
                            return Form.Coeurl;
                        if (state.BeastChakra[0] == BeastChakra.COEURL && state.DisciplinedFistLeft > 2)
                            return Form.OpoOpo;
                        if (state.BeastChakra[0] == BeastChakra.COEURL && state.DisciplinedFistLeft < 2)
                            return Form.Raptor;
                        break;
                    default:
                        if (state.DisciplinedFistLeft > 5)
                        {
                            return Form.OpoOpo;
                        }
                        else if (state.TargetDemolishLeft < 2.1)
                        {
                            return Form.Coeurl;
                        }
                        else if (state.DisciplinedFistLeft < 5)
                        {
                            return Form.Raptor;
                        }
                        else
                        {
                            return Form.OpoOpo;
                        }
                }
            }

            return state.Form;
        }



        private static bool SolarTime(State state, Strategy.NadiChoice nextNadi)
        {
            bool recentDemo = state.TargetDemolishLeft > 12;
            bool recentTwin = state.DisciplinedFistLeft > 8;
            if (state.PerfectBalanceLeft <= state.GCD)
                return false;
             switch (nextNadi)
            {
                case Strategy.NadiChoice.Automatic:
                    if ((!recentDemo || !recentTwin) && (!state.HasSolar || (state.HasLunar && state.HasSolar)))
                        return true;
                    break;
                case Strategy.NadiChoice.Lunar:
                    if (recentTwin && recentDemo && (!state.HasLunar || (state.HasLunar && state.HasSolar)))
                        return false;
                    break;
                default:
                    if ((!recentTwin || !recentDemo) && (!state.HasLunar || (state.HasLunar && state.HasSolar)))
                        return true;
                    return false;
            };
            return false;
        }

        public static AID GetNextBestGCD(State state, Strategy strategy)
        {
            if (strategy.CombatTimer < 0)
            {
                if (state.Chakra < 5)
                    return AID.Meditation;

                if (
                    (
                        strategy.CombatTimer > -20 && state.FormShiftLeft < 5
                        || strategy.PreCombatFormShift && state.FormShiftLeft < 2
                    ) && state.Unlocked(AID.FormShift)
                )
                    return AID.FormShift;

                if (strategy.CombatTimer > -100)
                    return AID.None;
            }

            if (!state.TargetingEnemy)
            {
                if (state.Chakra < 5 && state.Unlocked(AID.Meditation))
                    return AID.Meditation;

                return AID.None;
            }

            if (state.Unlocked(AID.SixSidedStar) && strategy.SSSUse == OffensiveAbilityUse.Force)
                return AID.SixSidedStar;

            if (state.BestBlitz != AID.MasterfulBlitz && state.FireLeft >= state.GCD && state.CD(CDGroup.RiddleOfFire) > 0 && state.CD(CDGroup.Brotherhood) > 0)
                return state.BestBlitz;

            if (
                strategy.SSSUse == OffensiveAbilityUse.Automatic
                && (strategy.FightEndIn > state.GCD
                && strategy.FightEndIn < state.GCD + state.AttackGCDTime
                || (state.TTK < state.AttackGCDTime && !Service.Config.Get<MNKConfig>().IgnoreTTK))
                && state.Unlocked(AID.SixSidedStar)
            )
                return AID.SixSidedStar;

            if (strategy.DragonKickSpam == Strategy.DragonKickStrat.Force
                && (strategy.FightEndIn > state.GCD && state.DisciplinedFistLeft < state.AttackGCDTime * 1 && state.Form == Form.Raptor))
                return AID.TwinSnakes;

            if (strategy.DragonKickSpam == Strategy.DragonKickStrat.Force
                && (strategy.FightEndIn > state.GCD
                && strategy.FightEndIn < state.GCD + state.AttackGCDTime * 5) && state.DisciplinedFistLeft > state.AttackGCDTime * 1)
                return state.LeadenFistLeft > 0 ? AID.Bootshine : AID.DragonKick;

            // TODO: L52+
            return GetNextComboAction(
                state,
                strategy,
                strategy.NumPointBlankAOETargets,
                strategy.ForbidDOTs,
                strategy.NextNadi
            );
        }

        public static (Positional, bool) GetNextPositional(State state, Strategy strategy)
        {
            if (strategy.NumPointBlankAOETargets >= 3)
                return (Positional.Any, false);

            var gcdsInAdvance = GetEffectiveForm(state, strategy.NextNadi) switch
            {
                Form.Coeurl => 0,
                Form.Raptor => 1,
                _ => 2
            };
            var willDemolish =
                state.Unlocked(AID.Demolish)
                && state.TargetDemolishLeft < state.GCD + 4 + (1.94 * gcdsInAdvance);

            return (willDemolish ? Positional.Rear : Positional.Flank, gcdsInAdvance == 0);
        }

        public static ActionID GetNextBestOGCD(State state, Strategy strategy, float deadline)
        {
            // TODO: potion

            if (strategy.CombatTimer < 0 && strategy.CombatTimer > -100)
            {
                if (
                    strategy.CombatTimer > -0.2
                    && state.RangeToTarget > 3
                    && strategy.DashUse != Strategy.DashStrategy.Forbid
                )
                    return ActionID.MakeSpell(AID.Thunderclap);

                return new();
            }

            if (ShouldUsePotion(state, strategy) && state.CanWeave(state.PotionCD, 1.1f, deadline))
                return CommonDefinitions.IDPotionStr;

            if (ShouldUseRoF(state, strategy, deadline))
                return ActionID.MakeSpell(AID.RiddleOfFire);

            if (ShouldUseBrotherhood(state, strategy, deadline))
                return ActionID.MakeSpell(AID.Brotherhood);

            if (ShouldUsePB(state, strategy, deadline))
                return ActionID.MakeSpell(AID.PerfectBalance);

            if (ShouldUseRoW(state, strategy, deadline))
                return ActionID.MakeSpell(AID.RiddleOfWind);

            // 2. steel peek, if have chakra
            if (
                state.Unlocked(AID.SteelPeak)
                && state.Chakra == 5
                && state.CanWeave(CDGroup.SteelPeak, 0.5f, deadline)
                && (
                    // prevent early use in opener
                    state.CD(CDGroup.RiddleOfFire) > 0
                    || strategy.FireUse == Strategy.FireStrategy.Delay
                    || strategy.FireUse == Strategy.FireStrategy.DelayUntilBrotherhood
                )
            )
            {
                // L15 Steel Peak is 180p
                // L40 Howling Fist is 100p/target => HF at 2+ targets
                // L54 Forbidden Chakra is 340p => HF at 4+ targets
                // L72 Enlightenment is 170p/target => at 2+ targets
                if (state.Unlocked(AID.Enlightenment))
                    return ActionID.MakeSpell(
                        strategy.NumEnlightenmentTargets >= 2
                            ? AID.Enlightenment
                            : AID.ForbiddenChakra
                    );
                else if (state.Unlocked(AID.ForbiddenChakra))
                    return ActionID.MakeSpell(
                        strategy.NumEnlightenmentTargets >= 4
                            ? AID.HowlingFist
                            : AID.ForbiddenChakra
                    );
                else if (state.Unlocked(AID.HowlingFist))
                    return ActionID.MakeSpell(
                        strategy.NumEnlightenmentTargets >= 2 ? AID.HowlingFist : AID.SteelPeak
                    );
                else
                    return ActionID.MakeSpell(AID.SteelPeak);
            }

            if (
                ShouldUseTrueNorth(state, strategy)
                && state.CanWeave(state.CD(CDGroup.TrueNorth) - 45, 0.5f, deadline)
            )
                return ActionID.MakeSpell(AID.TrueNorth);

            if (ShouldDash(state, strategy, deadline))
                return ActionID.MakeSpell(AID.Thunderclap);

            // no suitable oGCDs...
            return new();
        }

        private static bool ShouldDash(State state, Strategy strategy, float deadline)
        {
            if (
                state.RangeToTarget <= 3
                || !state.CanWeave(state.CD(CDGroup.Thunderclap) - 60, 0.5f, deadline)
                || strategy.DashUse == Strategy.DashStrategy.Forbid
            )
                return false;

            if (strategy.DashUse == Strategy.DashStrategy.GapClose)
                return true;

            // someone early pulled
            if (
                strategy.DashUse == Strategy.DashStrategy.Automatic
                && strategy.CombatTimer > 0
                && strategy.CombatTimer < 3
            )
                return true;

            return false;
        }

        private static bool ShouldUseRoF(State state, Strategy strategy, float deadline)
        {
            if (
                !state.Unlocked(AID.RiddleOfFire)
                || strategy.FireUse == Strategy.FireStrategy.Delay
                || !state.CanWeave(CDGroup.RiddleOfFire, 0.5f, deadline)
            )
                return false;

            if (strategy.FireUse == Strategy.FireStrategy.Force)
                return true;

            if (state.GCD > 0.800)
                return false;

            if (strategy.CombatTimer < 30)
                // use before demolish in opener
                return state.Form == Form.Coeurl;
            else
            {
                var buffWait =
                    strategy.FireUse == Strategy.FireStrategy.Automatic
                    || state.CD(CDGroup.Brotherhood) < 4;

                // cooldown alignment for braindead looping rotation
                // TODO: implement optimal drift (it can't be that hard with math, right?)
                return (state.DisciplinedFistLeft > state.AnimationLock && buffWait) || (state.PerfectBalanceLeft > state.AnimationLock);
            }
        }

        private static bool ShouldUseRoW(State state, Strategy strategy, float deadline)
        {
            if (
                !state.Unlocked(AID.RiddleOfWind)
                || strategy.WindUse == OffensiveAbilityUse.Delay
                || !state.CanWeave(CDGroup.RiddleOfWind, 0.5f, deadline)
            )
                return false;

            if (strategy.WindUse == OffensiveAbilityUse.Force)
                return true;

            // thebalance recommends using RoW like an oGCD dot, so we use on cooldown as long as RoF has been used first
            return state.CD(CDGroup.RiddleOfFire) > 0 && state.CD(CDGroup.Brotherhood) > 0;
        }

        private static bool ShouldUseBrotherhood(State state, Strategy strategy, float deadline)
        {
            if (
                !state.Unlocked(AID.Brotherhood)
                || strategy.BrotherhoodUse == OffensiveAbilityUse.Delay
                || !state.CanWeave(CDGroup.Brotherhood, 0.5f, deadline)
            )
                return false;

            if (strategy.BrotherhoodUse == OffensiveAbilityUse.Force)
                return true;

            return strategy.NumPointBlankAOETargets == 0
                && state.FireLeft > state.GCD
                && ((state.CD(CDGroup.RiddleOfFire) < 60 - (state.AttackGCDTime + 0.5f)
                    || (state.PerfectBalanceLeft > state.AnimationLock && state.CD(CDGroup.RiddleOfFire) < 1.2)));
        }

        private static bool ShouldUsePB(State state, Strategy strategy, float deadline)
        {
            bool recentDemo = state.TargetDemolishLeft > 12;
            bool recentTwin = state.DisciplinedFistLeft > 8;
            if (
                state.PerfectBalanceLeft > 0
                || !state.Unlocked(AID.PerfectBalance)
                || !state.CanWeave(state.CD(CDGroup.PerfectBalance) - 40, 0.5f, deadline)
                || strategy.PerfectBalanceStrategy == Strategy.PerfectBalanceUse.Delay
            )
                return false;

            if (strategy.PerfectBalanceStrategy == Strategy.PerfectBalanceUse.Force)
                return true;
            if (strategy.PerfectBalanceStrategy == Strategy.PerfectBalanceUse.Lunar)
            {
                if (recentDemo && recentTwin)
                    return true;
                return false;
            }
            if (strategy.PerfectBalanceStrategy == Strategy.PerfectBalanceUse.Solar)
            {
                if (!recentDemo && !recentTwin)
                    return true;
                return false;
            }
            if (strategy.NextNadi == Strategy.NadiChoice.Lunar && state.DisciplinedFistLeft < state.GCD + 7)
                return false;
            if (strategy.NextNadi == Strategy.NadiChoice.Lunar && state.TargetDemolishLeft < state.GCD + 7)
                return false;

            if (state.HasLunar && state.HasSolar && state.DisciplinedFistLeft < state.GCD + 7 && state.CD(CDGroup.Brotherhood) > state.AnimationLock && state.Form != Form.OpoOpo)
                return false;
            if (state.HasLunar && state.HasSolar && state.TargetDemolishLeft < state.GCD + 7 && state.CD(CDGroup.Brotherhood) > state.AnimationLock && state.Form != Form.OpoOpo)
                return false;
            if (state.CD(CDGroup.RiddleOfFire) < 6.5f && recentDemo && recentTwin && state.FormShiftLeft < state.AnimationLock && state.Form != Form.OpoOpo && state.DisciplinedFistLeft > state.AnimationLock && state.TargetDemolishLeft > state.AnimationLock && state.CD(CDGroup.Brotherhood) < 15)
                return true;
            if (state.CD(CDGroup.RiddleOfFire) < 2.5f && recentDemo && recentTwin && state.FormShiftLeft < state.AnimationLock && state.DisciplinedFistLeft > state.AnimationLock && state.TargetDemolishLeft > state.AnimationLock && state.Form != Form.OpoOpo)
                return true;
            if (state.CD(CDGroup.RiddleOfFire) < 1.5f && state.TargetDemolishLeft < 6f && state.FormShiftLeft < state.AnimationLock && state.Form != Form.OpoOpo && state.DisciplinedFistLeft > state.AnimationLock && state.TargetDemolishLeft > state.AnimationLock)
                return true;

            return (state.FireLeft > state.GCD || !state.Unlocked(AID.RiddleOfFire))
                && state.FormShiftLeft < state.AnimationLock && state.Form != Form.OpoOpo;
        }

        private static bool ShouldUseTrueNorth(State state, Strategy strategy)
        {
            if (state.GCD > 0.800)
                return false;
            if (
                strategy.TrueNorthUse == OffensiveAbilityUse.Delay
                || state.TrueNorthLeft > state.AnimationLock
            )
                return false;
            if (strategy.TrueNorthUse == OffensiveAbilityUse.Force)
                return true;

            return strategy.NextPositionalImminent && !strategy.NextPositionalCorrect;
        }
        public static bool ShouldUsePotion(State state, Strategy strategy) => strategy.PotionStrategy switch
        {
            Strategy.PotionUse.Manual => false,
            Strategy.PotionUse.Immediate => state.CD(CDGroup.RiddleOfFire) < state.AttackGCDTime * 2 + 0.5 && strategy.CombatTimer > 0,
            Strategy.PotionUse.Force => true,
            _ => false
        };
    }
}