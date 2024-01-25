namespace BossMod
{
    [ConfigDisplay(Parent = typeof(AutorotationConfig))]
    class SAMConfig : ConfigNode
    {
        [PropertyDisplay("Execute optimal rotations on Hakaze (ST) or Fuga/Fuko (AOE)")]
        public bool FullRotation = true;

        [PropertyDisplay("Should use Filler")]
        public bool Filler = true;

        [PropertyDisplay("Ignore TTK")]
        public bool TTKignore = true;

        [PropertyDisplay("Ignore TTK only for Higanbana")]
        public bool TTKignoreHiganbanaOnly = true;

        [PropertyDisplay("Use both Tsubame charges")]
        public bool UseBothTsubameCharges = true;

        [PropertyDisplay("Early Higanbana")]
        public bool EarlyHiganbana = true;
    }
}
