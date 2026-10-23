using Verse;
using Verse.AI.Group;

namespace AsuRim.Armoury;

public class LordJob_PlayerBreach : LordJob
{
	private IntVec3 target;

	public override bool AddFleeToil => false;

	public LordJob_PlayerBreach()
	{
	}

	public LordJob_PlayerBreach(IntVec3 target)
	{
		this.target = target;
	}

	public override StateGraph CreateGraph()
	{
		return new StateGraph
		{
			StartingToil = new LordToil_PlayerBreach
			{
				breachTarget = target
			}
		};
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref target, "target");
	}
}
