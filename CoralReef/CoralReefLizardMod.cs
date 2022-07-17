using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LizardCosmetics;
using On.DevInterface;
using BepInEx;
using RWCustom;
using UnityEngine;
using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Random = UnityEngine.Random;

namespace CoralReef {
    [BepInPlugin("lb-fgf-m4r-ik.coral-reef", "Coral Reef Custom Lizard & Custom Daddy", "0.2.0")]
    public class CoralReefLizardMod : BaseUnityPlugin {

        public static bool HasSandboxCore = false;
        public static AttachedField<YellowAI, CommunicationFlicker> commFlicker = new();

        public class CommunicationFlicker {
            public float lastFlicker;
            public float currentFlicker;
            public bool increase;
            public bool packLeader;
        }
        
        public void OnEnable() {
            On.RainWorld.Start += RainWorldOnStart;
        }

        private void RainWorldOnStart(On.RainWorld.orig_Start orig, RainWorld self) {
            orig(self);

            JellyLongLegs.ApplyHooks();

            /* Lizard Hooks */
            On.Lizard.ctor += LizardOnCtor;
            On.LizardBreeds.BreedTemplate += LizardBreedsOnBreedTemplate;
            On.LizardGraphics.ctor += LizardGraphicsOnCtor;
            On.LizardVoice.GetMyVoiceTrigger += LizardVoiceOnGetMyVoiceTrigger;
            On.LizardTongue.ctor += LizardTongueOnCtor;
            On.LizardCosmetics.AxolotlGills.DrawSprites += AxolotlGillsOnDrawSprites;

            /* Creature Hooks */
            MapPage.CreatureVis.CritCol += CreatureVisOnCritCol;
            MapPage.CreatureVis.CritString += CreatureVisOnCritString;
            On.CreatureTemplate.ctor += CreatureTemplateOnCtor;

            /* Custom Hooks */
            On.LizardAI.ctor += LizardAIOnCtor;
            On.YellowAI.Update += YellowAIOnUpdate;
            On.Lizard.SwimBehavior += LizardOnSwimBehavior;
            IL.Snail.Click += SnailILClick;
            On.Lizard.Update += LizardOnUpdate;
            On.LizardAI.TravelPreference += LizardAIOnTravelPreference;
            On.LizardAI.IdleSpotScore += LizardAIOnIdleSpotScore;
            On.LizardAI.ComfortableIdlePosition += LizardAIOnComfortableIdlePosition;
            On.LizardAI.LurkTracker.LurkPosScore += LurkTrackerOnLurkPosScore;
            On.LizardAI.LurkTracker.Utility += LurkTrackerOnUtility;
            IL.YellowAI.Update += YellowAIILUpdate;
            On.YellowAI.ctor += YellowAIOnCtor;
            On.YellowAI.YellowPack.FindLeader += YellowPackOnFindLeader;
            On.YellowAI.YellowPack.RemoveLizard_AbstractCreature += YellowPackOnRemoveLizard_AbstractCreature;
            On.YellowAI.YellowPack.RemoveLizard_int += YellowPackOnRemoveLizard_int;
            On.LizardGraphics.HeadColor += LizardGraphicsOnHeadColor;

            /* Sandbox Unlock */
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) {
                if (asm.FullName.Contains("SandboxCore") || asm.FullName.Contains("CreatureCore") || asm.FullName.Contains("SandboxUnlockCore"))
                    HasSandboxCore = true;
            }

            void LoadEmbeddedResource(string spriteName)
            {
                var ass = Assembly.GetExecutingAssembly();
                var resourceName = ass.GetManifestResourceNames().First(s => s.Contains(spriteName));
                var resource = ass.GetManifestResourceStream(resourceName) ?? Stream.Null;

                using MemoryStream memStream = new();
                var tempBuffer = new byte[16384]; //Some random number according to Garrakx
                int count;

                while ((count = resource.Read(tempBuffer, 0, tempBuffer.Length)) > 0)
                    memStream.Write(tempBuffer, 0, count);

                Texture2D texture2D = new(0, 0, TextureFormat.ARGB32, false);
                texture2D.LoadImage(memStream.ToArray());
                texture2D.anisoLevel = 1;
                texture2D.filterMode = 0;
                FAtlas atlas = new(spriteName, texture2D, FAtlasManager._nextAtlasIndex);
                Futile.atlasManager.AddAtlas(atlas);
                FAtlasManager._nextAtlasIndex++;
            }

