using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

internal static class TargetScoring
{
	private const float ShortRange = 10f;

	private const float LongRange = 40f;

	private const float ProximityWeightShort = 1f;

	private const float ProximityWeightLong = 0.5f;

	private const float AccuracyWeightShort = 0.3f;

	private const float AccuracyWeightLong = 1f;

	private const float AccuracyScale = 70f;

	private const float RecentTargetWindowTicks = 300f;

	private const float RecentTargetBonus = 40f;

	private const float ThreatSwapMargin = 1.6f;

	private const int RefinedCandidates = 3;

	private const float VanillaDownedPenalty = 50f;

	private const float DeprioritizedDownedPenalty = 100f;

	private const float MeleeThreatNear = 1.5f;

	private const float MeleeThreatRange = 6f;

	private const float MeleeThreatFloor = 45f;

	private const float MeleeThreatPeak = 100f;

	private const float ClosingRange = 20f;

	private const float ClosingBonus = 25f;

	private const float TargetingMeBonus = 8f;

	private const float AllyInMeleeRange = 2.5f;

	private const float AllyInMeleeBonus = 30f;

	private static IAttackTargetSearcher searcher;

	private static Thing searcherThing;

	private static Pawn searcherPawn;

	private static Map map;

	private static Verb verb;

	private static Thing equipment;

	private static IntVec3 casterPos;

	private static float proximityWeight;

	private static float accuracyWeight;

	private static bool ignoreMaluses;

	private static bool canGoWild;

	private static bool empOnly;

	private static Faction searcherFaction;

	private static bool casterRoofed;

	private static float weather;

	private static int ticksGame;

	private static bool threatActive;

	private static bool shooterStatsReady;

	private static float accPawn;

	private static float accTouch;

	private static float accShort;

	private static float accMedium;

	private static float accLong;

	private static float equipTouch = float.NaN;

	private static float equipShort = float.NaN;

	private static float equipMedium = float.NaN;

	private static float equipLong = float.NaN;

	private static float darkLit = float.NaN;

	private static float darkDark = float.NaN;

	private static float darkIndoorDark = float.NaN;

	private static float darkIndoorLit = float.NaN;

	private static readonly List<float> scores = new List<float>();

	private static readonly List<float> hitChances = new List<float>();

	private static readonly List<float> aimFactors = new List<float>();

	private static readonly List<float> darkOffsets = new List<float>();

	private static readonly List<float> outerFactors = new List<float>();

	private static readonly List<bool> shootable = new List<bool>();

	private static readonly int[] topIndices = new int[3];

	private static bool loggedError;

	private static readonly Func<IAttackTarget, IAttackTargetSearcher, Verb, bool> canShootAtFromCurrentPosition =
		AccessTools.MethodDelegate<Func<IAttackTarget, IAttackTargetSearcher, Verb, bool>>(AccessTools.Method(typeof(AttackTargetFinder), "CanShootAtFromCurrentPosition"));

	private static readonly Func<Thing, float> nonCombatantScore =
		AccessTools.MethodDelegate<Func<Thing, float>>(AccessTools.Method(typeof(AttackTargetFinder), "NonCombatantScore"));

	private static readonly Func<IAttackTarget, IAttackTargetSearcher, Verb, float> friendlyFireBlastRadiusTargetScoreOffset =
		AccessTools.MethodDelegate<Func<IAttackTarget, IAttackTargetSearcher, Verb, float>>(AccessTools.Method(typeof(AttackTargetFinder), "FriendlyFireBlastRadiusTargetScoreOffset"));

	private static readonly Func<IAttackTarget, IAttackTargetSearcher, Verb, float> friendlyFireConeTargetScoreOffset =
		AccessTools.MethodDelegate<Func<IAttackTarget, IAttackTargetSearcher, Verb, float>>(AccessTools.Method(typeof(AttackTargetFinder), "FriendlyFireConeTargetScoreOffset"));

	private static readonly Func<VerbProperties, RangeCategory, Thing, float> adjustedAccuracy =
		AccessTools.MethodDelegate<Func<VerbProperties, RangeCategory, Thing, float>>(AccessTools.Method(typeof(VerbProperties), "AdjustedAccuracy"));

	internal static bool Busy => searcher != null;

	internal static bool Applies(IAttackTargetSearcher s)
	{
		if (!ArmourySettings.unifiedTargeting || Busy)
		{
			return false;
		}
		Thing thing = s?.Thing;
		if (thing == null || thing.Map == null)
		{
			return false;
		}
		if (ArmourySettings.unifiedTargetingForAI)
		{
			return true;
		}
		if (thing is Pawn { Faction: not null } pawn)
		{
			return pawn.Faction.IsPlayer;
		}
		return false;
	}

