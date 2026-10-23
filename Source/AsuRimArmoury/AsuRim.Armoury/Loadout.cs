using Verse;

namespace AsuRim.Armoury;

public class Loadout : IExposable
{
	private ThingDef mainDef;

	private ThingDef mainStuff;

	private ThingDef offDef;

	private ThingDef offStuff;

	public bool set;

	public ThingDef MainDef => mainDef;

	public ThingDef MainStuff => mainStuff;

	public ThingDef OffDef => offDef;

	public ThingDef OffStuff => offStuff;

	public bool IsEmpty => !set;

	public bool IsPair
	{
		get
		{
			if (set)
			{
				return offDef != null;
			}
			return false;
		}
	}

	public bool WantsUnarmed
	{
		get
		{
			if (set)
			{
				return mainDef == null;
			}
			return false;
		}
	}

	public string Label
	{
		get
		{
			if (!set)
			{
				return "ASA_Loadout_Unset".Translate();
			}
			if (mainDef == null)
			{
				return "ASA_Loadout_Unarmed".Translate();
			}
			if (offDef == null)
			{
				return mainDef.label;
			}
			if (offDef == mainDef && offStuff == mainStuff)
			{
				return "ASA_Loadout_Paired".Translate(mainDef.label);
			}
			return "ASA_Loadout_Mixed".Translate(mainDef.label, offDef.label);
		}
	}

	public void CaptureFrom(Pawn pawn)
	{
		ThingWithComps thingWithComps = Arsenal.MainHand(pawn);
		ThingWithComps thingWithComps2 = Arsenal.OffHand(pawn);
		mainDef = thingWithComps?.def;
		mainStuff = thingWithComps?.Stuff;
		offDef = thingWithComps2?.def;
		offStuff = thingWithComps2?.Stuff;
		set = true;
	}

	public void Clear()
	{
		mainDef = (mainStuff = (offDef = (offStuff = null)));
		set = false;
	}

	public bool Matches(ThingWithComps main, ThingWithComps off)
	{
		if (main?.def == mainDef && main?.Stuff == mainStuff && off?.def == offDef)
		{
			return off?.Stuff == offStuff;
		}
		return false;
	}

	public bool WantsAsMain(Thing t)
	{
		if (t != null && t.def == mainDef)
		{
			return t.Stuff == mainStuff;
		}
		return false;
	}

	public bool WantsAsOff(Thing t)
	{
		if (t != null && t.def == offDef)
		{
			return t.Stuff == offStuff;
		}
		return false;
	}

	public void ExposeData()
	{
		Scribe_Values.Look(ref set, "set", defaultValue: false);
		Scribe_Defs.Look(ref mainDef, "mainDef");
		Scribe_Defs.Look(ref mainStuff, "mainStuff");
		Scribe_Defs.Look(ref offDef, "offDef");
		Scribe_Defs.Look(ref offStuff, "offStuff");
	}
}
