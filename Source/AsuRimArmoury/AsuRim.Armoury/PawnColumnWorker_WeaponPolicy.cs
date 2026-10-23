using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsuRim.Armoury;

public class PawnColumnWorker_WeaponPolicy : PawnColumnWorker
{
	private const int TopAreaHeight = 65;

	private const int ManageButtonHeight = 32;

	public override int GetMinWidth(PawnTable table)
	{
		if (!Arsenal.SidearmsStoodDownForCE)
		{
			return Mathf.Max(base.GetMinWidth(table), 194);
		}
		return 0;
	}

	public override int GetOptimalWidth(PawnTable table)
	{
		if (!Arsenal.SidearmsStoodDownForCE)
		{
			return Mathf.Clamp(220, GetMinWidth(table), GetMaxWidth(table));
		}
		return 0;
	}

	public override int GetMinHeaderHeight(PawnTable table)
	{
		if (!Arsenal.SidearmsStoodDownForCE)
		{
			return Mathf.Max(base.GetMinHeaderHeight(table), 65);
		}
		return base.GetMinHeaderHeight(table);
	}

	public override void DoHeader(Rect rect, PawnTable table)
	{
		base.DoHeader(rect, table);
		if (!Arsenal.SidearmsStoodDownForCE && Widgets.ButtonText(new Rect(rect.x, rect.y + (rect.height - 65f), Mathf.Min(rect.width, 360f), 32f), "ASA_ManageLoadoutPolicies".Translate()))
		{
			Find.WindowStack.Add(new Dialog_ManageLoadoutPolicies(null));
		}
	}

	public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
	{
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal != null && StanceRules.CanTakeOrders(pawn) && Arsenal.CanCarrySidearms(pawn) && WeaponPolicyDatabase.Current != null)
		{
			Rect rect2 = rect.ContractedBy(0f, 2f);
			string text = ((compArsenal.policy != null) ? compArsenal.policy.label : ((string)"ASA_Policy_None".Translate()));
			Widgets.Dropdown(rect2, pawn, (Pawn p) => CompArsenal.For(p)?.policy, GenerateMenu, text, null, text, null, null, paintable: true);
		}
	}

	private IEnumerable<Widgets.DropdownMenuElement<WeaponPolicy>> GenerateMenu(Pawn pawn)
	{
		CompArsenal comp = CompArsenal.For(pawn);
		WeaponPolicyDatabase db = WeaponPolicyDatabase.Current;
		yield return new Widgets.DropdownMenuElement<WeaponPolicy>
		{
			option = new FloatMenuOption("ASA_Policy_None".Translate(), () =>
			{
				comp.policy = null;
			}),
			payload = null
		};
		for (int i = 0; i < db.Policies.Count; i++)
		{
			WeaponPolicy policy = db.Policies[i];
			yield return new Widgets.DropdownMenuElement<WeaponPolicy>
			{
				option = new FloatMenuOption(policy.label, () =>
				{
					comp.policy = policy;
					PolicyApplier.Apply(pawn, policy);
				}),
				payload = policy
			};
		}
		if (comp.policy != null)
		{
			WeaponPolicy editing = comp.policy;
			yield return new Widgets.DropdownMenuElement<WeaponPolicy>
			{
				option = new FloatMenuOption("ASA_Policy_Edit".Translate(editing.label), () =>
				{
					Find.WindowStack.Add(new Dialog_ManageLoadoutPolicies(editing));
				})
			};
		}
	}

	public override int Compare(Pawn a, Pawn b)
	{
		string strA = CompArsenal.For(a)?.policy?.label ?? "";
		string strB = CompArsenal.For(b)?.policy?.label ?? "";
		return string.Compare(strA, strB, StringComparison.Ordinal);
	}
}