            LoadEmbeddedResource("Kill_Polliwog");
            LoadEmbeddedResource("JellyLLGraf");
            LoadEmbeddedResource("JellyLLGrad");

            if (HasSandboxCore) {
                
                SandboxUnlockCore.Main.creatures.Add(EnumExt_CoralReef.PolliwogUnlock);
                SandboxUnlockCore.Main.killScores.Add(EnumExt_CoralReef.PolliwogUnlock, 5);
                
                On.MultiplayerUnlocks.SandboxUnlockForSymbolData += MultiplayerUnlocksOnSandboxUnlockForSymbolData;
                On.MultiplayerUnlocks.SymbolDataForSandboxUnlock += MultiplayerUnlocksOnSymbolDataForSandboxUnlock;
                On.CreatureSymbol.ColorOfCreature += CreatureSymbolOnColorOfCreature;
                On.CreatureSymbol.SpriteNameOfCreature += CreatureSymbolOnSpriteNameOfCreature;
                On.MultiplayerUnlocks.SandboxItemUnlocked += MultiplayerUnlocksOnSandboxItemUnlocked;
            }

            /* Static World Patch */
            StaticWorldPatch.ApplyPatch();
        }

        private Color LizardGraphicsOnHeadColor(On.LizardGraphics.orig_HeadColor orig, LizardGraphics self, float timeStacker) {
            var color = orig(self, timeStacker);
           
            if (self.lizard is Lizard l && l.AI?.yellowAI is YellowAI y && l.Template.type == EnumExt_CoralReef.Polliwog && commFlicker.TryGet(y, out var c) && c.packLeader) {
                var flicker = Mathf.Lerp(c.lastFlicker, c.currentFlicker, timeStacker);

                if (!l.Consious)
                    flicker = 0f;

                color = Color.Lerp(color, new(1f, .007843137254902f, .3529411764705882f), flicker);
            }
            
            return color;
        }

        private void YellowPackOnRemoveLizard_int(On.YellowAI.YellowPack.orig_RemoveLizard_int orig, YellowAI.YellowPack self, int index) {
            if (self.members[index]?.lizard?.realizedCreature is Lizard l && l.AI?.yellowAI is YellowAI y && l.Template.type == EnumExt_CoralReef.Polliwog && commFlicker.TryGet(y, out var c) && c.packLeader)
                c.packLeader = false;
            orig(self, index);
        }

        private void YellowPackOnRemoveLizard_AbstractCreature(On.YellowAI.YellowPack.orig_RemoveLizard_AbstractCreature orig, YellowAI.YellowPack self, AbstractCreature removeLizard) {
            for (var num = self.members.Count - 1; num >= 0; num--) {
                if (self.members[num]?.lizard == removeLizard && removeLizard?.realizedCreature is Lizard l && l.AI?.yellowAI is YellowAI y && l.Template.type == EnumExt_CoralReef.Polliwog && commFlicker.TryGet(y, out var c) && c.packLeader)
                    c.packLeader = false;
            }
            orig(self, removeLizard);
        }

        private void YellowPackOnFindLeader(On.YellowAI.YellowPack.orig_FindLeader orig, YellowAI.YellowPack self) {
            orig(self);
            for (var i = 0; i < self.members.Count; i++) {
                if (self.members[i]?.lizard?.realizedCreature is Lizard l && l.AI?.yellowAI is YellowAI y && l.Template.type == EnumExt_CoralReef.Polliwog && commFlicker.TryGet(y, out var c)) {
                    if (self.members[i].role is YellowAI.YellowPack.Role.Leader)
                        c.packLeader = true;
                    else
                        c.packLeader = false;
                }
            }
        }

        private void YellowAIOnCtor(On.YellowAI.orig_ctor orig, YellowAI self, ArtificialIntelligence AI) {
            orig(self, AI);
            commFlicker[self] = new();
        }

        private void YellowAIILUpdate(ILContext il) {
            ILCursor c = new(il);

            if (c.TryGotoNext(MoveType.After,
               x => x.MatchCall<Mathf>("Max"),
               x => x.MatchStfld<YellowAI>("commFlicker"))) {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Action<YellowAI>>((YellowAI self) => {
                    if (self.lizard is Lizard l && l.Template.type == EnumExt_CoralReef.Polliwog && commFlicker.TryGet(self, out var co)) {
                        co.lastFlicker = co.currentFlicker;
                        co.currentFlicker = Mathf.Clamp(co.increase ? co.currentFlicker + 0.25f : co.currentFlicker - 0.2f, -0.5f, 1f);
                        if (co.currentFlicker >= 1f || !l.Consious)
                            co.increase = false;
                        else if (self.communicating > 0 && co.currentFlicker <= -0.5f)
                            co.increase = true;
                    }
                });
            }
            else
                Logger.LogError("Couldn't ILHook YellowAI.Update!");
        }