	private static void Begin(IAttackTargetSearcher s, Verb v)
	{
		searcher = s;
		searcherThing = s.Thing;
		searcherPawn = searcherThing as Pawn;
		map = searcherThing.Map;
		verb = v;
		equipment = v.EquipmentSource;
		casterPos = searcherThing.Position;
		float t = Mathf.InverseLerp(10f, 40f, v.EffectiveRange);
		proximityWeight = Mathf.Lerp(1f, 0.5f, t);
		accuracyWeight = Mathf.Lerp(0.3f, 1f, t);
		ignoreMaluses = equipment != null && equipment.TryGetComp(out CompUniqueWeapon comp) && comp.IgnoreAccuracyMaluses;
		canGoWild = v.verbProps.canGoWild;
		empOnly = v.IsEMP();
		searcherFaction = searcherThing.Faction;
		casterRoofed = casterPos.Roofed(map);
		weather = map.weatherManager.CurWeatherAccuracyMultiplier;
		ticksGame = Find.TickManager.TicksGame;
		threatActive = ThreatActive(searcherPawn);
		shooterStatsReady = false;
		equipTouch = (equipShort = (equipMedium = (equipLong = float.NaN)));
		darkLit = (darkDark = (darkIndoorDark = (darkIndoorLit = float.NaN)));
	}

	private static void End()
	{
		searcher = null;
		searcherThing = null;
		searcherPawn = null;
		map = null;
		verb = null;
		equipment = null;
		searcherFaction = null;
	}

