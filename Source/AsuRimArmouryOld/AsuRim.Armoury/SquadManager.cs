using System.Collections.Generic;
using Verse;

namespace AsuRim.Armoury;

public class SquadManager : GameComponent
{
	private static SquadManager instance;

	private readonly Game game;

	private readonly Dictionary<Pawn, Squad> squadByPawn = new Dictionary<Pawn, Squad>();

	private int mappedVersion = -1;

	private List<Squad> squads = new List<Squad>();

	private int nextId = 1;

	public static SquadManager Current
	{
		get
		{
			Game game = Verse.Current.Game;
			if (game == null)
			{
				return null;
			}
			if (instance == null || instance.game != game)
			{
				instance = game.GetComponent<SquadManager>();
			}
			return instance;
		}
	}

	public List<Squad> Squads => squads;

	public SquadManager(Game game)
	{
		this.game = game;
		instance = this;
		Squad.Version++;
	}

	public Squad NewSquad()
	{
		Squad squad = new Squad(nextId++, "ASA_SquadDefaultName".Translate(squads.Count + 1));
		squads.Add(squad);
		Squad.Version++;
		return squad;
	}

	public void Delete(Squad squad)
	{
		squads.Remove(squad);
		Squad.Version++;
		SurveillanceShiftEscort.Sync(squad);
		Find.ColonistBar?.MarkColonistsDirty();
	}

	public Squad SquadOf(Pawn pawn)
	{
		if (pawn == null)
		{
			return null;
		}
		if (mappedVersion != Squad.Version)
		{
			RebuildMap();
		}
		if (!squadByPawn.TryGetValue(pawn, out var value))
		{
			return null;
		}
		return value;
	}

	private void RebuildMap()
	{
		squadByPawn.Clear();
		for (int i = 0; i < squads.Count; i++)
		{
			List<Pawn> members = squads[i].Members;
			for (int j = 0; j < members.Count; j++)
			{
				if (members[j] != null)
				{
					squadByPawn.TryAdd(members[j], squads[i]);
				}
			}
		}
		mappedVersion = Squad.Version;
	}

	public Squad GetById(int id)
	{
		if (id < 0)
		{
			return null;
		}
		for (int i = 0; i < squads.Count; i++)
		{
			if (squads[i].id == id)
			{
				return squads[i];
			}
		}
		return null;
	}

	public void RemoveFromAll(Pawn pawn)
	{
		for (int i = 0; i < squads.Count; i++)
		{
			squads[i].Remove(pawn);
		}
	}

	public override void GameComponentTick()
	{
		int ticksGame = Find.TickManager.TicksGame;
		if (ticksGame % 2000 == 0)
		{
			for (int i = 0; i < squads.Count; i++)
			{
				squads[i].CleanUp();
			}
		}
		SurveillanceShiftEscort.Tick(ticksGame);
	}

	public override void FinalizeInit()
	{
		SurveillanceShiftEscort.SyncAll();
	}

	public override void ExposeData()
	{
		Scribe_Values.Look(ref nextId, "nextId", 1);
		Scribe_Collections.Look(ref squads, "squads", LookMode.Deep);
		if (Scribe.mode == LoadSaveMode.PostLoadInit && squads == null)
		{
			squads = new List<Squad>();
		}
		Squad.Version++;
	}
}
