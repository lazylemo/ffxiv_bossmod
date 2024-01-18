using Dalamud.Game.ClientState.JobGauge.Types;
using System;

namespace BossMod.DRK
{
    public static class Rotation
    {
        // full state needed for determining next action
        public class State : CommonRotation.PlayerState
        {
            public DRKGauge Gauge; // 0 to 100
            public float BloodWeaponLeft; // 0 if buff not up, max 60
            public float DeliriumLeft; // 0 if buff not up, max 30
            public float SaltedEarthLeft;
            public int BloodWeaponStacks; // 0 if buff not up, max 30
            public int DeliriumStacks; // 0 if buff not up, max 3


            // upgrade paths
            public AID BestFlood => Unlocked(AID.FloodOfShadow) ? AID.FloodOfShadow : AID.FloodOfDarkness;
            public AID BestEdge => Unlocked(AID.EdgeOfShadow) ? AID.EdgeOfShadow : AID.EdgeOfDarkness;
            public AID ComboLastMove => (AID)ComboLastAction;
            //public float InnerReleaseCD => CD(UnlockedInnerRelease ? CDGroup.InnerRelease : CDGroup.Berserk); // note: technically berserk and IR don't share CD, and with level sync you can have both...

            public State(float[] cooldowns) : base(cooldowns) { }

            public bool Unlocked(AID aid) => Definitions.Unlocked(aid, Level, UnlockProgress);
            public bool Unlocked(TraitID tid) => Definitions.Unlocked(tid, Level, UnlockProgress);

            public override string ToString()
            {
                return $"g={Gauge}, RB={RaidBuffsLeft:f1}, BW={BloodWeaponStacks}/{BloodWeaponLeft:f1}, DL={DeliriumStacks}/{DeliriumLeft:f1}, BWCD={CD(CDGroup.BloodWeapon):f1}, DLCD={CD(CDGroup.Delirium):f1}, PlunCD={CD(CDGroup.Plunge):f1}, PotCD={PotionCD:f1}, GCD={GCD:f3}, ALock={AnimationLock:f3}+{AnimationLockDelay:f3}, lvl={Level}/{UnlockProgress}";
            }
        }

        // strategy configuration
        public class Strategy : CommonRotation.Strategy
        {
            public enum GaugeUse : uint
            {
                Automatic = 0, // spend gauge either under raid buffs or if next downtime is soon (so that next raid buff window won't cover at least 4 GCDs)

                [PropertyDisplay("Spend gauge freely", 0x8000ff00)]
                Spend = 1, // spend all gauge, don't bother conserving - but still ensure that ST is properly maintained

                [PropertyDisplay("Conserve unless under raid buffs", 0x8000ffff)]
                ConserveIfNoBuffs = 2, // spend under raid buffs, conserve otherwise (even if downtime is imminent)

                [PropertyDisplay("Conserve as much as possible", 0x800000ff)]
                Conserve = 3, // conserve even if under raid buffs (useful if heavy vuln phase is imminent)

                [PropertyDisplay("Force extend ST buff, potentially overcapping gauge and/or ST", 0x80ff00ff)]
                ForceExtendST = 4, // force combo to extend buff (useful before downtime of medium length)

                [PropertyDisplay("Force SP combo, potentially overcapping gauge", 0x80ff0080)]
                ForceSPCombo = 5, // force SP combo (useful to get max gauge, if ST extension is not needed)

                [PropertyDisplay("Use tomahawk if outside melee", 0x80c08000)]
                TomahawkIfNotInMelee = 6,

                [PropertyDisplay("Use combo, unless it can't be finished before downtime and unless gauge and/or ST would overcap", 0x80c0c000)]
                ComboFitBeforeDowntime = 7, // useful on late phases before downtime

                [PropertyDisplay("Use combo until second-last step, then spend gauge", 0x80400080)]
                PenultimateComboThenSpend = 8, // useful for ensuring ST extension is used right before long downtime

                [PropertyDisplay("Force gauge spender if possible, even if ST is not up/running out soon", 0x8000ffc0)]
                ForceSpend = 9, // useful right after downtime
            }

            public enum PotionUse : uint
            {
                Manual = 0, // potion won't be used automatically

