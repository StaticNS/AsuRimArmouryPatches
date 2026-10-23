using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public class JobGiver_PickupDroppedOffHand : ThinkNode_JobGiver
{
	public override Job TryGiveJob(Pawn pawn)
	{
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal == null || !KitRecovery.HasPending(compArsenal))
		{
			return null;
		}
		return KitRecovery.NextJob(pawn, compArsenal);
	}
}
