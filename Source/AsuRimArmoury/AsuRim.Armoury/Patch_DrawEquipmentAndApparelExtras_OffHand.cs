using ArmouryTactics;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(PawnRenderUtility), "DrawEquipmentAndApparelExtras")]
internal static class Patch_DrawEquipmentAndApparelExtras_OffHand
{
	private const float SideOffset = 0.22f;

	private static readonly Vector3 EqLocNorth = new Vector3(0f, 0f, -0.11f);

	private static readonly Vector3 EqLocSouth = new Vector3(0f, 0f, -0.22f);

	private static void Postfix(Pawn pawn, Vector3 drawPos, Rot4 facing, PawnRenderFlags flags)
	{
		if (!ArmouryTracked.OffHand.Contains(pawn) || !ArmourySettings.dualWieldEnabled || pawn.equipment == null)
		{
			return;
		}
		ThingWithComps thingWithComps = Arsenal.OffHand(pawn);
		if (thingWithComps == null)
		{
			return;
		}
		Job curJob = pawn.CurJob;
		if (curJob?.def == null || curJob.def.neverShowWeapon || pawn.GetPosture() != PawnPosture.Standing)
		{
			return;
		}
		float equipmentDrawDistanceFactor = pawn.ageTracker.CurLifeStage.equipmentDrawDistanceFactor;
		Stance_Busy stance_Busy = (DualWield.OffHandBusy(pawn) ? (DualWield.CurrentOffHandStance(pawn) as Stance_Busy) : (pawn.stances?.curStance as Stance_Busy));
		if (!flags.HasFlag(PawnRenderFlags.NeverAimWeapon) && stance_Busy != null && !stance_Busy.neverAimWeapon && stance_Busy.focusTarg.IsValid)
		{
			Vector3 vector = (stance_Busy.focusTarg.HasThing ? stance_Busy.focusTarg.Thing.DrawPos : stance_Busy.focusTarg.Cell.ToVector3Shifted());
			float angle = 0f;
			if ((vector - pawn.DrawPos).MagnitudeHorizontalSquared() > 0.001f)
			{
				angle = (vector - pawn.DrawPos).AngleFlat();
			}
			Vector3 pos = drawPos + new Vector3(0f, 0f, 0.4f + thingWithComps.def.equippedDistanceOffset).RotatedBy(angle) * equipmentDrawDistanceFactor;
			pos += new Vector3(0.22f, 0f, 0f).RotatedBy(angle);
			Draw(thingWithComps, pos, angle);
		}
		else
		{
			if (!PawnRenderUtility.CarryWeaponOpenly(pawn))
			{
				return;
			}
			if (facing.IsHorizontal)
			{
				Vector3 pos2 = drawPos + new Vector3(0f, -0.05f, (facing == Rot4.East) ? (-0.22f) : 0.22f);
				pos2 += ((facing == Rot4.East) ? new Vector3(0.22f, 0f, -0.22f) : new Vector3(-0.22f, 0f, -0.22f)) * equipmentDrawDistanceFactor;
				Draw(thingWithComps, pos2, (facing == Rot4.West) ? 217f : 143f);
				return;
			}
			bool isMeleeWeapon = thingWithComps.def.IsMeleeWeapon;
			float num = (isMeleeWeapon ? ModSettings_ArmouryTactics.meleeXOffset : ModSettings_ArmouryTactics.rangedXOffset);
			float num2 = (isMeleeWeapon ? ModSettings_ArmouryTactics.meleeZOffset : ModSettings_ArmouryTactics.rangedZOffset);
			float num3 = (isMeleeWeapon ? ModSettings_ArmouryTactics.meleeAngle : ModSettings_ArmouryTactics.rangedAngle);
			Vector3 pos3;
			if (facing == Rot4.North)
			{
				pos3 = drawPos + EqLocNorth * equipmentDrawDistanceFactor + new Vector3(0f - num, -0.05f, 0f - num2);
			}
			else
			{
				pos3 = drawPos + EqLocSouth * equipmentDrawDistanceFactor + new Vector3(num, 0.05f, num2);
				if (isMeleeWeapon ? ModSettings_ArmouryTactics.meleeMirrored : ModSettings_ArmouryTactics.rangedMirrored)
				{
					num3 = 360f - num3;
				}
			}
			Draw(thingWithComps, pos3, num3);
		}
	}

	private static void Draw(ThingWithComps offHand, Vector3 pos, float angle)
	{
		if (ModSettings_ArmouryTactics.customRotationsCache != null && ModSettings_ArmouryTactics.customRotationsCache.TryGetValue(offHand.def.shortHash, out var value))
		{
			float num = (float)value - offHand.def.equippedAngleOffset;
			angle += ((angle > 200f && angle < 340f) ? (0f - num) : num);
		}
		PawnRenderUtility.DrawEquipmentAiming(offHand, pos, angle);
	}
}
