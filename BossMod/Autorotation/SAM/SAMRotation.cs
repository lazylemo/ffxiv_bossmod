
using BossMod.Network.ServerIPC;
using Dalamud.Game.ClientState.JobGauge.Enums;
using Dalamud.Game.ClientState.JobGauge.Types;
using SharpDX.Win32;
using System;
using System.Reflection.Metadata;

namespace BossMod.SAM
{
    public static class Rotation
    {
        public class State : CommonRotation.PlayerState
        {
            public float FugetsuLeft;
            public float FukaLeft;
            public float OgiNamikiriReady;
            public float MeikyoShisuiLeft;
            public float TrueNorthLeft;
            public bool Oneseal;
            public bool Twoseal;
            public bool Threeseal;
            public SAMGauge Gauge;
            public float TargetHiganbanaLeft;
            public bool HasFuka;
            public bool HasFugetsu;
            public bool InRear;
            public bool InFlank;

            public bool Filler;

            public AID BestIaijutsu => Oneseal ? AID.Higanbana
                : Twoseal ? AID.TenkaGoken
                : Threeseal ? AID.MidareSetsugekka
                : AID.Iaijutsu;
            public AID BestTsubame => Gauge.Kaeshi == Kaeshi.HIGANBANA ? AID.KaeshiHiganbana
                : Gauge.Kaeshi == Kaeshi.GOKEN ? AID.KaeshiGoken
                : Gauge.Kaeshi == Kaeshi.SETSUGEKKA ? AID.KaeshiSetsugekka
                : Gauge.Kaeshi == Kaeshi.NAMIKIRI ? AID.KaeshiNamikiri
                : AID.TsubameGaeshi;
            public SID ExpectedHiganbana => SID.Higanbana;
            public AID BestFuga => Unlocked(TraitID.FugaMastery) ? AID.Fuko : AID.Fuga;
            public AID ComboLastMove => (AID)ComboLastAction;
            public State(float[] cooldowns) : base(cooldowns) { }

            public bool Unlocked(AID aid) => Definitions.Unlocked(aid, Level, UnlockProgress);
            public bool Unlocked(TraitID tid) => Definitions.Unlocked(tid, Level, UnlockProgress);

            public override string ToString()
            {
                return $"SenCount={Gauge.Sen}, Filler={Filler}, Ikishoten={CD(CDGroup.Ikishoten)}, Meikyo={MeikyoShisuiLeft}, Kenki={Gauge.Kenki} RB={RaidBuffsLeft:f1}, Higanbana={TargetHiganbanaLeft:f1}, TsubameCD={CD(CDGroup.TsubameGaeshi)}, PotCD={PotionCD:f1}, GCD={GCD:f3}, ALock={AnimationLock:f3}+{AnimationLockDelay:f3}, lvl={Level}/{UnlockProgress}";
            }
        }

        // strategy configuration
        public class Strategy : CommonRotation.Strategy
        {
            public int NumAOEGCDTargets;
            public bool UseAOERotation;

            public override string ToString()
            {
                return $"AOE={NumAOEGCDTargets}, no-dots={ForbidDOTs}";
            }

            public enum GaugeUse : uint
            {
                Automatic = 0,

                [PropertyDisplay("Force extend Higanbana Target Debuff, potentially overcapping Higanbana", 0x80ff00ff)]
                ForceExtendDD = 1,

                [PropertyDisplay("Use Enpi if outside melee", 0x80c08000)]
                EnpiIfNotInMelee = 2,
            }

            public enum TrueNorthUse : uint
            {
                Automatic = 0,

                [PropertyDisplay("Delay", 0x800000ff)]
                Delay = 1,
                [PropertyDisplay("Force", 0x8000ff00)]
                Force = 2,
            }

            public enum PotionUse : uint
            {
                Manual = 0, // potion won't be used automatically

                [PropertyDisplay("Opener", 0x8000ff00)]
                Opener = 1,

                [PropertyDisplay("2+ minute windows", 0x8000ffff)]
                Burst = 2,

                [PropertyDisplay("Force", 0x800000ff)]
                Force = 3,
            }

