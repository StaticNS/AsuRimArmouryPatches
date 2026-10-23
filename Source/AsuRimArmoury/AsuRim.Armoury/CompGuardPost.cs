using System.Collections.Generic;
using Verse;

namespace AsuRim.Armoury;

public class CompGuardPost : ThingComp
{
	private List<Pawn> assigned = new List<Pawn>();

	private Squad squad;

	public CombatStance stance;

	private Area overwatchArea;

	public PostGroup group;

	public int order;

	public int dwellTicks = 240;

	public bool loops;

	public PostFacing facing = PostFacing.South;

	public CompProperties_GuardPost Props => (CompProperties_GuardPost)props;

	public PostKind Kind => Props.kind;

	public Area OverwatchArea
	{
		get
		{
			if (overwatchArea != null && !overwatchArea.Map.areaManager.AllAreas.Contains(overwatchArea))
			{
				overwatchArea = null;
			}
			return overwatchArea;
		}
		set
		{
			overwatchArea = value;
		}
	}

	public Squad Squad
	{
		get
		{
			if (squad != null && SquadManager.Current != null && !SquadManager.Current.Squads.Contains(squad))
			{
				squad = null;
			}
			return squad;
		}
		set
		{
			squad = value;
		}
	}

	public List<Pawn> Manning
	{
		get
		{
			Squad squad = Squad;
			if (squad != null)
			{
				squad.CleanUp();
				return squad.Members;
			}
			assigned.RemoveAll((Pawn p) => p == null || p.Dead || p.Destroyed);
			return assigned;
		}
	}

	public bool HasAnyone => Manning.Count > 0;

	public IntVec3 StandCell
	{
		get
		{
			Thing thing = parent;
			if (!thing.def.hasInteractionCell)
			{
				return thing.Position;
			}
			return thing.InteractionCell;
		}
	}

	public bool IsAssigned(Pawn pawn)
	{
		if (pawn != null)
		{
			return Manning.Contains(pawn);
		}
		return false;
	}

	public bool IsIndividuallyAssigned(Pawn pawn)
	{
		if (pawn != null)
		{
			return assigned.Contains(pawn);
		}
		return false;
	}

	public void Assign(Pawn pawn)
	{
		if (pawn != null && !assigned.Contains(pawn))
		{
			assigned.Add(pawn);
		}
	}

	public void Unassign(Pawn pawn)
	{
		assigned.Remove(pawn);
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Collections.Look(ref assigned, "assigned", LookMode.Reference);
		Scribe_References.Look(ref squad, "squad");
		Scribe_References.Look(ref overwatchArea, "overwatchArea");
		Scribe_Values.Look(ref stance, "stance", CombatStance.Hold);
		Scribe_Values.Look(ref group, "group", PostGroup.Red);
		Scribe_Values.Look(ref order, "order", 0);
		Scribe_Values.Look(ref dwellTicks, "dwellTicks", 240);
		Scribe_Values.Look(ref loops, "loops", defaultValue: false);
		Scribe_Values.Look(ref facing, "facing", PostFacing.South);
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			if (assigned == null)
			{
				assigned = new List<Pawn>();
			}
			assigned.RemoveAll((Pawn p) => p == null);
		}
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		GuardRegistry guardRegistry = GuardRegistry.For(parent.Map);
		if (!respawningAfterLoad && Kind == PostKind.Patrol && guardRegistry != null)
		{
			order = guardRegistry.NextOrderFor(group);
		}
		guardRegistry?.Register(this);
	}

	public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
	{
		base.PostDeSpawn(map, mode);
		GuardRegistry.For(map)?.Deregister(this);
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
		if (GuardStandDown.Active)
		{
			yield break;
		}
		foreach (Gizmo item2 in PostGizmos.For(this))
		{
			yield return item2;
		}
	}

	public override string CompInspectStringExtra()
	{
		if (GuardStandDown.Active)
		{
			return ArmouryText.Get("ASA_SS_PostInactive");
		}
		List<Pawn> manning = Manning;
		if (manning.Count == 0)
		{
			return "ASA_Post_Unassigned".Translate();
		}
		string text = ((Squad != null) ? Squad.label : manning[0].LabelShortCap);
		return "ASA_Post_MannedBy".Translate(text) + "\n" + "ASA_Post_Orders".Translate(stance.LabelKey().Translate());
	}
}
