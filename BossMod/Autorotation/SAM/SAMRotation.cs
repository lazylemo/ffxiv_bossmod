using Dalamud.Game.ClientState.JobGauge.Enums;
using Dalamud.Game.ClientState.JobGauge.Types;

namespace BossMod.SAM
{
    public static class Rotation
    {
        internal static bool Fillerdone = false;
        public class State : CommonRotation.PlayerState
        {
            public float FugetsuLeft;
            public float FukaLeft;
            public float OgiNamikiriReady;
            public float MeikyoShisuiLeft;
            public float TrueNorthLeft;
            public SAMGauge Gauge;
            public float TargetHiganbanaLeft;
            public bool HasFuka;
            public bool HasFugetsu;
            public bool HasMeikyoShisui;
            public float GCDTime;
            public bool isMoving;
            public float TTK;
            public bool lastActionisHagakure;
            public int MeikyoShisuiStacks;

            public Positional ClosestPositional;
            public int SenCount => (Gauge.HasSetsu ? 1 : 0) + (Gauge.HasGetsu ? 1 : 0) + (Gauge.HasKa ? 1 : 0);

            public AID BestIaijutsu => SenCount == 1 ? AID.Higanbana
                : SenCount == 2 ? AID.TenkaGoken
                : SenCount == 3 ? AID.MidareSetsugekka
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
                return $"SenGauge={Gauge.Sen}, SenCount={SenCount} Moving={isMoving}, Ikishoten={CD(CDGroup.Ikishoten)}, Meikyo={MeikyoShisuiLeft}, FillerStatus={Fillerdone} Kenki={Gauge.Kenki} RB={RaidBuffsLeft:f1}, GCDTime={GCDTime} Higanbana={TargetHiganbanaLeft:f1}, TsubameCD={CD(CDGroup.TsubameGaeshi)}, PotCD={PotionCD:f1}, GCD={GCD:f3}, ALock={AnimationLock:f3}+{AnimationLockDelay:f3}, lvl={Level}/{UnlockProgress}, TTK={TTK}";
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

            public enum HiganbanaUse : uint
            {
                Automatic = 0,

                [PropertyDisplay("Delay", 0x800000ff)]
                Delay = 1,
                [PropertyDisplay("Force", 0x8000ff00)]
                Force = 2,
                [PropertyDisplay("Ignore TTK", 0x800000ff)]
                IgnoreTTK = 3,
            }

            public enum TsubameUse : uint
            {
                Automatic = 0,

                [PropertyDisplay("Delay", 0x800000ff)]
                Delay = 1,
                [PropertyDisplay("Force", 0x8000ff00)]
                Force = 2,
                [PropertyDisplay("Ignore TTK", 0x800000ff)]
                IgnoreTTK = 3,
            }

            public enum OgiUse : uint
            {
                Automatic = 0,

                [PropertyDisplay("Delay", 0x800000ff)]
                Delay = 1,
                [PropertyDisplay("Force", 0x8000ff00)]
                Force = 2,
                [PropertyDisplay("Ignore TTK", 0x800000ff)]
                IgnoreTTK = 3,
            }

            public enum PotionUse : uint
            {
                Manual = 0, // potion won't be used automatically

                [PropertyDisplay("Two Tsubame", 0x8000ff00)]
                TwoTsubame = 1,

                [PropertyDisplay("One Tsubame", 0x8000ff00)]
                OneTsubame = 2,

                [PropertyDisplay("Force", 0x800000ff)]
                Force = 3,
            }

            public enum SpecialAction : uint
            {
                None = 0, // don't use any special actions
            }

            public HiganbanaUse HiganbanaStrategy;
            public TsubameUse TsubameGaeshiStrategy;
            public OgiUse OgiNamikiriStrategy;
            public GaugeUse GaugeStrategy; // how are we supposed to handle gauge
            public PotionUse PotionStrategy; // how are we supposed to use potions
            public SpecialAction SpecialActionUse;
            public TrueNorthUse TrueNorthStrategy;
            public bool Aggressive;

            public void ApplyStrategyOverrides(uint[] overrides)
            {
                if (overrides.Length >= 7)
                {
                    HiganbanaStrategy = (HiganbanaUse)overrides[0];
                    TsubameGaeshiStrategy = (TsubameUse)overrides[1];
                    OgiNamikiriStrategy = (OgiUse)overrides[2];
                    GaugeStrategy = (GaugeUse)overrides[3];
                    TrueNorthStrategy = (TrueNorthUse)overrides[4];
                    PotionStrategy = (PotionUse)overrides[5];
                    SpecialActionUse = (SpecialAction)overrides[6];
                }
                else
                {
                    HiganbanaStrategy = HiganbanaUse.Automatic;
                    TsubameGaeshiStrategy = TsubameUse.Automatic;
                    OgiNamikiriStrategy = OgiUse.Automatic;
                    GaugeStrategy = GaugeUse.Automatic;
                    TrueNorthStrategy = TrueNorthUse.Automatic;
                    PotionStrategy = PotionUse.Manual;
                    SpecialActionUse = SpecialAction.None;
                }
            }
        }

        public static int KenkiGaugeGainedFromAction(State state, AID action) => action switch
        {
            AID.Hakaze or AID.Jinpu or AID.Shifu => 5,
            AID.Gekko or AID.Kasha => 10,
            AID.Yukikaze => 15,
            AID.Ikishoten => 50,
            AID.ThirdEye => 10,
            AID.Hagakure => state.SenCount == 1 ? 10 : state.SenCount == 2 ? 20 : state.SenCount == 3 ? 30 : 0,
            _ => 0
        };

        public static int KenkiGaugeGainedFromOGCD(State state, ActionID action)
        {
            switch (action)
            {
                case ActionID x when x.ID == (uint)AID.Ikishoten:
                    return 50;
                case ActionID x when x.ID == (uint)AID.ThirdEye:
                    return 10;
                case ActionID x when x.ID == (uint)AID.Hagakure && state.SenCount == 1:
                    return 10;
                case ActionID x when x.ID == (uint)AID.Hagakure && state.SenCount == 2:
                    return 20;
                case ActionID x when x.ID == (uint)AID.Hagakure && state.SenCount == 3:
                    return 30;
                default:
                    return 0;
            }
        }

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

