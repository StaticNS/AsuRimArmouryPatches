using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AsuRim.Armoury;

public class WeaponPolicy : Policy
{
	public ThingDef mainDef;

	public ThingDef mainStuff;

	public ThingDef offDef;

	public ThingDef offStuff;

	public QualityCategory mainMinQuality;

	public QualityCategory offMinQuality;

	public List<LoadoutSlot> sidearms = new List<LoadoutSlot>();

	public List<LoadoutSlot> ammo = new List<LoadoutSlot>();

	public List<LoadoutSlot> meals = new List<LoadoutSlot>();

	public CombatStance stance;

	public Area retreatArea;

	public float retreatAtHealth;

	public bool walkFire;

	protected override string LoadKey
	{
		public get
		{
			return "ASA_WeaponPolicy";
		}
	}

	public bool IsPair
	{
		get
		{
			if (mainDef != null)
			{
				return offDef != null;
			}
			return false;
		}
	}

	public string Summary
	{
		get
		{
			if (mainDef == null)
			{
				return "ASA_Policy_Unarmed".Translate();
			}
			if (offDef == null)
			{
				return mainDef.label;
			}
			if (offDef == mainDef)
			{
				return "ASA_Policy_Paired".Translate(mainDef.label);
			}
			return "ASA_Policy_Mixed".Translate(mainDef.label, offDef.label);
		}
	}

	public Area RetreatAreaFor(Pawn pawn)
	{
		if (retreatArea == null || pawn?.Map == null)
		{
			return null;
		}
		if (retreatArea.Map != pawn.Map)
		{
			return null;
		}
		if (!retreatArea.Map.areaManager.AllAreas.Contains(retreatArea))
		{
			retreatArea = null;
			return null;
		}
		return retreatArea;
	}

	public WeaponPolicy()
	{
	}

	public WeaponPolicy(int id, string label)
		: base(id, label)
	{
	}

	public override void CopyFrom(Policy other)
	{
		if (other is WeaponPolicy weaponPolicy)
		{
			mainDef = weaponPolicy.mainDef;
			mainStuff = weaponPolicy.mainStuff;
			offDef = weaponPolicy.offDef;
			offStuff = weaponPolicy.offStuff;
			mainMinQuality = weaponPolicy.mainMinQuality;
			offMinQuality = weaponPolicy.offMinQuality;
			stance = weaponPolicy.stance;
			retreatArea = weaponPolicy.retreatArea;
			retreatAtHealth = weaponPolicy.retreatAtHealth;
			walkFire = weaponPolicy.walkFire;
			sidearms = CopySlots(weaponPolicy.sidearms);
			ammo = CopySlots(weaponPolicy.ammo);
			meals = CopySlots(weaponPolicy.meals);
		}
	}

	private static List<LoadoutSlot> CopySlots(List<LoadoutSlot> from)
	{
		List<LoadoutSlot> list = new List<LoadoutSlot>();
		if (from == null)
		{
			return list;
		}
		for (int i = 0; i < from.Count; i++)
		{
			LoadoutSlot loadoutSlot = from[i];
			if (loadoutSlot != null)
			{
				list.Add(new LoadoutSlot
				{
					def = loadoutSlot.def,
					stuff = loadoutSlot.stuff,
					count = loadoutSlot.count,
					minQuality = loadoutSlot.minQuality
				});
			}
		}
		return list;
	}

	public void CaptureFrom(Pawn pawn)
	{
		ThingWithComps thingWithComps = Arsenal.MainHand(pawn);
		ThingWithComps thingWithComps2 = Arsenal.OffHand(pawn);
		if (thingWithComps?.def != mainDef)
		{
			mainMinQuality = QualityCategory.Awful;
		}
		if (thingWithComps2?.def != offDef)
		{
			offMinQuality = QualityCategory.Awful;
		}
		mainDef = thingWithComps?.def;
		mainStuff = thingWithComps?.Stuff;
		offDef = thingWithComps2?.def;
		offStuff = thingWithComps2?.Stuff;
		sidearms = new List<LoadoutSlot>();
		ThingOwner<Thing> thingOwner = pawn.inventory?.innerContainer;
		if (thingOwner != null)
		{
			for (int i = 0; i < thingOwner.Count; i++)
			{
				if (thingOwner[i] is ThingWithComps thingWithComps3 && ArsenalRules.IsWieldable(thingWithComps3))
				{
					sidearms.Add(new LoadoutSlot(thingWithComps3));
				}
			}
		}
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal != null)
		{
			stance = compArsenal.stance;
			walkFire = compArsenal.walkFire;
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Defs.Look(ref mainDef, "mainDef");
		Scribe_Defs.Look(ref mainStuff, "mainStuff");
		Scribe_Defs.Look(ref offDef, "offDef");
		Scribe_Defs.Look(ref offStuff, "offStuff");
		Scribe_Values.Look(ref mainMinQuality, "mainMinQuality", QualityCategory.Awful);
		Scribe_Values.Look(ref offMinQuality, "offMinQuality", QualityCategory.Awful);
		Scribe_Values.Look(ref stance, "stance", CombatStance.Hold);
		Scribe_References.Look(ref retreatArea, "retreatArea");
		Scribe_Values.Look(ref retreatAtHealth, "retreatAtHealth", 0f);
		Scribe_Values.Look(ref walkFire, "walkFire", defaultValue: false);
		Scribe_Collections.Look(ref sidearms, "sidearms", LookMode.Deep);
		Scribe_Collections.Look(ref ammo, "ammo", LookMode.Deep);
		Scribe_Collections.Look(ref meals, "meals", LookMode.Deep);
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			if (sidearms == null)
			{
				sidearms = new List<LoadoutSlot>();
			}
			sidearms.RemoveAll((LoadoutSlot s) => s == null || s.def == null);
			if (ammo == null)
			{
				ammo = new List<LoadoutSlot>();
			}
			ammo.RemoveAll((LoadoutSlot s) => s == null || s.def == null);
			if (meals == null)
			{
				meals = new List<LoadoutSlot>();
			}
			meals.RemoveAll((LoadoutSlot s) => s == null || s.def == null);
		}
	}
}
