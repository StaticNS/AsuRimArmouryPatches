using System.Collections.Generic;
using AsuRimArmoury.Kit;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsuRim.Armoury;

public static class WeaponScorer
{
	private readonly record struct Key(ThingDef def, ThingDef stuff, QualityCategory quality, int unique, int bucket);

	private const float BucketSize = 4f;

	private const int MaxBuckets = 16;

	private const float OutOfRangeScore = 0.01f;

	private const float MeleeViableDistance = 1.5f;

	private static readonly Dictionary<Key, float> cache;

	private const float EmpAgainstMachines = 2f;

	private const float EmpAgainstStructures = 0.5f;

	static WeaponScorer()
	{
		cache = new Dictionary<Key, float>();
		CacheReset.Register(cache.Clear);
	}

	public static void ClearCache()
	{
		cache.Clear();
	}

	public static float Score(Pawn pawn, ThingWithComps weapon, float distance)
	{
		return Score(pawn, weapon, distance, null);
	}

	public static float Score(Pawn pawn, ThingWithComps weapon, float distance, Thing target)
	{
		if (weapon == null || pawn == null)
		{
			return 0f;
		}
		int num = Mathf.Clamp(Mathf.RoundToInt(distance / 4f), 0, 16);
		weapon.TryGetQuality(out var qc);
		int unique = ((ModsConfig.OdysseyActive && weapon.TryGetComp<CompUniqueWeapon>() != null) ? weapon.thingIDNumber : 0);
		Key key = new Key(weapon.def, weapon.Stuff, qc, unique, num);
		if (!cache.TryGetValue(key, out var value))
		{
			value = ComputeBaseScore(weapon, (float)num * 4f);
			cache[key] = value;
		}
		if (value <= 0f)
		{
			return 0f;
		}
		float num2 = DamageFactor(weapon, target);
		if (num2 <= 0f)
		{
			return 0.01f;
		}
		return value * num2 * PawnFactor(pawn, weapon);
	}

	public static bool CanHit(ThingWithComps weapon, float distance, Thing target)
	{
		if (weapon == null || DamageFactor(weapon, target) <= 0f)
		{
			return false;
		}
		if (!weapon.def.IsRangedWeapon)
		{
			return distance <= 1.5f;
		}
		VerbProperties verbProperties = weapon.GetComp<CompEquippable>()?.PrimaryVerb?.verbProps;
		if (verbProperties != null && distance <= verbProperties.range)
		{
			return distance >= verbProperties.minRange;
		}
		return false;
	}

	private static float DamageFactor(ThingWithComps weapon, Thing target)
	{
		if (!weapon.def.IsRangedWeapon)
		{
			return 1f;
		}
		DamageDef damageDef = weapon.GetComp<CompEquippable>()?.PrimaryVerb?.GetDamageDef();
		if (damageDef == null)
		{
			return 1f;
		}
		if (damageDef == DamageDefOf.EMP)
		{
			if (target is Pawn pawn)
			{
				if (!pawn.RaceProps.IsFlesh)
				{
					return 2f;
				}
				return 0f;
			}
			if (target == null)
			{
				return 0f;
			}
			return 0.5f;
		}
		if (!damageDef.harmsHealth)
		{
			return 0f;
		}
		return 1f;
	}

	private static float ComputeBaseScore(ThingWithComps weapon, float distance)
	{
		if (weapon.def.IsRangedWeapon)
		{
			Verb verb = weapon.GetComp<CompEquippable>()?.PrimaryVerb;
			if (verb == null)
			{
				return 0f;
			}
			return RangedDps(verb, weapon, distance);
		}
		return MeleeDps(weapon, distance);
	}

	private static float RangedDps(Verb verb, ThingWithComps weapon, float distance)
	{
		VerbProperties verbProps = verb.verbProps;
		if (verbProps == null)
		{
			return 0f;
		}
		if (distance > verbProps.range)
		{
			return 0.01f;
		}
		if (distance < verbProps.minRange)
		{
			return 0.01f;
		}
		float num = ((float?)verbProps.defaultProjectile?.projectile?.GetDamageAmount(weapon)) ?? 0f;
		if (num <= 0f)
		{
			return 0.01f;
		}
		int num2 = Mathf.Max(1, verbProps.burstShotCount);
		float num3 = verbProps.warmupTime + (float)(num2 - 1) * verbProps.ticksBetweenBurstShots.TicksToSeconds() + weapon.GetStatValue(StatDefOf.RangedWeapon_Cooldown);
		if (num3 <= 0f)
		{
			return 0.01f;
		}
		float hitChanceFactor = verbProps.GetHitChanceFactor(weapon, distance);
		return num * (float)num2 * hitChanceFactor / num3;
	}

	private static float MeleeDps(ThingWithComps weapon, float distance)
	{
		float statValue = weapon.GetStatValue(StatDefOf.MeleeWeapon_AverageDPS);
		if (statValue <= 0f)
		{
			return 0f;
		}
		if (distance > 1.5f)
		{
			return 0.01f;
		}
		return statValue;
	}

	private static float PawnFactor(Pawn pawn, ThingWithComps weapon)
	{
		if (pawn.skills == null)
		{
			return 1f;
		}
		if (weapon.def.IsRangedWeapon)
		{
			return Mathf.Max(0.05f, pawn.GetStatValue(StatDefOf.ShootingAccuracyPawn));
		}
		return Mathf.Max(0.05f, pawn.GetStatValue(StatDefOf.MeleeHitChance));
	}
}
