using RimWorld;
using Verse;

namespace AsuRim.Armoury;

public class LoadoutSlot : IExposable
{
	public ThingDef def;

	public ThingDef stuff;

	public int count = 1;

	public QualityCategory minQuality;

	public LoadoutSlot()
	{
	}

	public LoadoutSlot(Thing t)
	{
		def = t.def;
		stuff = t.Stuff;
	}

	public bool Matches(Thing t)
	{
		if (t != null)
		{
			return t.def == def;
		}
		return false;
	}

	public void ExposeData()
	{
		Scribe_Defs.Look(ref def, "def");
		Scribe_Defs.Look(ref stuff, "stuff");
		Scribe_Values.Look(ref count, "count", 1);
		Scribe_Values.Look(ref minQuality, "minQuality", QualityCategory.Awful);
	}
}
