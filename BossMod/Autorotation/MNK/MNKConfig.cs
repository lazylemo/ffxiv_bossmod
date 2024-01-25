namespace BossMod
{
    [ConfigDisplay(Parent = typeof(AutorotationConfig))]
    class MNKConfig : ConfigNode
    {
        [PropertyDisplay(
            "Execute optimal rotations on Bootshine (ST) or Arm of the Destroyer (AOE)"
        )]
        public bool FullRotation = true;

        [PropertyDisplay("Execute form-specific aoe GCD on Four-point Fury")]
        public bool AOECombos = true;

        [PropertyDisplay("Automatic mouseover targeting for Thunderclap")]
        public bool SmartThunderclap = true;

        [PropertyDisplay("Delay Thunderclap if already in melee range of target")]
        public bool PreventCloseDash = true;

        [PropertyDisplay("Early Opener")]
        public bool EarlyOpener = true;

        [PropertyDisplay("Ignore TTK")]
        public bool IgnoreTTK = true;
        
        [PropertyDisplay("Use Form Shift out of combat")]
        public bool AutoFormShift = false;
    }
}