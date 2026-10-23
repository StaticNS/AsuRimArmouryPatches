using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(PawnGenerator), "GenerateGearFor")]
internal static class Patch_GenerateGearFor
{
	private enum SidearmRole
	{
		None,
		Primitive,
		Breacher
	}

	private static readonly List<ThingDef> scratch = new List<ThingDef>();

	private static void Postfix(Pawn pawn, PawnGenerationRequest request)
	{
		if (!ArmourySettings.npcSidearms || pawn?.equipment?.Primary == null || pawn.inventory == null || !Arsenal.CanCarrySidearms(pawn) || pawn.Faction == null || pawn.Faction.IsPlayer || pawn.WorkTagIsDisabled(WorkTags.Violent))
		{
			return;
		}
		List<string> list = pawn.kindDef?.weaponTags;
		if (list.NullOrEmpty())
		{
			return;
		}
		ThingDef def = pawn.equipment.Primary.def;
		SidearmRole sidearmRole = RoleFor(def);
		if (sidearmRole == SidearmRole.None || !Rand.Chance(ArmourySettings.npcSidearmChance))
		{
			return;
		}
		ThingDef thingDef = Pick(list, def, sidearmRole);
		if (thingDef != null)
		{
			ThingDef stuff = (thingDef.MadeFromStuff ? GenStuff.RandomStuffByCommonalityFor(thingDef) : null);
			Thing thing = ThingMaker.MakeThing(thingDef, stuff);
			if (pawn.inventory.innerContainer.TryAdd(thing, canMergeWithExistingStacks: false))
			{
				ArsenalActions.Remember(pawn, thing);
			}
			else
			{
				thing.Destroy();
			}
		}
	}

	private static SidearmRole RoleFor(ThingDef primary)
	{
		if ((int)primary.techLevel <= 3)
		{
			return SidearmRole.Primitive;
		}
		if (primary.IsMeleeWeapon && IsBuildingDestroyer(primary))
		{
			return SidearmRole.Breacher;
		}
		return SidearmRole.None;
	}

	private static bool IsBuildingDestroyer(ThingDef def)
	{
		List<VerbProperties> verbs = def.Verbs;
		for (int i = 0; i < verbs.Count; i++)
		{
			if (verbs[i].ai_IsBuildingDestroyer)
			{
				return true;
			}
		}
		return false;
	}

	private static ThingDef Pick(List<string> kindTags, ThingDef primary, SidearmRole role)
	{
		List<ThingDef> list;
		if (role != SidearmRole.Breacher)
		{
			list = (primary.IsRangedWeapon ? Candidates.Melee : Candidates.Ranged);
		}
		else
		{
			list = Candidates.Ranged;
		}
		if (list.Count == 0)
		{
			return null;
		}
		scratch.Clear();
		for (int i = 0; i < list.Count; i++)
		{
			ThingDef thingDef = list[i];
			if ((int)thingDef.techLevel <= (int)primary.techLevel && (int)thingDef.techLevel >= (int)ArmourySettings.npcSidearmMinTechLevel && (int)thingDef.techLevel <= (int)ArmourySettings.npcSidearmMaxTechLevel && !(thingDef.BaseMarketValue > primary.BaseMarketValue) && SharesTag(kindTags, thingDef.weaponTags))
			{
				scratch.Add(thingDef);
			}
		}
		if (scratch.Count == 0)
		{
			return null;
		}
		ThingDef result = scratch.RandomElementByWeight((ThingDef d) => d.generateCommonality);
		scratch.Clear();
		return result;
	}

	private static bool SharesTag(List<string> kindTags, List<string> weaponTags)
	{
		if (weaponTags.NullOrEmpty())
		{
			return false;
		}
		for (int i = 0; i < kindTags.Count; i++)
		{
			if (weaponTags.Contains(kindTags[i]))
			{
				return true;
			}
		}
		return false;
	}
}