        private void SnailILClick(ILContext il) {
            ILCursor c = new(il);
            var loc = -1;
            ILLabel beq = null;

            if (c.TryGotoNext(MoveType.After, 
                x => x.MatchBr(out _), 
                x => x.MatchLdloca(out _), 
                x => x.MatchCall(out _), 
                x => x.MatchStloc(out _), 
                x => x.MatchLdloc(out loc), 
                x => x.MatchLdarg(0),
                x => x.MatchBeq(out beq))
                && loc != -1 && beq != null) {

                c.Emit(OpCodes.Ldloc, il.Body.Variables[loc]);
                c.EmitDelegate<Func<PhysicalObject, bool>>((PhysicalObject self)
                    => self is Creature cr && cr.Template.type == EnumExt_CoralReef.Polliwog);
                c.Emit(OpCodes.Brtrue, beq);
            }
            else  
                Logger.LogError("Couldn't ILHook Snail.Click!");
        }

        private void AxolotlGillsOnDrawSprites(On.LizardCosmetics.AxolotlGills.orig_DrawSprites orig, AxolotlGills self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos) {
            orig(self, sLeaser, rCam, timeStacker, camPos);

            if (self.lGraphics?.lizard is Lizard l && l.AI?.yellowAI is YellowAI y && l.Template.type == EnumExt_CoralReef.Polliwog && commFlicker.TryGet(y, out var c)) {
                var flicker = Mathf.Lerp(c.lastFlicker, c.currentFlicker, timeStacker);
                
                if (!l.Consious)
                    flicker = 0f;

                for (var num = self.startSprite + self.scalesPositions.Length - 1; num >= self.startSprite; num--) {
                    sLeaser.sprites[num].color = Color.Lerp(self.lGraphics.HeadColor(timeStacker), Color.Lerp(self.lGraphics.HeadColor(timeStacker), self.lGraphics.effectColor, 0.6f), flicker);
                    
                    if (self.colored)
                        sLeaser.sprites[num + self.scalesPositions.Length].color = c.packLeader ? self.lGraphics.HeadColor(timeStacker) : Color.Lerp(self.lGraphics.HeadColor(timeStacker), new(1f, .007843137254902f, .3529411764705882f), flicker);
                }
            }
        }

        private void LizardTongueOnCtor(On.LizardTongue.orig_ctor orig, LizardTongue self, Lizard lizard) {
            orig(self, lizard);
            if (lizard.Template.type == EnumExt_CoralReef.Polliwog) {
                self.range = 140f;
                self.lashOutSpeed = 16f;
                self.reelInSpeed = 0.000625f;
                self.baseDrag = 0.01f;
                self.dragElasticity = 0.1f;
                self.emptyElasticity = 0.8f;
                self.involuntaryReleaseChance = 1f / 400f;
                self.voluntaryReleaseChance = 0.0125f;

                /*FieldInfo elasticRange = typeof(LizardTongue).GetField(nameof(LizardTongue.elasticRange), BindingFlags.NonPublic | BindingFlags.Instance);
                elasticRange?.SetValue(self, 0.55f);*/ //stubbed asm that removes readonly
                self.elasticRange = 0.55f;

                /*FieldInfo totR = typeof(LizardTongue).GetField(nameof(LizardTongue.totR), BindingFlags.NonPublic | BindingFlags.Instance);
                totR?.SetValue(self, self.range * 1.1f);*/ //stubbed asm that removes readonly
                self.totR = self.range * 1.1f;
            }
        }

        private bool MultiplayerUnlocksOnSandboxItemUnlocked(On.MultiplayerUnlocks.orig_SandboxItemUnlocked orig, MultiplayerUnlocks self, MultiplayerUnlocks.SandboxUnlockID unlockid) {
            return orig(self, unlockid) || unlockid == EnumExt_CoralReef.PolliwogUnlock;
        }