            public enum SpecialAction : uint
            {
                None = 0, // don't use any special actions
            }

            public OffensiveAbilityUse HiganbanaStrategy;
            public OffensiveAbilityUse TsubameGaeshiStrategy;
            public OffensiveAbilityUse OgiNamikiriStrategy;
            public GaugeUse GaugeStrategy; // how are we supposed to handle gauge
            public PotionUse PotionStrategy; // how are we supposed to use potions
            //public OffensiveAbilityUse CommunioUse;
            public SpecialAction SpecialActionUse;
            public TrueNorthUse TrueNorthStrategy;
            public bool Aggressive;

            public void ApplyStrategyOverrides(uint[] overrides)
            {
                if (overrides.Length >= 7)
                {
                    HiganbanaStrategy = (OffensiveAbilityUse)overrides[0];
                    TsubameGaeshiStrategy = (OffensiveAbilityUse)overrides[1];
                    OgiNamikiriStrategy = (OffensiveAbilityUse)overrides[2];
                    GaugeStrategy = (GaugeUse)overrides[3];
                    TrueNorthStrategy = (TrueNorthUse)overrides[4];
                    PotionStrategy = (PotionUse)overrides[5];
                    SpecialActionUse = (SpecialAction)overrides[6];
                }
                else
                {
                    HiganbanaStrategy = OffensiveAbilityUse.Automatic;
                    TsubameGaeshiStrategy = OffensiveAbilityUse.Automatic;
                    OgiNamikiriStrategy = OffensiveAbilityUse.Automatic;
                    GaugeStrategy = GaugeUse.Automatic;
                    TrueNorthStrategy = TrueNorthUse.Automatic;
                    PotionStrategy = PotionUse.Manual;
                    SpecialActionUse = SpecialAction.None;
                }
            }
        }

        //public static int SoulGaugeGainedFromAction(State state, AID action) => action switch
        //{
        //    AID.Slice or AID.WaxingSlice or AID.InfernalSlice => 10,
        //    AID.SoulSlice => 50,
        //    AID.SoulScythe => 50,
        //    AID.SpinningScythe or AID.NightmareScythe => 10,
        //    _ => 0
        //};

        //public static int ShroudGaugeGainedFromAction(State state, AID action) => action switch
        //{
        //    AID.Gibbet or AID.Gallows or AID.Guillotine => 10,
        //    AID.PlentifulHarvest => 50,
        //    _ => 0
        //};

        //public static AID GetNextSTComboAction(AID comboLastMove, AID finisher) => comboLastMove switch
        //{
        //    AID.WaxingSlice => finisher,
        //    AID.Slice => AID.WaxingSlice,
        //    _ => AID.Slice
        //};

        //public static int GetSTComboLength(AID comboLastMove) => comboLastMove switch
        //{
        //    AID.WaxingSlice => 1,
        //    AID.Slice => 2,
        //    _ => 3
        //};

        //public static int GetAOEComboLength(AID comboLastMove) => comboLastMove == AID.SpinningScythe ? 1 : 2;

        //public static AID GetNextMaimComboAction(AID comboLastMove) => comboLastMove == AID.Slice ? AID.WaxingSlice : AID.Slice;

        //public static AID GetNextAOEComboAction(AID comboLastMove) => comboLastMove == AID.SpinningScythe ? AID.NightmareScythe : AID.SpinningScythe;

