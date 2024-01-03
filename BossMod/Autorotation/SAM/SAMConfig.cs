namespace BossMod
{
    [ConfigDisplay(Parent = typeof(AutorotationConfig))]
    class SAMConfig : ConfigNode
    {
        [PropertyDisplay("Execute optimal rotations on Hakaze (ST) or Fuga/Fuko (AOE)")]
        public bool FullRotation = true;
    }
}
