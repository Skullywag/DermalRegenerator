using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;
using Verse.Sound;
using RimWorld;
using System;

namespace DermalRegenerator
{
    class Building_DermalRegenerator : Building_WorkTable, IThingContainerGiver
    {
        private int count = 0;
        private Hediff foundInj = null;
        private int oldCount = 0;
        private static readonly ThingDef medicine = ThingDef.Named("Medicine");
        private Pawn JobPawn;
        private Pawn OwnerPawn;
        private Job job1 = new Job();
        private Job job2 = new Job();
        private static readonly SoundDef sound1 = SoundDef.Named("Interact_Research");
        private static readonly SoundDef sound2 = SoundDef.Named("Recipe_ButcherCorpseFlesh");
        public ThingContainer container;
        public Building_DermalRegenerator()
        {
            this.container = new ThingContainer(this, false);
        }

        public IntVec3 GetPosition()
        {
            return base.PositionHeld;
        }

        public bool CanDispenseNow
        {
            get
            {
                return this.UsableNow && this.HasFoodInHopper;
            }
        }

        public bool HasFoodInHopper
        {
            get
            {
                return this.FoodInHopper != null;
            }
        }

        private Thing FoodInHopper
        {
            get
            {
                ThingDef thingDef = ThingDef.Named("Misc_MedicalGelSynth");
                foreach (IntVec3 current in GenAdj.CellsAdjacentCardinal(this))
                {
                    Thing thing = null;
                    Thing thing2 = null;
                    foreach (Thing current2 in Find.ThingGrid.ThingsAt(current))
                    {
                        if (current2.def.defName == "HerbalMedicine")
                        {
                            thing = current2;
                        }
                        if (current2.def == thingDef)
                        {
                            thing2 = current2;
                        }
                    }
                    if (thing != null && thing2 != null)
                    {
                        return thing;
                    }
                }
                return null;
            }
        }

        public bool DispenseFood()
        {
            if (!this.CanDispenseNow)
            {
                return false;
            }
            int num = this.def.building.foodCostPerDispense;
            int num2 = 0;
            List<ThingDef> list = new List<ThingDef>();
            Thing foodInHopper = this.FoodInHopper;
            int num3 = Mathf.Min(foodInHopper.stackCount, num);
            num2 += num3;
            list.Add(foodInHopper.def);
            if (num2 < num)
            {
                return false;
            }
            foodInHopper.SplitOff(num3);
            foodInHopper = this.FoodInHopper;
            this.def.building.soundDispense.PlayOneShot(base.Position);
            return true;
        }

        public Building AdjacentReachableHopper(Pawn reacher)
        {
            ThingDef thingDef = ThingDef.Named("Misc_MedicalGelSynth");
            foreach (IntVec3 current in GenAdj.CellsAdjacentCardinal(this))
            {
                Building edifice = current.GetEdifice();
                if (edifice != null && edifice.def == thingDef && reacher.CanReach(edifice, PathEndMode.Touch, Danger.Deadly))
                {
                    return (Building_Storage)edifice;
                }
            }
            return null;
        }

        public ThingContainer GetContainer()
        {
            return this.container;
        }

        public override void ExposeData()
        {
            base.ExposeData();
        }

        public void AcceptThing(Pawn pawn)
        {
            if (this.container.TryAdd(pawn))
            {
                SoundDef.Named("CryptosleepCasketAccept").PlayOneShot(base.Position);
            }
        }

        public override void SpawnSetup()
        {
            base.SpawnSetup();
            if (this.container == null)
            {
                this.container = new ThingContainer(this, true);
            }
            foreach (Thing current in this.container.Contents)
            {
                Pawn pawn = current as Pawn;
                if (pawn != null)
                {
                    Find.ListerPawns.RegisterPawn(pawn);
                }
            }
        }