        public static AID GetNextUnlockedComboAction(State state, Strategy strategy, (Positional pos, bool imm) positional, bool aoe)
        {
            bool Fugetsubuff = state.FugetsuLeft > state.AnimationLock;
            bool Fukabuff = state.FukaLeft > state.AnimationLock;
            if (aoe && state.Unlocked(AID.Fuga))
            {
                if (state.MeikyoShisuiLeft > state.AnimationLock)
                {
                    if (!state.Gauge.HasGetsu && state.Unlocked(AID.Mangetsu))
                        return AID.Mangetsu;
                    if (!state.Gauge.HasKa && state.Unlocked(AID.Oka))
                        return AID.Oka;
                }
                if (state.ComboLastMove == state.BestFuga)
                {
                    if (!state.Gauge.HasGetsu && state.Unlocked(AID.Mangetsu))
                        return AID.Mangetsu;
                    if (!state.Gauge.HasKa && state.Unlocked(AID.Oka))
                        return AID.Oka;
                    if (state.BestIaijutsu == AID.TenkaGoken && state.Unlocked(AID.TenkaGoken))
                    {
                        return state.BestIaijutsu;
                    }
                }
                return state.BestFuga;
            }
            else
            {
                if (state.MeikyoShisuiLeft > state.AnimationLock)
                {
                    if (!state.Gauge.HasGetsu && state.HasFuka && state.HasFugetsu && positional.pos == Positional.Rear && state.Unlocked(AID.Gekko))
                        return AID.Gekko;
                    if (!state.Gauge.HasKa && state.HasFuka && state.HasFugetsu && positional.pos == Positional.Flank && state.Unlocked(AID.Kasha))
                        return AID.Kasha;
                    if ((!state.Gauge.HasGetsu && (state.HasFuka && state.HasFugetsu && strategy.NextPositionalCorrect || state.FugetsuLeft <= state.FukaLeft || !state.HasFugetsu)) && state.Unlocked(AID.Gekko))
                        return AID.Gekko;
                    if ((!state.Gauge.HasKa && (state.HasFuka && state.HasFugetsu && strategy.NextPositionalCorrect || state.FukaLeft <= state.FugetsuLeft || !state.HasFuka)) && state.Unlocked(AID.Kasha))
                        return AID.Kasha;
                    if (!state.Gauge.HasSetsu && state.Unlocked(AID.Yukikaze))
                        return AID.Yukikaze;
                }
                if (state.ComboLastMove == AID.Hakaze)
                {
                    if (!state.Gauge.HasSetsu && state.Unlocked(AID.Yukikaze) && Fukabuff && Fugetsubuff)
                        return AID.Yukikaze;
                    if ((state.FugetsuLeft <= state.FukaLeft || !state.HasFugetsu) && !state.Gauge.HasGetsu)
                    {
                        if (state.Unlocked(AID.Jinpu))
                            return AID.Jinpu;
                    }
                    if ((state.FukaLeft <= state.FugetsuLeft || !state.HasFuka) && !state.Gauge.HasKa)
                    {
                        if (state.Unlocked(AID.Shifu))
                            return AID.Shifu;
                    }
                }
                if (state.ComboLastMove == AID.Jinpu)
                    return AID.Gekko;
                if (state.ComboLastMove == AID.Shifu)
                    return AID.Kasha;
            }
            return AID.Hakaze;
        }

        //public static AID IaijutsuLogic(State state, bool aoe)
        //{
        //    if (aoe)
        //    { 
        //        if (state.Twoseal && state.Unlocked(AID.TenkaGoken))
        //            return state.BestIaijutsu;
        //    }
        //    if (!aoe)
        //    {
        //        if (state.CD(CDGroup.TsubameGaeshi) > state.GCD && state.TargetHiganbanaLeft > state.GCD && state.OgiNamikiriReady > state.AnimationLock)
        //            return AID.OgiNamikiri;
        //        if (state.Gauge.Kaeshi == Kaeshi.NAMIKIRI)
        //            return state.BestNamikiri;
        //        if (state.Threeseal)
        //            return state.BestIaijutsu;
        //        if (state.Gauge.Kaeshi == Kaeshi.SETSUGEKKA && state.CD(CDGroup.TsubameGaeshi) <= 60)
        //            return state.BestTsubame;
        //        if (state.Oneseal && state.TargetHiganbanaLeft <= state.GCD && state.CD(CDGroup.TsubameGaeshi) > 45)
        //            return state.BestIaijutsu;
        //    }

        //    return GetNextUnlockedComboAction(state, aoe);
        //}

        public static bool RefreshDOT(State state, float timeLeft) => timeLeft < state.GCD;

        public static bool ShouldUsePotion(State state, Strategy strategy) => strategy.PotionStrategy switch
        {
            Strategy.PotionUse.Manual => false,
            Strategy.PotionUse.Opener => state.FugetsuLeft > state.GCD,
            Strategy.PotionUse.Force => true,
            _ => false
        };

