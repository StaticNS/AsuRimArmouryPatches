using HarmonyLib;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(PawnRenderUtility), "DrawEquipmentAndApparelExtras")]
internal static class Patch_DrawEquipmentAndApparelExtras_OffHand
{
	private const float SideOffset = 0.22f;

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
		if (curJob != null && curJob.def != null && curJob.def.neverShowWeapon)
		{
			return;
		}
		float equipmentDrawDistanceFactor = pawn.ageTracker.CurLifeStage.equipmentDrawDistanceFactor;
		Stance_Busy stance_Busy = (DualWield.CurrentOffHandStance(pawn) as Stance_Busy) ?? (pawn.stances?.curStance as Stance_Busy);
		if (!flags.HasFlag(PawnRenderFlags.NeverAimWeapon) && stance_Busy != null && !stance_Busy.neverAimWeapon && stance_Busy.focusTarg.IsValid)
		{
			Vector3 vector = (stance_Busy.focusTarg.HasThing ? stance_Busy.focusTarg.Thing.DrawPos : stance_Busy.focusTarg.Cell.ToVector3Shifted());
			float num = 0f;
			if ((vector - pawn.DrawPos).MagnitudeHorizontalSquared() > 0.001f)
			{
				num = (vector - pawn.DrawPos).AngleFlat();
			}
			Vector3 drawLoc = pawn.DrawPos + new Vector3(0f, 0f, 0.4f + thingWithComps.def.equippedDistanceOffset).RotatedBy(num) * equipmentDrawDistanceFactor;
			drawLoc += new Vector3(0.22f, 0f, 0f).RotatedBy(num);
			PawnRenderUtility.DrawEquipmentAiming(thingWithComps, drawLoc, num);
		}
		else if (PawnRenderUtility.CarryWeaponOpenly(pawn))
		{
			Vector3 drawPos2 = drawPos + SideOffsetFor(facing);
			PawnRenderUtility.DrawCarriedWeapon(thingWithComps, drawPos2, facing, equipmentDrawDistanceFactor);
		}
	}

	private static Vector3 SideOffsetFor(Rot4 facing)
	{
		if (facing == Rot4.North)
		{
			return new Vector3(-0.22f, -0.05f, 0f);
		}
		if (facing == Rot4.South)
		{
			return new Vector3(0.22f, 0.05f, 0f);
		}
		if (facing == Rot4.East)
		{
			return new Vector3(0f, -0.05f, -0.22f);
		}
		return new Vector3(0f, -0.05f, 0.22f);
	}
}
