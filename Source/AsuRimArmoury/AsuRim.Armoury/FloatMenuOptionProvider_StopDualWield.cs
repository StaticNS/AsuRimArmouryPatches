using RimWorld;
using Verse;

namespace AsuRim.Armoury;

public class FloatMenuOptionProvider_StopDualWield : FloatMenuOptionProvider
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
			return false;
		}
	}

	protected override bool CanSelfTarget
	{
		public get
		{
			return true;
		}
	}

	public override bool AppliesInt(FloatMenuContext context)
	{
		if (ArmourySettings.dualWieldEnabled)
		{
			return Arsenal.CanCarryWeapons(context.FirstSelectedPawn);
		}
		return false;
	}

	public override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
	{
		Pawn pawn = context.FirstSelectedPawn;
		if (clickedPawn != pawn)
		{
			return null;
		}
		ThingWithComps thingWithComps = Arsenal.OffHand(pawn);
		if (thingWithComps == null)
		{
			return null;
		}
		return new FloatMenuOption("DW_DropOffHand".Translate(thingWithComps.LabelShort), () =>
		{
			CompArsenal compArsenal = CompArsenal.For(pawn);
			if (compArsenal?.policy != null)
			{
				compArsenal.policy.offDef = null;
				compArsenal.policy.offStuff = null;
			}
			ArsenalActions.StowOffHand(pawn);
		});
	}
}