        public static (Positional, bool) GetNextPositional(State state, Strategy strategy, (Positional pos, bool imm) positional, bool aoe)
        {
            if (strategy.UseAOERotation)
                return default;

            if (!strategy.UseAOERotation)
            {
                if (GetNextBestGCD(state, strategy, positional, aoe) == AID.Kasha)
                    return (Positional.Flank, true);
                if (GetNextBestGCD(state, strategy, positional, aoe) == AID.Gekko)
                    return (Positional.Rear, true);

                return default;
            }
            else
            {
                return default;
            }
        }

        public static bool ShouldUseTrueNorth(State state, Strategy strategy, (Positional pos, bool imm) positional, bool aoe)
        {
            switch (strategy.TrueNorthStrategy)
            {
                case Strategy.TrueNorthUse.Delay:
                    return false;

                default:
                    if (!state.TargetingEnemy)
                        return false;
                    if (state.TrueNorthLeft > state.AnimationLock)
                        return false;
                    if (GetNextPositional(state, strategy, positional, aoe).Item2 && strategy.NextPositionalCorrect)
                        return false;
                    if (GetNextPositional(state, strategy, positional, aoe).Item2 && !strategy.NextPositionalCorrect)
                        return true;
                    return false;
            }
        }

        public static bool ShouldUseOgiNamikiri(State state, Strategy strategy, bool aoe)
        {
            bool meikyobuff = state.MeikyoShisuiLeft > state.AnimationLock;
            if (strategy.OgiNamikiriStrategy == Strategy.OffensiveAbilityUse.Delay)
                return false;
            if (strategy.OgiNamikiriStrategy == Strategy.OffensiveAbilityUse.Force)
                return true;
            if (!aoe && state.OgiNamikiriReady > state.AnimationLock && state.TargetHiganbanaLeft >= 45)
                return true;
            if (aoe && state.OgiNamikiriReady > state.AnimationLock && state.CD(CDGroup.TsubameGaeshi) > 10)
                return true;
            return false;
        }

        public static bool ShouldUseTsubameGaeshi(State state, Strategy strategy, bool aoe)
        {
            if (strategy.TsubameGaeshiStrategy == Strategy.OffensiveAbilityUse.Delay)
                return false;
            if (strategy.TsubameGaeshiStrategy == Strategy.OffensiveAbilityUse.Force)
                return true;
            if (state.Gauge.Kaeshi == Kaeshi.NAMIKIRI)
                return true;
            if (!aoe && state.Gauge.Kaeshi == Kaeshi.SETSUGEKKA && state.CD(CDGroup.TsubameGaeshi) - 60 <= state.GCD && (state.CD(CDGroup.Ikishoten) > 89 || state.CD(CDGroup.TsubameGaeshi) < state.GCD))
                return true;
            if (aoe && state.Gauge.Kaeshi == Kaeshi.GOKEN && state.CD(CDGroup.TsubameGaeshi) - 60 <= state.GCD && (state.CD(CDGroup.Ikishoten) > 89 || state.CD(CDGroup.Ikishoten) <= 0))
                return true;
            if (!aoe && state.Gauge.Kaeshi == Kaeshi.SETSUGEKKA && state.CD(CDGroup.TsubameGaeshi) <= state.GCD && state.CD(CDGroup.Ikishoten) < 65 && state.CD(CDGroup.Ikishoten) > 50)
                return true;
            return false;
        }

        public static bool ShouldUseHiganbana(State state, Strategy strategy, bool aoe)
        {
            if (strategy.HiganbanaStrategy == Strategy.OffensiveAbilityUse.Delay)
                return false;
            if (strategy.HiganbanaStrategy == Strategy.OffensiveAbilityUse.Force)
                return true;
            if (state.TargetHiganbanaLeft <= 5 && state.Oneseal && (state.CD(CDGroup.Ikishoten) > 100 || (state.CD(CDGroup.Ikishoten) < 60 && state.CD(CDGroup.Ikishoten) > 45)))
                return true;
            return false;
        }