	private static bool ThreatActive(Pawn pawn)
	{
		if (pawn == null || !ArmourySettings.threatTargetingEnabled)
		{
			return false;
		}
		if (pawn.Faction == null || !pawn.Faction.IsPlayer)
		{
			return false;
		}
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal != null)
		{
			return compArsenal.stance != CombatStance.Hold;
		}
		return false;
	}

	internal static float ScoreOne(IAttackTarget target, IAttackTargetSearcher s, Verb v)
	{
		Begin(s, v);
		try
		{
			float num = SafeScore(target, out var hitChance, out var aim, out var dark, out var outer);
			if (!float.IsNegativeInfinity(num))
			{
				num = Refine(target, num, hitChance, aim, dark, outer);
			}
			return num;
		}
		finally
		{
			End();
		}
	}

	internal static List<Pair<IAttackTarget, float>> ScoreAll(List<IAttackTarget> rawTargets, IAttackTargetSearcher s, Verb v, List<Pair<IAttackTarget, float>> result)
	{
		result.Clear();
		if (rawTargets.Count == 0)
		{
			return result;
		}
		Begin(s, v);
		try
		{
			scores.Clear();
			hitChances.Clear();
			aimFactors.Clear();
			darkOffsets.Clear();
			outerFactors.Clear();
			shootable.Clear();
			for (int i = 0; i < topIndices.Length; i++)
			{
				topIndices[i] = -1;
			}
			for (int j = 0; j < rawTargets.Count; j++)
			{
				IAttackTarget attackTarget = rawTargets[j];
				float num = float.MinValue;
				float hitChance = 0f;
				float aim = 0f;
				float dark = 0f;
				float outer = 0f;
				bool flag = attackTarget != s && canShootAtFromCurrentPosition(attackTarget, s, v);
				if (flag)
				{
					num = SafeScore(attackTarget, out hitChance, out aim, out dark, out outer);
				}
				if (float.IsNegativeInfinity(num))
				{
					flag = false;
				}
				scores.Add(num);
				hitChances.Add(hitChance);
				aimFactors.Add(aim);
				darkOffsets.Add(dark);
				outerFactors.Add(outer);
				shootable.Add(flag);
				if (flag && !float.IsNegativeInfinity(num))
				{
					Track(j);
				}
			}
			for (int k = 0; k < topIndices.Length; k++)
			{
				int num2 = topIndices[k];
				if (num2 >= 0)
				{
					scores[num2] = Refine(rawTargets[num2], scores[num2], hitChances[num2], aimFactors[num2], darkOffsets[num2], outerFactors[num2]);
				}
			}
			float num3 = 0f;
			IAttackTarget attackTarget2 = null;
			for (int l = 0; l < rawTargets.Count; l++)
			{
				if (shootable[l])
				{
					float num4 = scores[l];
					if (attackTarget2 == null || num4 > num3)
					{
						attackTarget2 = rawTargets[l];
						num3 = num4;
					}
				}
			}
			if (num3 < 1f)
			{
				if (attackTarget2 != null)
				{
					result.Add(new Pair<IAttackTarget, float>(attackTarget2, 1f));
				}
				return result;
			}
			float num5 = num3 - 30f;
			for (int m = 0; m < rawTargets.Count; m++)
			{
				if (shootable[m])
				{
					float num6 = scores[m];
					if (num6 >= num5)
					{
						result.Add(new Pair<IAttackTarget, float>(rawTargets[m], Mathf.InverseLerp(num3 - 30f, num3, num6)));
					}
				}
			}
			return result;
		}
		finally
		{
			End();
		}
	}

	private static void Track(int index)
	{
		float num = scores[index];
		for (int i = 0; i < topIndices.Length; i++)
		{
			int num2 = topIndices[i];
			if (num2 < 0 || !(scores[num2] >= num))
			{
				for (int num3 = topIndices.Length - 1; num3 > i; num3--)
				{
					topIndices[num3] = topIndices[num3 - 1];
				}
				topIndices[i] = index;
				break;
			}
		}
	}

	private static float SafeScore(IAttackTarget target, out float hitChance, out float aim, out float dark, out float outer)
	{
		try
		{
			return Score(target, out hitChance, out aim, out dark, out outer);
		}
		catch (Exception ex)
		{
			if (!loggedError)
			{
				loggedError = true;
				Log.Error("[RulesOfEngagement] Target scoring failed, using distance instead: " + ex);
			}
			hitChance = 0f;
			aim = float.NaN;
			dark = 0f;
			outer = 1f;
			return 60f - Mathf.Min((target.Thing.Position - casterPos).LengthHorizontal, 40f);
		}
	}

	private static float Score(IAttackTarget target, out float hitChance, out float aim, out float dark, out float outer)
	{
		Thing thing = target.Thing;
		IntVec3 position = thing.Position;
		float lengthHorizontal = (position - casterPos).LengthHorizontal;
		Pawn pawn = target as Pawn;
		bool flag = pawn?.Downed ?? false;
		DownedHandling downedHandling = ArmourySettings.downedHandling;
		hitChance = 0f;
		aim = 0f;
		dark = 0f;
		outer = 1f;
		if (flag && downedHandling == DownedHandling.Ignore)
		{
			return float.NegativeInfinity;
		}
		if (empOnly && pawn != null && pawn.RaceProps.IsFlesh)
		{
			return float.NegativeInfinity;
		}
		float num = CoverUtility.CalculateOverallBlockChance(position, casterPos, map);
		hitChance = EstimateHitChance(thing, lengthHorizontal, num, flag, out aim, out dark, out outer);
		float num2 = 60f;
		num2 += (0f - Mathf.Min(lengthHorizontal, 40f)) * proximityWeight;
		num2 += hitChance * 70f * accuracyWeight;
		if (!flag || downedHandling == DownedHandling.None)
		{
			if (target.TargetCurrentlyAimingAt == searcherThing)
			{
				num2 += 10f;
			}
			if (searcher.LastAttackedTarget == thing && (float)(ticksGame - searcher.LastAttackTargetTick) <= 300f)
			{
				num2 += 40f;
			}
		}
		num2 -= num * 10f;
		if (pawn != null)
		{
			num2 -= nonCombatantScore(pawn);
			if (verb.verbProps.ai_TargetHasRangedAttackScoreOffset != 0f && pawn.CurrentEffectiveVerb != null && pawn.CurrentEffectiveVerb.verbProps.Ranged)
			{
				num2 += verb.verbProps.ai_TargetHasRangedAttackScoreOffset;
			}
			num2 = ((!flag) ? (num2 + ThreatPressure(pawn, position, lengthHorizontal)) : (num2 - ((downedHandling == DownedHandling.Deprioritize) ? 100f : 50f)));
		}
		num2 += friendlyFireBlastRadiusTargetScoreOffset(target, searcher, verb);
		num2 += friendlyFireConeTargetScoreOffset(target, searcher, verb);
		if (threatActive && !flag)
		{
			num2 += PlayerThreatBonus(pawn, lengthHorizontal);
		}
		return num2 * target.TargetPriorityFactor;
	}

	private static float ThreatPressure(Pawn targetPawn, IntVec3 targetPos, float distance)
	{
		if (!targetPawn.IsCombatant())
		{
			return 0f;
		}
		ThingWithComps thingWithComps = targetPawn.equipment?.Primary;
		bool num = thingWithComps == null || !thingWithComps.def.IsRangedWeapon;
		Thing thing = targetPawn.mindState?.enemyTarget;
		bool flag = thing != null && thing == searcherThing;
		float num2 = (flag ? 8f : 0f);
		if (!num)
		{
			return num2;
		}
		if (distance <= 6f)
		{
			float t = 1f - Mathf.InverseLerp(1.5f, 6f, distance);
			return num2 + Mathf.Lerp(45f, 100f, t);
		}
		if (distance <= 20f && (flag || ClosingOn(targetPawn, targetPos)))
		{
			num2 += 25f * (1f - Mathf.InverseLerp(6f, 20f, distance));
		}
		if (thing is Pawn pawn && pawn != searcherThing && searcherFaction != null && pawn.Faction == searcherFaction && pawn.Spawned && pawn.Map == map && (float)(pawn.Position - targetPos).LengthHorizontalSquared <= 6.25f)
		{
			num2 += 30f;
		}
		return num2;
	}

	private static bool ClosingOn(Pawn targetPawn, IntVec3 targetPos)
	{
		Pawn_PathFollower pather = targetPawn.pather;
		if (pather == null || !pather.Moving)
		{
			return false;
		}
		IntVec3 cell = pather.Destination.Cell;
		if (!cell.IsValid)
		{
			return false;
		}
		return (cell - casterPos).LengthHorizontalSquared * 2 < (targetPos - casterPos).LengthHorizontalSquared;
	}

	private static float EstimateHitChance(Thing targetThing, float distance, float cellCoverBlock, bool downed, out float aim, out float dark, out float outer)
	{
		aim = 1f;
		dark = 0f;
		outer = 1f;
		try
		{
			float num = (canGoWild ? ShooterFactor(distance) : 1f);
			float num2 = EquipmentFactor(distance);
			float num3 = ((!ignoreMaluses && (!casterRoofed || !targetThing.Position.Roofed(map))) ? weather : 1f);
			float num4 = 1f;
			float num5 = 1f;
			Pawn pawn = targetThing as Pawn;
			float value;
			if (pawn != null)
			{
				value = pawn.BodySize;
				if (pawn.GetPosture() != PawnPosture.Standing)
				{
					if (distance >= 4.5f)
					{
						num4 = 0.5f;
					}
					if (distance <= 3.9f && !downed)
					{
						num5 = 7.5f;
					}
				}
			}
			else
			{
				value = targetThing.def.fillPercent * (float)targetThing.def.size.x * (float)targetThing.def.size.z * 2.5f;
			}
			value = Mathf.Clamp(value, 0.5f, 2f);
			dark = DarknessOffset(targetThing);
			aim = num * num2 * num3 * num5;
			float num6 = ((pawn != null) ? cellCoverBlock : CoverUtility.CalculateOverallBlockChance(targetThing, casterPos, map));
			outer = value * num4 * (1f - num6);
			float num7 = Combine(aim, dark, outer);
			return float.IsNaN(num7) ? 0.5f : num7;
		}
		catch
		{
			aim = float.NaN;
			float num8 = Mathf.Max(verb.verbProps.range, 1f);
			return Mathf.Clamp01(1f - distance / num8);
		}
	}

	private static float Combine(float aim, float dark, float outer)
	{
		float num = aim + dark;
		if (num < 0.0201f)
		{
			num = 0.0201f;
		}
		return Mathf.Clamp01(num * outer);
	}

	private static float Refine(IAttackTarget target, float score, float hitChance, float aim, float dark, float outer)
	{
		if (ignoreMaluses || float.IsNaN(aim))
		{
			return score;
		}
		if (!verb.TryFindShootLineFromTo(verb.caster.Position, target.Thing, out var resultingLine))
		{
			return score;
		}
		bool flag = false;
		foreach (IntVec3 item in resultingLine.Points())
		{
			if (item.AnyGas(map, GasType.BlindSmoke))
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return score;
		}
		float num = Combine(aim * 0.7f, dark, outer);
		if (float.IsNaN(num))
		{
			return score;
		}
		return score + (num - hitChance) * 70f * accuracyWeight * target.TargetPriorityFactor;
	}

	private static float ShooterFactor(float distance)
	{
		if (!shooterStatsReady)
		{
			shooterStatsReady = true;
			if (searcherPawn != null)
			{
				accPawn = searcherPawn.GetStatValue(StatDefOf.ShootingAccuracyPawn);
				accTouch = searcherPawn.GetStatValue(StatDefOf.ShootingAccuracyFactor_Touch);
				accShort = searcherPawn.GetStatValue(StatDefOf.ShootingAccuracyFactor_Short);
				accMedium = searcherPawn.GetStatValue(StatDefOf.ShootingAccuracyFactor_Medium);
				accLong = searcherPawn.GetStatValue(StatDefOf.ShootingAccuracyFactor_Long);
			}
			else
			{
				accPawn = searcherThing.GetStatValue(StatDefOf.ShootingAccuracyTurret);
			}
		}
		float num = Mathf.Pow(accPawn, distance);
		if (searcherPawn != null)
		{
			if (distance <= 3f)
			{
				num *= accTouch;
			}
			else if (distance <= 12f)
			{
				num *= Mathf.Lerp(accTouch, accShort, (distance - 3f) / 9f);
			}
			else if (distance <= 25f)
			{
				num *= Mathf.Lerp(accShort, accMedium, (distance - 12f) / 13f);
			}
			else
			{
				num = ((!(distance <= 40f)) ? (num * accLong) : (num * Mathf.Lerp(accMedium, accLong, (distance - 25f) / 15f)));
			}
		}
		return Mathf.Max(num, 0.0201f);
	}

	private static float EquipmentFactor(float distance)
	{
		VerbProperties verbProps = verb.verbProps;
		float value;
		if (distance <= 3f)
		{
			value = Equip(ref equipTouch, RangeCategory.Touch, verbProps);
		}
		else if (distance <= 12f)
		{
			value = Mathf.Lerp(Equip(ref equipTouch, RangeCategory.Touch, verbProps), Equip(ref equipShort, RangeCategory.Short, verbProps), (distance - 3f) / 9f);
		}
		else if (distance <= 25f)
		{
			value = Mathf.Lerp(Equip(ref equipShort, RangeCategory.Short, verbProps), Equip(ref equipMedium, RangeCategory.Medium, verbProps), (distance - 12f) / 13f);
		}
		else
		{
			value = ((!(distance <= 40f)) ? Equip(ref equipLong, RangeCategory.Long, verbProps) : Mathf.Lerp(Equip(ref equipMedium, RangeCategory.Medium, verbProps), Equip(ref equipLong, RangeCategory.Long, verbProps), (distance - 25f) / 15f));
		}
		return Mathf.Clamp(value, 0.01f, 1f);
	}

	private static float Equip(ref float slot, RangeCategory category, VerbProperties props)
	{
		if (float.IsNaN(slot))
		{
			slot = adjustedAccuracy(props, category, equipment);
		}
		return slot;
	}

	private static float DarknessOffset(Thing targetThing)
	{
		if (!ModsConfig.IdeologyActive)
		{
			return 0f;
		}
		if (DarknessCombatUtility.IsOutdoorsAndLit(targetThing))
		{
			return Dark(ref darkLit, StatDefOf.ShootingAccuracyOutdoorsLitOffset);
		}
		if (DarknessCombatUtility.IsOutdoorsAndDark(targetThing))
		{
			return Dark(ref darkDark, StatDefOf.ShootingAccuracyOutdoorsDarkOffset);
		}
		if (DarknessCombatUtility.IsIndoorsAndDark(targetThing))
		{
			return Dark(ref darkIndoorDark, StatDefOf.ShootingAccuracyIndoorsDarkOffset);
		}
		if (DarknessCombatUtility.IsIndoorsAndLit(targetThing))
		{
			return Dark(ref darkIndoorLit, StatDefOf.ShootingAccuracyIndoorsLitOffset);
		}
		return 0f;
	}

	private static float Dark(ref float slot, StatDef stat)
	{
		if (float.IsNaN(slot))
		{
			slot = searcherThing.GetStatValue(stat);
		}
		return slot;
	}

	private static float PlayerThreatBonus(Pawn targetPawn, float distance)
	{
		if (targetPawn == null)
		{
			return 0f;
		}
		ThingWithComps thingWithComps = targetPawn.equipment?.Primary;
		float num = ((thingWithComps != null) ? WeaponScorer.Score(targetPawn, thingWithComps, distance, searcherThing) : targetPawn.GetStatValue(StatDefOf.MeleeDPS));
		if (targetPawn.mindState?.enemyTarget == searcherPawn)
		{
			num *= 1.4f;
		}
		return num * 1.6f * 0.1f;
	}
}