        public override void TickRare()
        {
            base.TickRare();
            if (JobPawn != null)
            {
                if (!this.container.Empty && JobPawn.Position == this.InteractionCell)
                {
                    string messageText5;
                    messageText5 = "Dermal Regenerator in use.";
                    Messages.Message(messageText5, MessageSound.Benefit);
                    JobPawn = null;
                    return;
                }
                if (this.container.Empty && this.UsableNow)
                {
                    if (JobPawn.Position == this.InteractionCell)
                    {
                        if (DispenseFood())
                        {
                            OwnerPawn = JobPawn;
                            AcceptThing(OwnerPawn);
                            JobPawn = null;
                            return;
                        }
                        else
                        {
                            string messageText4;
                            messageText4 = "Not enough Synthetic Medical Gel";
                            Messages.Message(messageText4, MessageSound.Benefit);
                            OwnerPawn = null;
                            JobPawn = null;
                            return;
                        }
                    }
                }
                else if (this.container.Empty && !this.UsableNow)
                {
                    string messageText5;
                    messageText5 = "Dermal Regenerator doesnt have power.";
                    Messages.Message(messageText5, MessageSound.Benefit);
                    OwnerPawn = null;
                    JobPawn = null;
                    return;
                }
            }
            if (!this.container.Empty && !this.UsableNow)
            {
                string messageText1;
                messageText1 = "Dermal Regenerator power interupted, ejecting patient.";
                Messages.Message(messageText1, MessageSound.Benefit);
                this.EjectContents();
                this.container.TryDropAll(this.InteractionCell, ThingPlaceMode.Near);
                OwnerPawn = null;
                JobPawn = null;
                count = 0;
                return;
            }
            if (!this.container.Empty && this.UsableNow)
            {
                if (count <= 25)
                {
                    MoteThrower.ThrowMicroSparks(Position.ToVector3Shifted());
                }
                else
                {
                    MoteThrower.ThrowStatic(this.Position, ThingDefOf.Mote_HealingCross, 1f);
                }
                
                count++;

                List<Pawn> list = new List<Pawn>();
                foreach (Thing current in this.container.Contents)
                {
                    Pawn pawn = current as Pawn;
                    if (pawn != null)
                    {
                        list.Add(pawn);
                    }
                }
                foreach (Pawn current2 in list)
                {
                    foreach (Hediff current in current2.health.hediffSet.GetHediffs<Hediff>())
                    {
                        if (current is Hediff_Injury && current.IsOld() && current.Label.Contains("scar"))
                        {
                            oldCount++;
                            foundInj = current;
                        }
                    }

                    if (count >= 50)
                    {
                        current2.health.hediffSet.hediffs.Remove(foundInj);
                        current2.health.Notify_HediffChanged(foundInj);
                        foundInj = null;
                        if (!current2.health.ShouldGetTreatment)
                        {
                            string messageText;
                            messageText = "Treatment complete, ejecting patient.";
                            Messages.Message(messageText, MessageSound.Benefit);
                            this.EjectContents();
                            this.container.TryDropAll(this.InteractionCell, ThingPlaceMode.Near);
                            count = 0;
                            OwnerPawn = null;
                            JobPawn = null;
                        }
                    }
                }

                if (count == 25)
                {
                    if (foundInj == null)
                    {
                        string messageText2;
                        messageText2 = "No surface injuries discovered, ejecting patient.";
                        Messages.Message(messageText2, MessageSound.Benefit);
                        this.EjectContents();
                        this.container.TryDropAll(this.InteractionCell, ThingPlaceMode.Near);
                        OwnerPawn = null;
                        JobPawn = null;
                        count = 0;
                        return;
                    }
                }
            }
        }

