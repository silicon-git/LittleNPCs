using System.Collections.Generic;

using Microsoft.Xna.Framework.Graphics;

using HarmonyLib;

using StardewModdingAPI;
using StardewModdingAPI.Events;

using StardewValley;
using StardewValley.Characters;

using LittleNPCs.Framework;


namespace LittleNPCs {

    public class ModEntry : Mod {

        public static IModHelper helper_;

        public static IMonitor monitor_;

        public static ModConfig config_;


        public override void Entry(IModHelper helper) {
            ModEntry.helper_ = helper;
            monitor_ = this.Monitor;

            // Read config.
            config_ = helper.ReadConfig<ModConfig>();

            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;

            // Create Harmony instance.
            Harmony harmony = new Harmony(this.ModManifest.UniqueID);
            // NPC.arriveAtFarmHouse (postfix).
            harmony.Patch(
                original: AccessTools.Method(typeof(NPC), nameof(NPC.arriveAtFarmHouse)),
                postfix: new HarmonyMethod(typeof(LittleNPCs.Framework.Patches.NPCArriveAtFarmHousePatch), nameof(LittleNPCs.Framework.Patches.NPCArriveAtFarmHousePatch.Postfix))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(NPC), nameof(NPC.checkSchedule)),
                prefix: new HarmonyMethod(typeof(LittleNPCs.Framework.Patches.NPCCheckSchedulePatch), nameof(LittleNPCs.Framework.Patches.NPCCheckSchedulePatch.Prefix))
            );
            // NPC.parseMasterSchedule patch (prefix, postfix, finalizer).
            harmony.Patch(
                original: AccessTools.Method(typeof(NPC), nameof(NPC.parseMasterSchedule)),
                prefix: new HarmonyMethod(typeof(LittleNPCs.Framework.Patches.NPCParseMasterSchedulePatch), nameof(LittleNPCs.Framework.Patches.NPCParseMasterSchedulePatch.Prefix)),
                postfix: new HarmonyMethod(typeof(LittleNPCs.Framework.Patches.NPCParseMasterSchedulePatch), nameof(LittleNPCs.Framework.Patches.NPCParseMasterSchedulePatch.Postfix)),
                finalizer: new HarmonyMethod(typeof(LittleNPCs.Framework.Patches.NPCParseMasterSchedulePatch), nameof(LittleNPCs.Framework.Patches.NPCParseMasterSchedulePatch.Finalizer))
            );
            // NPC.prepareToDisembarkOnNewSchedulePath patch (postfix).
            harmony.Patch(
                original: AccessTools.Method(typeof(NPC), "prepareToDisembarkOnNewSchedulePath"),
                postfix: new HarmonyMethod(typeof(LittleNPCs.Framework.Patches.NPCPrepareToDisembarkOnNewSchedulePathPatch), nameof(LittleNPCs.Framework.Patches.NPCPrepareToDisembarkOnNewSchedulePathPatch.Postfix))
            );
            // PathFindController.handleWarps patch (prefix).
            harmony.Patch(
                original: AccessTools.Method(typeof(PathFindController), nameof(PathFindController.handleWarps)),
                prefix: new HarmonyMethod(typeof(LittleNPCs.Framework.Patches.PFCHandleWarpsPatch), nameof(LittleNPCs.Framework.Patches.PFCHandleWarpsPatch.Prefix))
            );
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e) {
            var farmHouse = Utility.getHomeOfFarmer(Game1.player);
            var npcs = farmHouse.characters;
            // ATTENTION: We use a separate dictionary for dispositions.
            // Reason: If we add something to 'Data/NPCDispositions' the game attempts to create that NPC.
            // We must control NPC creation, however, so we add dispositions from a different dictionary to the created NPC.
            var litteNpcDispositions = Game1.content.Load<Dictionary<string, string>>("Data/LittleNPCDispositions");
            var npcDispositions = Game1.content.Load<Dictionary<string, string>>("Data/NPCDispositions");
            // Plain old for loop because we have to replace list elements.
            for (int i = 0; i < npcs.Count; ++i) {
                if (npcs[i] is Child child) {
                    var sprite = new AnimatedSprite($"Characters/{child.Name}", 0, 16, 32);
                    var portrait = Game1.content.Load<Texture2D>($"Portraits/{child.Name}");
                    var npc = new LittleNPC(this.Monitor,
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
                    npcs[i] = npc;

                    // Set dispositions now.
                    npcDispositions[npc.Name] = litteNpcDispositions[npc.Name];

                    // Reload schedule.
                    npc.Schedule = npc.getSchedule(Game1.dayOfMonth);

                    this.Monitor.Log($"Replaced child {child.Name} by LittleNPC", LogLevel.Warn);
                }
            }
        }

        public static long GetFarmerParentId(Character npc) {
            return (npc is LittleNPC littleNPC) ? littleNPC.IdOfParent : 0; 
        }
    }

}
