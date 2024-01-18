using System.Collections.Generic;
using static BossMod.CommonActions;

namespace BossMod.DRK
{
    public enum AID : uint
    {
        None = 0,
        Sprint = 3,

        // GCDs
        HardSlash = 3617, // L1, instant, range 3, single-target 0/0, targets=hostile, animLock=0.600s
        SyphonStrike = 3623, // L2, instant, range 3, single-target 0/0, targets=hostile, animLock=0.600s
        Unleash = 3621, // L6, instant, range 0, AOE circle 5/0, targets=self, animLock=???
        Unmend = 3624, // L15, instant, range 20, single-target 0/0, targets=hostile, animLock=???
        Souleater = 3632, // L26, instant, range 3, single-target 0/0, targets=hostile, animLock=0.600s
        StalwartSoul = 16468, // L40, instant, range 0, AOE circle 5/0, targets=self, animLock=???
        Bloodspiller = 7392, // L62, instant, range 3, single-target 0/0, targets=hostile, animLock=0.600s
        Quietus = 7391, // L64, instant, range 0, AOE circle 5/0, targets=self, animLock=???

        // oGCDs
        Rampart = 7531, // L8, instant, 90.0s CD (group 46), range 0, single-target 0/0, targets=self, animLock=0.600s
        ReleaseGrit = 32067, // L10, instant, 1.0s CD (group 1), range 0, single-target 0/0, targets=self, animLock=???
        Grit = 3629, // L10, instant, 2.0s CD (group 1), range 0, single-target 0/0, targets=self, animLock=0.600s
        LowBlow = 7540, // L12, instant, 25.0s CD (group 41), range 3, single-target 0/0, targets=hostile, animLock=0.600s
        Provoke = 7533, // L15, instant, 30.0s CD (group 42), range 25, single-target 0/0, targets=hostile, animLock=0.600s
        Interject = 7538, // L18, instant, 30.0s CD (group 43), range 3, single-target 0/0, targets=hostile, animLock=0.600s
        Reprisal = 7535, // L22, instant, 60.0s CD (group 44), range 0, AOE circle 5/0, targets=self, animLock=0.600s
        FloodOfDarkness = 16466, // L30, instant, 1.0s CD (group 0), range 10, AOE rect 10/4, targets=hostile, animLock=???
        ArmsLength = 7548, // L32, instant, 120.0s CD (group 48), range 0, single-target 0/0, targets=self, animLock=0.600s
        BloodWeapon = 3625, // L35, instant, 60.0s CD (group 11), range 0, single-target 0/0, targets=self, animLock=0.600s
        ShadowWall = 3636, // L38, instant, 120.0s CD (group 20), range 0, single-target 0/0, targets=self, animLock=0.600s
        EdgeOfDarkness = 16467, // L40, instant, 1.0s CD (group 0), range 3, single-target 0/0, targets=hostile, animLock=???
        DarkMind = 3634, // L45, instant, 60.0s CD (group 12), range 0, single-target 0/0, targets=self, animLock=0.600s
        Shirk = 7537, // L48, instant, 120.0s CD (group 49), range 25, single-target 0/0, targets=party, animLock=???
        LivingDead = 3638, // L50, instant, 300.0s CD (group 23), range 0, single-target 0/0, targets=self, animLock=0.600s
        SaltedEarth = 3639, // L52, instant, 90.0s CD (group 15), range 0, AOE circle 5/0, targets=self, animLock=0.600s
        Plunge = 3640, // L54, instant, 30.0s CD (group 9) (2 charges), range 20, single-target 0/0, targets=hostile, animLock=0.600s
        AbyssalDrain = 3641, // L56, instant, 60.0s CD (group 13), range 20, AOE circle 5/0, targets=hostile, animLock=???
        CarveAndSpit = 3643, // L60, instant, 60.0s CD (group 13), range 3, single-target 0/0, targets=hostile, animLock=0.600s
        Delirium = 7390, // L68, instant, 60.0s CD (group 10), range 0, single-target 0/0, targets=self, animLock=0.600s
        TheBlackestNight = 7393, // L70, instant, 15.0s CD (group 2), range 30, single-target 0/0, targets=self/party, animLock=0.600s
        FloodOfShadow = 16469, // L74, instant, 1.0s CD (group 0), range 10, AOE rect 10/4, targets=hostile, animLock=???
        EdgeOfShadow = 16470, // L74, instant, 1.0s CD (group 0), range 3, single-target 0/0, targets=hostile, animLock=0.600s
        DarkMissionary = 16471, // L76, instant, 90.0s CD (group 17), range 0, AOE circle 30/0, targets=self, animLock=0.600s
        LivingShadow = 16472, // L80, instant, 120.0s CD (group 21), range 0, single-target 0/0, targets=self, animLock=0.600s
        Oblation = 25754, // L82, instant, 60.0s CD (group 22) (2 charges), range 30, single-target 0/0, targets=self/party, animLock=0.600s
        SaltAndDarkness = 25755, // L86, instant, 20.0s CD (group 3), range 0, single-target 0/0, targets=self, animLock=0.600s
        SaltAndDarkness2 = 25756, // L86, instant, 0.0s CD (group 3), range 100, AOE circle 5/0, targets=self/area/!dead, animLock=0.600s
        Shadowbringer = 25757, // L90, instant, 60.0s CD (group 19) (2 charges), range 10, AOE rect 10/4, targets=hostile, animLock=0.600s
    }