        private string CreatureSymbolOnSpriteNameOfCreature(On.CreatureSymbol.orig_SpriteNameOfCreature orig, IconSymbol.IconSymbolData icondata) {
            return icondata.critType == EnumExt_CoralReef.Polliwog
                ? "Kill_Polliwog"
                : orig(icondata);
        }

        private Color CreatureSymbolOnColorOfCreature(On.CreatureSymbol.orig_ColorOfCreature orig, IconSymbol.IconSymbolData icondata) {
            return icondata.critType == EnumExt_CoralReef.Polliwog
                ? new Color(0.38f, 0.259f, 0.741f)
                : orig(icondata);
        }

        private IconSymbol.IconSymbolData MultiplayerUnlocksOnSymbolDataForSandboxUnlock(On.MultiplayerUnlocks.orig_SymbolDataForSandboxUnlock orig, MultiplayerUnlocks.SandboxUnlockID unlockid) {
            return unlockid == EnumExt_CoralReef.PolliwogUnlock 
                ? new IconSymbol.IconSymbolData(EnumExt_CoralReef.Polliwog, AbstractPhysicalObject.AbstractObjectType.Creature, 0) 
                : orig(unlockid);
        }

        private MultiplayerUnlocks.SandboxUnlockID MultiplayerUnlocksOnSandboxUnlockForSymbolData(On.MultiplayerUnlocks.orig_SandboxUnlockForSymbolData orig, IconSymbol.IconSymbolData data) {
            return data.critType == EnumExt_CoralReef.Polliwog ? EnumExt_CoralReef.PolliwogUnlock : orig(data);
        }

        private float LurkTrackerOnUtility(On.LizardAI.LurkTracker.orig_Utility orig, LizardAI.LurkTracker self) {
            var result = orig(self);
            // Copied code from Salamander
            if (self.lizard.Template.type == EnumExt_CoralReef.Polliwog && self.LurkPosScore(self.lurkPosition) <= 0) return 0f;
            return result;
        }

        private float LurkTrackerOnLurkPosScore(On.LizardAI.LurkTracker.orig_LurkPosScore orig, LizardAI.LurkTracker self, WorldCoordinate testLurkPos) {
            var result = orig(self, testLurkPos);
            if (self.lizard.Template.type == EnumExt_CoralReef.Polliwog) {
                // Copied code from Salamander
                if (!self.lizard.room.aimap.TileAccessibleToCreature(testLurkPos.Tile, self.lizard.Template) || self.lizard.room.GetTile(testLurkPos).Terrain == Room.Tile.TerrainType.Slope || testLurkPos.room != self.lizard.abstractCreature.pos.room || !self.AI.pathFinder.CoordinateReachable(testLurkPos) || !self.AI.pathFinder.CoordinatePossibleToGetBackFrom(testLurkPos))
                    return -100000f;
                if (self.lizard.room.aimap.getAItile(testLurkPos).terrainProximity > 2 || self.lizard.room.aimap.getAItile(testLurkPos).acc != AItile.Accessibility.Floor && !self.lizard.room.GetTile(testLurkPos).DeepWater)
                    return -100000f;
                float num2 = 40f / Mathf.Max(1f, Mathf.Abs(self.lizard.room.defaultWaterLevel - testLurkPos.y) - 5);
                var num1 = testLurkPos.y >= self.lizard.room.defaultWaterLevel ? num2 / 5f : num2 * 5f;
                if (testLurkPos.y < self.lizard.room.defaultWaterLevel - 10)
                    num1 -= Custom.LerpMap(testLurkPos.y, self.lizard.room.defaultWaterLevel - 10, self.lizard.room.defaultWaterLevel - 40, 0.0f, 100f);
                if (self.lizard.room.aimap.getAItile(testLurkPos).acc == AItile.Accessibility.Floor)
                    num1 += 20f;

                int visibility = self.lizard.room.aimap.getAItile(testLurkPos).visibility;
                float num3 = num1 - visibility / 1000f;
                for (int index = 0; index < 8; ++index) {
                    if (self.lizard.room.VisualContact(testLurkPos.Tile, testLurkPos.Tile + Custom.eightDirections[index] * 10))
                        num3 += self.lizard.room.aimap.getAItile(testLurkPos.Tile + Custom.eightDirections[index] * 10).visibility / 8000f;
                }

                if (self.lizard.room.aimap.getAItile(testLurkPos).narrowSpace)
                    num3 -= 10000f;
                for (int index = 0; index < self.AI.tracker.CreaturesCount; ++index) {
                    if (self.AI.tracker.GetRep(index).BestGuessForPosition().room == testLurkPos.room && !self.AI.tracker.GetRep(index).representedCreature.creatureTemplate.smallCreature && self.AI.tracker.GetRep(index).dynamicRelationship.currentRelationship.type != CreatureTemplate.Relationship.Type.Eats && self.AI.tracker.GetRep(index).BestGuessForPosition().Tile.FloatDist(testLurkPos.Tile) < 20.0 && self.AI.tracker.GetRep(index).representedCreature.creatureTemplate.bodySize >= self.lizard.Template.bodySize * 0.800000011920929)
                        num3 += self.AI.tracker.GetRep(index).BestGuessForPosition().Tile.FloatDist(testLurkPos.Tile) / 10f;
                }

                return num3;
            }

            return result;
        }

