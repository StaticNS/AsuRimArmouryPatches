using Verse;

namespace ArmouryTactics;

public class PawnStorage : IExposable
{
	public ThingWithComps offHandWeapon;

	public Pawn_StanceTracker stances;

	public void ExposeData()
	{
		Scribe_References.Look(ref offHandWeapon, "offHandWeapon");
		if (stances != null)
		{
			object[] array = new object[1] { stances.pawn };
			Scribe_Deep.Look(ref stances, "stances", false, array);
		}
	}
}
