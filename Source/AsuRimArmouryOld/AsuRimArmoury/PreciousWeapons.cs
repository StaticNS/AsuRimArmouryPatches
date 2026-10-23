using RimWorld;
using Verse;

namespace AsuRimArmoury;

internal static class PreciousWeapons
{
	internal static bool IsPrecious(Thing thing)
	{
		if (!(thing is ThingWithComps thing2))
		{
			return false;
		}
		if (thing2.TryGetComp<CompUniqueWeapon>() != null)
		{
			return true;
		}
		CompBladelinkWeapon compBladelinkWeapon = thing2.TryGetComp<CompBladelinkWeapon>();
		if (compBladelinkWeapon != null && compBladelinkWeapon.Biocoded)
		{
			return true;
		}
		return CompBiocodable.IsBiocoded(thing2);
	}

	internal static bool ShouldKeep(Thing outgoing)
	{
		return IsPrecious(outgoing);
	}
}
