using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoralReef {
    public static class StaticWorldPatch {
        public static void ApplyPatch() {
            List<CreatureTemplate> vList = new List<CreatureTemplate>();
            // Load all creature from StaticWorld; this may include custom creatures from other mods
            vList.AddRange(StaticWorld.creatureTemplates);

            List<TileTypeResistance> ttr = new List<TileTypeResistance>();
            List<TileConnectionResistance> tcr = new List<TileConnectionResistance>();

            CreatureTemplate blueLizard = null;
            foreach (CreatureTemplate ct in vList) {
                if (ct.type == CreatureTemplate.Type.BlueLizard) {
                    blueLizard = ct;
                }
            }

            CreatureTemplate lizardTemplate = null;
            foreach (CreatureTemplate ct in vList) {
                if (ct.type == CreatureTemplate.Type.LizardTemplate) {
                    lizardTemplate = ct;
                }
            }

            // Creatures

            // ExampleLizard
            vList.Add(LizardBreeds.BreedTemplate(EnumExt_CoralReef.Polliwog, lizardTemplate, null, null, null));
            vList[vList.Count - 1].name = "Polliwog";
            ttr.Clear();
            tcr.Clear();


            // Add this back to StaticWorld
            int critterCount = Enum.GetValues(typeof(CreatureTemplate.Type)).Length;
            StaticWorld.creatureTemplates = new CreatureTemplate[critterCount];
            for (int i = 0; i < vList.Count; i++) {
                int ind = (int)vList[i].type;
                if (ind == -1) continue;
                if (StaticWorld.creatureTemplates.Length <= ind) Array.Resize(ref StaticWorld.creatureTemplates, ind + 1);
                StaticWorld.creatureTemplates[ind] = vList[i];
            }

            // Fill in holes if some happen to be missing
            // This can occur when other mods add creatures through EnumExt, but establish their templates later than this
            for (int i = 0; i < StaticWorld.creatureTemplates.Length; i++) {
                CreatureTemplate t = StaticWorld.creatureTemplates[i];
                if (t == null)
                    t = StaticWorld.creatureTemplates[i] = new CreatureTemplate((CreatureTemplate.Type)i, null, new List<TileTypeResistance>(), new List<TileConnectionResistance>(), new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f));

                int oldCount = t.relationships.Length;
                if (oldCount < critterCount) {
                    Array.Resize(ref t.relationships, critterCount);
                    for (int relInd = oldCount; relInd < critterCount; relInd++)
                        t.relationships[relInd] = t.relationships[0];
                }
            }

            for (int i = 0; i < StaticWorld.creatureTemplates.Length; i++) {
                CreatureTemplate.Type t = StaticWorld.creatureTemplates[i].type;
                Debug.Log($"{i}: {t}, {StaticWorld.creatureTemplates[i].relationships?.Length.ToString() ?? "NULL"} relationships");
            }
            
            //Relationships
            
            StaticWorld.EstablishRelationship(
                EnumExt_CoralReef.Polliwog,
                CreatureTemplate.Type.GreenLizard,
                new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 0.3f));
            StaticWorld.EstablishRelationship(
                EnumExt_CoralReef.Polliwog,
                CreatureTemplate.Type.YellowLizard,
                new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Attacks, 0.3f));
            StaticWorld.EstablishRelationship(
                EnumExt_CoralReef.Polliwog,
                EnumExt_CoralReef.Polliwog,
                new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Pack, 0.6f));
            StaticWorld.EstablishRelationship(
                EnumExt_CoralReef.Polliwog,
                CreatureTemplate.Type.Snail,
                new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 0.4f));
            StaticWorld.EstablishRelationship(
                CreatureTemplate.Type.BigEel,
                EnumExt_CoralReef.Polliwog,
                new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 0.6f));
            StaticWorld.EstablishRelationship(
                EnumExt_CoralReef.Polliwog,
                CreatureTemplate.Type.BigEel,
                new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 0.7f));
            StaticWorld.EstablishRelationship(
                EnumExt_CoralReef.Polliwog,
                CreatureTemplate.Type.Vulture,
                new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 0.7f));
            StaticWorld.EstablishRelationship(
                EnumExt_CoralReef.Polliwog,
                CreatureTemplate.Type.KingVulture,
                new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 0.8f));
            StaticWorld.EstablishRelationship(
                CreatureTemplate.Type.GreenLizard,
                EnumExt_CoralReef.Polliwog,
                new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 0.5f));
            StaticWorld.EstablishRelationship(
                CreatureTemplate.Type.YellowLizard,
                EnumExt_CoralReef.Polliwog,
                new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Attacks, 0.5f));
            StaticWorld.EstablishRelationship(
                CreatureTemplate.Type.Vulture,
                EnumExt_CoralReef.Polliwog,
                new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 0.6f));
            StaticWorld.EstablishRelationship(
                CreatureTemplate.Type.KingVulture,
                EnumExt_CoralReef.Polliwog,
                new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 0.5f));
        }
    }
}