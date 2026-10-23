using RimWorld;
using Verse;

namespace AsuRim.Armoury;

public class FloatMenuOptionProvider_MarkVIP : FloatMenuOptionProvider
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
			return true;
		}
	}

	protected override bool RequiresManipulation
	{
		get
		{
			return false;
		}
	}

	protected override bool AppliesInt(FloatMenuContext context)
	{
		return context.FirstSelectedPawn != null;
	}

	protected override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
	{
		Pawn target = clickedThing as Pawn;
		if (target == null || target.Map == null)
		{
			return null;
		}
		Pawn firstSelectedPawn = context.FirstSelectedPawn;
		if (firstSelectedPawn == null || !target.HostileTo(firstSelectedPawn.Faction))
		{
			return null;
		}
		VIPTracker tracker = VIPTracker.For(target.Map);
		if (tracker == null)
		{
			return null;
		}
		if (tracker.VIP == target)
		{
			return new FloatMenuOption("ASA_VIP_Clear".Translate(target.LabelShortCap), () =>
			{
				tracker.VIP = null;
			});
		}
		return new FloatMenuOption("ASA_VIP_Mark".Translate(target.LabelShortCap), () =>
		{
			tracker.VIP = target;
		});
	}
}
