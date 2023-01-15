
namespace LittleNPCs.Framework {
    /// <summary>
    /// ModConfig Options:
    /// AgeWhenKidsAreModified: the age at which the CP mod will patch over the child.
    /// The Default is currently 83, which is 14 newborn/14 baby/28 crawler/28 toddler,
    /// on the assumption that CP mods will make them a child.
    /// DoChildrenWander: when true, children wander around the house every hour (unless scheduled.)
    /// DoChildrenHaveCurfew: when true, children will head home at curfew time (unless already walking somewhere.)
    /// CurfewTime: the time of curfew (when DoChildrenHaveCurfew is true).
    /// </summary>
    public class ModConfig {
        public int AgeWhenKidsAreModified { get; set; }
        public bool DoChildrenWander { get; set; }
        public bool DoChildrenHaveCurfew { get; set; }
        public int CurfewTime { get; set; }

        public ModConfig() {
            AgeWhenKidsAreModified = 83;
            DoChildrenWander = true;
            DoChildrenHaveCurfew = true;
            CurfewTime = 1900; //7 pm
        }
    }
}