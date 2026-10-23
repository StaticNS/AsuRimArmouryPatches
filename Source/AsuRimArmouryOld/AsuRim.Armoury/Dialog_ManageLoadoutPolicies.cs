using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsuRim.Armoury;

public class Dialog_ManageLoadoutPolicies : Dialog_ManagePolicies<WeaponPolicy> {
	private Vector2 scrollPosition;

	private float lastHeight = 300f;

	private int activeTab;

	public override Vector2 InitialSize {
		get {
			float w = Screen.windowWidth / 2560f;
			float h = Screen.windowHeight / 1440f;
			return new Vector2(960f * w, 720f * h);
		}
	}

	protected override string TitleKey {
		get {
			return "ASA_LoadoutPolicyTitle";
		}
	}

	protected override string TipKey {
		get {
			return "ASA_LoadoutPolicyTip";
		}
	}

	public Dialog_ManageLoadoutPolicies(WeaponPolicy policy)
		: base(policy) {
	}

	protected override WeaponPolicy CreateNewPolicy() {
		return WeaponPolicyDatabase.Current.MakeNew();
	}

	protected override WeaponPolicy GetDefaultPolicy() {
		List<WeaponPolicy> list = WeaponPolicyDatabase.Current?.Policies;
		if (list == null || list.Count <= 0) {
			return null;
		}
		return list[0];
	}

	protected override void SetDefaultPolicy(WeaponPolicy policy) {
		List<WeaponPolicy> list = WeaponPolicyDatabase.Current?.Policies;
		if (list != null && list.Remove(policy)) {
			list.Insert(0, policy);
		}
	}

	protected override AcceptanceReport TryDeletePolicy(WeaponPolicy policy) {
		WeaponPolicyDatabase.Current?.Delete(policy);
		return AcceptanceReport.WasAccepted;
	}

	protected override List<WeaponPolicy> GetPolicies() {
		return WeaponPolicyDatabase.Current?.Policies ?? new List<WeaponPolicy>();
	}

	protected override void DoContentsRect(Rect rect) {
		// BIGGER WIDTH IT'S NOT FUCKING WIDE ENOUGH RAHHHHHHHhhhhhhhh
		//rect.width += 300f;
		LoadoutEditorUI.DrawFields(rect, SelectedPolicy, ref scrollPosition, ref lastHeight, ref activeTab, null);
	}

	public override void PreClose() {
		base.PreClose();
		List<WeaponPolicy> list = WeaponPolicyDatabase.Current?.Policies;
		if (list != null) {
			for (int i = 0; i < list.Count; i++) {
				LoadoutEditorUI.ReapplyToWearers(list[i]);
			}
		}
	}
}
