using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public class WorkGiver_Bodyguard : WorkGiver_Scanner
{
	public override PathEndMode PathEndMode => PathEndMode.Touch;

	public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
	{
		Pawn pawn2 = CompArsenal.For(pawn)?.guarding;
		if (Bodyguard.CanGuard(pawn, pawn2) && GuardStandDown.BodyguardAllowed(pawn, pawn2))
		{
			yield return pawn2;
		}
	}

	public override bool ShouldSkip(Pawn pawn, bool forced = false)
	{
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal?.guarding != null)
		{
			return !GuardStandDown.BodyguardAllowed(pawn, compArsenal.guarding);
		}
		return true;
	}

	public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		if (!(t is Pawn pawn2))
		{
			return false;
		}
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal == null || compArsenal.guarding != pawn2)
		{
			return false;
		}
		if (pawn.WorkTagIsDisabled(WorkTags.Violent))
		{
			return false;
		}
		if (!GuardStandDown.BodyguardAllowed(pawn, pawn2))
		{
			return false;
		}
		if (Bodyguard.CanGuard(pawn, pawn2))
		{
			return pawn.CanReach(pawn2, PathEndMode.Touch, Danger.Deadly);
		}
		return false;
	}

	public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		if (!HasJobOnThing(pawn, t, forced))
		{
			return null;
		}
		return JobMaker.MakeJob(ASA_PostJobDefOf.ASA_Bodyguard, t);
	}
}
