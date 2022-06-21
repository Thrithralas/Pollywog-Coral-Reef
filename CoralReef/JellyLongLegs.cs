using System.Reflection;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;
using MonoMod.RuntimeDetour;

namespace CoralReef {
    internal sealed class JellyLongLegs {
        internal static void ApplyHooks() {
            /* Jelly Long Legs */
            On.DaddyLongLegs.ctor += DaddyLongLegsOnCtor;
            On.DaddyLongLegs.InitiateGraphicsModule += DaddyLongLegsOnInitiateGraphicsModule;
            On.DaddyGraphics.RenderSlits += DaddyGraphicsOnRenderSlits;
            // Don't change the first string to nameof because it would return only "UpdateDynamicRelationship"
            new Hook(
                typeof(DaddyAI).GetMethod("IUseARelationshipTracker.UpdateDynamicRelationship", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance),
                typeof(JellyLongLegs).GetMethod(nameof(DaddyAIHookUpdateDynamicRelationship), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static));
            On.DaddyGraphics.ReactToNoise += DaddyGraphicsOnReactToNoise;
            On.DaddyTentacle.CollideWithCreature += DaddyTentacleOnCollideWithCreature;

            /* For tentacle plants to be properly colored in the coral caves arenas */
            On.TentaclePlantGraphics.ApplyPalette += TentaclePlantGraphicsOnApplyPalette;
        }

        private static void DaddyTentacleOnCollideWithCreature(On.DaddyTentacle.orig_CollideWithCreature orig, DaddyTentacle self, int tChunk, BodyChunk creatureChunk) {
            if ((self.daddy?.room?.world.region?.name == "RF" || self.daddy?.room?.abstractRoom.name is "Reef" or "Pump" or "Cavity") && creatureChunk?.owner != null && creatureChunk.owner is DaddyLongLegs) 
                return;

            orig(self, tChunk, creatureChunk);
        }

        private static void DaddyGraphicsOnReactToNoise(On.DaddyGraphics.orig_ReactToNoise orig, DaddyGraphics self, NoiseTracker.TheorizedSource source, Noise.InGameNoise noise) {
            if (self.daddy.SizeClass && (self.daddy?.room?.world.region?.name == "RF" || self.daddy?.room?.abstractRoom.name is "Reef" or "Pump" or "Cavity")) {
                Vector2 middleOfBody = self.daddy.MiddleOfBody;
                float num4 = 0.5f;

                if (self.daddy.AI.behavior == DaddyAI.Behavior.Hunt && self.daddy.AI.preyTracker.MostAttractivePrey != null)
                    num4 = (self.daddy.AI.preyTracker.MostAttractivePrey != source.creatureRep) ? 0.2f : 1f;
                else if (self.daddy.AI.behavior == DaddyAI.Behavior.ExamineSound && self.daddy.AI.noiseTracker.soundToExamine != null)
                    num4 = (self.daddy.AI.noiseTracker.soundToExamine != source) ? 0.2f : 1f;

                num4 = Mathf.Clamp01((num4 + Mathf.InverseLerp(150f, 400f, noise.strength) + num4 * noise.interesting) / 3f);

                if (source.creatureRep != null && source.creatureRep.VisualContact)
                    num4 *= 0.1f;

                num4 = Mathf.Max(num4, Mathf.InverseLerp(1f, 4f, noise.interesting));

                if (num4 * 40f > self.reactionSoundDelay) {
                    float num5 = (num4 * 3f + Mathf.InverseLerp(4f, 16f, self.daddy.firstChunk.rad) + Mathf.InverseLerp(1200f, 800f, Vector2.Distance(middleOfBody, noise.pos)) + Random.value) / 6f;

                    for (int j = 0; j < (int)Mathf.Lerp(2f, 9f, num5); j++)
                        self.daddy.room.AddObject(new DaddyBubble(self as JellyGraphics, Custom.DirVec(self.daddy.firstChunk.pos, noise.pos) * 12f / (1f + j * 0.2f), 1f, Random.value, 0f));

                    self.daddy.room.AddObject(new DaddyRipple(self as JellyGraphics, noise.pos, default, num5, self.daddy.eyeColor));
                    self.owner.room.PlaySound(SoundID.Daddy_React_To_Noise, middleOfBody);
                    self.reactionSoundDelay = System.Math.Max(self.reactionSoundDelay, Random.Range(10, 40));
                }
            }
            else
                orig(self, source, noise);
        }

        private static void TentaclePlantGraphicsOnApplyPalette(On.TentaclePlantGraphics.orig_ApplyPalette orig, TentaclePlantGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) {
            orig(self, sLeaser, rCam, palette);

            if (self.plant?.room?.abstractRoom.name is "Reef" or "Pump" or "Cavity") {
                for (int i = 0; i < self.danglers.Length; i++)
                    sLeaser.sprites[i + 1].color = Color.Lerp(new Color32(10, 84, 66, 255), palette.blackColor, rCam.room.Darkness(self.plant.rootPos));
            }
        }

        private static void DaddyLongLegsOnInitiateGraphicsModule(On.DaddyLongLegs.orig_InitiateGraphicsModule orig, DaddyLongLegs self) {
            orig(self);

            if (self.room?.world.region?.name == "RF" || self.room?.abstractRoom.name is "Reef" or "Pump" or "Cavity")
                self.graphicsModule = new JellyGraphics(self);
        }

        public class JellyGraphics : DaddyGraphics, DaddyGraphics.DaddyBubbleOwner {
            public JellyGraphics(PhysicalObject ow) : base(ow) { }