    public enum TraitID : uint
    {
        None = 0,
        TankMastery = 319, // L1
        Blackblood = 158, // L62
        EnhancedBlackblood = 159, // L66
        DarksideMastery = 271, // L74
        EnhancedPlunge = 272, // L78
        EnhancedUnmend = 422, // L84
        MeleeMastery = 506, // L84
        EnhancedLivingShadow = 511, // L88
        EnhancedLivingShadowII = 423, // L90
    }

    public enum CDGroup : int
    {
        FloodOfDarkness = 0, // 1.0 max, shared by Flood of Darkness, Edge of Darkness, Flood of Shadow, Edge of Shadow
        EdgeOfDarkness = 0,
        FloodOfShadow = 0,
        EdgeOfShadow = 0,
        ReleaseGrit = 1, // variable max, shared by Release Grit, Grit
        Grit = 1,
        TheBlackestNight = 2, // 15.0 max
        SaltAndDarkness = 3, // 20.0 max
        Plunge = 9, // 2*30.0 max
        Delirium = 10, // 60.0 max
        BloodWeapon = 11, // 60.0 max
        DarkMind = 12, // 60.0 max
        AbyssalDrain = 13, // 60.0 max, shared by Abyssal Drain, Carve and Spit
        CarveAndSpit = 13,
        SaltedEarth = 15, // 90.0 max
        DarkMissionary = 17, // 90.0 max
        Shadowbringer = 19, // 2*60.0 max
        ShadowWall = 20, // 120.0 max
        LivingShadow = 21, // 120.0 max
        Oblation = 22, // 2*60.0 max
        LivingDead = 23, // 300.0 max
        LowBlow = 41, // 25.0 max
        Provoke = 42, // 30.0 max
        Interject = 43, // 30.0 max
        Reprisal = 44, // 60.0 max
        Rampart = 46, // 90.0 max
        ArmsLength = 48, // 120.0 max
        Shirk = 49, // 120.0 max
    }

    public enum SID : uint
    {
        None = 0,
        BloodWeapon = 742, // applied by Blood Weapon to self
        Delirium = 1972, // applied by Delirium to self
        SaltedEarth = 749, // applied by Salted Earth to self
        BlackestNight = 1178, // applied by The Blackest Night to self
        Oblation = 2682, // applied by Oblation to self
        DarkMissionary = 1894, // applied by Dark Missionary to self
        LivingDead = 810, // applied by Living Dead to self
        Grit = 743, // applied by Grit to self
        ShadowWall = 747, // applied by Shadow Wall to self
        Rampart = 1191, // applied by Rampart to self
        Reprisal = 1193, // applied by Reprisal to target
        DarkMind = 746, // applied by Dark Mind to self
        ArmsLength = 1209, // applied by Arm's Length to self
        Stun = 2, // applied by Low Blow to target
    }

    public static class Definitions
    {
        public static uint[] UnlockQuests = { 67590, 67591, 67592, 67594, 67596, 67597, 67598, 67600, 68455 };

        public static bool Unlocked(AID aid, int level, int questProgress)
        {
            return aid switch
            {
                AID.SyphonStrike => level >= 2,
                AID.Unleash => level >= 6,
                AID.Rampart => level >= 8,
                AID.ReleaseGrit => level >= 10,
                AID.Grit => level >= 10,
                AID.LowBlow => level >= 12,
                AID.Unmend => level >= 15,
                AID.Provoke => level >= 15,
                AID.Interject => level >= 18,
                AID.Reprisal => level >= 22,
                AID.Souleater => level >= 26,
                AID.FloodOfDarkness => level >= 30 && questProgress > 0,
                AID.ArmsLength => level >= 32,
                AID.BloodWeapon => level >= 35 && questProgress > 1,
                AID.ShadowWall => level >= 38,
                AID.StalwartSoul => level >= 40,
                AID.EdgeOfDarkness => level >= 40 && questProgress > 2,
                AID.DarkMind => level >= 45,
                AID.Shirk => level >= 48,
                AID.LivingDead => level >= 50 && questProgress > 3,
                AID.SaltedEarth => level >= 52 && questProgress > 4,
                AID.Plunge => level >= 54 && questProgress > 5,
                AID.AbyssalDrain => level >= 56 && questProgress > 6,
                AID.CarveAndSpit => level >= 60 && questProgress > 7,
                AID.Bloodspiller => level >= 62,
                AID.Quietus => level >= 64,
                AID.Delirium => level >= 68,
                AID.TheBlackestNight => level >= 70 && questProgress > 8,
                AID.FloodOfShadow => level >= 74,
                AID.EdgeOfShadow => level >= 74,
                AID.DarkMissionary => level >= 76,
                AID.LivingShadow => level >= 80,
                AID.Oblation => level >= 82,
                AID.SaltAndDarkness => level >= 86,
                AID.SaltAndDarkness2 => level >= 86,
                AID.Shadowbringer => level >= 90,
                _ => true,
            };
        }
        public static bool Unlocked(TraitID tid, int level, int questProgress)
        {
            return tid switch
            {
                TraitID.Blackblood => level >= 62,
                TraitID.EnhancedBlackblood => level >= 66,
                TraitID.DarksideMastery => level >= 74,
                TraitID.EnhancedPlunge => level >= 78,
                TraitID.EnhancedUnmend => level >= 84,
                TraitID.MeleeMastery => level >= 84,
                TraitID.EnhancedLivingShadow => level >= 88,
                TraitID.EnhancedLivingShadowII => level >= 90,
                _ => true,
            };
        }

