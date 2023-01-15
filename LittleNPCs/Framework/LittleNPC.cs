using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI;

using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;


namespace LittleNPCs.Framework {
    public class LittleNPC : NPC {
        private IMonitor monitor_;

        public long IdOfParent { get; private set; }

        public int ChildIndex { get; private set; }

        protected LittleNPC(IMonitor monitor, long idOfParent, int childIndex, AnimatedSprite sprite, Vector2 position, string defaultMap, int facingDir, string name, Dictionary<int, int[]> schedule, Texture2D portrait, bool eventActor)
        : base(sprite, position, defaultMap, facingDir, name, schedule, portrait, eventActor, null) {
            monitor_ = monitor;
            IdOfParent = idOfParent;
            ChildIndex = childIndex;
        }

        public static LittleNPC FromChild(Child child, IMonitor monitor) {
            // ATTENTION: We use a separate dictionary for dispositions.
            // Reason: If we add something to 'Data/NPCDispositions' the game attempts to create that NPC.
            // We must control NPC creation, however, so we add dispositions from a different dictionary to the created NPC.
            var litteNpcDispositions = Game1.content.Load<Dictionary<string, string>>("Data/LittleNPCDispositions");
            var npcDispositions = Game1.content.Load<Dictionary<string, string>>("Data/NPCDispositions");

            var sprite = new AnimatedSprite($"Characters/{child.Name}", 0, 16, 32);
            var portrait = Game1.content.Load<Texture2D>($"Portraits/{child.Name}");
            var npc = new LittleNPC(monitor,
                                    child.idOfParent.Value,
                                    child.GetChildIndex(),
                                    sprite,
                                    child.Position,
                                    child.DefaultMap,
                                    child.FacingDirection,
                                    child.Name,
                                    null,
                                    portrait,
                                    false);

            // Set dispositions now.
            npcDispositions[npc.Name] = litteNpcDispositions[npc.Name];

            // Reload schedule.
            npc.Schedule = npc.getSchedule(Game1.dayOfMonth);

            return npc;
        }

        /// <inheritdoc/>
        public override void performTenMinuteUpdate(int timeOfDay, GameLocation l) {
            monitor_.Log($"LittleNPC.performTenMinuteUpdate {this.Name} {this.currentLocation}, {Utility.Vector2ToPoint(this.Position / 64f)}", LogLevel.Warn);

            FarmHouse farmHouse = Utility.getHomeOfFarmer(Game1.player);
            if (farmHouse.characters.Contains(this)) {
                ModConfig config = ModEntry.config_;
                // Send children to bed when inside home.
                if (config.DoChildrenHaveCurfew && Game1.timeOfDay == config.CurfewTime) {
                    IsWalkingInSquare = false;
                    Halt();
                    temporaryController = null;

                    // Child is at home, direct path to bed (DefaultPosition).
                    Point bedPoint = new Point((int) DefaultPosition.X / 64, (int) DefaultPosition.Y / 64);
                    controller = new PathFindController(this, farmHouse, bedPoint, 2);

                    if (controller.pathToEndPoint is null || !farmHouse.isTileOnMap(controller.pathToEndPoint.Last().X, controller.pathToEndPoint.Last().Y)) {
                        controller = null;
                    }
                }
                // Make children wander if they have nothing better to do.
                // ATTENTION: We have to skip that for scheduled times, otherwise schedule and random wandering overlap in a weird way:
                // The NPCs get warped out of farm house before they reach their random destination points in the house
                // and thus are doomed to walk around in the BusStop location endlessly without a chance to reach their destination!
                else if (controller is null
                         && config.DoChildrenWander
                         && (Schedule is null || !Schedule.ContainsKey(Game1.timeOfDay))
                         && Game1.timeOfDay % 100 == 0
                         && Game1.timeOfDay < config.CurfewTime) {
                    if (!currentLocation.Equals(Utility.getHomeOfFarmer(Game1.player))) {
                        return;
                    }

                    IsWalkingInSquare = false;
                    Halt();

                    // If I'm going to prevent them from wandering into doorways, I need to do it here.
                    controller = new PathFindController(this, farmHouse, farmHouse.getRandomOpenPointInHouse(Game1.random, 0, 30), 2);
                    if (controller.pathToEndPoint is null || !farmHouse.isTileOnMap(controller.pathToEndPoint.Last().X, controller.pathToEndPoint.Last().Y)) {
                        controller = null;
                    }
                }
            }

            // Call base method.
            base.performTenMinuteUpdate(timeOfDay, l);
        }
    }
}