        private bool LizardAIOnComfortableIdlePosition(On.LizardAI.orig_ComfortableIdlePosition orig, LizardAI self) {
            var result = orig(self);
            // Copied code from Salamander
            return result || (self.lizard.room.GetTile(self.lizard.bodyChunks[0].pos).AnyWater && self.lizard.Template.type == EnumExt_CoralReef.Polliwog);
        }

        private float LizardAIOnIdleSpotScore(On.LizardAI.orig_IdleSpotScore orig, LizardAI self, WorldCoordinate coord) {
            var result = orig(self, coord);
            if (self.lizard.Template.type == EnumExt_CoralReef.Polliwog) {
                // Copied code from Salamander
                if (!self.lizard.room.GetTile(coord).AnyWater) result += 20f;
                result += Mathf.Max(
                    0.0f,
                    coord.Tile.FloatDist(self.creature.pos.Tile) - 30f) * 1.5f + Mathf.Abs(coord.y - self.lizard.room.defaultWaterLevel) * 10f + self.lizard.room.aimap.getAItile(coord).terrainProximity * 10f;
            }

            return result;
        }

        private PathCost LizardAIOnTravelPreference(On.LizardAI.orig_TravelPreference orig, LizardAI self, MovementConnection connection, PathCost cost) {
            var result = orig(self, connection, cost);
            if (self.lizard.Template.type == EnumExt_CoralReef.Polliwog) {
                // Copied code from Salamander
                if (!self.lizard.room.GetTile(connection.destinationCoord).AnyWater) result.resistance += 5f;
            }

            return result;
        }

        private void LizardOnUpdate(On.Lizard.orig_Update orig, Lizard self, bool eu) {
            orig(self, eu);
            // Makes Polliwogs have infinite breath
            if (self.Template.type == EnumExt_CoralReef.Polliwog) self.lungs = 1f;
        }

