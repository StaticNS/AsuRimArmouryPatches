using System;

namespace AsuRim.Armoury;

public enum CombatStance
{
	Hold,
	Engage,
	Brawl,
	Hide,
	Hunt,
	VIP,
	Escort,
	Breach,
	Immobilise,
	Suppress,
	Covering,
	Medic,
	[Obsolete("Folded into Engage. Kept so pre-merge saves still load.")]
	Skirmish
}
