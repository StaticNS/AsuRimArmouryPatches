using AsuRimArmoury.Kit;
using Verse;

namespace AsuRim.Armoury;

public static class ArmouryTracked
{
	public static readonly Registry OffHand = Registry.Create("OffHand", (Pawn p) => CompArsenal.For(p)?.offHand != null);
}