        private void LizardOnSwimBehavior(On.Lizard.orig_SwimBehavior orig, Lizard self) {
            if (self.Template.type == EnumExt_CoralReef.Polliwog) {
                // Copied code from Salamander
                self.swim = Mathf.Clamp(self.swim + 0.06666667f, 0.0f, 1f);
                self.desperationSmoother = Mathf.Lerp(self.desperationSmoother, 30f, 0.1f);
                if (self.bodyChunks[0].submersion > 0.0 && self.bodyChunks[0].submersion < 1.0 && (self.followingConnection == null || self.followingConnection.DestTile.y == self.room.defaultWaterLevel)) {
                    self.bodyChunks[0].vel.y *= 0.8f;
                    self.bodyChunks[0].vel.y += Mathf.Clamp(self.room.FloatWaterLevel(self.bodyChunks[0].pos.x) - self.bodyChunks[0].pos.y, -10f, 10f) * 0.1f;
                }

                WorldCoordinate worldCoordinate = self.room.GetWorldCoordinate(self.mainBodyChunk.pos);
                if (self.AI.behavior == LizardAI.Behavior.Lurk && Custom.ManhattanDistance(self.AI.pathFinder.GetDestination, self.room.GetWorldCoordinate(self.bodyChunks[2].pos)) < 3) {
                    self.bodyChunks[2].vel += Vector2.ClampMagnitude(self.room.MiddleOfTile(self.AI.pathFinder.GetDestination) - self.bodyChunks[2].pos, 10f) / 7f;
                    self.bodyChunks[0].vel += Custom.DirVec(self.bodyChunks[2].pos, self.room.MiddleOfTile(self.AI.lurkTracker.lookPosition)) * 0.2f;
                    self.salamanderLurk = true;
                }
                else {
                    self.salamanderLurk = false;
                    Vector2 vector2 = new(0.0f, 0.0f);
                    if (!self.AI.pathFinder.GetDestination.NodeDefined && self.AI.pathFinder.GetDestination.room == self.room.abstractRoom.index && self.room.GetTile(self.AI.pathFinder.GetDestination).AnyWater && self.room.VisualContact(self.mainBodyChunk.pos, self.room.MiddleOfTile(self.AI.pathFinder.GetDestination))) {
                        vector2 = Custom.DirVec(self.mainBodyChunk.pos, self.room.MiddleOfTile(self.AI.pathFinder.GetDestination));
                    }
                    else {
                        if (self.followingConnection == null)
                            self.followingConnection = ((LizardPather)self.AI.pathFinder).FollowPath(self.room.GetWorldCoordinate(self.mainBodyChunk.pos), new int?(), true);
                        if (self.followingConnection != null)
                            vector2 = Custom.DirVec(self.mainBodyChunk.pos, self.room.MiddleOfTile(self.followingConnection.destinationCoord));
                    }

                    self.mainBodyChunk.vel += vector2 * 1.3f * Mathf.Lerp(0.5f, 1f, self.AI.runSpeed) * (!self.room.GetTile(self.mainBodyChunk.pos).WaterSurface ? 1f : 0.7f);
                    self.bodyChunks[1].vel -= vector2 * 0.3f * Mathf.Lerp(0.5f, 1f, self.AI.runSpeed) * (!self.room.GetTile(self.mainBodyChunk.pos).WaterSurface ? 1f : 0.7f);
                }

                if (self.followingConnection != null) {
                    if (worldCoordinate.x != self.followingConnection.StartTile.x && worldCoordinate.x != self.followingConnection.DestTile.x)
                        self.followingConnection = null;
                    else if (worldCoordinate.Tile == self.followingConnection.DestTile)
                        self.followingConnection = null;
                }

                if (self.followingConnection == null || !self.followingConnection.destinationCoord.TileDefined || self.room.GetTile(self.followingConnection.DestTile).AnyWater)
                    return;
                self.inAllowedTerrainCounter = self.lizardParams.regainFootingCounter + 2;
                self.swim = 0.0f;
                if (!self.followingConnection.destinationCoord.TileDefined || !self.room.aimap.TileAccessibleToCreature(new IntVector2(self.followingConnection.DestTile.x, self.followingConnection.DestTile.y + 1), self.Template))
                    return;
                self.movementAnimation = new Lizard.MovementAnimation(self, new MovementConnection(MovementConnection.MovementType.ReachUp, self.followingConnection.startCoord, new WorldCoordinate(self.followingConnection.destinationCoord.room, self.followingConnection.DestTile.x, self.followingConnection.DestTile.y + 1, -1), 2), 1, 0.99f, 0.75f);
            }
            else {
                orig(self);
            }
        }

        private void YellowAIOnUpdate(On.YellowAI.orig_Update orig, YellowAI self) {
            orig(self);
            if (self.lizard.Template.type != EnumExt_CoralReef.Polliwog) return;
            // Copied code from Yellow Lizard
            foreach (AbstractCreature creature in self.lizard.room.abstractRoom.creatures) {
                if (creature.creatureTemplate.type == EnumExt_CoralReef.Polliwog && creature.realizedCreature != null && creature.realizedCreature.Consious && creature != self.AI.creature) {
                    self.ConsiderOtherYellowLizard(creature);
                }
            }
        }

        private void LizardAIOnCtor(On.LizardAI.orig_ctor orig, LizardAI self, AbstractCreature creature, World world) {
            orig(self, creature, world);
            if (self.lizard.Template.type == EnumExt_CoralReef.Polliwog) {
                // Adds both Yellow AI and Lurk Tracker. May conflict?
                self.yellowAI = new YellowAI(self);
                self.AddModule(self.yellowAI);
                self.lurkTracker = new LizardAI.LurkTracker(self, self.lizard);
                self.AddModule(self.lurkTracker);
                self.utilityComparer.AddComparedModule(self.lurkTracker, null, Mathf.Lerp(0.4f, 0.3f, creature.personality.energy), 1f);
            }
        }

