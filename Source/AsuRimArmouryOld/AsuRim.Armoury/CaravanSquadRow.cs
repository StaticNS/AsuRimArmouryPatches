using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using System.Reflection;

namespace AsuRim.Armoury;

internal static class CaravanSquadRow
{
	internal const float RowHeight = 34f;

	private const float ButtonHeight = 24f;

	private const float SideMargin = 4f;

	private const float Gap = 6f;

	private const float TopMargin = 5f;

	private const float MinButtonWidth = 60f;

	private static Dialog_FormCaravan current;

	private static readonly FieldInfo pawnsTransferField = AccessTools.Field(typeof(Dialog_FormCaravan), "pawnsTransfer");

	private static readonly List<Pawn> buffer = new List<Pawn>();

	internal static void Begin(Dialog_FormCaravan dialog)
	{
		current = dialog;
		TransferableOneWayWidget pawnsTransfer = PawnsTransfer(dialog);
		if (pawnsTransfer != null)
		{
			pawnsTransfer.extraHeaderSpace = (AnySquads() ? 34f : 0f);
		}
	}

	internal static void End()
	{
		current = null;
	}

	internal static bool IsPawnsWidget(TransferableOneWayWidget widget)
	{
		if (current != null && widget != null)
		{
			return widget == PawnsTransfer(current);
		}
		return false;
	}

	private static TransferableOneWayWidget PawnsTransfer(Dialog_FormCaravan dialog)
	{
		return (TransferableOneWayWidget)pawnsTransferField.GetValue(dialog);
	}

	private static bool AnySquads()
	{
		List<Squad> list = SquadManager.Current?.Squads;
		if (list != null)
		{
			return list.Count > 0;
		}
		return false;
	}

	internal static void Draw(Rect inRect, ref bool anythingChanged)
	{
		Dialog_FormCaravan dialog_FormCaravan = current;
		if (dialog_FormCaravan == null)
		{
			return;
		}
		List<Squad> list = SquadManager.Current?.Squads;
		if (list == null || list.Count == 0)
		{
			return;
		}
		Rect rect = new Rect(inRect.x + 4f, inRect.y + 37f + 5f, inRect.width - 8f, 24f);
		float num = rect.x;
		Text.Font = GameFont.Small;
		for (int i = 0; i < list.Count; i++)
		{
			Squad squad = list[i];
			if (squad == null)
			{
				continue;
			}
			buffer.Clear();
			int num2 = 0;
			for (int j = 0; j < squad.Members.Count; j++)
			{
				Pawn pawn = squad.Members[j];
				TransferableOneWay transferableOneWay = Find(dialog_FormCaravan, pawn);
				if (transferableOneWay != null)
				{
					buffer.Add(pawn);
					if (transferableOneWay.CountToTransferToDestination > 0)
					{
						num2++;
					}
				}
			}
			if (buffer.Count == 0)
			{
				continue;
			}
			string text = squad.label + " " + num2 + "/" + buffer.Count;
			float num3 = Mathf.Max(60f, Text.CalcSize(text).x + 20f);
			if (num + num3 > rect.xMax)
			{
				break;
			}
			Rect rect2 = new Rect(num, rect.y, num3, rect.height);
			if (Widgets.ButtonText(rect2, text))
			{
				bool flag = num2 < buffer.Count;
				for (int k = 0; k < buffer.Count; k++)
				{
					TransferableOneWay transferableOneWay2 = Find(dialog_FormCaravan, buffer[k]);
					transferableOneWay2?.AdjustTo(flag ? transferableOneWay2.GetMaximumToTransfer() : transferableOneWay2.GetMinimumToTransfer());
				}
				anythingChanged = true;
			}
			TooltipHandler.TipRegion(rect2, "ASA_Caravan_SquadTip".Translate(squad.label));
			num += num3 + 6f;
		}
		buffer.Clear();
	}

	private static TransferableOneWay Find(Dialog_FormCaravan dialog, Pawn pawn)
	{
		if (pawn == null || dialog.transferables == null)
		{
			return null;
		}
		for (int i = 0; i < dialog.transferables.Count; i++)
		{
			TransferableOneWay transferableOneWay = dialog.transferables[i];
			if (transferableOneWay.things.Count == 1 && transferableOneWay.things[0] == pawn)
			{
				return transferableOneWay;
			}
		}
		return null;
	}
}
