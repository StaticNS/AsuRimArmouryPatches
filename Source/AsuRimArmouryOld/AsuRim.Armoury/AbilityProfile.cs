using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;

namespace AsuRim.Armoury;

internal sealed class AbilityProfile
{
	private static readonly Dictionary<AbilityDef, AbilityProfile> cache = new Dictionary<AbilityDef, AbilityProfile>();

	private static readonly string[] SelfFlags = new string[3] { "applyToSelf", "onlyApplyToSelf", "applyToCaster" };

	public bool HarmsCaster;

	private readonly List<HediffDef> buffs = new List<HediffDef>();

	public bool BuffActive(Pawn pawn)
	{
		HediffSet hediffSet = pawn.health?.hediffSet;
		if (hediffSet == null)
		{
			return false;
		}
		for (int i = 0; i < buffs.Count; i++)
		{
			if (hediffSet.HasHediff(buffs[i]))
			{
				return true;
			}
		}
		return false;
	}

	public static AbilityProfile Of(AbilityDef def)
	{
		if (cache.TryGetValue(def, out var value))
		{
			return value;
		}
		value = Build(def);
		cache[def] = value;
		return value;
	}

	private static AbilityProfile Build(AbilityDef def)
	{
		AbilityProfile abilityProfile = new AbilityProfile();
		VerbProperties verbProperties = def.verbProperties;
		bool flag = verbProperties?.targetParams != null && verbProperties.targetParams.canTargetSelf && (!verbProperties.targetable || !def.targetRequired);
		if (def.comps == null)
		{
			return abilityProfile;
		}
		for (int i = 0; i < def.comps.Count; i++)
		{
			AbilityCompProperties abilityCompProperties = def.comps[i];
			if (abilityCompProperties == null || (!flag && !HasSelfFlag(abilityCompProperties)))
			{
				continue;
			}
			FieldInfo[] fields = abilityCompProperties.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (FieldInfo fieldInfo in fields)
			{
				object value;
				try
				{
					value = fieldInfo.GetValue(abilityCompProperties);
				}
				catch (Exception)
				{
					continue;
				}
				if (value is MentalStateDef)
				{
					abilityProfile.HarmsCaster = true;
				}
				else if (value is HediffDef hediffDef)
				{
					if (Risky(hediffDef))
					{
						abilityProfile.HarmsCaster = true;
					}
					else if (!abilityProfile.buffs.Contains(hediffDef))
					{
						abilityProfile.buffs.Add(hediffDef);
					}
				}
			}
		}
		return abilityProfile;
	}

	private static bool HasSelfFlag(AbilityCompProperties comp)
	{
		Type type = comp.GetType();
		for (int i = 0; i < SelfFlags.Length; i++)
		{
			FieldInfo field = type.GetField(SelfFlags[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field != null && field.FieldType == typeof(bool) && (bool)field.GetValue(comp))
			{
				return true;
			}
		}
		return false;
	}

	private static bool Risky(HediffDef hediff)
	{
		if (hediff.lethalSeverity > 0f)
		{
			return true;
		}
		if (hediff.stages == null)
		{
			return false;
		}
		for (int i = 0; i < hediff.stages.Count; i++)
		{
			HediffStage hediffStage = hediff.stages[i];
			if (hediffStage != null)
			{
				if (hediffStage.lifeThreatening)
				{
					return true;
				}
				if (hediffStage.mentalBreakMtbDays > 0f)
				{
					return true;
				}
				if (!hediffStage.mentalStateGivers.NullOrEmpty())
				{
					return true;
				}
				if (RaisesBreakThreshold(hediffStage.statOffsets))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool RaisesBreakThreshold(List<StatModifier> offsets)
	{
		if (offsets == null)
		{
			return false;
		}
		for (int i = 0; i < offsets.Count; i++)
		{
			if (offsets[i]?.stat == StatDefOf.MentalBreakThreshold && offsets[i].value > 0f)
			{
				return true;
			}
		}
		return false;
	}
}
