using RimWorld;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

internal static class WalkFire
{
	private const int ScanInterval = 10;

	private const int MissBackoff = 30;

	private const TargetScanFlags Flags = TargetScanFlags.NeedLOSToAll | TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable;

	internal static void Tick(Pawn pawn, CompArsenal comp, int delta)
	{
		if (!pawn.IsHashIntervalTick(10, delta) || (pawn.mindState?.enemyTarget == null && !comp.UnderCombatOrders))
		{
			return;
		}
		bool flag = pawn.pather?.MovingNow ?? false;
		bool num = (ArmourySettings.walkFireEnabled && comp.walkFire) & flag;
		bool flag2 = !flag && IsMixedDualWield(pawn);
		if ((!num && !flag2) || pawn.Downed || pawn.stances.curStance is Stance_Busy || DualWield.OffHandBusy(pawn) || !HoldsRanged(pawn) || Find.TickManager.TicksGame < comp.nextWalkScanTick || pawn.WorkTagIsDisabled(WorkTags.Violent) || pawn.HasAttachment(ThingDefOf.Fire) || (pawn.drafter != null && !pawn.drafter.FireAtWill))
		{
			return;
		}
		Verb verb = pawn.TryGetAttackVerb(null);
		if (verb != null && !verb.IsMeleeAttack)
		{
			Thing thing = (comp.UnderCombatOrders ? ((Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(pawn, TargetScanFlags.NeedLOSToAll | TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable)) : OwnTarget(pawn, verb));
			if (thing == null)
			{
				comp.nextWalkScanTick = Find.TickManager.TicksGame + 30;
			}
			else
			{
				pawn.TryStartAttack(thing);
			}
		}
	}

	private static Thing OwnTarget(Pawn pawn, Verb verb)
	{
		Thing thing = pawn.mindState?.enemyTarget;
		if (thing == null || thing.Destroyed || !thing.Spawned || thing.Map != pawn.Map)
		{
			return null;
		}
		if (thing is IAttackTarget attackTarget && attackTarget.ThreatDisabled(pawn))
		{
			return null;
		}
		if (thing is Pawn { Downed: not false })
		{
			return null;
		}
		if (!verb.CanHitTarget(thing))
		{
			return null;
		}
		return thing;
	}

	private static bool HoldsRanged(Pawn pawn)
	{
		ThingWithComps thingWithComps = Arsenal.MainHand(pawn);
		if (thingWithComps != null && thingWithComps.def.IsRangedWeapon)
		{
			return true;
		}
		return Arsenal.OffHand(pawn)?.def.IsRangedWeapon ?? false;
	}

	private static bool IsMixedDualWield(Pawn pawn)
	{
		ThingWithComps thingWithComps = Arsenal.MainHand(pawn);
		ThingWithComps thingWithComps2 = Arsenal.OffHand(pawn);
		if (thingWithComps != null && thingWithComps2 != null)
		{
			return thingWithComps.def.IsMeleeWeapon != thingWithComps2.def.IsMeleeWeapon;
		}
		return false;
	}
}