                [PropertyDisplay("Use ASAP, but delay slightly during opener", 0x8000ff00)]
                Immediate = 1,

                [PropertyDisplay("Delay until raidbuffs", 0x8000ffff)]
                DelayUntilRaidBuffs = 2,

                [PropertyDisplay("Use ASAP, even if without ST", 0x800000ff)]
                Force = 3,
            }

            public enum PlungeUse : uint
            {
                Automatic = 0, // always keep one charge reserved, use other charges under raidbuffs or prevent overcapping

                [PropertyDisplay("Forbid automatic use", 0x800000ff)]
                Forbid = 1, // forbid until window end

                [PropertyDisplay("Do not reserve charges: use all charges if under raidbuffs, otherwise use as needed to prevent overcapping", 0x8000ffff)]
                NoReserve = 2, // automatic logic, except without reserved charge

                [PropertyDisplay("Use all charges ASAP", 0x8000ff00)]
                Force = 3, // use all charges immediately, don't wait for raidbuffs

                [PropertyDisplay("Use all charges except one ASAP", 0x80ff0000)]
                ForceReserve = 4, // if 2+ charges, use immediately

                [PropertyDisplay("Reserve 1 charges, trying to prevent overcap", 0x80ffff00)]
                ReserveOne = 5, // use only if about to overcap

                [PropertyDisplay("Use as gapcloser if outside melee range", 0x80ff00ff)]
                UseOutsideMelee = 6, // use immediately if outside melee range
            }

            public enum SpecialAction : uint
            {
                None = 0, // don't use any special actions

                [PropertyDisplay("LB3", 0x8000ff00)]
                LB3, // use LB3 if available
            }

            public GaugeUse GaugeStrategy; // how are we supposed to handle gauge
            public PotionUse PotionStrategy; // how are we supposed to use potions
            public OffensiveAbilityUse DeliriumUse; // how are we supposed to use IR
            public OffensiveAbilityUse BloodWeaponUse; // how are we supposed to use IR
            public OffensiveAbilityUse CarveAndSpitUse; // how are we supposed to use IR
            public OffensiveAbilityUse ShadowBringerUse; // how are we supposed to use IR
            public OffensiveAbilityUse SaltedEarthUse; // how are we supposed to use IR
            public OffensiveAbilityUse LivingShadowUse; // how are we supposed to use IR
            public OffensiveAbilityUse EdgeUse; // how are we supposed to use IR
            public PlungeUse PlungeStrategy; // how are we supposed to use onslaught
            public SpecialAction SpecialActionUse; // any special actions we want to use
            public bool Aggressive; // if true, we use buffs and stuff at last possible moment; otherwise we make sure to keep at least 1 GCD safety net
            public bool OnslaughtHeadroom; // if true, consider onslaught to have slightly higher animation lock than in reality, to account for potential small movement animation delay

            public override string ToString()
            {
                return $"";
            }

            // TODO: these bindings should be done by the framework...
            public void ApplyStrategyOverrides(uint[] overrides)
            {
                if (overrides.Length >= 10)
                {
                    GaugeStrategy = (GaugeUse)overrides[0];
                    PotionStrategy = (PotionUse)overrides[1];
                    DeliriumUse = (OffensiveAbilityUse)overrides[2];
                    CarveAndSpitUse = (OffensiveAbilityUse)overrides[3];
                    ShadowBringerUse = (OffensiveAbilityUse)overrides[4];
                    SaltedEarthUse = (OffensiveAbilityUse)overrides[5];
                    LivingShadowUse = (OffensiveAbilityUse)overrides[6];
                    EdgeUse = (OffensiveAbilityUse)overrides[7];
                    PlungeStrategy = (PlungeUse)overrides[8];
                    SpecialActionUse = (SpecialAction)overrides[9];
                }
                else
                {
                    GaugeStrategy = GaugeUse.Automatic;
                    PotionStrategy = PotionUse.Manual;
                    DeliriumUse = OffensiveAbilityUse.Automatic;
                    CarveAndSpitUse = OffensiveAbilityUse.Automatic;
                    ShadowBringerUse = OffensiveAbilityUse.Automatic;
                    SaltedEarthUse = OffensiveAbilityUse.Automatic;
                    LivingShadowUse = OffensiveAbilityUse.Automatic;
                    EdgeUse = OffensiveAbilityUse.Automatic;
                    PlungeStrategy = PlungeUse.Automatic;
                    SpecialActionUse = SpecialAction.None;
                }
            }
        }