        public static bool ShouldUseYukikaze(State state, Strategy strategy, bool aoe)
        {
            bool hasSetsu = state.Gauge.HasSetsu;
            bool unlockedYukikaze = state.Unlocked(AID.Yukikaze);
            bool hasFugetsu = state.FugetsuLeft > (state.GCDTime) * 4;
            bool hasFuka = state.FukaLeft > (state.GCDTime) * 4;
            bool higanbanaCondition1 = state.TargetHiganbanaLeft < state.GCD * 3 && !(state.SenCount == 1) && Service.Config.Get<SAMConfig>().TTKignoreHiganbanaOnly && strategy.HiganbanaStrategy != Strategy.HiganbanaUse.IgnoreTTK;
            bool ttkCondition = state.TTK < 45 && state.SenCount != 3 && !Service.Config.Get<SAMConfig>().TTKignore;
            bool senCountIs0 = state.SenCount == 0;
            bool senCountIsNot3 = state.SenCount != 3;
            bool senCountIs0AndHiganbanaCondition3 = state.SenCount == 0 && state.TargetHiganbanaLeft < (state.GCDTime) * 6 && state.CD(CDGroup.TsubameGaeshi) > (state.GCDTime) * 3;
            bool senCountIs1AndHiganbanaCondition4 = state.SenCount == 1 && state.TargetHiganbanaLeft < (state.GCDTime) * 3;
            bool senCountIs1AndHiganbanaCondition5 = state.SenCount == 0 && state.TargetHiganbanaLeft < (state.GCDTime) * 3;
            bool senCountIs0AndHiganbanaCondition7 = state.SenCount == 0 && state.TargetHiganbanaLeft < (state.GCDTime) * 7 && state.CD(CDGroup.TsubameGaeshi) > (state.GCDTime) * 4 && Service.Config.Get<SAMConfig>().EarlyHiganbana;
            bool senCountIs1AndHiganbanaCondition8 = state.SenCount == 1 && state.TargetHiganbanaLeft < (state.GCDTime) * 4 && Service.Config.Get<SAMConfig>().EarlyHiganbana;
            bool senCountIs1AndHiganbanaCondition9 = state.SenCount == 0 && state.TargetHiganbanaLeft < (state.GCDTime) * 4 && Service.Config.Get<SAMConfig>().EarlyHiganbana;
            bool senCountIs0AndHiganbanaCondition6 = state.SenCount == 0 && state.TargetHiganbanaLeft < (state.GCDTime) * 6 && state.CD(CDGroup.TsubameGaeshi) - 60 < (state.GCDTime) * 4 && !Service.Config.Get<SAMConfig>().Filler && state.CD(CDGroup.Ikishoten) < 15;
            bool senCountIs0AndHagakureCondition1 = state.SenCount == 0 && state.CD(CDGroup.TsubameGaeshi) < (state.GCDTime) * 10 && state.CD(CDGroup.TsubameGaeshi) > (state.GCDTime) * 8 && Fillerdone == false && Service.Config.Get<SAMConfig>().Filler;

//            if (!hasSetsu
//&& unlockedYukikaze
//&& hasFugetsu
//&& hasFuka
//&& senCountIs1AndHiganbanaCondition9)
//                return false;

            if (!hasSetsu
&& unlockedYukikaze
&& hasFugetsu
&& hasFuka
&& senCountIs1AndHiganbanaCondition9)
                return true;

            if (!hasSetsu
&& unlockedYukikaze
&& hasFugetsu
&& hasFuka
&& senCountIs1AndHiganbanaCondition5)
                return true;

            if (!hasSetsu
   && unlockedYukikaze
   && hasFugetsu
   && hasFuka
   && senCountIs0AndHiganbanaCondition6)
                return true;

            if (!hasSetsu
   && unlockedYukikaze
   && hasFugetsu
   && hasFuka
   && senCountIs0AndHiganbanaCondition7)
                return false;

            if (!hasSetsu
   && unlockedYukikaze
   && hasFugetsu
   && hasFuka
   && senCountIs1AndHiganbanaCondition8)
                return false;

            if (!hasSetsu
               && unlockedYukikaze
               && hasFugetsu
               && hasFuka
               && senCountIs0AndHiganbanaCondition3)
                return false;

            if (!hasSetsu
   && unlockedYukikaze
   && hasFugetsu
   && hasFuka
   && senCountIs1AndHiganbanaCondition4)
                return false;

            if (!hasSetsu
&& unlockedYukikaze
&& hasFugetsu
&& hasFuka
&& higanbanaCondition1)
                return true;

            if (!hasSetsu
&& unlockedYukikaze
&& hasFugetsu
&& hasFuka
&& ttkCondition)
                return true;

            //            if (!hasSetsu
            //&& unlockedYukikaze
            //&& hasFugetsu
            //&& hasFuka
            //&& senCountIsNot3)
            //                return true;

            if (!hasSetsu
&& unlockedYukikaze
&& hasFugetsu
&& hasFuka
&& state.SenCount == 2)
                return true;

            return !hasSetsu && hasFugetsu && hasFuka;
        }

