using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public class FloatMenuOptionProvider_AssignSidearm : FloatMenuOptionProvider
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

	protected override bool AppliesInt(FloatMenuContext context)
	{
		return Arsenal.CanCarrySidearms(context.FirstSelectedPawn);
	}

	protected override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
	{
		ThingWithComps weapon = clickedThing as ThingWithComps;
		if (weapon == null)
		{
			return null;
		}
		if (!ArsenalRules.IsWieldable(weapon))
		{
			return null;
		}
		Pawn pawn = context.FirstSelectedPawn;
		if (!ArsenalRules.CanWield(pawn, weapon))
		{
			return null;
		}
		if (!pawn.CanReach(weapon, PathEndMode.ClosestTouch, Danger.Deadly))
		{
			return null;
		}
		if (MassUtility.CountToPickUpUntilOverEncumbered(pawn, weapon) <= 0)
		{
			return null;
		}
		return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("ASA_Sidearm_Assign".Translate(weapon.LabelShort), () =>
		{
			weapon.SetForbidden(value: false, warnOnFail: false);
			CompArsenal compArsenal = CompArsenal.For(pawn);
			WeaponPolicy weaponPolicy = compArsenal?.policy;
			if (weaponPolicy == null && compArsenal != null)
			{
				WeaponPolicyDatabase current = WeaponPolicyDatabase.Current;
				if (current != null)
				{
					weaponPolicy = (compArsenal.policy = current.MakeFrom(pawn));
				}
			}
			if (weaponPolicy != null)
			{
				AddIfMissing(weaponPolicy.sidearms, weapon);
			}
			else
			{
				ArsenalActions.Remember(pawn, weapon);
			}
			Job job = JobMaker.MakeJob(JobDefOf.TakeInventory, weapon);
			job.count = 1;
			pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
		}, MenuOptionPriority.High), pawn, weapon);
	}

	private static void AddIfMissing(List<LoadoutSlot> sidearms, ThingWithComps weapon)
	{
		for (int i = 0; i < sidearms.Count; i++)
		{
			if (sidearms[i].Matches(weapon))
			{
				return;
			}
		}
		sidearms.Add(new LoadoutSlot(weapon));
	}
}
