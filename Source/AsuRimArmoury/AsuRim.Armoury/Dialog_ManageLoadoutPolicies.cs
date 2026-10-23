using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsuRim.Armoury;

public class Dialog_ManageLoadoutPolicies : Dialog_ManagePolicies<WeaponPolicy>
{
	private Vector2 scrollPosition;

	private float lastHeight = 300f;

	private int activeTab;

	protected override string TitleKey
	{
		public get
		{
			return "ASA_LoadoutPolicyTitle";
		}
	}

	protected override string TipKey
	{
		public get
		{
			return "ASA_LoadoutPolicyTip";
		}
	}

	public Dialog_ManageLoadoutPolicies(WeaponPolicy policy)
		: base(policy)
	{
	}

	public override WeaponPolicy CreateNewPolicy()
	{
		return WeaponPolicyDatabase.Current.MakeNew();
	}

	public override WeaponPolicy GetDefaultPolicy()
	{
		List<WeaponPolicy> list = WeaponPolicyDatabase.Current?.Policies;
		if (list == null || list.Count <= 0)
		{
			return null;
		}
		return list[0];
	}

	public override void SetDefaultPolicy(WeaponPolicy policy)
	{
		List<WeaponPolicy> list = WeaponPolicyDatabase.Current?.Policies;
		if (list != null && list.Remove(policy))
		{
			list.Insert(0, policy);
		}
	}

	public override AcceptanceReport TryDeletePolicy(WeaponPolicy policy)
	{
		WeaponPolicyDatabase.Current?.Delete(policy);
		return AcceptanceReport.WasAccepted;
	}

	public override List<WeaponPolicy> GetPolicies()
	{
		return WeaponPolicyDatabase.Current?.Policies ?? new List<WeaponPolicy>();
	}

	public override void DoContentsRect(Rect rect)
	{
		LoadoutEditorUI.DrawFields(rect, SelectedPolicy, ref scrollPosition, ref lastHeight, ref activeTab, null);
	}

	public override void PreClose()
	{
		base.PreClose();
		List<WeaponPolicy> list = WeaponPolicyDatabase.Current?.Policies;
		if (list != null)
		{
			for (int i = 0; i < list.Count; i++)
			{
				LoadoutEditorUI.ReapplyToWearers(list[i]);
			}
		}
	}
}
