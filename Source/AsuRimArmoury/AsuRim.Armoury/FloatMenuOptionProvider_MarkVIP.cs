using RimWorld;
using Verse;

namespace AsuRim.Armoury;

public class FloatMenuOptionProvider_MarkVIP : FloatMenuOptionProvider
{
	protected override bool Drafted
	{
		public get
		{
			return true;
		}
	}

	protected override bool Undrafted
	{
		public get
		{
			return true;
		}
	}

	protected override bool Multiselect
	{
		public get
		{
			return true;
		}
	}

	protected override bool RequiresManipulation
	{
		public get
		{
			return false;
		}
	}

	public override bool AppliesInt(FloatMenuContext context)
	{
		return context.FirstSelectedPawn != null;
	}

	public override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
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
