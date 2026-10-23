using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AsuRimArmoury.Kit;

public sealed class Registry
{
	private static readonly List<Registry> all;

	private static readonly Dictionary<Def, Registry> byDef;

	private static int total;

	private readonly HashSet<Pawn> pawns = new HashSet<Pawn>();

	private readonly Func<Pawn, bool> holds;

	public readonly string Name;

	public static bool Any => total > 0;

	public static int Total => total;

	public int Count => pawns.Count;

	static Registry()
	{
		all = new List<Registry>();
		byDef = new Dictionary<Def, Registry>();
		CacheReset.Register(ClearAll);
		CacheReset.RegisterRebuild(RebuildAll);
	}

	private Registry(string name, Func<Pawn, bool> holds)
	{
		Name = name;
		this.holds = holds;
		all.Add(this);
		if (holds != null && Current.ProgramState == ProgramState.Playing)
		{
			Rebuild();
		}
	}

	public static Registry Create(string name, Func<Pawn, bool> holds = null)
	{
		return new Registry(name, holds);
	}

	public static Registry ForHediff(HediffDef def)
	{
		return Keyed(def, (Pawn p) => p.health?.hediffSet != null && p.health.hediffSet.HasHediff(def));
	}

	public static Registry ForGene(GeneDef def, bool activeOnly = true)
	{
		return Keyed(def, (Pawn p) => p.genes != null && ((!activeOnly) ? (p.genes.GetGene(def) != null) : p.genes.HasActiveGene(def)));
	}

	public static Registry ForApparel(ThingDef def)
	{
		return Keyed(def, (Pawn p) => Wears(p, def));
	}

	private static Registry Keyed(Def def, Func<Pawn, bool> holds)
	{
		if (byDef.TryGetValue(def, out var value))
		{
			return value;
		}
		Registry registry = new Registry(def.defName, holds);
		byDef[def] = registry;
		return registry;
	}

	private static bool Wears(Pawn pawn, ThingDef def)
	{
		List<Apparel> list = pawn.apparel?.WornApparel;
		if (list == null)
		{
			return false;
		}
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].def == def)
			{
				return true;
			}
		}
		return false;
	}

	public bool Contains(Pawn pawn)
	{
		if (pawns.Count != 0 && pawn != null)
		{
			return pawns.Contains(pawn);
		}
		return false;
	}

	public void Add(Pawn pawn)
	{
		if (pawn != null && pawns.Add(pawn))
		{
			total++;
		}
	}

	public void Remove(Pawn pawn)
	{
		if (pawn != null && pawns.Remove(pawn))
		{
			total--;
		}
	}

	public void Set(Pawn pawn, bool present)
	{
		if (present)
		{
			Add(pawn);
		}
		else
		{
			Remove(pawn);
		}
	}

	public void Refresh(Pawn pawn)
	{
		if (holds != null && pawn != null)
		{
			Set(pawn, holds(pawn));
		}
	}

	public HashSet<Pawn>.Enumerator GetEnumerator()
	{
		return pawns.GetEnumerator();
	}

	public void Clear()
	{
		total -= pawns.Count;
		pawns.Clear();
	}

	public void Rebuild()
	{
		Clear();
		if (holds == null)
		{
			return;
		}
		List<Pawn> allMapsWorldAndTemporary_Alive = PawnsFinder.AllMapsWorldAndTemporary_Alive;
		for (int i = 0; i < allMapsWorldAndTemporary_Alive.Count; i++)
		{
			Pawn pawn = allMapsWorldAndTemporary_Alive[i];
			if (pawn != null && holds(pawn))
			{
				Add(pawn);
			}
		}
	}

	public static void ClearAll()
	{
		for (int i = 0; i < all.Count; i++)
		{
			all[i].pawns.Clear();
		}
		total = 0;
	}

	public static void RebuildAll()
	{
		for (int i = 0; i < all.Count; i++)
		{
			if (all[i].holds != null)
			{
				all[i].Rebuild();
			}
		}
	}
}
