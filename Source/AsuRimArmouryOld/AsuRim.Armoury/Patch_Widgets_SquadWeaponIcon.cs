using System;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(Widgets), "ThingIcon", new Type[]
{
	typeof(Rect),
	typeof(Thing),
	typeof(float),
	typeof(Rot4?),
	typeof(bool),
	typeof(float),
	typeof(bool)
})]
internal static class Patch_Widgets_SquadWeaponIcon
{
	private static bool Prefix(Rect rect)
	{
		return !WeaponIconSuppressor.ShouldSkip(rect);
	}
}
