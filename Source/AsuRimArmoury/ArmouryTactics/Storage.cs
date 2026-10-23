using System.Collections.Generic;
using Verse;

namespace ArmouryTactics;

internal class Storage : GameComponent
{
	public HashSet<int> hasOffhandCache = new HashSet<int>();

	public HashSet<Thing> offHands = new HashSet<Thing>();

	public Dictionary<Pawn, PawnStorage> store = new Dictionary<Pawn, PawnStorage>();

	public static Storage _instance;

	private static List<Pawn> keysWorkingList = new List<Pawn>();

	private static List<PawnStorage> valuesWorkingList = new List<PawnStorage>();

	public Storage(Game game)
	{
		_instance = this;
	}

	public override void FinalizeInit()
	{
		foreach (var (pawn2, pawnStorage2) in store)
		{
			if (pawnStorage2.offHandWeapon != null)
			{
				hasOffhandCache.Add(pawn2.thingIDNumber);
			}
		}
	}

	public override void ExposeData()
	{
		if (Scribe.mode == LoadSaveMode.Saving)
		{
			store.RemoveAll((KeyValuePair<Pawn, PawnStorage> x) => x.Key.Destroyed);
		}
		Scribe_Collections.Look(ref offHands, "offHands", LookMode.Reference);
		Scribe_Collections.Look(ref store, "store", LookMode.Reference, LookMode.Deep, ref keysWorkingList, ref valuesWorkingList);
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			if (offHands == null)
			{
				offHands = new HashSet<Thing>();
			}
			if (store == null)
			{
				store = new Dictionary<Pawn, PawnStorage>();
			}
			offHands.Remove(null);
		}
	}
}
