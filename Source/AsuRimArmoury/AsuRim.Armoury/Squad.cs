using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public class Squad : IExposable, ILoadReferenceable, IRenameable
{
	public static int Version;

	public int id = -1;

	public string label;

	public bool collapsed;

	private List<Pawn> members = new List<Pawn>();

	private Pawn leader;

	private CombatStance stance;

	private Area area;

	public Area Area
	{
		get
		{
			if (area != null && !area.Map.areaManager.AllAreas.Contains(area))
			{
				area = null;
			}
			return area;
		}
		set
		{
			area = value;
		}
	}

	public List<Pawn> Members => members;

	public Pawn Leader => leader;

	public bool Escorting
	{
		get
		{
			if (stance == CombatStance.Escort)
			{
				return leader != null;
			}
			return false;
		}
	}

	public string RenamableLabel
	{
		get
		{
			return label;
		}
		set
		{
			label = value;
		}
	}

	public string BaseLabel => label;

	public string InspectLabel => label;

	public CombatStance Stance
	{
		get
		{
			return stance;
		}
		set
		{
			if (stance == CombatStance.Escort && value != CombatStance.Escort)
			{
				ClearEscort();
			}
			stance = value;
			if (stance == CombatStance.Escort)
			{
				PushEscort();
			}
			else
			{
				PushStance();
			}
			SurveillanceShiftEscort.Sync(this);
		}
	}

	public void SetLeader(Pawn pawn)
	{
		if ((pawn == null || members.Contains(pawn)) && leader != pawn)
		{
			ClearEscort();
			leader = pawn;
			Version++;
			if (stance == CombatStance.Escort)
			{
				PushEscort();
			}
			SurveillanceShiftEscort.Sync(this);
		}
	}

	public Squad()
	{
	}

	public Squad(int id, string label)
	{
		this.id = id;
		this.label = label;
	}

	private void PushEscort()
	{
		CleanUp();
		if (leader == null)
		{
			return;
		}
		for (int i = 0; i < members.Count; i++)
		{
			Pawn pawn = members[i];
			CompArsenal compArsenal = CompArsenal.For(pawn);
			if (compArsenal != null)
			{
				compArsenal.guarding = ((pawn == leader) ? null : leader);
				pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced);
			}
		}
	}

	private void ClearEscort()
	{
		if (leader == null)
		{
			return;
		}
		for (int i = 0; i < members.Count; i++)
		{
			Pawn pawn = members[i];
			CompArsenal compArsenal = CompArsenal.For(pawn);
			if (compArsenal != null && compArsenal.guarding == leader)
			{
				compArsenal.guarding = null;
				pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced);
			}
		}
	}

	public void PushStance()
	{
		CleanUp();
		for (int i = 0; i < members.Count; i++)
		{
			CompArsenal compArsenal = CompArsenal.For(members[i]);
			if (compArsenal != null && StanceRules.Allows(members[i], stance, out var _))
			{
				StanceGizmo.SetStance(members[i], compArsenal, stance, manual: true);
			}
		}
	}

	public bool Contains(Pawn pawn)
	{
		return members.Contains(pawn);
	}

	public void Add(Pawn pawn)
	{
		if (pawn == null || members.Contains(pawn))
		{
			return;
		}
		SquadManager.Current?.RemoveFromAll(pawn);
		members.Add(pawn);
		Version++;
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal == null)
		{
			return;
		}
		string reason;
		if (stance == CombatStance.Escort)
		{
			if (leader != null && pawn != leader)
			{
				compArsenal.guarding = leader;
				pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced);
			}
			SurveillanceShiftEscort.Sync(pawn);
		}
		else if (StanceRules.Allows(pawn, stance, out reason))
		{
			StanceGizmo.SetStance(pawn, compArsenal, stance);
		}
	}

	public void Remove(Pawn pawn)
	{
		if (pawn != null)
		{
			CompArsenal compArsenal = CompArsenal.For(pawn);
			if (compArsenal != null && leader != null && compArsenal.guarding == leader)
			{
				compArsenal.guarding = null;
				pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced);
			}
			if (members.Remove(pawn))
			{
				Version++;
			}
			if (leader == pawn)
			{
				SetLeader(null);
			}
			SurveillanceShiftEscort.Sync(pawn);
		}
	}

	public void CleanUp()
	{
		for (int i = 0; i < members.Count; i++)
		{
			Pawn pawn = members[i];
			if (pawn != null && pawn.Dead && pawn.RaceProps.IsMechanoid)
			{
				CompArsenal compArsenal = CompArsenal.For(pawn);
				if (compArsenal != null)
				{
					compArsenal.lastSquadId = id;
				}
			}
		}
		if (members.RemoveAll((Pawn p) => p == null || p.Dead || p.Destroyed || p.Faction == null || !p.Faction.IsPlayer) > 0)
		{
			Version++;
		}
		if (leader != null && !members.Contains(leader))
		{
			leader = null;
			Version++;
			if (stance == CombatStance.Escort)
			{
				stance = CombatStance.Hold;
			}
		}
	}

	public string GetUniqueLoadID()
	{
		return "ASA_Squad_" + id;
	}

	public void ExposeData()
	{
		Scribe_Values.Look(ref collapsed, "collapsed", defaultValue: false);
		Scribe_Values.Look(ref id, "id", -1);
		Scribe_Values.Look(ref label, "label");
		Scribe_Values.Look(ref stance, "stance", CombatStance.Hold);
		Scribe_Collections.Look(ref members, "members", LookMode.Reference);
		Scribe_References.Look(ref leader, "leader");
		Scribe_References.Look(ref area, "area");
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			if (members == null)
			{
				members = new List<Pawn>();
			}
			members.RemoveAll((Pawn p) => p == null);
		}
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			stance = stance.Normalise();
		}
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			Version++;
		}
	}
}