        public static int MPGainedFromAction(State state, AID action) => action switch
        {
            AID.Souleater => state.BloodWeaponStacks > 0 ? 600 : 0,
            AID.HardSlash => state.BloodWeaponStacks > 0 ? 600 : 0,
            AID.SyphonStrike => state.BloodWeaponStacks > 0 ? 1200 : 600,
            AID.Unleash => state.BloodWeaponStacks > 0 ? 600 : 0,
            AID.StalwartSoul => state.Unlocked(TraitID.Blackblood) ? 1200 : 600,
            AID.Bloodspiller => state.BloodWeaponStacks > 0 ? 600 : 0,
            AID.Quietus => state.BloodWeaponStacks > 0 ? 600 : 0,
            _ => 0
        };

        public static int GaugeGainedFromAction(State state, AID action) => action switch
        {
            AID.Souleater => state.BloodWeaponStacks > 0 ? 30 : 20,
            AID.HardSlash => state.BloodWeaponStacks > 0 ? 10 : 0,
            AID.SyphonStrike => state.BloodWeaponStacks > 0 ? 10 : 0,
            AID.Unleash => state.BloodWeaponStacks > 0 ? 10 : 0,
            AID.StalwartSoul => state.Unlocked(TraitID.Blackblood) ? 20 : 0,
            AID.Bloodspiller => state.BloodWeaponStacks > 0 ? -40 : -50,
            AID.Quietus => state.BloodWeaponStacks > 0 ? -40 : -50,
            _ => 0
        };

        public static AID GetNextSTComboAction(AID comboLastMove) => comboLastMove switch
        {
            AID.SyphonStrike => AID.Souleater,
            AID.HardSlash => AID.SyphonStrike,
            _ => AID.HardSlash
        };

        public static int GetSTComboLength(AID comboLastMove) => comboLastMove switch
        {
            AID.SyphonStrike => 1,
            AID.HardSlash => 2,
            _ => 3
        };

        public static int GetAOEComboLength(AID comboLastMove) => comboLastMove == AID.Unleash ? 1 : 2;

        public static AID GetNextAOEComboAction(AID comboLastMove) => comboLastMove == AID.Unleash ? AID.StalwartSoul : AID.Unleash;

        public static AID GetNextUnlockedComboAction(State state, bool aoe)
        {
            if (aoe && state.Unlocked(AID.Unleash))
            {
                // for AOE rotation, assume dropping ST combo is fine
                return state.Unlocked(AID.StalwartSoul) && state.ComboLastMove == AID.Unleash ? AID.StalwartSoul : AID.Unleash;
            }
            else
            {
                // for ST rotation, assume dropping AOE combo is fine (HS is 200 pot vs MT 100, is 20 gauge + 30 sec ST worth it?..)
                return state.ComboLastMove switch
                {
                    AID.SyphonStrike => state.Unlocked(AID.Souleater) ? AID.Souleater : AID.HardSlash,
                    AID.HardSlash => state.Unlocked(AID.SyphonStrike) ? AID.SyphonStrike : AID.HardSlash,
                    _ => AID.HardSlash
                };
            }
        }

        public static AID GetNextBLOODAction(State state, bool aoe)
        {
            // aoe gauge spender
            if (aoe && state.Unlocked(AID.Quietus))
                return state.Unlocked(AID.Quietus) ? AID.Quietus : AID.Bloodspiller;

            // single-target gauge spender
            return state.Unlocked(AID.Bloodspiller) ? AID.Bloodspiller : AID.Bloodspiller;
        }