        public static bool ShouldUseFiller(State state, Strategy strategy, bool aoe)
        {
            bool meikyobuff = state.MeikyoShisuiLeft > state.AnimationLock;
            bool inOddMinute = (int)(strategy.CombatTimer / 60) % 2 == 1 && state.Gauge.Sen == Sen.SETSU && !meikyobuff && state.TargetHiganbanaLeft >= 45;
            if (inOddMinute)
                return state.Filler = true;
            if (state.ComboLastMove == AID.Yukikaze && state.Filler)
                return true;
            return false;
        }

        public static AID GetNextBestGCD(State state, Strategy strategy, (Positional pos, bool imm) positional, bool aoe)
        {
            bool meikyobuff = state.MeikyoShisuiLeft > state.AnimationLock;
            bool Fugetsubuff = state.FugetsuLeft > state.AnimationLock;
            bool Fukabuff = state.FukaLeft > state.AnimationLock;
            if (aoe)
            {
                if (state.Twoseal && state.Unlocked(AID.TenkaGoken))
                    return state.BestIaijutsu;
            }

            if (!aoe && Fugetsubuff && Fukabuff)
            {
                if (ShouldUseOgiNamikiri(state, strategy, aoe))
                    return AID.OgiNamikiri;
                if (ShouldUseTsubameGaeshi(state, strategy, aoe))
                    return state.BestTsubame;
                if (state.Threeseal)
                    return state.BestIaijutsu;
                if (ShouldUseHiganbana(state, strategy, aoe))
                    return state.BestIaijutsu;
            }
            if (aoe && Fugetsubuff && Fukabuff)
            {
                if (ShouldUseOgiNamikiri(state, strategy, aoe))
                    return AID.OgiNamikiri;
                if (ShouldUseTsubameGaeshi(state, strategy, aoe))
                    return state.BestTsubame;
                if (state.Threeseal)
                    return state.BestIaijutsu;
            }

            return GetNextUnlockedComboAction(state, strategy, positional, aoe);
        }

