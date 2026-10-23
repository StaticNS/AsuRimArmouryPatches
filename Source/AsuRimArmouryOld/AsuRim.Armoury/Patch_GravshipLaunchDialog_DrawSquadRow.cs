using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(Dialog_BeginGravshipLaunch), "DoRightColumn")]
internal static class Patch_GravshipLaunchDialog_DrawSquadRow
{
	private static readonly AccessTools.FieldRef<Dialog_BeginRitual, RitualRoleAssignments> AssignmentsRef = AccessTools.FieldRefAccess<Dialog_BeginRitual, RitualRoleAssignments>("assignments");

	private static bool Prepare()
	{
		return ModsConfig.OdysseyActive;
	}

	private static void Prefix(Dialog_BeginGravshipLaunch __instance, ref RectDivider layout)
	{
		SquadManager current = SquadManager.Current;
		if (current != null && current.Squads.Count != 0)
		{
			HashSet<Squad> hashSet = GravshipSquadBoarding.SelectionFor(__instance);
			SyncLiveCrew(__instance, hashSet);
			Rect rect = layout.NewRow(28f).Rect;
			Squad squad = DrawSquadRow(hashSet, rect);
			layout.NewRow(5f);
			if (squad != null)
			{
				UnsyncRemovedSquad(__instance, hashSet, squad);
			}
		}
	}

	private static void SyncLiveCrew(Dialog_BeginGravshipLaunch dialog, HashSet<Squad> selected)
	{
		RitualRoleAssignments ritualRoleAssignments = AssignmentsRef(dialog);
		if (ritualRoleAssignments == null)
		{
			return;
		}
		foreach (Squad item in selected)
		{
			List<Pawn> members = item.Members;
			for (int i = 0; i < members.Count; i++)
			{
				Pawn pawn = members[i];
				if (pawn != null && pawn.RaceProps.Humanlike && ritualRoleAssignments.RoleForPawn(pawn) == null)
				{
					ritualRoleAssignments.TryAssignSpectate(pawn);
				}
			}
		}
	}

	private static Squad DrawSquadRow(HashSet<Squad> selected, Rect rowRect)
	{
		List<Squad> list = SquadManager.Current?.Squads ?? new List<Squad>();
		float x = rowRect.x;
		DrawPiece(ref x, rowRect.y, "ASA_Gravship_BoardLabel".Translate());
		Squad squad = null;
		bool flag = true;
		foreach (Squad item in selected)
		{
			if (!flag)
			{
				DrawPiece(ref x, rowRect.y, ", ");
			}
			flag = false;
			DrawPiece(ref x, rowRect.y, item.label.Colorize(ColoredText.NameColor));
			x += 2f;
			Squad squad2 = item;
			if (DrawIcon(ref x, rowRect.y, TexButton.CloseXSmall))
			{
				squad = squad2;
			}
		}
		if (squad != null)
		{
			selected.Remove(squad);
		}
		if (!flag)
		{
			DrawPiece(ref x, rowRect.y, ", ");
		}
		if (DrawPiece(ref x, rowRect.y, "ASA_Gravship_BoardAssign".Translate(), clickable: true))
		{
			List<FloatMenuOption> list2 = new List<FloatMenuOption>();
			foreach (Squad item2 in list)
			{
				if (!selected.Contains(item2))
				{
					Squad captured = item2;
					list2.Add(new FloatMenuOption(captured.label + " (" + captured.Members.Count + ")", () =>
					{
						selected.Add(captured);
					}));
				}
			}
			if (list2.Count == 0)
			{
				list2.Add(new FloatMenuOption("ASA_Gravship_BoardAllAssigned".Translate(), null));
			}
			Find.WindowStack.Add(new FloatMenu(list2));
		}
		return squad;
	}

	private static void UnsyncRemovedSquad(Dialog_BeginGravshipLaunch dialog, HashSet<Squad> stillSelected, Squad removedSquad)
	{
		RitualRoleAssignments ritualRoleAssignments = AssignmentsRef(dialog);
		if (ritualRoleAssignments == null)
		{
			return;
		}
		List<Pawn> list = new List<Pawn>();
		foreach (Squad item in stillSelected)
		{
			list.AddRange(item.Members);
		}
		List<Pawn> members = removedSquad.Members;
		List<Pawn> spectatorsForReading = ritualRoleAssignments.SpectatorsForReading;
		for (int i = 0; i < members.Count; i++)
		{
			Pawn pawn = members[i];
			if (pawn != null && !list.Contains(pawn))
			{
				spectatorsForReading.Remove(pawn);
			}
		}
	}

	private static bool DrawPiece(ref float x, float y, string text, bool clickable = false)
	{
		Vector2 vector = Text.CalcSize(text);
		Rect rect = new Rect(x, y, vector.x, 28f);
		x += vector.x;
		bool result = false;
		if (clickable)
		{
			GUI.color = Widgets.NormalOptionColor;
			if (Mouse.IsOver(rect))
			{
				Widgets.DrawHighlight(rect);
			}
			result = Widgets.ButtonInvisible(rect);
		}
		Widgets.Label(rect, text);
		GUI.color = Color.white;
		return result;
	}

	private static bool DrawIcon(ref float x, float y, Texture2D icon)
	{
		Rect rect = new Rect(x, y + 8f, 12f, 12f);
		x += 12f;
		if (Mouse.IsOver(rect))
		{
			Widgets.DrawHighlight(rect);
		}
		bool result = Widgets.ButtonInvisible(rect);
		GUI.DrawTexture(rect, icon);
		return result;
	}
}
