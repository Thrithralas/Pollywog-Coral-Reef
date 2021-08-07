using System.Collections.Generic;
using System.Linq;
using LizardCosmetics;
using On.DevInterface;
using Partiality.Modloader;
using RWCustom;
using UnityEngine;

namespace CoralReef {
    public class CoralReefLizardMod : PartialityMod {
        public CoralReefLizardMod() {
            Version = "0.1";
            author = "Thrith";
            ModID = "Coral Reef Custom Lizard";
        }

        public override void OnEnable() {
            On.RainWorld.Start += RainWorldOnStart;
        }

        private void RainWorldOnStart(On.RainWorld.orig_Start orig, RainWorld self) {
            orig(self);

            /* Lizard Hooks */
            On.Lizard.ctor += LizardOnCtor;
            On.LizardBreeds.BreedTemplate += LizardBreedsOnBreedTemplate;
            On.LizardGraphics.ctor += LizardGraphicsOnCtor;
            On.LizardVoice.GetMyVoiceTrigger += LizardVoiceOnGetMyVoiceTrigger;

            /* Creature Hooks */
            MapPage.CreatureVis.CritCol += CreatureVisOnCritCol;
            MapPage.CreatureVis.CritString += CreatureVisOnCritString;
            On.CreatureTemplate.ctor += CreatureTemplateOnCtor;

            /* Custom Hooks */
            On.LizardAI.ctor += LizardAIOnCtor;
            On.YellowAI.Update += YellowAIOnUpdate;
            On.Lizard.SwimBehavior += LizardOnSwimBehavior;
            On.Snail.Click += SnailOnClick;
            On.Lizard.Update += LizardOnUpdate;
            On.LizardAI.TravelPreference += LizardAIOnTravelPreference;
            On.LizardAI.IdleSpotScore += LizardAIOnIdleSpotScore;
            On.LizardAI.ComfortableIdlePosition += LizardAIOnComfortableIdlePosition;
            On.LizardAI.LurkTracker.LurkPosScore += LurkTrackerOnLurkPosScore;
            On.LizardAI.LurkTracker.Utility += LurkTrackerOnUtility;

            /* Static World Patch */
            StaticWorldPatch.ApplyPatch();
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

        private void SnailOnClick(On.Snail.orig_Click orig, Snail self) {
            //Lets hope nobody hooks self function hooks On.Snail.Click beside me ;-; Curse you IL Editing
            if (self.triggerTicker > 0)
                return;
            if (self.room.BeingViewed) {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (self.bodyChunks[1].submersion == 1.0) {
                    self.room.AddObject(new ShockWave(self.bodyChunks[1].pos, 160f * self.size, 0.07f, 9));
                }
                else {
                    self.room.AddObject(new ShockWave(self.bodyChunks[1].pos, 100f * self.size, 0.07f, 6));
                    for (int index = 0; index < 10; ++index)
                        self.room.AddObject(new WaterDrip(self.bodyChunks[1].pos, Custom.DegToVec(Random.value * 360f) * Mathf.Lerp(4f, 21f, Random.value), false));
                }
            }

            self.Stun(60);
            self.clickCounter = 0.0f;
            self.room.PlaySound(SoundID.Snail_Pop, self.mainBodyChunk);
            float num1 = 60f * self.size;
            foreach (var pList in self.room.physicalObjects) {
                foreach (var current in pList.Where(current => current != self)) {
                    if (current is Creature creature && creature.Template.type == EnumExt_CoralReef.Polliwog) {
                        continue;
                    }

                    foreach (BodyChunk bodyChunk in current.bodyChunks) {
                        float num2 = (float)(1.0 + bodyChunk.submersion * (double)self.bodyChunks[1].submersion * 4.5);
                        if (Custom.DistLess(bodyChunk.pos, self.bodyChunks[1].pos, num1 * num2 + bodyChunk.rad + self.bodyChunks[1].rad) && self.room.VisualContact(bodyChunk.pos, self.bodyChunks[1].pos)) {
                            float num3 = Mathf.InverseLerp(num1 * num2 + bodyChunk.rad + self.bodyChunks[1].rad, (float)((num1 * (double)num2 + bodyChunk.rad + self.bodyChunks[1].rad) / 2.0), Vector2.Distance(bodyChunk.pos, self.bodyChunks[1].pos));
                            bodyChunk.vel += Custom.DirVec(self.bodyChunks[1].pos + new Vector2(0.0f, !self.IsTileSolid(1, 0, -1) ? 0.0f : -20f), bodyChunk.pos) * num3 * num2 * 3f / bodyChunk.mass;
                            if (current is Creature creature2)
                                creature2.Stun((int)(60.0 * num3));
                            if (current is Leech leech) {
                                if (Random.value < 0.0333333350718021 || Custom.DistLess(self.bodyChunks[1].pos, bodyChunk.pos, (float)(self.bodyChunks[1].rad + (double)bodyChunk.rad + 5.0)))
                                    leech.Die();
                                else
                                    leech.Stun((int)(num3 * (double)bodyChunk.submersion * Mathf.Lerp(800f, 900f, Random.value)));
                            }
                        }
                    }
                }
            }

            if (self.room.waterObject != null) {
                float num4 = (float)(1.0 + self.bodyChunks[1].submersion * 1.5);
                self.room.waterObject.Explosion(self.bodyChunks[1].pos, (float)(num1 * (double)num4 * 1.20000004768372), num4 * 3f);
            }

            self.suckPoint = new Vector2?();
            for (int bChunk = 0; bChunk < 2; ++bChunk) {
                if (self.IsTileSolid(bChunk, 0, -1))
                    self.bodyChunks[bChunk].vel += Custom.DegToVec((float)(100.0 * Random.value - 50.0)) * 10f;
                else
                    self.bodyChunks[bChunk].vel += Custom.DegToVec(Random.value * 360f) * 10f;
            }

            self.VibrateLeeches(1000f);
            self.justClicked = true;
            self.bloated = true;
            self.triggered = false;
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
                    Vector2 vector2 = new Vector2(0.0f, 0.0f);
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
                List<SoundID> list = new List<SoundID>();
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

                return template;
            }

            return orig(type, lizardancestor, pinktemplate, bluetemplate, greentemplate);
        }

        private void LizardOnCtor(On.Lizard.orig_ctor orig, Lizard self, AbstractCreature abstractcreature, World world) {
            orig(self, abstractcreature, world);
            if (self.Template.type == EnumExt_CoralReef.Polliwog) {
                self.effectColor = Custom.HSL2RGB(Custom.WrappedRandomVariation(0.708f, 0.1f, 0.6f), 0.482f, Custom.ClampedRandomVariation(0.5f, 0.15f, 0.1f));
            }
        }
    }
}