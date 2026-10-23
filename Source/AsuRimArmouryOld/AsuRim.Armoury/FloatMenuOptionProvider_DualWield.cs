using RimWorld;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public class FloatMenuOptionProvider_DualWield : FloatMenuOptionProvider
{
	protected override bool Drafted
	{
		get
		{
			return true;
		}
	}

	protected override bool Undrafted
	{
		get
		{
			return true;
		}
	}

	protected override bool Multiselect
	{
		get
		{
			return false;
		}
	}

	protected override bool RequiresManipulation
	{
		get
		{
			return true;
		}
	}

	protected override bool AppliesInt(FloatMenuContext context)
	{
		if (ArmourySettings.dualWieldEnabled)
		{
			return Arsenal.CanCarryWeapons(context.FirstSelectedPawn);
		}
		return false;
	}

	protected override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
	{
		ThingWithComps weapon = clickedThing as ThingWithComps;
		if (weapon == null)
		{
			return null;
		}
		if (!ArsenalRules.IsWieldable(weapon))
		{
			return null;
		}
		Pawn pawn = context.FirstSelectedPawn;
		string cannot = "CannotEquip".Translate(weapon.LabelShort);
		ThingWithComps thingWithComps = Arsenal.MainHand(pawn);
		if (Arsenal.OffHand(pawn) == weapon)
		{
			return null;
		}
		if (!ArsenalRules.CanBeOffHand(weapon.def))
		{
			return Refused(cannot, "DW_CannotBeOffHand".Translate());
		}
		if (ArsenalRules.IsTwoHanded(weapon.def))
		{
			return Refused(cannot, "DW_NoTwoHandedInOffHand".Translate());
		}
		if (thingWithComps == null)
		{
			return Refused(cannot, "DW_MissingPrimary".Translate());
		}
		if (ArsenalRules.IsTwoHanded(thingWithComps.def))
		{
			return Refused(cannot, "DW_WieldingTwoHanded".Translate());
		}
		if (pawn.WorkTagIsDisabled(WorkTags.Violent))
		{
			return Refused(cannot, "IsIncapableOfViolenceLower".Translate(pawn.LabelShort, pawn));
		}
		if (weapon.def.IsRangedWeapon && pawn.WorkTagIsDisabled(WorkTags.Shooting))
		{
			return Refused(cannot, "IsIncapableOfShootingLower".Translate(pawn));
		}
		if (ArsenalRules.HasMissingHand(pawn))
		{
			return Refused(cannot, "DW_MissArmOrHand".Translate());
		}
		if (VEFShieldCompat.Active && VEFShieldCompat.OffHandShield(pawn) != null)
		{
			return Refused(cannot, "DW_CannotBeOffHand".Translate());
		}
		if (!pawn.CanReach(weapon, PathEndMode.ClosestTouch, Danger.Deadly))
		{
			return Refused(cannot, "NoPath".Translate());
		}
		if (weapon.IsBurning())
		{
			return Refused(cannot, "BurningLower".Translate());
		}
		if (pawn.IsQuestLodger() && !EquipmentUtility.QuestLodgerCanEquip(weapon, pawn))
		{
			return Refused(cannot, "QuestRelated".Translate().CapitalizeFirst());
		}
		if (!EquipmentUtility.CanEquip(weapon, pawn, out var cantReason))
		{
			return Refused(cannot, cantReason);
		}
		if (!ArsenalRules.CanDualWield(pawn, thingWithComps, weapon))
		{
			return Refused(cannot, "DW_CannotBeOffHand".Translate());
		}
		string text = "DW_EquipOffHand".Translate(weapon.LabelShort);
		if (weapon.def.IsRangedWeapon && pawn.story != null && pawn.story.traits.HasTrait(TraitDefOf.Brawler))
		{
			text += "EquipWarningBrawler".Translate();
		}
		return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, () =>
		{
			FleckMaker.Static(weapon.DrawPos, weapon.Map, FleckDefOf.FeedbackEquip);
			weapon.SetForbidden(value: false);
			CompArsenal compArsenal = CompArsenal.For(pawn);
			if (compArsenal?.policy != null)
			{
				compArsenal.policy.offDef = weapon.def;
				compArsenal.policy.offStuff = weapon.Stuff;
			}
			pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(AsuRimJobDefOf.ASA_RetrieveOffHand, weapon), JobTag.Misc);
			PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.EquippingWeapons, KnowledgeAmount.Total);
		}, MenuOptionPriority.High), pawn, weapon);
	}

	private static FloatMenuOption Refused(string cannot, string reason)
	{
		return new FloatMenuOption("DW_AsOffHand".Translate(cannot, reason), null);
	}
}