        public static AID GetNextUnlockedComboAction(State state, Strategy strategy, bool aoe)
        {
            if (aoe && state.Unlocked(AID.Fuga))
            {
                if (state.MeikyoShisuiLeft > state.AnimationLock)
                {
                    if ((!state.Gauge.HasGetsu || !state.HasFugetsu) && state.Unlocked(AID.Mangetsu))
                        return AID.Mangetsu;
                    if ((!state.Gauge.HasKa || !state.HasFuka) && state.Unlocked(AID.Oka))
                        return AID.Oka;
                }
                if (state.ComboLastMove == state.BestFuga)
                {
                    if ((!state.Gauge.HasGetsu || !state.HasFugetsu) && state.Unlocked(AID.Mangetsu))
                        return AID.Mangetsu;
                    if ((!state.Gauge.HasKa || !state.HasFuka) && state.Unlocked(AID.Oka))
                        return AID.Oka;
                }
                return state.BestFuga;
            }
            else
            {
                if (state.MeikyoShisuiLeft > state.AnimationLock)
                {
                    if (!state.Gauge.HasGetsu && state.HasFuka && state.HasFugetsu && state.ClosestPositional == Positional.Rear && state.Unlocked(AID.Gekko))
                        return AID.Gekko;
                    if (!state.Gauge.HasKa && state.HasFuka && state.HasFugetsu && state.ClosestPositional == Positional.Flank && state.Unlocked(AID.Kasha))
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
                    bool hasSetsu = state.Gauge.HasSetsu;
                    bool unlockedYukikaze = state.Unlocked(AID.Yukikaze);
                    bool hasFugetsu = state.HasFugetsu;
                    bool hasFuka = state.HasFuka;
                    bool higanbanaCondition1 = state.TargetHiganbanaLeft < state.GCD * 3 && !(state.SenCount == 1) && Service.Config.Get<SAMConfig>().TTKignore && strategy.HiganbanaStrategy != Strategy.HiganbanaUse.IgnoreTTK;
                    bool higanbanaCondition2 = state.TargetHiganbanaLeft < state.GCD * 3 && (state.SenCount == 0);
                    bool ttkCondition = state.TTK < 45 && state.SenCount != 3 && !Service.Config.Get<SAMConfig>().TTKignore;
                    bool senCountIs0 = state.SenCount == 0;
                    bool senCountIsNot3 = state.SenCount != 3;
                    bool senCountIs0AndHiganbanaCondition3 = state.SenCount == 0 && state.TargetHiganbanaLeft < (state.GCDTime) * 6;
                    bool senCountIs1AndHiganbanaCondition4 = state.SenCount == 1 && state.TargetHiganbanaLeft < (state.GCDTime) * 3;
                    bool senCountIs1AndHiganbanaCondition5 = state.SenCount == 0 && state.TargetHiganbanaLeft < (state.GCDTime) * 3;

                    if (ShouldUseYukikaze(state, strategy, aoe))
                    {
                        return AID.Yukikaze;
                    }
                    if (state.FugetsuLeft <= state.FukaLeft || !state.HasFugetsu)
                    {
                        if (state.Unlocked(AID.Jinpu))
                            return AID.Jinpu;
                    }
                    if (state.FukaLeft <= state.FugetsuLeft || !state.HasFuka)
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

        public static bool RefreshDOT(State state, float timeLeft) => timeLeft < state.GCD;

        public static bool ShouldUsePotion(State state, Strategy strategy) => strategy.PotionStrategy switch
        {
            Strategy.PotionUse.Manual => false,
            Strategy.PotionUse.TwoTsubame => state.CD(CDGroup.TsubameGaeshi) < state.GCD + ((state.GCDTime) * 3) && strategy.CombatTimer > 0.6f,
            Strategy.PotionUse.OneTsubame => state.CD(CDGroup.TsubameGaeshi) - 60 < (state.GCDTime) * 3 && strategy.CombatTimer > 0f && state.CD(CDGroup.Ikishoten) < 15,
            Strategy.PotionUse.Force => true,
            _ => false
        };

        public static (Positional, bool) GetNextPositional(State state, Strategy strategy, bool aoe)
        {
            if (strategy.UseAOERotation)
                return default;

            if (!strategy.UseAOERotation)
            {
                if (GetNextBestGCD(state, strategy, aoe) == AID.Kasha)
                    return (Positional.Flank, true);
                if (GetNextBestGCD(state, strategy, aoe) == AID.Gekko)
                    return (Positional.Rear, true);

                return default;
            }
            else
            {
                return default;
            }
        }

        public static bool ShouldUseTrueNorth(State state, Strategy strategy, bool aoe)
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
                    if (GetNextPositional(state, strategy, aoe).Item2 && strategy.NextPositionalCorrect)
                        return false;
                    if (GetNextPositional(state, strategy, aoe).Item2 && !strategy.NextPositionalCorrect)
                        return true;
                    return false;
            }
        }

        public static bool ShouldUseOgiNamikiri(State state, Strategy strategy, bool aoe)
        {
            if (strategy.OgiNamikiriStrategy == Strategy.OgiUse.Delay)
                return false;
            if (strategy.OgiNamikiriStrategy == Strategy.OgiUse.Force)
                return true;
            if (state.HasFugetsu && state.HasFuka && !aoe && state.OgiNamikiriReady > state.AnimationLock && state.CD(CDGroup.TsubameGaeshi) < state.GCDTime * 2 && state.CD(CDGroup.TsubameGaeshi) - 60 > state.GCDTime && !ShouldUseSetsugekka(state,strategy,aoe) && state.CD(CDGroup.HissatsuSenei) < state.GCDTime)
                return true;
            if (state.HasFugetsu && state.HasFuka && !aoe && state.OgiNamikiriReady > state.AnimationLock && state.CD(CDGroup.TsubameGaeshi) > state.AnimationLock && state.CD(CDGroup.TsubameGaeshi) - 60 < state.GCDTime * 1 && state.SenCount != 3 && !ShouldUseSetsugekka(state, strategy, aoe) && state.CD(CDGroup.HissatsuSenei) < state.GCDTime)
                return true;
            if (state.HasFugetsu && state.HasFuka && !aoe && state.OgiNamikiriReady > state.AnimationLock && state.CD(CDGroup.TsubameGaeshi) > 10 && state.CD(CDGroup.TsubameGaeshi) < 60 && strategy.HiganbanaStrategy == Strategy.HiganbanaUse.Delay && !state.HasMeikyoShisui)
                return true;
            if (state.HasFugetsu && state.HasFuka && !aoe && state.OgiNamikiriReady > state.AnimationLock && state.TargetHiganbanaLeft >= 5 && state.CD(CDGroup.TsubameGaeshi) > 10 && state.CD(CDGroup.TsubameGaeshi) < 60 && !state.HasMeikyoShisui)
                return true;
            if (state.HasFugetsu && state.HasFuka && !aoe && state.OgiNamikiriReady > state.AnimationLock && state.CD(CDGroup.TsubameGaeshi) > 10 && state.CD(CDGroup.TsubameGaeshi) < 60 && strategy.HiganbanaStrategy == Strategy.HiganbanaUse.Delay && state.HasMeikyoShisui && state.MeikyoShisuiLeft > (state.MeikyoShisuiStacks == 1 ? state.GCDTime * 2 : state.MeikyoShisuiStacks == 2 ? state.GCDTime * 3 : state.MeikyoShisuiStacks == 3 ? state.GCDTime * 4 : 0))
                return true;
            if (state.HasFugetsu && state.HasFuka && !aoe && state.OgiNamikiriReady > state.AnimationLock && state.TargetHiganbanaLeft >= 5 && state.CD(CDGroup.TsubameGaeshi) > 10 && state.CD(CDGroup.TsubameGaeshi) < 60 && state.HasMeikyoShisui && state.MeikyoShisuiLeft > (state.MeikyoShisuiStacks == 1 ? state.GCDTime*2 : state.MeikyoShisuiStacks == 2 ? state.GCDTime*3 : state.MeikyoShisuiStacks == 3 ? state.GCDTime*4 : 0))
                return true;
            if (state.HasFugetsu && state.HasFuka && !aoe && state.OgiNamikiriReady > state.AnimationLock && !ShouldUseHiganbana(state, strategy, aoe) && state.CD(CDGroup.TsubameGaeshi) > 10 && state.CD(CDGroup.TsubameGaeshi) < 60 && state.HasMeikyoShisui && state.MeikyoShisuiLeft > (state.MeikyoShisuiStacks == 1 ? state.GCDTime * 2 : state.MeikyoShisuiStacks == 2 ? state.GCDTime * 3 : state.MeikyoShisuiStacks == 3 ? state.GCDTime * 4 : 0))
                return true;
            if (state.HasFugetsu && state.HasFuka && aoe && state.OgiNamikiriReady > state.AnimationLock && ((state.CD(CDGroup.TsubameGaeshi) > 10 && state.CD(CDGroup.TsubameGaeshi) < 60 && !state.HasMeikyoShisui) || state.OgiNamikiriReady < (state.GCDTime) * 5))
                return true;
            //XDD
            if (state.HasFugetsu && state.HasFuka && aoe && state.OgiNamikiriReady > state.AnimationLock && ((state.CD(CDGroup.TsubameGaeshi) - 60 > 10 && state.CD(CDGroup.TsubameGaeshi) > 60 && state.HasMeikyoShisui && state.MeikyoShisuiLeft > (state.MeikyoShisuiStacks == 1 ? state.GCDTime * 2 : state.MeikyoShisuiStacks == 2 ? state.GCDTime * 3 : state.MeikyoShisuiStacks == 3 ? state.GCDTime * 4 : 0)) || state.OgiNamikiriReady < (state.GCDTime) * 5))
                return true;
            if (state.HasFugetsu && state.HasFuka && !aoe && state.OgiNamikiriReady > state.AnimationLock && state.CD(CDGroup.TsubameGaeshi) - 60 > 10 && state.CD(CDGroup.TsubameGaeshi) > 60 && strategy.HiganbanaStrategy == Strategy.HiganbanaUse.Delay && !state.HasMeikyoShisui)
                return true;
            if (state.HasFugetsu && state.HasFuka && !aoe && state.OgiNamikiriReady > state.AnimationLock && state.TargetHiganbanaLeft >= 5 && state.CD(CDGroup.TsubameGaeshi) - 60 > 10 && state.CD(CDGroup.TsubameGaeshi) > 60 && !state.HasMeikyoShisui)
                return true;
            if (state.HasFugetsu && state.HasFuka && !aoe && state.OgiNamikiriReady > state.AnimationLock && state.CD(CDGroup.TsubameGaeshi) - 60 > 10 && state.CD(CDGroup.TsubameGaeshi) > 60 && strategy.HiganbanaStrategy == Strategy.HiganbanaUse.Delay && state.HasMeikyoShisui && state.MeikyoShisuiLeft > (state.MeikyoShisuiStacks == 1 ? state.GCDTime * 2 : state.MeikyoShisuiStacks == 2 ? state.GCDTime * 3 : state.MeikyoShisuiStacks == 3 ? state.GCDTime * 4 : 0))
                return true;
            if (state.HasFugetsu && state.HasFuka && !aoe && state.OgiNamikiriReady > state.AnimationLock && state.TargetHiganbanaLeft >= 5 && state.CD(CDGroup.TsubameGaeshi) - 60 > 10 && state.CD(CDGroup.TsubameGaeshi) > 60 && state.HasMeikyoShisui && state.MeikyoShisuiLeft > (state.MeikyoShisuiStacks == 1 ? state.GCDTime * 2 : state.MeikyoShisuiStacks == 2 ? state.GCDTime * 3 : state.MeikyoShisuiStacks == 3 ? state.GCDTime * 4 : 0))
                return true;
            if (state.HasFugetsu && state.HasFuka && !aoe && state.OgiNamikiriReady > state.AnimationLock && !ShouldUseHiganbana(state, strategy, aoe) && state.CD(CDGroup.TsubameGaeshi) - 60 > 10 && state.CD(CDGroup.TsubameGaeshi) > 60 && state.HasMeikyoShisui && state.MeikyoShisuiLeft > (state.MeikyoShisuiStacks == 1 ? state.GCDTime * 2 : state.MeikyoShisuiStacks == 2 ? state.GCDTime * 3 : state.MeikyoShisuiStacks == 3 ? state.GCDTime * 4 : 0))
                return true;
            if (state.HasFugetsu && state.HasFuka && aoe && state.OgiNamikiriReady > state.AnimationLock && ((state.CD(CDGroup.TsubameGaeshi) - 60 > 10 && state.CD(CDGroup.TsubameGaeshi) > 60 && !state.HasMeikyoShisui) || state.OgiNamikiriReady < (state.GCDTime) * 5))
                return true;
            if (state.HasFugetsu && state.HasFuka && aoe && state.OgiNamikiriReady > state.AnimationLock && ((state.CD(CDGroup.TsubameGaeshi) - 60 > 10 && state.CD(CDGroup.TsubameGaeshi) > 60 && state.HasMeikyoShisui && state.MeikyoShisuiLeft > (state.MeikyoShisuiStacks == 1 ? state.GCDTime * 2 : state.MeikyoShisuiStacks == 2 ? state.GCDTime * 3 : state.MeikyoShisuiStacks == 3 ? state.GCDTime * 4 : 0)) || state.OgiNamikiriReady < (state.GCDTime) * 5))
                return true;
            return false;
        }

        public static bool ShouldUseTsubameGaeshi(State state, Strategy strategy, bool aoe)
        {
            if (strategy.TsubameGaeshiStrategy == Strategy.TsubameUse.Delay)
                return false;
            if (strategy.TsubameGaeshiStrategy == Strategy.TsubameUse.Force)
                return true;
            if (state.Gauge.Kaeshi == Kaeshi.NAMIKIRI)
                return true;
            if (state.HasFugetsu && state.HasFuka && !aoe && state.Gauge.Kaeshi == Kaeshi.SETSUGEKKA && state.CD(CDGroup.TsubameGaeshi) <= state.GCD && (state.CD(CDGroup.Ikishoten) > 65 || state.CD(CDGroup.Ikishoten) < 15))
                return true;
            if (state.HasFugetsu && state.HasFuka && !aoe && state.Gauge.Kaeshi == Kaeshi.SETSUGEKKA && state.CD(CDGroup.TsubameGaeshi) - 60 <= state.GCD && (state.CD(CDGroup.Ikishoten) > 70 && (state.CD(CDGroup.TsubameGaeshi) - 60 < (state.GCDTime) * 2)))
                return true;
            if (state.HasFugetsu && state.HasFuka && !aoe && state.Gauge.Kaeshi == Kaeshi.SETSUGEKKA && state.CD(CDGroup.TsubameGaeshi) - 60 <= state.GCD && state.TTK < 20 && strategy.TsubameGaeshiStrategy != Strategy.TsubameUse.IgnoreTTK && !Service.Config.Get<SAMConfig>().TTKignore)
                return true;
            if (state.HasFugetsu && state.HasFuka && !aoe && state.Gauge.Kaeshi == Kaeshi.SETSUGEKKA && state.CD(CDGroup.TsubameGaeshi) <= state.GCD && state.CD(CDGroup.Ikishoten) < 65 && state.CD(CDGroup.Ikishoten) > 50 && Service.Config.Get<SAMConfig>().Filler)
                return true;
            if (state.HasFugetsu && state.HasFuka && !aoe && state.Gauge.Kaeshi == Kaeshi.SETSUGEKKA 
                && state.CD(CDGroup.TsubameGaeshi) - 60 <= state.GCD 
                && strategy.PotionStrategy != Strategy.PotionUse.TwoTsubame 
                && ((state.TargetHiganbanaLeft > (state.GCDTime) * 2) 
                || (state.CD(CDGroup.Ikishoten) < 15 || state.CD(CDGroup.Ikishoten) > 100) || (state.CD(CDGroup.HissatsuSenei) < 15 || state.CD(CDGroup.HissatsuSenei) > 100))
                && !Service.Config.Get<SAMConfig>().Filler)
                return true;
            if (state.HasFugetsu && state.HasFuka && aoe && state.Gauge.Kaeshi == Kaeshi.GOKEN && state.CD(CDGroup.TsubameGaeshi) - 60 <= state.GCD && (state.CD(CDGroup.Ikishoten) > 65 || state.CD(CDGroup.Ikishoten) <= 0))
                return true;
            return false;
        }

        public static bool ShouldUseHiganbana(State state, Strategy strategy, bool aoe)
        {
            if (strategy.HiganbanaStrategy == Strategy.HiganbanaUse.Delay)
                return false;
            if (strategy.HiganbanaStrategy == Strategy.HiganbanaUse.Force)
                return true;
            if (!aoe)
            {
                if ((state.TargetHiganbanaLeft <= (state.GCDTime) * 2 || state.TargetHiganbanaLeft <= (state.GCDTime) * 2 && GetNextUnlockedComboAction(state, strategy, aoe) == AID.Yukikaze || state.TargetHiganbanaLeft <= (state.GCDTime) * 2 && GetNextUnlockedComboAction(state, strategy, aoe) == AID.Gekko || state.TargetHiganbanaLeft <= (state.GCDTime) * 2 && GetNextUnlockedComboAction(state, strategy, aoe) == AID.Kasha) && Service.Config.Get<SAMConfig>().TTKignoreHiganbanaOnly && Service.Config.Get<SAMConfig>().EarlyHiganbana)
                    return true;
                if ((state.TargetHiganbanaLeft <= (state.GCDTime) * 2 || state.TargetHiganbanaLeft <= (state.GCDTime) * 2 && GetNextUnlockedComboAction(state, strategy, aoe) == AID.Yukikaze || state.TargetHiganbanaLeft <= (state.GCDTime) * 2 && GetNextUnlockedComboAction(state, strategy, aoe) == AID.Gekko || state.TargetHiganbanaLeft <= (state.GCDTime) * 2 && GetNextUnlockedComboAction(state, strategy, aoe) == AID.Kasha) && state.TTK > 45 && !Service.Config.Get<SAMConfig>().TTKignoreHiganbanaOnly && Service.Config.Get<SAMConfig>().EarlyHiganbana)
                    return true;
                if ((state.TargetHiganbanaLeft <= (state.GCDTime) * 2 || state.TargetHiganbanaLeft <= (state.GCDTime) * 2 && GetNextUnlockedComboAction(state, strategy, aoe) == AID.Yukikaze || state.TargetHiganbanaLeft <= (state.GCDTime) * 2 && GetNextUnlockedComboAction(state, strategy, aoe) == AID.Gekko || state.TargetHiganbanaLeft <= (state.GCDTime) * 2 && GetNextUnlockedComboAction(state, strategy, aoe) == AID.Kasha) && state.HasFugetsu && state.HasFuka && state.CD(CDGroup.TsubameGaeshi) > 10 && state.TTK > 45 && strategy.HiganbanaStrategy != Strategy.HiganbanaUse.IgnoreTTK && !Service.Config.Get<SAMConfig>().TTKignoreHiganbanaOnly && !Service.Config.Get<SAMConfig>().EarlyHiganbana)
                    return true;
                if ((state.TargetHiganbanaLeft <= (state.GCDTime) * 2 || state.TargetHiganbanaLeft <= (state.GCDTime) * 2 && GetNextUnlockedComboAction(state, strategy, aoe) == AID.Yukikaze || state.TargetHiganbanaLeft <= (state.GCDTime) * 2 && GetNextUnlockedComboAction(state, strategy, aoe) == AID.Gekko || state.TargetHiganbanaLeft <= (state.GCDTime) * 2 && GetNextUnlockedComboAction(state, strategy, aoe) == AID.Kasha) && state.HasFugetsu && state.HasFuka && state.CD(CDGroup.TsubameGaeshi) > 10 && Service.Config.Get<SAMConfig>().TTKignoreHiganbanaOnly && !Service.Config.Get<SAMConfig>().EarlyHiganbana)
                    return true;
                if (state.HasFugetsu && state.HasFuka && strategy.HiganbanaStrategy != Strategy.HiganbanaUse.IgnoreTTK && !Service.Config.Get<SAMConfig>().TTKignoreHiganbanaOnly && state.TTK < 20 && state.CD(CDGroup.TsubameGaeshi) > 80 && state.OgiNamikiriReady < state.AnimationLock && state.Gauge.MeditationStacks == 2)
                    return true;
            }
            return false;
        }

        public static bool ShouldUseSetsugekka(State state, Strategy strategy, bool aoe)
        {
            if (state.SenCount == 3 && state.HasMeikyoShisui)
                return true;
            if (state.HasFugetsu && state.HasFuka && state.CD(CDGroup.TsubameGaeshi) < (state.GCDTime * 2))
                return true;
            if (Service.Config.Get<SAMConfig>().EarlyHiganbana && state.HasFugetsu && state.HasFuka && state.TargetHiganbanaLeft < state.GCDTime * 4)
                return true;
            if (state.HasFugetsu && state.HasFuka && state.CD(CDGroup.Ikishoten) < 15 && state.CD(CDGroup.TsubameGaeshi) - 60 < (state.GCDTime) * 4 && ((state.ComboLastMove == AID.Jinpu || state.ComboLastMove == AID.Shifu) || state.CD(CDGroup.TsubameGaeshi) < (state.GCDTime) * 2) && Service.Config.Get<SAMConfig>().Filler)
                return true;
            if (state.HasFugetsu && state.HasFuka && state.CD(CDGroup.Ikishoten) < 15 && state.CD(CDGroup.TsubameGaeshi) - 60 < (state.GCDTime) * 4 && ((state.ComboLastMove == AID.Jinpu || state.ComboLastMove == AID.Shifu) || state.CD(CDGroup.TsubameGaeshi) - 60 < (state.GCDTime) * 1) && !Service.Config.Get<SAMConfig>().Filler)
                return true;
            if (Service.Config.Get<SAMConfig>().Filler && state.HasFugetsu && state.HasFuka
                && ((state.CD(CDGroup.TsubameGaeshi) > (state.GCDTime) * 4 && !state.isMoving)
                || (state.CD(CDGroup.Ikishoten) > 65 && state.CD(CDGroup.TsubameGaeshi) - 60 <= state.GCD)
                || (state.CD(CDGroup.TsubameGaeshi) > (state.GCDTime) * 4 && state.TargetHiganbanaLeft <= (state.GCDTime) * 4)
                || (state.CD(CDGroup.TsubameGaeshi) > (state.GCDTime) * 4 && state.isMoving && (GetNextUnlockedComboAction(state, strategy, aoe) == AID.Yukikaze || GetNextUnlockedComboAction(state, strategy, aoe) == AID.Gekko || GetNextUnlockedComboAction(state, strategy, aoe) == AID.Kasha || state.HasMeikyoShisui))))
                return true;
            if (!Service.Config.Get<SAMConfig>().Filler && state.HasFugetsu && state.HasFuka
                && ((state.CD(CDGroup.TsubameGaeshi) - 60 > (state.GCDTime) * 4 && !state.isMoving && state.CD(CDGroup.Ikishoten) < 15)
                || (state.CD(CDGroup.TsubameGaeshi) > (state.GCDTime) * 4 && !state.isMoving && state.CD(CDGroup.Ikishoten) > 15)
                || (state.CD(CDGroup.Ikishoten) > 65 && state.CD(CDGroup.TsubameGaeshi) - 60 <= state.GCD)
                || (state.CD(CDGroup.TsubameGaeshi) - 60 > (state.GCDTime) * 4 && state.TargetHiganbanaLeft <= (state.GCDTime) * 4)
                || (state.CD(CDGroup.TsubameGaeshi) - 60 > (state.GCDTime) * 4 && state.isMoving && (GetNextUnlockedComboAction(state, strategy, aoe) == AID.Yukikaze || GetNextUnlockedComboAction(state, strategy, aoe) == AID.Gekko || GetNextUnlockedComboAction(state, strategy, aoe) == AID.Kasha || state.HasMeikyoShisui))))
                return true;
            return false;
        }

        public static bool ShouldUseTenkaGoken(State state, Strategy strategy, bool aoe)
        {
            if (aoe)
            {
                if (state.HasFugetsu && state.HasFuka && state.SenCount == 2 && state.Unlocked(AID.TenkaGoken))
                    return true;
            }
            return false;
        }

        public static bool ShouldUseMeikyoShisui(State state, Strategy strategy, bool aoe)
        {
            if (!aoe && state.Unlocked(AID.MeikyoShisui) && !state.HasMeikyoShisui && state.ComboTimeLeft == 0 && state.RangeToTarget < 5)
            {
                if (state.CD(CDGroup.TsubameGaeshi) - 60 < (state.GCDTime) * 8 && state.SenCount == 2 && !Service.Config.Get<SAMConfig>().Filler && state.CD(CDGroup.HissatsuSenei) < 20 && state.CD(CDGroup.MeikyoShisui) < 20)
                    return true;

                if (state.TargetHiganbanaLeft <= (state.GCDTime) * 2 && state.SenCount == 0 && ShouldUseHiganbana(state, strategy, aoe))
                    return true;

                if (state.TargetHiganbanaLeft <= (state.GCDTime) * 5 && state.TargetHiganbanaLeft > (state.GCDTime) * 2 && state.SenCount == 1 && ShouldUseHiganbana(state, strategy, aoe))
                    return true;

                if (state.TargetHiganbanaLeft <= (state.GCDTime) * 4 && state.SenCount == 2 && ShouldUseHiganbana(state, strategy, aoe))
                    return true;

                if (state.TargetHiganbanaLeft > 50 && state.CD(CDGroup.Ikishoten) < 65 && state.CD(CDGroup.Ikishoten) > 50 && state.Gauge.HasSetsu)
                    return true;

                if (state.CD(CDGroup.MeikyoShisui) <= (state.GCDTime) && state.CD(CDGroup.TsubameGaeshi) > (state.GCDTime) * 4 && state.CD(CDGroup.Ikishoten) < 119.9f && state.CD(CDGroup.Ikishoten) > 100 && state.SenCount == 0 && Service.Config.Get<SAMConfig>().Filler)
                    return true;

                if (state.CD(CDGroup.MeikyoShisui) <= (state.GCDTime) && (state.Gauge.HasSetsu) && !Service.Config.Get<SAMConfig>().Filler)
                    return true;

                if (state.CD(CDGroup.TsubameGaeshi) < (state.GCDTime) * (state.SenCount == 2 ? 2 : state.SenCount == 1 ? 3 : 4) && (state.SenCount == 1 || state.SenCount == 2) && state.SenCount != 3)
                    return true;

                //if (state.CD(CDGroup.TsubameGaeshi) - 60 < (state.GCDTime) * (state.SenCount == 2 ? 2 : state.SenCount == 1 ? 3 : 4) && (state.SenCount == 1 || state.SenCount == 2) && state.SenCount != 3 && !Service.Config.Get<SAMConfig>().Filler && state.CD(CDGroup.Ikishoten) < 20 && strategy.PotionStrategy != Strategy.PotionUse.TwoTsubame)
                //    return true;

                if (state.CD(CDGroup.TsubameGaeshi) - 60 < (state.GCDTime) * (state.SenCount == 2 ? 2 : state.SenCount == 1 ? 3 : 4) && (state.SenCount == 1 || state.SenCount == 2) && state.SenCount != 3 && !Service.Config.Get<SAMConfig>().Filler && state.CD(CDGroup.Ikishoten) < 90 && state.CD(CDGroup.Ikishoten) > 20 && state.TargetHiganbanaLeft > 19 && state.Gauge.HasSetsu && strategy.PotionStrategy != Strategy.PotionUse.TwoTsubame)
                    return true;

                if ((state.CD(CDGroup.TsubameGaeshi) < 120 - (state.GCDTime) * 4 && state.CD(CDGroup.TsubameGaeshi) > 0)  && state.Gauge.HasSetsu && state.CD(CDGroup.Ikishoten) < 119.9f && state.CD(CDGroup.Ikishoten) > 100f && state.Gauge.HasSetsu && (state.SenCount == 1 || state.SenCount == 2))
                    return true;

                //if (state.CD(CDGroup.TsubameGaeshi) - 60 < state.GCD && state.CD(CDGroup.Ikishoten) > 95 && state.SenCount == 0 && !Service.Config.Get<SAMConfig>().Filler)
                //return true;

                if (state.TTK < 20 && !Service.Config.Get<SAMConfig>().TTKignore)
                    return true;
            }

            if (aoe && state.Unlocked(AID.MeikyoShisui) && !state.HasMeikyoShisui && state.ComboTimeLeft == 0 && state.CD(CDGroup.TsubameGaeshi) > 55)
                return true;

            return false;
        }


        public static bool ShouldUseShintenOrKyuten(State state, Strategy strategy, bool aoe)
        {
            bool isUnlockedHissatsuSenei = state.Unlocked(AID.HissatsuSenei);
            bool useKenkiToUseIkishoten = state.Gauge.Kenki >= 50;
            bool isIkishotenReady = state.CD(CDGroup.Ikishoten) < (state.GCDTime) * 2;
            bool isHissatsuSeneiReady = state.CD(CDGroup.HissatsuSenei) < 100;
            bool isKenkiGaugeFull = state.Gauge.Kenki + KenkiGaugeGainedFromAction(state, GetNextBestGCD(state, strategy, aoe)) > 90;
            bool isKenki25OrMore = state.Gauge.Kenki >= 25;
            bool isRaidBuffsLeft = state.RaidBuffsLeft > state.AnimationLock;
            bool isHissatsuSeneiInRange = state.CD(CDGroup.HissatsuSenei) < 119.9f && state.CD(CDGroup.HissatsuSenei) > 20;
            bool isTTKLessThan30 = state.TTK < 20;
            bool isTTKIgnore = !Service.Config.Get<SAMConfig>().TTKignore;
            bool hasFugetsu = state.HasFugetsu;

            bool isUnlockedHissatsuGuren = state.Unlocked(AID.HissatsuGuren);
            bool isHissatsuGurenReady = state.CD(CDGroup.HissatsuGuren) < 100;
            bool isHissatsuGurenInRange = state.CD(CDGroup.HissatsuGuren) < 119.9f && state.CD(CDGroup.HissatsuGuren) > 20;

            // Replace these conditions with your actual logic
            bool shouldUseShinten = !aoe && isUnlockedHissatsuSenei &&
                ((useKenkiToUseIkishoten && isIkishotenReady) ||
                (isKenkiGaugeFull && isHissatsuSeneiReady) ||
                (isKenki25OrMore && state.CD(CDGroup.HissatsuSenei) > 100) ||
                (isKenki25OrMore && isRaidBuffsLeft && isHissatsuSeneiInRange) ||
                (state.CD(CDGroup.HissatsuSenei) < state.TTK && useKenkiToUseIkishoten && isTTKLessThan30 && isTTKIgnore) ||
                (state.CD(CDGroup.HissatsuSenei) > state.TTK && isKenki25OrMore && isTTKLessThan30 && isTTKIgnore)) &&
                hasFugetsu;

            bool shouldUseKyuten = aoe && isUnlockedHissatsuGuren &&
                ((useKenkiToUseIkishoten && isIkishotenReady) ||
                (isKenkiGaugeFull && isHissatsuGurenReady) ||
                (isKenki25OrMore && state.CD(CDGroup.HissatsuSenei) > 100) ||
                (isKenki25OrMore && isRaidBuffsLeft && isHissatsuGurenInRange) ||
                (state.CD(CDGroup.HissatsuSenei) < state.TTK && useKenkiToUseIkishoten && isTTKLessThan30 && isTTKIgnore) ||
                (state.CD(CDGroup.HissatsuSenei) > state.TTK && isKenki25OrMore && isTTKLessThan30 && isTTKIgnore)) &&
                hasFugetsu;

            return aoe ? shouldUseKyuten : shouldUseShinten;
        }


        public static AID GetNextBestGCD(State state, Strategy strategy, bool aoe)
        {
            bool meikyobuff = state.MeikyoShisuiLeft > state.AnimationLock;
            bool Fugetsubuff = state.FugetsuLeft > state.AnimationLock;
            bool Fukabuff = state.FukaLeft > state.AnimationLock;
            if (strategy.CombatTimer > -100 && strategy.CombatTimer < -0.7f)
                return AID.None;

            if (state.Gauge.Kaeshi == Kaeshi.NAMIKIRI)
                return state.BestTsubame;

            if (state.Gauge.Kaeshi == Kaeshi.SETSUGEKKA && state.CD(CDGroup.TsubameGaeshi) < 0.6)
                return state.BestTsubame;

            if (state.SenCount == 3)
            {
                if (state.ComboLastMove == AID.Jinpu)
                    return state.BestIaijutsu;
                if (state.ComboLastMove == AID.Shifu)
                    return state.BestIaijutsu;
                if (state.HasMeikyoShisui)
                    return state.BestIaijutsu;
            }

            if (ShouldUseTsubameGaeshi(state, strategy, aoe))
                return state.BestTsubame;
            if (ShouldUseOgiNamikiri(state, strategy, aoe))
                return AID.OgiNamikiri;
            if (ShouldUseSetsugekka(state, strategy, aoe) && state.SenCount == 3)
            {
                if (state.SenCount == 3 && state.CD(CDGroup.TsubameGaeshi) < state.GCDTime * 3 && state.CD(CDGroup.TsubameGaeshi) > state.GCDTime * 1 && !state.HasMeikyoShisui)
                {
                    // Delay Setsugekka using Hakaze > Jinpu or Hakaze > Shifu
                    if (state.ComboLastMove == AID.Hakaze)
                    {
                        if (state.Unlocked(AID.Jinpu))
                        {
                            return AID.Jinpu;
                        }

                        if (state.Unlocked(AID.Shifu))
                        {
                            return AID.Shifu;
                        }
                    }
                }

                if (state.SenCount == 3 && state.CD(CDGroup.TsubameGaeshi) - 60 < state.GCDTime * 3 && state.CD(CDGroup.TsubameGaeshi) - 60 > state.GCDTime * 1 && !state.HasMeikyoShisui && state.CD(CDGroup.HissatsuSenei) < 10 && (!Service.Config.Get<SAMConfig>().Filler || strategy.PotionStrategy != Strategy.PotionUse.TwoTsubame))
                {
                    // Delay Setsugekka using Hakaze > Jinpu or Hakaze > Shifu
                    if (state.ComboLastMove == AID.Hakaze)
                    {
                        if (state.Unlocked(AID.Jinpu))
                        {
                            return AID.Jinpu;
                        }

                        if (state.Unlocked(AID.Shifu))
                        {
                            return AID.Shifu;
                        }
                    }
                }
                if (state.HasFugetsu && state.HasFuka 
                    && state.CD(CDGroup.Ikishoten) < 15 
                    && state.SenCount == 3 
                    && state.CD(CDGroup.TsubameGaeshi) < (state.GCDTime) * 3 
                    && state.CD(CDGroup.TsubameGaeshi) > (state.GCDTime) * 1 
                    && !state.HasMeikyoShisui 
                    && (GetNextUnlockedComboAction(state, strategy, aoe) != AID.Gekko || GetNextUnlockedComboAction(state, strategy, aoe) != AID.Kasha))
                    return GetNextUnlockedComboAction(state, strategy, aoe);
                return state.BestIaijutsu;
            }
            if (ShouldUseHiganbana(state, strategy, aoe) && state.SenCount == 1)
            {
                if (state.TargetHiganbanaLeft < (state.GCDTime) * 2 && Service.Config.Get<SAMConfig>().EarlyHiganbana)
                    return state.BestIaijutsu;
                if (state.SenCount == 1 && state.TargetHiganbanaLeft < (state.GCDTime) * 1 && (GetNextUnlockedComboAction(state, strategy, aoe) != AID.Gekko || GetNextUnlockedComboAction(state, strategy, aoe) != AID.Kasha || GetNextUnlockedComboAction(state, strategy, aoe) != AID.Yukikaze))
                    return state.BestIaijutsu;
                if (state.SenCount == 1 && !state.HasMeikyoShisui && state.TargetHiganbanaLeft < (state.GCDTime) * 2 && (GetNextUnlockedComboAction(state, strategy, aoe) != AID.Gekko || GetNextUnlockedComboAction(state, strategy, aoe) != AID.Kasha || GetNextUnlockedComboAction(state, strategy, aoe) != AID.Yukikaze))
                    return GetNextUnlockedComboAction(state, strategy, aoe);
                if (state.ComboTimeLeft > 0 && state.TargetHiganbanaLeft < (state.GCDTime) * 2 && (GetNextUnlockedComboAction(state, strategy, aoe) != AID.Gekko || GetNextUnlockedComboAction(state, strategy, aoe) != AID.Kasha || GetNextUnlockedComboAction(state, strategy, aoe) != AID.Yukikaze) && !state.HasMeikyoShisui)
                    return state.BestIaijutsu;
                return state.BestIaijutsu;
            }
            if (ShouldUseTenkaGoken(state, strategy, aoe) && state.SenCount == 2)
                return state.BestIaijutsu;

            return GetNextUnlockedComboAction(state, strategy, aoe);
        }

        public static ActionID GetNextBestOGCD(State state, Strategy strategy, float deadline, bool aoe)
        {
            bool raidbuffs = state.RaidBuffsLeft > state.GCD;
            bool inOddMinute = state.CD(CDGroup.Ikishoten) < 60 && state.CD(CDGroup.Ikishoten) > 30;

            if (strategy.CombatTimer > -100 && strategy.CombatTimer < -0.7f)
            {
                if (strategy.CombatTimer > -9 && !state.HasMeikyoShisui && !Service.Config.Get<SAMConfig>().EarlyHiganbana)
                    return ActionID.MakeSpell(AID.MeikyoShisui);
                if (strategy.CombatTimer > -6.5 && !state.HasMeikyoShisui && Service.Config.Get<SAMConfig>().EarlyHiganbana)
                    return ActionID.MakeSpell(AID.MeikyoShisui);
                if (strategy.CombatTimer > -4 && state.TrueNorthLeft == 0)
                    return ActionID.MakeSpell(AID.TrueNorth);
            }
            if (ShouldUsePotion(state, strategy) && state.CanWeave(state.PotionCD, 1.1f, deadline))
                return CommonDefinitions.IDPotionStr;
            if (ShouldUseTrueNorth(state, strategy, aoe) && state.CanWeave(CDGroup.TrueNorth - 45, 0.6f, deadline) && !aoe && state.GCD < 0.8 && strategy.CombatTimer > -0.7f)
                return ActionID.MakeSpell(AID.TrueNorth);

            if (Service.Config.Get<SAMConfig>().Filler && !aoe && !Service.Config.Get<SAMConfig>().EarlyHiganbana)
            {
                if (state.CanWeave(CDGroup.Hagakure, 0.6f, deadline) && state.CD(CDGroup.Ikishoten) < 60 && state.SenCount == 1 && state.Gauge.HasSetsu && !state.HasMeikyoShisui && Fillerdone == false && state.TTK > 45 && !Service.Config.Get<SAMConfig>().TTKignore && state.CD(CDGroup.TsubameGaeshi) < (state.GCDTime) * 13 && state.CD(CDGroup.TsubameGaeshi) > (state.GCDTime) * 8)
                    return ActionID.MakeSpell(AID.Hagakure);
                if (state.CanWeave(CDGroup.Hagakure, 0.6f, deadline) && state.CD(CDGroup.Ikishoten) < 60 && state.SenCount == 1 && !state.HasMeikyoShisui && Fillerdone == false && state.TTK > 45 && !Service.Config.Get<SAMConfig>().TTKignore && state.CD(CDGroup.TsubameGaeshi) < (state.GCDTime) * 10 && state.CD(CDGroup.TsubameGaeshi) > (state.GCDTime) * 8)
                    return ActionID.MakeSpell(AID.Hagakure);
                if (state.CanWeave(CDGroup.Hagakure, 0.6f, deadline) && state.CD(CDGroup.Ikishoten) < 60 && state.SenCount == 1 && !state.HasMeikyoShisui && Fillerdone == false && Service.Config.Get<SAMConfig>().TTKignore && state.CD(CDGroup.TsubameGaeshi) < (state.GCDTime) * 10 && state.CD(CDGroup.TsubameGaeshi) > (state.GCDTime) * 8)
                    return ActionID.MakeSpell(AID.Hagakure);
                if (state.lastActionisHagakure)
                    Fillerdone = true;
                if (state.CD(CDGroup.Ikishoten) > 65)
                    Fillerdone = false;
            }
            //if (state.CanWeave(CDGroup.Hagakure, 0.6f, deadline) && state.CD(CDGroup.Ikishoten) < 60 && state.SenCount == 1 && state.Gauge.HasSetsu && !state.HasMeikyoShisui && Fillerdone == false && Service.Config.Get<SAMConfig>().TTKignore && state.CD(CDGroup.TsubameGaeshi) - 60 < (state.GCDTime) * 10 && state.CD(CDGroup.TsubameGaeshi) - 60 > (state.GCDTime) * 9 && !Service.Config.Get<SAMConfig>().Filler)
            //    return ActionID.MakeSpell(AID.Hagakure);

            //if (state.CanWeave(CDGroup.Hagakure, 0.6f, deadline) && state.CD(CDGroup.Ikishoten) < 60 && state.SenCount == 1 && (state.Gauge.HasGetsu || state.Gauge.HasKa) && !state.HasMeikyoShisui && Fillerdone == false && Service.Config.Get<SAMConfig>().TTKignore && state.CD(CDGroup.TsubameGaeshi) - 60 < (state.GCDTime) * 9 && state.CD(CDGroup.TsubameGaeshi) - 60 > (state.GCDTime) * 8 && !Service.Config.Get<SAMConfig>().Filler)
            //    return ActionID.MakeSpell(AID.Hagakure);

            //if (state.CanWeave(CDGroup.Hagakure, 0.6f, deadline) && state.CD(CDGroup.Ikishoten) < 60 && state.SenCount == 1 && !state.HasMeikyoShisui && Fillerdone == false && Service.Config.Get<SAMConfig>().TTKignore && state.CD(CDGroup.TsubameGaeshi) - 60 < (state.GCDTime) * 6 && state.CD(CDGroup.TsubameGaeshi) - 60 > (state.GCDTime) * 5 && !Service.Config.Get<SAMConfig>().Filler && state.HasMeikyoShisui && state.MeikyoShisuiStacks == 2)
            //    return ActionID.MakeSpell(AID.Hagakure);

            //if (state.TargetHiganbanaLeft <= (state.GCDTime) * 3 && state.SenCount == 1 && state.SenCount != 3 && !state.HasMeikyoShisui && (GetNextBestGCD(state, strategy, aoe) != AID.Gekko || GetNextBestGCD(state, strategy, aoe) != AID.Kasha || GetNextBestGCD(state, strategy, aoe) != AID.Yukikaze) && state.CanWeave(CDGroup.Hagakure, 0.6f, deadline) && ShouldUseHiganbana(state, strategy, aoe) && !aoe && state.CD(CDGroup.Ikishoten) < 100 && state.CD(CDGroup.Ikishoten) > 20)
            //    return ActionID.MakeSpell(AID.Hagakure);
            if (!aoe && state.Unlocked(AID.HissatsuSenei) && state.CanWeave(CDGroup.HissatsuSenei, 0.6f, deadline) && state.Gauge.Kenki >= 25 && state.HasFugetsu && state.HasFuka
                && (((state.Gauge.Kaeshi == Kaeshi.SETSUGEKKA || state.Gauge.Kaeshi == Kaeshi.NAMIKIRI) || (state.CD(CDGroup.TsubameGaeshi) < 60 && state.CD(CDGroup.TsubameGaeshi) > 40) || (state.CD(CDGroup.TsubameGaeshi) - 60 < 60 && state.CD(CDGroup.TsubameGaeshi) - 60 > 40))
                && (state.CD(CDGroup.Ikishoten) <= state.AnimationLock || state.CD(CDGroup.Ikishoten) > 89 || state.CD(CDGroup.Ikishoten) < 10)
                || state.TTK < 20 && !Service.Config.Get<SAMConfig>().TTKignore))
                return ActionID.MakeSpell(AID.HissatsuSenei);
            if (aoe && state.Unlocked(AID.HissatsuGuren) && state.CanWeave(CDGroup.HissatsuGuren, 0.6f, deadline) && state.Gauge.Kenki >= 25 && state.HasFugetsu && state.HasFuka
                && (state.Gauge.Kaeshi == Kaeshi.GOKEN || (state.CD(CDGroup.TsubameGaeshi) < 60 && state.CD(CDGroup.TsubameGaeshi) > 40)) && (state.CD(CDGroup.Ikishoten) <= state.AnimationLock || state.CD(CDGroup.Ikishoten) > 89 || state.CD(CDGroup.Ikishoten) < 10))
                return ActionID.MakeSpell(AID.HissatsuGuren);
            if (ShouldUseMeikyoShisui(state, strategy, aoe) && state.CanWeave(state.CD(CDGroup.MeikyoShisui) - 55, 0.6f, deadline))
                return ActionID.MakeSpell(AID.MeikyoShisui);
            if (!aoe && state.Gauge.MeditationStacks == 3 && state.CanWeave(CDGroup.Shoha, 0.6f, deadline) && state.HasFugetsu && state.HasFuka && (raidbuffs || GetNextBestGCD(state, strategy, aoe) == state.BestIaijutsu || GetNextBestGCD(state, strategy, aoe) == AID.OgiNamikiri || state.TTK < 20 && !Service.Config.Get<SAMConfig>().TTKignore))
                return ActionID.MakeSpell(AID.Shoha);
            if (aoe && state.Gauge.MeditationStacks == 3 && state.CanWeave(CDGroup.ShohaII, 0.6f, deadline) && state.HasFugetsu && state.HasFuka && (raidbuffs || GetNextBestGCD(state, strategy, aoe) == state.BestIaijutsu || GetNextBestGCD(state, strategy, aoe) == AID.OgiNamikiri || state.TTK < 20 && !Service.Config.Get<SAMConfig>().TTKignore))
                return ActionID.MakeSpell(AID.ShohaII);
            if (state.Unlocked(AID.Ikishoten) && state.CanWeave(CDGroup.Ikishoten, 0.6f, deadline) && state.Gauge.Kenki <= 50 && state.HasFugetsu && state.HasFuka
                && ((state.CD(CDGroup.HissatsuSenei) < (state.GCDTime) && state.Gauge.Kenki < 25)
                || (state.CD(CDGroup.TsubameGaeshi) > 10 && Service.Config.Get<SAMConfig>().Filler)
                || (state.CD(CDGroup.TsubameGaeshi) - 60 > 10 && !Service.Config.Get<SAMConfig>().Filler)
                || state.CD(CDGroup.HissatsuSenei) > 20
                || (Service.Config.Get<SAMConfig>().EarlyHiganbana && state.CD(CDGroup.HissatsuSenei) < state.GCDTime *2)
                || (state.TTK < 20 && !Service.Config.Get<SAMConfig>().TTKignore)))
                return ActionID.MakeSpell(AID.Ikishoten);
            if (ShouldUseShintenOrKyuten(state, strategy, aoe) && state.CanWeave(CDGroup.HissatsuShinten, 0.6f, deadline))
                return aoe ? ActionID.MakeSpell(AID.HissatsuKyuten) : ActionID.MakeSpell(AID.HissatsuShinten);
            if ((state.Unlocked(AID.HissatsuGyoten) && state.CanWeave(CDGroup.HissatsuGyoten, 0.6f, deadline) && state.Gauge.Kenki == 10 && state.CD(CDGroup.Ikishoten) > 91 && state.RangeToTarget < 3 && state.OgiNamikiriReady <= state.AnimationLock && strategy.CombatTimer < 30) || (state.Unlocked(AID.HissatsuGyoten) && state.CanWeave(CDGroup.HissatsuGyoten, 0.6f, deadline) && state.Gauge.Kenki == 10 && state.CD(CDGroup.Ikishoten) > 97 && state.RangeToTarget < 3 && state.OgiNamikiriReady <= state.AnimationLock) && !(strategy.PositionLockIn <= state.AnimationLock))
                return ActionID.MakeSpell(AID.HissatsuGyoten);

            return new();
        }
    }
}