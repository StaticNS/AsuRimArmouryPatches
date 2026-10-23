using AsuRim.Armoury;
using Verse;

namespace AsuRimArmoury;

internal class ArmourySettingsStore : ModSettings
{
	public override void ExposeData()
	{
		base.ExposeData();
		ArmourySettings.ExposeData();
	}
}