        public List<Hediff_MissingPart> GetMissingBodyparts(Pawn pawn)
        {
            List<Hediff_MissingPart> list = new List<Hediff_MissingPart>();
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i] is Hediff_MissingPart)
                {
                    list.Add((Hediff_MissingPart)hediffs[i]);
                }
            }
            List<Hediff_MissingPart> result;
            if (list.Count == 0)
            {
                result = null;
            }
            else
            {
                result = list;
            }
            return result;
        }
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (!this.container.Empty && (mode == DestroyMode.Deconstruct || mode == DestroyMode.Kill))
            {
                if (mode != DestroyMode.Deconstruct)
                {
                    List<Pawn> list = new List<Pawn>();
                    foreach (Thing current in this.container.Contents)
                    {
                        Pawn pawn = current as Pawn;
                        if (pawn != null)
                        {
                            list.Add(pawn);
                        }
                    }
                    foreach (Pawn current2 in list)
                    {
                        HealthUtility.GiveInjuriesToForceDowned(current2);
                    }
                }
                this.EjectContents();
            }
            this.container.DestroyContents();
            base.Destroy(mode);
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            string inspectString = base.GetInspectString();
            if (!inspectString.NullOrEmpty())
            {
                stringBuilder.AppendLine(inspectString);
            }
            int percent = count * 2;
            if (count == 0)
            {
                stringBuilder.AppendLine("Waiting for patient.");
            }
            else if(count < 25)
            {
                stringBuilder.AppendLine("Scanning... Total Progress: " + percent + "%");
            }
            else
            {
                stringBuilder.AppendLine("Treating... Total Progress: " + percent + "%");
            }
            return stringBuilder.ToString();
        }

        public static Building_DermalRegenerator FindCryptoHealCasketFor(Pawn p, Pawn traveler)
        {
            IEnumerable<ThingDef> enumerable =
                from def in DefDatabase<ThingDef>.AllDefs
                where def == ThingDef.Named("CryptoHealCasket")
                select def;
            foreach (ThingDef current in enumerable)
            {
                Predicate<Thing> validator = (Thing x) => ((Building_DermalRegenerator)x).GetContainer().Empty;
                Building_DermalRegenerator building_CryptoHealCasket = (Building_DermalRegenerator)GenClosest.ClosestThingReachable(p.Position, ThingRequest.ForDef(current), PathEndMode.InteractionCell, TraverseParms.For(traveler, Danger.Deadly, TraverseMode.PassAnything, true), 9999f, validator, null, -1);
                if (building_CryptoHealCasket != null)
                {
                    return building_CryptoHealCasket;
                }
            }
            return null;
        }

        public virtual void EjectContents()
        {
            ThingDef named = DefDatabase<ThingDef>.GetNamed("FilthSlime", true);
            foreach (Thing current in this.container.Contents)
            {
                Pawn pawn = current as Pawn;
                if (pawn != null)
                {
                    pawn.filth.GainFilth(named);
                }
            }
            this.container.TryDropAll(this.InteractionCell, ThingPlaceMode.Near);
            if (!this.Destroyed)
            {
                SoundDef.Named("CryptosleepCasketOpen").PlayOneShot(SoundInfo.InWorld(this.Position, MaintenanceType.None));
            }
            OwnerPawn = null;
            JobPawn = null;
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            {
                if (!myPawn.CanReserve(this))
                {
                    FloatMenuOption item = new FloatMenuOption("CannotUseReserved", null);
                    return new List<FloatMenuOption>
				{
					item
				};
                }
                if (!myPawn.CanReach(this, PathEndMode.Touch, Danger.Some))
                {
                    FloatMenuOption item2 = new FloatMenuOption("CannotUseNoPath", null);
                    return new List<FloatMenuOption>
				{
					item2
				};

                }

                if (OwnerPawn == null)
                {
                    Action action1 = delegate
                    {
                        job1 = new Job(JobDefOf.Goto, this.InteractionCell);
                        job2 = new Job(JobDefOf.Wait, 2000);
                        myPawn.playerController.TakeOrderedJob(job1);
                        myPawn.playerController.QueueJob(job2);
                        JobPawn = myPawn;
                        myPawn.Reserve(this);
                    };
                    list.Add(new FloatMenuOption("Use AutoHeal Casket", action1));
                }
            }
            return list;
        }
    }
}