        private void CreatureTemplateOnCtor(On.CreatureTemplate.orig_ctor orig, CreatureTemplate self, CreatureTemplate.Type type, CreatureTemplate ancestor, List<TileTypeResistance> tileresistances, List<TileConnectionResistance> connectionresistances, CreatureTemplate.Relationship defaultrelationship) {
            orig(self, type, ancestor, tileresistances, connectionresistances, defaultrelationship);
            if (type == EnumExt_CoralReef.Polliwog) self.name = "Polliwog";
        }

        private string CreatureVisOnCritString(MapPage.CreatureVis.orig_CritString orig, AbstractCreature crit) {
            var result = orig(crit);
            return crit.creatureTemplate.type == EnumExt_CoralReef.Polliwog ? "Polliwog" : result;
        }

        private Color CreatureVisOnCritCol(MapPage.CreatureVis.orig_CritCol orig, AbstractCreature crit) {
            var result = orig(crit);
            return crit.creatureTemplate.type == EnumExt_CoralReef.Polliwog ? new Color(0.38f, 0.259f, 0.741f) : result;
        }

        private SoundID LizardVoiceOnGetMyVoiceTrigger(On.LizardVoice.orig_GetMyVoiceTrigger orig, LizardVoice self) {
            if (self.lizard.Template.type == EnumExt_CoralReef.Polliwog) {
                string str = "Blue"; // Lizard voice

                string[] array = { "A", "B", "C", "D", "E" };
                List<SoundID> list = new();
                for (int i = 0; i < 5; i++) {
                    SoundID soundID;
                    try {
                        soundID = Custom.ParseEnum<SoundID>("Lizard_Voice_" + str + "_" + array[i]);
                    }
                    catch {
                        soundID = SoundID.None;
                    }

                    if (self.lizard.abstractCreature.world.game.soundLoader.workingTriggers[(int)soundID]) {
                        list.Add(soundID);
                    }
                }

                return list.Count == 0 ? SoundID.None : list[Random.Range(0, list.Count)];
            }

            return orig(self);
        }

        private void LizardGraphicsOnCtor(On.LizardGraphics.orig_ctor orig, LizardGraphics self, PhysicalObject ow) {
            orig(self, ow);
            if (self.lizard.Template.type != EnumExt_CoralReef.Polliwog) return;
            int seed = Random.seed;
            Random.seed = self.lizard.abstractCreature.ID.RandomSeed;
            var spriteIndex = self.startOfExtraSprites + self.extraSprites;

            spriteIndex = self.AddCosmetic(spriteIndex, new AxolotlGills(self, spriteIndex));
            spriteIndex = self.AddCosmetic(spriteIndex, new TailFin(self, spriteIndex));

            Random.seed = seed;
        }

        private CreatureTemplate LizardBreedsOnBreedTemplate(On.LizardBreeds.orig_BreedTemplate orig, CreatureTemplate.Type type, CreatureTemplate lizardancestor, CreatureTemplate pinktemplate, CreatureTemplate bluetemplate, CreatureTemplate greentemplate) {
            if (type == EnumExt_CoralReef.Polliwog) {
                var template = orig(CreatureTemplate.Type.Salamander, lizardancestor, pinktemplate, bluetemplate, greentemplate);
                var breedParams = (LizardBreedParams)template.breedParameters;

                breedParams.terrainSpeeds[3] = new LizardBreedParams.SpeedMultiplier(1.1f, 1f, 1f, 1f); // Climb speeds
                breedParams.bodySizeFac = .7f;
                breedParams.headSize = 1f;
                breedParams.limbSize = .7f;
                breedParams.tongue = true;
                breedParams.toughness = .5f;

                template.type = type;
                template.baseDamageResistance = .5f;
                template.meatPoints = 5;

                return template;
            }

            return orig(type, lizardancestor, pinktemplate, bluetemplate, greentemplate);
        }

        private void LizardOnCtor(On.Lizard.orig_ctor orig, Lizard self, AbstractCreature abstractcreature, World world) {
            orig(self, abstractcreature, world);
            int seed = Random.seed;
            Random.seed = abstractcreature.ID.RandomSeed;
            if (self.Template.type == EnumExt_CoralReef.Polliwog) {
                self.tongue = new LizardTongue(self);
                self.effectColor = Custom.HSL2RGB(Custom.WrappedRandomVariation(0.708f, 0.1f, 0.6f), 0.482f, Custom.ClampedRandomVariation(0.5f, 0.15f, 0.1f));
            }
            Random.seed = seed;
        }
    }
}