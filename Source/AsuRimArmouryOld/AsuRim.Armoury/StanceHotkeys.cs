using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AsuRim.Armoury;

public class StanceHotkeys : GameComponent
{
	private static KeyBindingDef[] stanceKeys;

	private static CombatStance[] cycle;

	private static KeyBindingDef cycleKey;

	private static readonly List<Pawn> eligible = new List<Pawn>();

	public StanceHotkeys(Game game)
	{
	}

	private static void Resolve()
	{
		if (cycle != null)
		{
			return;
		}
		List<CombatStance> list = new List<CombatStance>();
		foreach (CombatStance value in Enum.GetValues(typeof(CombatStance)))
		{
			if (value.Offerable() && !value.SquadOnly())
			{
				list.Add(value);
			}
		}
		cycle = list.ToArray();
		stanceKeys = new KeyBindingDef[cycle.Length];
		for (int i = 0; i < cycle.Length; i++)
		{
			stanceKeys[i] = DefDatabase<KeyBindingDef>.GetNamedSilentFail("ASA_SetStance_" + cycle[i]);
		}
		cycleKey = DefDatabase<KeyBindingDef>.GetNamedSilentFail("ASA_CycleStance");
	}

	public override void GameComponentOnGUI()
	{
		Event current = Event.current;
		if (current == null || current.type != EventType.keyDown || current.keyCode == KeyCode.None || !ArmourySettings.stancesEnabled || Find.CurrentMap == null || WorldRendererUtility.WorldSelected)
		{
			return;
		}
		WindowStack windowStack = Find.WindowStack;
		if (windowStack == null || windowStack.WindowsForcePause || windowStack.AnyWindowAbsorbingAllInput)
		{
			return;
		}
		Resolve();
		bool flag = false;
		if (cycleKey != null && cycleKey.KeyDownEvent)
		{
			flag = Cycle();
		}
		else
		{
			for (int i = 0; i < stanceKeys.Length; i++)
			{
				if (stanceKeys[i] != null && stanceKeys[i].KeyDownEvent)
				{
					flag = Apply(cycle[i]);
					break;
				}
			}
		}
		if (flag)
		{
			current.Use();
		}
	}

	private static void CollectEligible()
	{
		eligible.Clear();
		List<object> selectedObjects = Find.Selector.SelectedObjects;
		for (int i = 0; i < selectedObjects.Count; i++)
		{
			if (selectedObjects[i] is Pawn pawn && CompArsenal.For(pawn) != null && StanceRules.CanTakeOrders(pawn) && ArmouryStances.HasStances(pawn))
			{
				eligible.Add(pawn);
			}
		}
	}

	private static bool Apply(CombatStance stance)
	{
		CollectEligible();
		bool flag = false;
		for (int i = 0; i < eligible.Count; i++)
		{
			Pawn pawn = eligible[i];
			if (ArmouryStances.Offers(pawn, stance) && StanceRules.Allows(pawn, stance, out var _))
			{
				StanceGizmo.SetStance(pawn, CompArsenal.For(pawn), stance, manual: true);
				flag = true;
			}
		}
		if (flag)
		{
			SoundDefOf.Tick_High.PlayOneShotOnCamera();
		}
		return flag;
	}

	private static bool Cycle()
	{
		CollectEligible();
		if (eligible.Count == 0)
		{
			return false;
		}
		Pawn pawn = eligible[0];
		CombatStance stance = CompArsenal.For(pawn).stance;
		int num = Array.IndexOf(cycle, stance);
		for (int i = 1; i <= cycle.Length; i++)
		{
			CombatStance combatStance = cycle[(num + i + cycle.Length) % cycle.Length];
			if (combatStance == stance)
			{
				break;
			}
			if (ArmouryStances.Offers(pawn, combatStance) && StanceRules.Allows(pawn, combatStance, out var _))
			{
				return Apply(combatStance);
			}
		}
		return false;
	}
}