        public static Dictionary<ActionID, ActionDefinition> SupportedActions;
        static Definitions()
        {
            SupportedActions = CommonDefinitions.CommonActionData(CommonDefinitions.IDPotionStr);
            SupportedActions.GCD(AID.HardSlash, 3);
            SupportedActions.GCD(AID.SyphonStrike, 3);
            SupportedActions.GCD(AID.Unleash, 0);
            SupportedActions.OGCD(AID.Rampart, 0, CDGroup.Rampart, 90.0f).EffectDuration = 20;
            SupportedActions.OGCD(AID.ReleaseGrit, 0, CDGroup.ReleaseGrit, 1.0f);
            SupportedActions.OGCD(AID.Grit, 0, CDGroup.Grit, 2.0f);
            SupportedActions.OGCD(AID.LowBlow, 3, CDGroup.LowBlow, 25.0f);
            SupportedActions.GCD(AID.Unmend, 20);
            SupportedActions.OGCD(AID.Provoke, 25, CDGroup.Provoke, 30.0f);
            SupportedActions.OGCD(AID.Interject, 3, CDGroup.Interject, 30.0f);
            SupportedActions.OGCD(AID.Reprisal, 0, CDGroup.Reprisal, 60.0f).EffectDuration = 10;
            SupportedActions.GCD(AID.Souleater, 3);
            SupportedActions.OGCD(AID.FloodOfDarkness, 10, CDGroup.FloodOfDarkness, 1.0f);
            SupportedActions.OGCD(AID.ArmsLength, 0, CDGroup.ArmsLength, 120.0f).EffectDuration = 6;
            SupportedActions.OGCD(AID.BloodWeapon, 0, CDGroup.BloodWeapon, 60.0f);
            SupportedActions.OGCD(AID.ShadowWall, 0, CDGroup.ShadowWall, 120.0f).EffectDuration = 15;
            SupportedActions.GCD(AID.StalwartSoul, 0);
            SupportedActions.OGCD(AID.EdgeOfDarkness, 3, CDGroup.EdgeOfDarkness, 1.0f);
            SupportedActions.OGCD(AID.DarkMind, 0, CDGroup.DarkMind, 60.0f).EffectDuration = 10;
            SupportedActions.OGCD(AID.Shirk, 25, CDGroup.Shirk, 120.0f);
            SupportedActions.OGCD(AID.LivingDead, 0, CDGroup.LivingDead, 300.0f);
            SupportedActions.OGCD(AID.SaltedEarth, 0, CDGroup.SaltedEarth, 90.0f);
            SupportedActions.OGCDWithCharges(AID.Plunge, 20, CDGroup.Plunge, 30.0f, 2);
            SupportedActions.OGCD(AID.AbyssalDrain, 20, CDGroup.AbyssalDrain, 60.0f);
            SupportedActions.OGCD(AID.CarveAndSpit, 3, CDGroup.CarveAndSpit, 60.0f);
            SupportedActions.GCD(AID.Bloodspiller, 3);
            SupportedActions.GCD(AID.Quietus, 0);
            SupportedActions.OGCD(AID.Delirium, 0, CDGroup.Delirium, 60.0f);
            SupportedActions.OGCD(AID.TheBlackestNight, 30, CDGroup.TheBlackestNight, 15.0f).EffectDuration = 7;
            SupportedActions.OGCD(AID.FloodOfShadow, 10, CDGroup.FloodOfShadow, 1.0f);
            SupportedActions.OGCD(AID.EdgeOfShadow, 3, CDGroup.EdgeOfShadow, 1.0f);
            SupportedActions.OGCD(AID.DarkMissionary, 0, CDGroup.DarkMissionary, 90.0f).EffectDuration = 15;
            SupportedActions.OGCD(AID.LivingShadow, 0, CDGroup.LivingShadow, 120.0f);
            SupportedActions.OGCDWithCharges(AID.Oblation, 30, CDGroup.Oblation, 60.0f, 2).EffectDuration = 10;
            SupportedActions.OGCD(AID.SaltAndDarkness, 0, CDGroup.SaltAndDarkness, 20.0f);
            SupportedActions.OGCD(AID.SaltAndDarkness2, 100, CDGroup.SaltAndDarkness, 0.0f);
            SupportedActions.OGCDWithCharges(AID.Shadowbringer, 10, CDGroup.Shadowbringer, 60.0f, 2);
        }
    }
}