        public static ActionID GetNextBestOGCD(State state, Strategy strategy, float deadline, bool aoe, (Positional pos, bool imm) positional)
        {
            bool Fugetsubuff = state.FugetsuLeft > state.AnimationLock;
            bool Fukabuff = state.FukaLeft > state.AnimationLock;
            bool raidbuffs = state.RaidBuffsLeft > state.GCD;
            bool meikyobuff = state.MeikyoShisuiLeft > state.AnimationLock;
            bool inOddMinute = state.CD(CDGroup.Ikishoten) < 60 && state.CD(CDGroup.Ikishoten) > 30 && state.Gauge.Sen == Sen.SETSU && !meikyobuff && state.TargetHiganbanaLeft >= 45;
            bool fillerdone = false;

            if (inOddMinute && fillerdone == false)
            {
                if (state.ComboLastAction == (uint)AID.Hagakure)
                {
                    fillerdone = true;
                    state.Filler = false;
                }
                if (state.CanWeave(CDGroup.Hagakure, 0.6f, deadline))
                    return ActionID.MakeSpell(AID.Hagakure);

            }
            if (ShouldUsePotion(state, strategy) && state.CanWeave(state.PotionCD, 1.1f, deadline))
                return CommonDefinitions.IDPotionStr;
            if (ShouldUseTrueNorth(state, strategy, positional, aoe) && state.CanWeave(CDGroup.TrueNorth - 45, 0.6f, deadline) && !aoe && state.GCD < 0.8)
                return ActionID.MakeSpell(AID.TrueNorth);
            if (!aoe && state.Unlocked(AID.MeikyoShisui) && !meikyobuff && state.CanWeave(state.CD(CDGroup.MeikyoShisui) - 55, 0.6f, deadline) && state.ComboTimeLeft == 0 && state.CD(CDGroup.TsubameGaeshi) > 0 && state.TargetHiganbanaLeft <= 5)
                return ActionID.MakeSpell(AID.MeikyoShisui);
            if (aoe && state.Unlocked(AID.MeikyoShisui) && !meikyobuff && state.CanWeave(state.CD(CDGroup.MeikyoShisui) - 55, 0.6f, deadline) && state.ComboTimeLeft == 0 && state.CD(CDGroup.TsubameGaeshi) > 0)
                return ActionID.MakeSpell(AID.MeikyoShisui);
            if (!aoe && state.Gauge.MeditationStacks == 3 && state.CanWeave(CDGroup.Shoha, 0.6f, deadline) && Fugetsubuff && Fukabuff && (raidbuffs || GetNextBestGCD(state, strategy, positional, aoe) == state.BestIaijutsu || GetNextBestGCD(state, strategy, positional, aoe) == AID.OgiNamikiri))
                return ActionID.MakeSpell(AID.Shoha);
            if (aoe && state.Gauge.MeditationStacks == 3 && state.CanWeave(CDGroup.ShohaII, 0.6f, deadline) && Fugetsubuff && Fukabuff && (raidbuffs || GetNextBestGCD(state, strategy, positional, aoe) == state.BestIaijutsu || GetNextBestGCD(state, strategy, positional, aoe) == AID.OgiNamikiri))
                return ActionID.MakeSpell(AID.ShohaII);
            if (!aoe && state.Unlocked(AID.HissatsuSenei) && state.CanWeave(CDGroup.HissatsuSenei, 0.6f, deadline) && state.Gauge.Kenki >= 25
                && state.Gauge.Kaeshi == Kaeshi.SETSUGEKKA && (state.CD(CDGroup.Ikishoten) <= state.AnimationLock || state.CD(CDGroup.Ikishoten) > 89 || state.CD(CDGroup.Ikishoten) < 10))
                return ActionID.MakeSpell(AID.HissatsuSenei);
            if (aoe && state.Unlocked(AID.HissatsuGuren) && state.CanWeave(CDGroup.HissatsuGuren, 0.6f, deadline) && state.Gauge.Kenki >= 25
                && state.Gauge.Kaeshi == Kaeshi.GOKEN && (state.CD(CDGroup.Ikishoten) <= state.AnimationLock || state.CD(CDGroup.Ikishoten) > 89))
                return ActionID.MakeSpell(AID.HissatsuGuren);
            if (!aoe && state.Unlocked(AID.HissatsuSenei) && state.CD(CDGroup.HissatsuSenei) > state.GCD && state.CanWeave(CDGroup.HissatsuShinten, 0.6f, deadline)
                && ((state.Gauge.Kenki >= 25 && state.CD(CDGroup.HissatsuSenei) > 20)
                || (state.Gauge.Kenki >= 75 && state.CD(CDGroup.HissatsuSenei) < 20))
                && Fugetsubuff)
                return ActionID.MakeSpell(AID.HissatsuShinten);
            if (aoe && state.Unlocked(AID.HissatsuGuren) && state.CD(CDGroup.HissatsuGuren) > state.GCD && state.CanWeave(CDGroup.HissatsuKyuten, 0.6f, deadline)
                && ((state.Gauge.Kenki >= 25 && state.CD(CDGroup.HissatsuGuren) > 20)
                || (state.Gauge.Kenki >= 75 && state.CD(CDGroup.HissatsuGuren) < 20))
                && Fugetsubuff)
                return ActionID.MakeSpell(AID.HissatsuKyuten);
            if (state.Unlocked(AID.Ikishoten) && state.CanWeave(CDGroup.Ikishoten, 0.6f, deadline) && state.Gauge.Kenki <= 50 && Fugetsubuff && Fukabuff
                && ((state.CD(CDGroup.HissatsuSenei) < state.AnimationLock && state.Gauge.Kenki < 25)
                || state.CD(CDGroup.TsubameGaeshi) > 10))
                return ActionID.MakeSpell(AID.Ikishoten);
            if (state.Unlocked(AID.HissatsuGyoten) && state.CanWeave(CDGroup.HissatsuGyoten, 0.6f, deadline) && state.Gauge.Kenki == 10 && state.CD(CDGroup.Ikishoten) > 91)
                return ActionID.MakeSpell(AID.HissatsuGyoten);

            return new();
        }
    }
}