        // by default, we spend resources either under raid buffs or if another raid buff window will cover at least 4 GCDs of the fight
        public static bool ShouldSpendGauge(State state, Strategy strategy, bool aoe) => strategy.GaugeStrategy switch
        {
            Strategy.GaugeUse.Automatic or Strategy.GaugeUse.TomahawkIfNotInMelee => (state.RaidBuffsLeft > state.GCD || strategy.FightEndIn <= strategy.RaidBuffsIn + 10),
            Strategy.GaugeUse.Spend or Strategy.GaugeUse.ForceSpend => true,
            Strategy.GaugeUse.ConserveIfNoBuffs => state.RaidBuffsLeft > state.GCD,
            Strategy.GaugeUse.Conserve => false,
            Strategy.GaugeUse.ForceExtendST => false,
            Strategy.GaugeUse.ForceSPCombo => false,
            Strategy.GaugeUse.ComboFitBeforeDowntime => strategy.FightEndIn <= state.GCD + 2.5f * ((aoe ? GetAOEComboLength(state.ComboLastMove) : GetSTComboLength(state.ComboLastMove)) - 1),
            Strategy.GaugeUse.PenultimateComboThenSpend => state.ComboLastMove is AID.SyphonStrike or AID.Unleash,
            _ => true
        };

        // note: this check will not allow using non-forced potions before lvl 50, but who cares...
        public static bool ShouldUsePotion(State state, Strategy strategy) => strategy.PotionStrategy switch
        {
            Strategy.PotionUse.Manual => false,
            Strategy.PotionUse.Immediate => state.Gauge.DarksideTimeRemaining > 0 || state.ComboLastMove == AID.SyphonStrike && state.CD(CDGroup.Delirium) < 6, // TODO: reconsider potion use during opener (delayed IR prefers after maim, early IR prefers after storm eye, to cover third IC on 13th GCD)
            Strategy.PotionUse.DelayUntilRaidBuffs => state.Gauge.DarksideTimeRemaining > 0 && state.RaidBuffsLeft > 0,
            Strategy.PotionUse.Force => true,
            _ => false
        };

        // by default, we use IR asap as soon as ST is up
        // TODO: early IR option: technically we can use right after heavy swing, we'll use maim->SE->IC->3xFC
        public static bool ShouldUseDelirium(State state, Strategy strategy) => strategy.DeliriumUse switch
        {
            Strategy.OffensiveAbilityUse.Delay => false,
            Strategy.OffensiveAbilityUse.Force => true,
            _ => strategy.CombatTimer >= 0 && state.TargetingEnemy && state.Gauge.DarksideTimeRemaining > 1 && (state.CD(CDGroup.LivingShadow) < 2.5f || state.CD(CDGroup.LivingShadow) > 40)
        };

        public static bool ShouldUseBloodWeapon(State state, Strategy strategy, bool aoe) => strategy.BloodWeaponUse switch
        {
            Strategy.OffensiveAbilityUse.Delay => false,
            Strategy.OffensiveAbilityUse.Force => true,
            _ => strategy.CombatTimer >= 0 && state.TargetingEnemy && GaugeGainedFromAction(state, GetNextUnlockedComboAction(state, aoe)) <= 100 && state.CD(CDGroup.Delirium) < 1
        };

        public static bool ShouldUseCarveAndSpit(State state, Strategy strategy, bool aoe) => strategy.CarveAndSpitUse switch
        {
            Strategy.OffensiveAbilityUse.Delay => false,
            Strategy.OffensiveAbilityUse.Force => true,
            _ => strategy.CombatTimer >= 0 && state.TargetingEnemy && (state.CD(CDGroup.LivingShadow) < 115f && state.CD(CDGroup.LivingShadow) > state.AnimationLock)
        };

        public static bool ShouldUseShadowBringer(State state, Strategy strategy, bool aoe) => strategy.ShadowBringerUse switch
        {
            Strategy.OffensiveAbilityUse.Delay => false,
            Strategy.OffensiveAbilityUse.Force => true,
            _ => strategy.CombatTimer >= 0 && state.TargetingEnemy && (state.CD(CDGroup.LivingShadow) < 115f && state.CD(CDGroup.LivingShadow) > 90)
        };

