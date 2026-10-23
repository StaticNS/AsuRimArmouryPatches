using RimWorld;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public class FloatMenuOptionProvider_Bodyguard : FloatMenuOptionProvider
{
	protected override bool Drafted
	{
		public get
		{
			return false;
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
			return false;
		}
	}

	protected override bool RequiresManipulation
	{
		public get
		{
			return false;
		}
	}

	protected override bool MechanoidCanDo
	{
		public get
		{
			return true;
		}
	}

	public override bool AppliesInt(FloatMenuContext context)
	{
		if (GuardStandDown.Active)
		{
			return false;
		}
		Pawn firstSelectedPawn = context.FirstSelectedPawn;
		if (firstSelectedPawn != null && StanceRules.CanTakeOrders(firstSelectedPawn))
		{
			return !firstSelectedPawn.WorkTagIsDisabled(WorkTags.Violent);
		}
		return false;
	}

	public override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
	{
		Pawn charge = clickedThing as Pawn;
		if (charge == null)
		{
			return null;
		}
		Pawn guard = context.FirstSelectedPawn;
		if (!Bodyguard.CanGuard(guard, charge))
		{
			return null;
		}
		if (charge.Faction == null || !charge.Faction.IsPlayer)
		{
			return null;
		}
		CompArsenal comp = CompArsenal.For(guard);
		if (comp == null)
		{
			return null;
		}
		if (comp.guarding == charge)
		{
			return new FloatMenuOption("ASA_Bodyguard_Stop".Translate(charge.LabelShort), () =>
			{
				comp.guarding = null;
				guard.jobs?.EndCurrentJob(JobCondition.InterruptForced);
			});
		}
		return new FloatMenuOption("ASA_Bodyguard_Start".Translate(charge.LabelShort), () =>
		{
			comp.guarding = charge;
			guard.jobs?.EndCurrentJob(JobCondition.InterruptForced);
		});
	}
}
