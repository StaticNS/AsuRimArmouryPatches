using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public static class RangeDirector
{
	private static readonly List<ThingWithComps> candidates = new List<ThingWithComps>();

	public const int EvaluateEveryTicks = 60;

	private const float MeleeReleaseDistance = 4f;

	private const float GrabbedRangedPenalty = 0.25f;

	public static void Evaluate(Pawn pawn, CompArsenal comp)
	{
		if (comp == null || !Arsenal.CanCarrySidearms(pawn) || pawn.Downed || pawn.InMentalState || pawn.WorkTagIsDisabled(WorkTags.Violent) || comp.forcedUnarmed || BrawlSwap(pawn, comp) || !SafeToSwap(pawn) || !ArmourySettings.rangeDirectorEnabled)
		{
			return;
		}
		if (comp.forced != null)
		{
			if (Arsenal.MainHand(pawn) == comp.forced || Arsenal.OffHand(pawn) == comp.forced)
			{
				return;
			}
			Pawn_InventoryTracker inventory = pawn.inventory;
			if (inventory != null && inventory.innerContainer.Contains(comp.forced))
			{
				return;
			}
			comp.forced = null;
		}
		Thing thing = CurrentThreat(pawn);
		if (thing == null)
		{
			comp.inMelee = false;
			comp.inMeleeWith = null;
			Rearm(pawn, comp);
			return;
		}
		float num = pawn.Position.DistanceTo(thing.Position);
		if (comp.inMelee && comp.inMeleeWith != thing)
		{
			comp.inMelee = false;
			comp.inMeleeWith = null;
		}
		bool flag = num < 2f;
		if (comp.inMelee)
		{
			if (num <= 4f)
			{
				num = 1f;
			}
			else
			{
				comp.inMelee = false;
				comp.inMeleeWith = null;
			}
		}
		else if (flag)
		{
			comp.inMelee = true;
			comp.inMeleeWith = thing;
		}
		bool flag2 = flag || comp.inMelee;
		if (!flag2 && Find.TickManager.TicksGame - comp.lastSwitchTick < ArmourySettings.switchCooldownTicks)
		{
			return;
		}
		if (flag2)
		{
			ThingWithComps thingWithComps = Arsenal.MainHand(pawn);
			if (thingWithComps != null && thingWithComps.def.IsMeleeWeapon)
			{
				return;
			}
			ThingWithComps thingWithComps2 = BestMelee(pawn, comp);
			if (thingWithComps2 == null)
			{
				return;
			}
			ThingWithComps thingWithComps3 = Arsenal.OffHand(pawn);
			comp.directorActing = true;
			try
			{
				if (ArsenalActions.EquipMain(pawn, thingWithComps2))
				{
					comp.lastSwitchTick = Find.TickManager.TicksGame;
					comp.meleeReturn = ((thingWithComps != null && thingWithComps.def.IsRangedWeapon) ? thingWithComps : null);
					comp.meleeReturnOff = ((comp.meleeReturn != null && thingWithComps3 != null && thingWithComps3.def.IsRangedWeapon) ? thingWithComps3 : null);
				}
				return;
			}
			finally
			{
				comp.directorActing = false;
			}
		}
		ThingWithComps thingWithComps4 = Arsenal.MainHand(pawn);
		float num2 = ((thingWithComps4 != null) ? Score(pawn, thingWithComps4, num, flag2, thing) : 0f);
		ThingWithComps thingWithComps5 = Arsenal.OffHand(pawn);
		CollectCandidates(pawn, comp, candidates);
		ThingWithComps thingWithComps6 = null;
		float num3 = 0f;
		for (int i = 0; i < candidates.Count; i++)
		{
			ThingWithComps thingWithComps7 = candidates[i];
			if (thingWithComps7 != thingWithComps4 && thingWithComps7 != thingWithComps5)
			{
				float num4 = Score(pawn, thingWithComps7, num, flag2, thing);
				if (num4 > num3)
				{
					num3 = num4;
					thingWithComps6 = thingWithComps7;
				}
			}
		}
		candidates.Clear();
		if (thingWithComps6 == null || ((!flag2 || !thingWithComps6.def.IsMeleeWeapon) && thingWithComps4 != null && num3 < num2 * ArmourySettings.switchMargin) || (!thingWithComps6.def.IsMeleeWeapon && IsMidShot(thingWithComps4)))
		{
			return;
		}
		comp.directorActing = true;
		try
		{
			Apply(pawn, comp, thingWithComps6);
			comp.lastSwitchTick = Find.TickManager.TicksGame;
			comp.meleeReturn = null;
			comp.meleeReturnOff = null;
		}
		finally
		{
			comp.directorActing = false;
		}
	}

	private static void Rearm(Pawn pawn, CompArsenal comp)
	{
		ThingWithComps meleeReturn = comp.meleeReturn;
		if (meleeReturn == null)
		{
			return;
		}
		ThingWithComps meleeReturnOff = comp.meleeReturnOff;
		ThingWithComps thingWithComps = Arsenal.MainHand(pawn);
		ThingOwner<Thing> thingOwner = pawn.inventory?.innerContainer;
		if (meleeReturn.Destroyed || thingWithComps == meleeReturn || (thingWithComps != null && !thingWithComps.def.IsMeleeWeapon) || thingOwner == null || !thingOwner.Contains(meleeReturn))
		{
			comp.meleeReturn = null;
			comp.meleeReturnOff = null;
		}
		else
		{
			if (IsMidSwing(pawn))
			{
				return;
			}
			comp.meleeReturn = null;
			comp.meleeReturnOff = null;
			comp.directorActing = true;
			try
			{
				if (ArsenalActions.EquipMain(pawn, meleeReturn))
				{
					comp.lastSwitchTick = Find.TickManager.TicksGame;
					if (meleeReturnOff != null && !meleeReturnOff.Destroyed && Arsenal.OffHand(pawn) == null && thingOwner.Contains(meleeReturnOff))
					{
						ArsenalActions.EquipOff(pawn, meleeReturnOff);
					}
				}
			}
			finally
			{
				comp.directorActing = false;
			}
		}
	}

	private static bool IsMidSwing(Pawn pawn)
	{
		if (pawn.stances?.curStance is Stance_Busy { verb: not null } stance_Busy)
		{
			return stance_Busy.verb.IsMeleeAttack;
		}
		return false;
	}

	private static float Score(Pawn pawn, ThingWithComps weapon, float distance, bool grabbed, Thing target)
	{
		float num = WeaponScorer.Score(pawn, weapon, distance, target);
		if (grabbed && !weapon.def.IsMeleeWeapon)
		{
			num *= 0.25f;
		}
		return num;
	}

	public static bool SafeToSwap(Pawn pawn)
	{
		if (pawn.carryTracker?.CarriedThing != null)
		{
			return false;
		}
		Job curJob = pawn.CurJob;
		if (curJob == null)
		{
			return true;
		}
		JobDef def = curJob.def;
		if (def != JobDefOf.Rescue && def != JobDefOf.TendPatient && def != JobDefOf.TendEntity && def != JobDefOf.Capture && def != JobDefOf.Arrest && def != JobDefOf.Strip && def != JobDefOf.TakeToBedToOperate && def != JobDefOf.DeliverToBed && def != JobDefOf.CarryDownedPawnToExit && def != JobDefOf.CarryDownedPawnDrafted && def != JobDefOf.CarryToCryptosleepCasket && def != JobDefOf.Ingest && def != JobDefOf.LayDown && def != JobDefOf.Equip && def != JobDefOf.DropEquipment && def != JobDefOf.Reload && def != JobDefOf.UseVerbOnThing)
		{
			return def != JobDefOf.CastAbilityOnThing;
		}
		return false;
	}

	public static bool InBrawl(Pawn pawn)
	{
		if (pawn == null || !ArmourySettings.stancesEnabled)
		{
			return false;
		}
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal != null)
		{
			return InBrawl(pawn, compArsenal);
		}
		return false;
	}

	public static bool InBrawl(Pawn pawn, CompArsenal comp)
	{
		if (ArmourySettings.stancesEnabled && pawn.Faction != null && pawn.Faction.IsPlayer && !pawn.InMentalState && comp.EffectiveStance == CombatStance.Brawl)
		{
			return comp.UnderCombatOrders;
		}
		return false;
	}

	public static void SyncBrawl(Pawn pawn)
	{
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal != null && pawn.Spawned && Arsenal.CanCarrySidearms(pawn) && !pawn.Downed && !pawn.InMentalState && !compArsenal.forcedUnarmed && !pawn.WorkTagIsDisabled(WorkTags.Violent))
		{
			BrawlSwap(pawn, compArsenal);
		}
	}

	private static bool BrawlSwap(Pawn pawn, CompArsenal comp)
	{
		ThingWithComps thingWithComps = Arsenal.MainHand(pawn);
		ThingWithComps thingWithComps2 = Arsenal.OffHand(pawn);
		if (!InBrawl(pawn, comp))
		{
			if (comp.brawlActive)
			{
				if (!SafeToSwap(pawn))
				{
					return true;
				}
				comp.brawlActive = false;
				ReturnFromBrawl(pawn, comp, thingWithComps);
				thingWithComps = Arsenal.MainHand(pawn);
				thingWithComps2 = Arsenal.OffHand(pawn);
			}
			comp.brawlReturn = ((thingWithComps != null && thingWithComps.def.IsRangedWeapon) ? thingWithComps : null);
			comp.brawlReturnOff = ((thingWithComps2 != null && thingWithComps2.def.IsRangedWeapon) ? thingWithComps2 : null);
			comp.brawlOffHand = false;
			return false;
		}
		if (!comp.brawlActive)
		{
			comp.brawlActive = true;
			comp.brawlOffHand = false;
			if (thingWithComps != null && thingWithComps.def.IsRangedWeapon)
			{
				comp.brawlReturn = thingWithComps;
			}
			if (thingWithComps2 != null && thingWithComps2.def.IsRangedWeapon)
			{
				comp.brawlReturnOff = thingWithComps2;
			}
		}
		if (thingWithComps != null && thingWithComps.def.IsMeleeWeapon)
		{
			return true;
		}
		if (!SafeToSwap(pawn))
		{
			return true;
		}
		ThingWithComps thingWithComps3 = BestMeleeCarried(pawn, comp);
		comp.directorActing = true;
		try
		{
			if (thingWithComps3 != null)
			{
				bool brawlOffHand = thingWithComps3 == thingWithComps2;
				if (ArsenalActions.EquipMain(pawn, thingWithComps3))
				{
					comp.brawlOffHand = brawlOffHand;
					comp.lastSwitchTick = Find.TickManager.TicksGame;
				}
			}
			else if ((thingWithComps != null || (thingWithComps2 != null && thingWithComps2.def.IsRangedWeapon)) && ArsenalActions.StowOffHand(pawn) && ArsenalActions.StowMainHand(pawn))
			{
				comp.lastSwitchTick = Find.TickManager.TicksGame;
			}
		}
		finally
		{
			comp.directorActing = false;
		}
		return true;
	}

	private static void ReturnFromBrawl(Pawn pawn, CompArsenal comp, ThingWithComps held)
	{
		ThingWithComps brawlReturn = comp.brawlReturn;
		ThingWithComps brawlReturnOff = comp.brawlReturnOff;
		bool brawlOffHand = comp.brawlOffHand;
		comp.brawlReturn = null;
		comp.brawlReturnOff = null;
		comp.brawlOffHand = false;
		if (brawlReturn == null || brawlReturn.Destroyed || held == brawlReturn || (held != null && !held.def.IsMeleeWeapon))
		{
			return;
		}
		ThingOwner<Thing> thingOwner = pawn.inventory?.innerContainer;
		if (thingOwner == null || !thingOwner.Contains(brawlReturn))
		{
			return;
		}
		comp.directorActing = true;
		try
		{
			if (ArsenalActions.EquipMain(pawn, brawlReturn))
			{
				comp.lastSwitchTick = Find.TickManager.TicksGame;
				if (brawlOffHand && held != null && thingOwner.Contains(held))
				{
					ArsenalActions.EquipOff(pawn, held);
				}
				else if (brawlReturnOff != null && !brawlReturnOff.Destroyed && Arsenal.OffHand(pawn) == null && thingOwner.Contains(brawlReturnOff))
				{
					ArsenalActions.EquipOff(pawn, brawlReturnOff);
				}
			}
		}
		finally
		{
			comp.directorActing = false;
		}
	}

	private static ThingWithComps BestMeleeCarried(Pawn pawn, CompArsenal comp)
	{
		ThingWithComps thingWithComps = null;
		float num = 0f;
		ThingWithComps thingWithComps2 = Arsenal.OffHand(pawn);
		if (thingWithComps2 != null && thingWithComps2.def.IsMeleeWeapon && ArsenalRules.CanAutoWield(pawn, thingWithComps2))
		{
			thingWithComps = thingWithComps2;
			num = thingWithComps2.GetStatValue(StatDefOf.MeleeWeapon_AverageDPS);
		}
		ThingWithComps thingWithComps3 = BestMelee(pawn, comp);
		if (thingWithComps3 != null && (thingWithComps == null || thingWithComps3.GetStatValue(StatDefOf.MeleeWeapon_AverageDPS) > num))
		{
			thingWithComps = thingWithComps3;
		}
		return thingWithComps;
	}

	private static ThingWithComps BestMelee(Pawn pawn, CompArsenal comp)
	{
		CollectCandidates(pawn, comp, candidates);
		ThingWithComps result = null;
		float num = 0f;
		for (int i = 0; i < candidates.Count; i++)
		{
			ThingWithComps thingWithComps = candidates[i];
			if (thingWithComps.def.IsMeleeWeapon)
			{
				float statValue = thingWithComps.GetStatValue(StatDefOf.MeleeWeapon_AverageDPS);
				if (statValue > num)
				{
					num = statValue;
					result = thingWithComps;
				}
			}
		}
		candidates.Clear();
		return result;
	}

	private static void Apply(Pawn pawn, CompArsenal comp, ThingWithComps chosen)
	{
		WeaponPolicy policy = comp.policy;
		if (policy != null && policy.mainDef != null && chosen.def == policy.mainDef && chosen.Stuff == policy.mainStuff)
		{
			PolicyApplier.Apply(pawn, policy, applyStance: false);
		}
		else
		{
			ArsenalActions.EquipMain(pawn, chosen);
		}
	}

	private static bool IsMidShot(ThingWithComps held)
	{
		if (held == null || !held.def.IsRangedWeapon)
		{
			return false;
		}
		Verb verb = held.TryGetComp<CompEquippable>()?.PrimaryVerb;
		if (verb != null)
		{
			if (!verb.Bursting)
			{
				return verb.WarmingUp;
			}
			return true;
		}
		return false;
	}

	private static Thing AdjacentEnemy(Pawn pawn)
	{
		Map map = pawn.Map;
		if (map == null)
		{
			return null;
		}
		for (int i = 0; i < GenAdj.AdjacentCells.Length; i++)
		{
			IntVec3 c = pawn.Position + GenAdj.AdjacentCells[i];
			if (!c.InBounds(map))
			{
				continue;
			}
			List<Thing> list = map.thingGrid.ThingsListAtFast(c);
			for (int j = 0; j < list.Count; j++)
			{
				if (list[j] is Pawn { Downed: false, Dead: false } pawn2 && pawn2.HostileTo(pawn))
				{
					return pawn2;
				}
			}
		}
		return null;
	}

	private static void CollectCandidates(Pawn pawn, CompArsenal comp, List<ThingWithComps> into)
	{
		into.Clear();
		ThingOwner<Thing> thingOwner = pawn.inventory?.innerContainer;
		if (thingOwner == null)
		{
			return;
		}
		WeaponPolicy policy = comp.policy;
		bool flag = policy != null && policy.mainDef != null && policy.offDef != null;
		bool flag2 = false;
		for (int i = 0; i < thingOwner.Count; i++)
		{
			if (!(thingOwner[i] is ThingWithComps thingWithComps) || !ArsenalRules.CanAutoWield(pawn, thingWithComps))
			{
				continue;
			}
			if (flag)
			{
				bool flag3 = thingWithComps.def == policy.mainDef && thingWithComps.Stuff == policy.mainStuff;
				bool flag4 = thingWithComps.def == policy.offDef && thingWithComps.Stuff == policy.offStuff;
				if (flag3 | flag4)
				{
					if (!flag2 & flag3)
					{
						flag2 = true;
						into.Add(thingWithComps);
					}
					continue;
				}
			}
			into.Add(thingWithComps);
		}
	}

	private static Thing CurrentThreat(Pawn pawn)
	{
		Thing thing = AdjacentEnemy(pawn);
		if (thing != null)
		{
			return thing;
		}
		Thing thing2 = pawn.mindState?.enemyTarget;
		if (thing2 != null && !thing2.Destroyed && thing2.Spawned && thing2.Map == pawn.Map && !(thing2 is Pawn { Downed: not false }))
		{
			return thing2;
		}
		if (!pawn.Drafted)
		{
			return null;
		}
		if (!ThreatDetection.AnyWithin(pawn, 40f))
		{
			return null;
		}
		return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.AttackTarget), PathEndMode.OnCell, TraverseParms.For(pawn), 40f, (Thing t) => t is Pawn { Downed: false } pawn3 && pawn3.HostileTo(pawn));
	}
}