        public static bool ShouldUseSaltAndDarkness(State state, Strategy strategy, bool aoe) => strategy.SaltedEarthUse switch
        {
            Strategy.OffensiveAbilityUse.Delay => false,
            Strategy.OffensiveAbilityUse.Force => true,
            _ => strategy.CombatTimer >= 0 && state.TargetingEnemy && (state.CD(CDGroup.LivingShadow) > state.AnimationLock) && state.RangeToTarget < 4
        };

        public static bool ShouldUseLivingShadow(State state, Strategy strategy, bool aoe) => strategy.LivingShadowUse switch
        {
            Strategy.OffensiveAbilityUse.Delay => false,
            Strategy.OffensiveAbilityUse.Force => true,
            _ => strategy.CombatTimer >= 0 && state.TargetingEnemy && state.Gauge.Blood >= 50 && state.CD(CDGroup.Delirium) > state.AnimationLock
        };

        // by default, we use upheaval asap as soon as ST is up
        // TODO: consider delaying for 1 GCD during opener...
        public static bool ShouldUseFloodOrEdge(State state, Strategy strategy, bool aoe) => strategy.EdgeUse switch
        {
            Strategy.OffensiveAbilityUse.Delay => false,
            Strategy.OffensiveAbilityUse.Force => true,
            _ => strategy.CombatTimer >= 0 && state.TargetingEnemy && state.CurMP >= 3000 && (state.Gauge.DarksideTimeRemaining < state.AnimationLock || (state.CurMP + MPGainedFromAction(state, GetNextUnlockedComboAction(state, aoe)) >= 10000) || state.RaidBuffsLeft > state.AnimationLock || (state.Gauge.ShadowTimeRemaining > state.AnimationLock))
        };

        public static bool ShouldUsePlunge(State state, Strategy strategy)
        {
            bool OnCD = state.CD(CDGroup.LivingShadow) < 115f && state.CD(CDGroup.LivingShadow) > state.AnimationLock && state.CD(CDGroup.Shadowbringer) > state.AnimationLock && (state.CD(CDGroup.BloodWeapon) > 35 || state.CD(CDGroup.Delirium) > 35);
            switch (strategy.PlungeStrategy)
            {
                case Strategy.PlungeUse.Forbid:
                    return false;
                case Strategy.PlungeUse.Force:
                    return true;
                case Strategy.PlungeUse.ForceReserve:
                    return state.CD(CDGroup.Plunge) <= state.AnimationLock;
                case Strategy.PlungeUse.ReserveOne:
                    return state.CD(CDGroup.Plunge) <= state.GCD;
                case Strategy.PlungeUse.UseOutsideMelee:
                    return state.RangeToTarget > 3;
                case Strategy.PlungeUse.NoReserve:
                    return state.CD(CDGroup.LivingDead) > state.AnimationLock && state.CD(CDGroup.Plunge) - 30 <= state.GCD;
                default:
                    if (strategy.CombatTimer < 0)
                        return false; // don't use out of combat
                    if (state.RangeToTarget > 3)
                        return false; // don't use out of melee range to prevent fucking up player's position
                    if (strategy.PositionLockIn <= state.AnimationLock)
                        return false; // forbidden due to state flags
                    if (OnCD)
                        return true; // delay until Gnashing Sonic and Doubledown on CD, even if overcapping charges
                    float chargeCapIn = state.CD(CDGroup.Plunge);
                    if (chargeCapIn < state.GCD + 2.5 && OnCD)
                        return true; // if we won't onslaught now, we risk overcapping charges
                    if (strategy.PlungeStrategy != Strategy.PlungeUse.NoReserve && state.CD(CDGroup.Plunge) > 30 + state.AnimationLock)
                        return false; // strategy prevents us from using last charge
                    if (state.RaidBuffsLeft > state.AnimationLock)
                        return true; // use now, since we're under raid buffs
                    return (chargeCapIn <= strategy.RaidBuffsIn) && OnCD; // use if we won't be able to delay until next raid buffs
            }
        }

