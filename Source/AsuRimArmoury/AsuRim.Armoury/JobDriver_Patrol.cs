using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public class JobDriver_Patrol : JobDriver
{
	private const int PauseTicks = 240;

	private CompGuardPost TargetNode => job.targetA.Thing?.TryGetComp<CompGuardPost>();

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return true;
	}

	public override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
		this.FailOn(() => GuardStandDown.Active);
		yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
		Toil toil = Toils_General.Wait(TargetNode?.dwellTicks ?? 240);
		toil.handlingFacing = true;
		toil.initAction = () =>
		{
			CompArsenal compArsenal = CompArsenal.For(pawn);
			if (compArsenal != null)
			{
				compArsenal.onDutyPost = TargetNode;
			}
		};
		toil.tickIntervalAction = (int delta) =>
		{
			CompArsenal compArsenal = CompArsenal.For(pawn);
			if (compArsenal != null)
			{
				compArsenal.onDutyPost = TargetNode;
			}
			PostedCombat.Check(pawn, delta);
		};
		toil.AddFinishAction(() =>
		{
			CompGuardPost targetNode = TargetNode;
			CompArsenal compArsenal = CompArsenal.For(pawn);
			if (compArsenal != null && targetNode != null)
			{
				if (compArsenal.onDutyPost == targetNode)
				{
					compArsenal.onDutyPost = null;
				}
				Advance(compArsenal, targetNode);
			}
		});
		yield return toil;
	}

	private void Advance(CompArsenal comp, CompGuardPost node)
	{
		GuardRegistry guardRegistry = GuardRegistry.For(pawn.Map);
		if (guardRegistry == null)
		{
			return;
		}
		List<CompGuardPost> list = new List<CompGuardPost>();
		guardRegistry.Route(node.group, list);
		if (list.Count <= 1)
		{
			return;
		}
		int num = list.IndexOf(node);
		if (num < 0)
		{
			return;
		}
		if (node.loops)
		{
			comp.patrolIndex = (num + 1) % list.Count;
			comp.patrolReversing = false;
		}
		else if (comp.patrolReversing)
		{
			if (num <= 0)
			{
				comp.patrolReversing = false;
				comp.patrolIndex = 1;
			}
			else
			{
				comp.patrolIndex = num - 1;
			}
		}
		else if (num >= list.Count - 1)
		{
			comp.patrolReversing = true;
			comp.patrolIndex = list.Count - 2;
		}
		else
		{
			comp.patrolIndex = num + 1;
		}
	}
}
