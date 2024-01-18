namespace BossMod
{
    [ConfigDisplay(Parent = typeof(AutorotationConfig))]
    class DRKConfig : ConfigNode
    {
        [PropertyDisplay("Execute optimal single-target rotation on Hard Slash and AOE rotation on Unleash")]
        public bool FullRotation = true;

        [PropertyDisplay("Execute preceeding actions for single-target combos (Maim, Storm Eye, Storm Path)")]
        public bool STCombos = true;

        [PropertyDisplay("Execute preceeding action for aoe combo (Stalwart Soul)")]
        public bool AOECombos = true;

        [PropertyDisplay("Smart targeting for Shirk (target if friendly, otherwise mouseover if friendly, otherwise offtank if available)")]
        public bool SmartNascentFlashShirkTarget = true;

        [PropertyDisplay("Use provoke on mouseover, if available and hostile")]
        public bool ProvokeMouseover = true;

        [PropertyDisplay("Forbid tomahawk too early in prepull")]
        public bool ForbidEarlyUnmend = true;
    }
}