        public static AID GetNextBestGCD(State state, Strategy strategy, bool aoe)
        {
            // prepull
            if (strategy.CombatTimer > -100 && strategy.CombatTimer < -0.7f)
                return AID.None;

            // 0. non-standard actions forced by strategy
            // forced tomahawk
            if (strategy.GaugeStrategy == Strategy.GaugeUse.TomahawkIfNotInMelee && state.RangeToTarget > 3)
                return AID.Unmend;
            // forced combo until penultimate step
            if (strategy.GaugeStrategy == Strategy.GaugeUse.PenultimateComboThenSpend && state.ComboLastMove != AID.SyphonStrike && state.ComboLastMove != AID.Unleash && (state.ComboLastMove != AID.HardSlash || state.Gauge.Blood <= 90))
                return aoe ? AID.Unleash : state.ComboLastMove == AID.HardSlash ? AID.SyphonStrike : AID.HardSlash;
            // forced gauge spender
            bool canUseFC = state.Gauge.Blood >= 50 || state.DeliriumStacks > 0 && state.Unlocked(AID.Delirium);
            if (strategy.GaugeStrategy == Strategy.GaugeUse.ForceSpend && canUseFC)
                return GetNextBLOODAction(state, aoe);

            if (state.CD(CDGroup.BloodWeapon) < 5 && state.Gauge.Blood >= 50)
                return GetNextBLOODAction(state, aoe);

            // forbid automatic PR when out of melee range, to avoid fucking up player positioning when avoiding mechanics
            bool spendGauge = ShouldSpendGauge(state, strategy, aoe);

            if (state.DeliriumStacks > 0)
            {
                if (state.Unlocked(AID.LivingShadow))
                {
                    if (state.CD(CDGroup.LivingShadow) < 117.5f && state.CD(CDGroup.LivingShadow) > state.AnimationLock)
                    {
                        // only consider not casting FC action if delaying won't cost IR stack
                        int fcCastsLeft = state.DeliriumStacks;
                        if (state.DeliriumLeft <= state.GCD + fcCastsLeft * 2.5f)
                            return GetNextBLOODAction(state, aoe);

                        // don't delay if it won't give us anything (but still prefer PR under buffs)
                        if (spendGauge || state.DeliriumLeft <= strategy.RaidBuffsIn)
                            return GetNextBLOODAction(state, aoe);
                    }
                    else if (state.CD(CDGroup.LivingShadow) < 117.5f && (state.DeliriumLeft < state.GCD + (state.DeliriumStacks == 3 ? 2.5f * 3 : state.DeliriumStacks == 2 ? 2.5f * 2 : state.DeliriumStacks == 1 ? 2.5f * 1 : 0) || state.Gauge.Blood >= 50 && state.RaidBuffsLeft > state.AnimationLock))
                        return GetNextBLOODAction(state, aoe);
                }
                else if (state.Gauge.Blood >= 50 && (state.Unlocked(AID.Bloodspiller) || state.ComboLastMove != AID.SyphonStrike || aoe && state.Unlocked(AID.Quietus)))
                {
                    // single-target: FC > SE/ST > IB > Maim > HS
                    // aoe: Decimate > SC > Combo
                    return GetNextBLOODAction(state, aoe);
                }
            }

            var nextCombo = GetNextUnlockedComboAction(state, aoe);
            if (state.Gauge.Blood + GaugeGainedFromAction(state, nextCombo) >= 100)
                return GetNextBLOODAction(state, aoe);

            if (strategy.RaidBuffsIn > 500 && (state.Gauge.Blood >= 50 || state.DeliriumStacks > 0) && state.CD(CDGroup.LivingShadow) < 115f && state.CD(CDGroup.LivingShadow) > state.AnimationLock)
                return GetNextBLOODAction(state, aoe);

            // TODO: reconsider min time left...
            return GetNextUnlockedComboAction(state, aoe);
        }