            float consious;
            float lerper;
            bool lerpUp = true;
            float lastRot;
            float rot;
            float lastAlpha;
            float alpha;

            public override void Update() {
                base.Update();

                consious = Mathf.Clamp01(consious + (daddy != null && !daddy.Consious ? 0.02f : -0.0075f));
                lerper = Mathf.Clamp(lerper + (lerpUp ? 0.0075f : -0.0075f), -0.75f, 1f);

                if (lerper == 1f)
                    lerpUp = false;
                else if (lerper == -0.75f)
                    lerpUp = true;

                lastAlpha = alpha;
                alpha = Mathf.Lerp(1f, 0f, Mathf.Max(consious, lerper, digesting / 2f));
                lastRot = rot;
                rot += daddy.Consious ? Mathf.Max(lerper / 3.8f + 0.35f, digesting * 32f) * 1.0001f : 0f;
            }

            public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam) {
                base.InitiateSprites(sLeaser, rCam);

                sLeaser.sprites[BodySprite(0)] = new("JellyLLGraf") {
                    scale = (owner.bodyChunks[0].rad * 1.1f + 2f) / 64f,
                    shader = rCam.room.game.rainWorld.Shaders["Basic"],
                    alpha = 1f
                };
                sLeaser.sprites[BodySprite(0) + 1] = new("JellyLLGrad") {
                    scale = (owner.bodyChunks[0].rad * 1.1f + 2f) / 64f,
                    shader = rCam.room.game.rainWorld.Shaders["Basic"],
                    alpha = 1f
                };

                AddToContainer(sLeaser, rCam, null);
            }

            public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos) {
                base.DrawSprites(sLeaser, rCam, timeStacker, camPos);

                if (!culled) {
                    var vector2 = Vector2.Lerp(owner.bodyChunks[0].lastPos, owner.bodyChunks[0].pos, timeStacker) + Custom.RNV() * digesting * 4f * Random.value;
                    var sRot = Mathf.Lerp(lastRot, rot, timeStacker);
                    var sAlpha = Mathf.Lerp(lastAlpha, alpha, timeStacker);
                    sLeaser.sprites[BodySprite(0)].rotation = sRot;
                    sLeaser.sprites[BodySprite(0) + 1].x = vector2.x - camPos.x;
                    sLeaser.sprites[BodySprite(0) + 1].y = vector2.y - camPos.y;
                    sLeaser.sprites[BodySprite(0) + 1].rotation = sRot;
                    sLeaser.sprites[BodySprite(0) + 1].color = daddy.eyeColor;
                    sLeaser.sprites[BodySprite(0) + 1].alpha = sAlpha;
                }
            }

            public virtual Color GetColor() => daddy.effectColor;

            public virtual Vector2 GetPosition() => daddy.firstChunk.pos;
        }

        // If you delete the try catch block it might crash in some cases, so don't
        private static void DaddyGraphicsOnRenderSlits(On.DaddyGraphics.orig_RenderSlits orig, DaddyGraphics self, int chunk, Vector2 pos, Vector2 middleOfBody, float rotation, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos) {
            try {
                if (self.daddy?.room?.world.region?.name == "RF" || self.daddy?.room?.abstractRoom.name is "Reef" or "Pump" or "Cavity")
                    return;

                orig(self, chunk, pos, middleOfBody, rotation, sLeaser, rCam, timeStacker, camPos);
            }
            catch (System.NullReferenceException) { }
        }

        private delegate CreatureTemplate.Relationship orig_UpdateDynamicRelationship(DaddyAI self, RelationshipTracker.DynamicRelationship dRelation);

        private static CreatureTemplate.Relationship DaddyAIHookUpdateDynamicRelationship(orig_UpdateDynamicRelationship orig, DaddyAI self, RelationshipTracker.DynamicRelationship dRelation) {
            var rel = orig(self, dRelation);

            if ((self.daddy?.room?.world.region?.name == "RF" || self.daddy?.room?.abstractRoom.name is "Reef" or "Pump" or "Cavity") && dRelation.trackerRep.representedCreature.creatureTemplate.type is CreatureTemplate.Type.BrotherLongLegs or CreatureTemplate.Type.DaddyLongLegs)
                rel = new(CreatureTemplate.Relationship.Type.Ignores, 0f);

            return rel;
        }

        private static void DaddyLongLegsOnCtor(On.DaddyLongLegs.orig_ctor orig, DaddyLongLegs self, AbstractCreature abstractCreature, World world) {
            orig(self, abstractCreature, world);

            if (abstractCreature.Room?.name is "Reef" or "Pump" or "Cavity") {
                self.colorClass = true;
                self.effectColor = new Color32(146, 33, 191, 255);
                self.eyeColor = self.effectColor;
            }

            if (world.region?.name == "RF" || abstractCreature.Room?.name is "Reef" or "Pump" or "Cavity") {
                int seed = Random.seed;
                Random.seed = abstractCreature.ID.RandomSeed;

                float num = (!self.SizeClass) ? 8f : 12f;
                self.bodyChunks = new BodyChunk[1];
                float num3 = Mathf.Lerp(num * 0.2f, num * 0.3f, Random.value);
                self.bodyChunks[0] = new BodyChunk(self, 0, new Vector2(0f, 0f), num3 * 3.5f + ((!self.SizeClass) ? 22f : 25f), num3 + ((!self.SizeClass) ? 2f : 2.5f));

                self.bodyChunkConnections = new PhysicalObject.BodyChunkConnection[0];

                for (int num12 = 0; num12 < self.tentacles.Length; num12++)
                    self.tentacles[num12].connectedChunk = self.bodyChunks[0];

                Random.seed = seed;
            }
        }
    }
}