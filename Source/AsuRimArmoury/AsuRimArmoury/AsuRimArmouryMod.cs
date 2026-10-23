using System.Collections.Generic;
using AsuRim.Armoury;
using UnityEngine;
using Verse;

namespace AsuRimArmoury;

public class AsuRimArmouryMod : Mod
{
	private static readonly string[] SubModTypeNames = new string[1] { "ArmouryTactics.Mod_ArmouryTactics" };

	private static List<Mod> subMods;

	private const float ContentPadding = 17f;

	private static List<Mod> SubMods
	{
		get
		{
			if (subMods == null)
			{
				subMods = new List<Mod>();
				for (int i = 0; i < SubModTypeNames.Length; i++)
				{
					foreach (Mod modHandle in LoadedModManager.ModHandles)
					{
						if (modHandle.GetType().FullName == SubModTypeNames[i])
						{
							subMods.Add(modHandle);
							break;
						}
					}
				}
			}
			return subMods;
		}
	}

	public AsuRimArmouryMod(ModContentPack content)
		: base(content)
	{
		GetSettings<ArmourySettingsStore>();
		GuardStandDown.CaptureSetting();
		ArmouryPatching.SettingsLoaded();
	}

	public override string SettingsCategory()
	{
		return "Rules of Engagement";
	}

	public override void DoSettingsWindowContents(Rect inRect)
	{
		List<Mod> list = SubMods;
		if (list.Count != 0)
		{
			Rect inRect2 = inRect.ContractedBy(17f);
			list[0].DoSettingsWindowContents(inRect2);
		}
	}

	public override void WriteSettings()
	{
		base.WriteSettings();
		List<Mod> list = SubMods;
		for (int i = 0; i < list.Count; i++)
		{
			list[i].WriteSettings();
		}
	}
}