        // window-end is either GCD or GCD - time-for-second-ogcd; we are allowed to use ogcds only if their animation lock would complete before window-end
        public static ActionID GetNextBestOGCD(State state, Strategy strategy, float deadline, bool aoe)
        {
            // 0. onslaught as a gap-filler - this should be used asap even if we're delaying GCD, since otherwise we'll probably end up delaying it even more
            bool wantOnslaught = state.Unlocked(AID.Plunge) && state.TargetingEnemy && ShouldUsePlunge(state, strategy);
            if (wantOnslaught && state.RangeToTarget > 3)
                return ActionID.MakeSpell(AID.Plunge);

            // 1. potion
            if (ShouldUsePotion(state, strategy) && state.CanWeave(state.PotionCD, 1.1f, deadline))
                return CommonDefinitions.IDPotionStr;

            if (ShouldUseBloodWeapon(state, strategy, aoe) && state.Unlocked(AID.BloodWeapon) && state.CanWeave(CDGroup.BloodWeapon, 0.6f, deadline))
                return ActionID.MakeSpell(AID.BloodWeapon);

            if (ShouldUseDelirium(state, strategy) && state.Unlocked(AID.Delirium) && state.CanWeave(CDGroup.Delirium, 0.6f, deadline))
                return ActionID.MakeSpell(AID.Delirium);

            // 3. upheaval
            // TODO: reconsider priority compared to IR

            if (state.Unlocked(AID.LivingShadow) && ShouldUseLivingShadow(state, strategy, aoe) && state.CanWeave(CDGroup.LivingShadow, 0.6f, deadline))
                return ActionID.MakeSpell(AID.LivingShadow);

            if (state.Unlocked(AID.SaltedEarth) && ShouldUseSaltAndDarkness(state, strategy, aoe) && state.CanWeave(CDGroup.SaltedEarth, 0.6f, deadline))
                return ActionID.MakeSpell(AID.SaltedEarth);

            if (state.Unlocked(AID.Shadowbringer) && ShouldUseShadowBringer(state, strategy, aoe) && state.CanWeave(state.CD(CDGroup.Shadowbringer), 0.6f, deadline))
                return ActionID.MakeSpell(AID.Shadowbringer);

            if (state.Unlocked(AID.FloodOfDarkness) && ShouldUseFloodOrEdge(state, strategy, aoe) && state.CanWeave(CDGroup.FloodOfDarkness, 0.6f, deadline) && MPGainedFromAction(state, GetNextBestGCD(state, strategy, aoe)) >= 10000)
                return ActionID.MakeSpell(aoe && state.Unlocked(AID.FloodOfDarkness) ? state.BestFlood : state.BestEdge);

            if (state.CD(CDGroup.Plunge) < state.AnimationLock && state.CD(CDGroup.Shadowbringer) > state.AnimationLock && state.CD(CDGroup.LivingShadow) > state.AnimationLock && state.CanWeave(CDGroup.Plunge, 0.6f, deadline))
                return ActionID.MakeSpell(AID.Plunge);

            if (state.Unlocked(AID.CarveAndSpit) && ShouldUseCarveAndSpit(state, strategy, aoe) && state.CanWeave(CDGroup.CarveAndSpit, 0.6f, deadline))
                return ActionID.MakeSpell(aoe && state.Unlocked(AID.AbyssalDrain) ? AID.AbyssalDrain : AID.CarveAndSpit);

            if (state.Unlocked(AID.Shadowbringer) && ShouldUseShadowBringer(state, strategy, aoe) && state.CanWeave(state.CD(CDGroup.Shadowbringer) - 58.5f, 0.6f, deadline))
                return ActionID.MakeSpell(AID.Shadowbringer);

            if (state.Unlocked(AID.FloodOfDarkness) && ShouldUseFloodOrEdge(state, strategy, aoe) && state.CanWeave(CDGroup.FloodOfDarkness, 0.6f, deadline))
                return ActionID.MakeSpell(aoe && state.Unlocked(AID.FloodOfDarkness) ? state.BestFlood : state.BestEdge);

            if (state.Unlocked(AID.SaltAndDarkness) && state.CD(CDGroup.Shadowbringer) > state.AnimationLock && state.CD(CDGroup.CarveAndSpit) > state.AnimationLock && state.CanWeave(CDGroup.SaltAndDarkness, 0.6f, deadline) && state.SaltedEarthLeft > state.AnimationLock)
                return ActionID.MakeSpell(AID.SaltAndDarkness);

            // 5. onslaught, if surging tempest up and not forbidden
            if (wantOnslaught && state.CanWeave(state.CD(CDGroup.Plunge) - 30, 0.6f, deadline))
                return ActionID.MakeSpell(AID.Plunge);

            // no suitable oGCDs...
            return new();
        }
    }
}
