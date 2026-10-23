using System;
using AsuRimArmoury;
using UnityEngine;
using Verse;

namespace ArmouryTactics;

public class Mod_ArmouryTactics : Mod
{
	public Mod_ArmouryTactics(ModContentPack content)
		: base(content)
	{
		GetSettings<ModSettings_ArmouryTactics>();
		ArmouryPatching.SettingsLoaded();
	}

	public override void DoSettingsWindowContents(Rect inRect)
	{
		TacticsPage.Draw(inRect);
	}

	public override string SettingsCategory()
	{
		return "";
	}

	public override void WriteSettings()
	{
		try
		{
			Setup.CheckInvesions();
			Setup.SetupDW(processInversions: true);
		}
		catch (Exception ex)
		{
			Log.Error("[ArmouryTactics] Error writing settings. Skipping...\n" + ex);
		}
		base.WriteSettings();
	}
}
