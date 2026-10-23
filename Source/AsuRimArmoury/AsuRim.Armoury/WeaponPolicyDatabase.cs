using System.Collections.Generic;
using Verse;

namespace AsuRim.Armoury;

public class WeaponPolicyDatabase : GameComponent
{
	private List<WeaponPolicy> policies = new List<WeaponPolicy>();

	private int nextId = 1;

	public static WeaponPolicyDatabase Current => Verse.Current.Game?.GetComponent<WeaponPolicyDatabase>();

	public List<WeaponPolicy> Policies => policies;

	public WeaponPolicyDatabase(Game game)
	{
	}

	public WeaponPolicy Default()
	{
		if (policies.Count == 0)
		{
			MakeNew("ASA_Policy_Default".Translate());
		}
		return policies[0];
	}

	public WeaponPolicy MakeNew(string label = null)
	{
		WeaponPolicy weaponPolicy = new WeaponPolicy(nextId++, label ?? ((string)"ASA_Policy_NewName".Translate(policies.Count + 1)));
		policies.Add(weaponPolicy);
		return weaponPolicy;
	}

	public WeaponPolicy MakeFrom(Pawn pawn)
	{
		WeaponPolicy weaponPolicy = MakeNew();
		weaponPolicy.CaptureFrom(pawn);
		return weaponPolicy;
	}

	public void Delete(WeaponPolicy policy)
	{
		foreach (Map map in Find.Maps)
		{
			foreach (Pawn item in map.mapPawns.AllPawnsSpawned)
			{
				CompArsenal compArsenal = CompArsenal.For(item);
				if (compArsenal != null && compArsenal.policy == policy)
				{
					compArsenal.policy = null;
				}
			}
		}
		policies.Remove(policy);
	}

	public override void ExposeData()
	{
		Scribe_Values.Look(ref nextId, "nextId", 1);
		Scribe_Collections.Look(ref policies, "policies", LookMode.Deep);
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			if (policies == null)
			{
				policies = new List<WeaponPolicy>();
			}
			policies.RemoveAll((WeaponPolicy p) => p == null);
		}
	}
}
