using RimWorld;
using Verse;

namespace AsuRim.Armoury;

public class FloatMenuOptionProvider_StopDualWield : FloatMenuOptionProvider
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

	protected override bool CanSelfTarget
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

	protected override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
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
