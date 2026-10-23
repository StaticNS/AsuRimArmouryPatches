using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace AsuRim.Armoury.Patches;

/// <summary>
/// New definition for the rearm job.
/// </summary>
[DefOf]
public static class PatchJobDefOf {
	public static JobDef ASA_Patches_ReArm = null;
}

/// <summary>
/// Here's the big one. I'm scared to take a look at this tbh.
/// Theoretically, this adds a rearm job which allows the player to assign the job to pawns manually.
/// Pawns will then go seek out items it's missing from it's loadout, then drop any excess.
/// It should still function while the pawn is drafted.
/// </summary>
[HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
internal static class ReArmPatch {
	/// <summary>
	/// Did we actually get to apply the patch that adds the gizmos and doodads?
	/// </summary>
	internal static bool PanelButtonPatchInstalled;

	/// <summary>
	/// Who knows! Maybe this already exists and we're stupid dumb idiots who don't want to bother with shitty decompiled code!
	/// I know it's not the original dev's fault. It's decompiled code afterall. 
	/// That usually means it's optimized by the compiler and comments go to heaven. or hell.
	/// </summary>
	internal static readonly bool NativeReArmAvailable =
		AccessTools.TypeByName("AsuRim.Armoury.ReArm") != null
		&& AccessTools.TypeByName("AsuRim.Armoury.JobDriver_ReArm") != null;

	/// <summary>
	/// The actions gizmo is too small to accommodate our new rearm button. 
	/// 広げりましょうか
	/// </summary>
	/// <param name="maxWidth"></param>
	/// <param name="__result"></param>
	internal static void PanelWidthPostfix(float maxWidth, ref float __result) {
		if (!NativeReArmAvailable) {
			__result = Mathf.Min(maxWidth, __result + 26f);
		}
	}

	/// <summary>
	/// Does what's on the tin.
	/// </summary>
	/// <param name="__result"></param>
	internal static void PanelButtonCountPostfix(ref int __result) {
		if (!NativeReArmAvailable) {
			__result++;
		}
	}

	/// <summary>
	/// Intercepts the code for setting up the panel buttons then adds ours.
	/// The collapse gizmo button should come before our rearm button.
	/// </summary>
	/// <param name="instructions"></param>
	/// <returns></returns>
	internal static IEnumerable<CodeInstruction> PanelButtonsTranspiler(IEnumerable<CodeInstruction> instructions) {
		List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
		if (NativeReArmAvailable) {
			return codes;
		}

		int drawButtonIndex = -1;
		for (int i = 0; i < codes.Count; i++) {
			if (codes[i].operand is MethodInfo method && method.Name == "DrawButton") {
				drawButtonIndex = i;
			}
		}

		int rectIndex = -1;
		for (int i = drawButtonIndex - 1; i >= 0; i--) {
			if (codes[i].opcode == OpCodes.Newobj
				&& codes[i].operand is ConstructorInfo constructor
				&& constructor.DeclaringType == typeof(Rect)) {
				rectIndex = i;
				break;
			}
		}

		int xCalculationIndex = -1;
		for (int i = rectIndex - 1; i >= 0; i--) {
			if (codes[i].opcode == OpCodes.Sub) {
				xCalculationIndex = i;
				break;
			}
		}

		if (drawButtonIndex < 0 || rectIndex < 0 || xCalculationIndex < 0) {
			Log.Error("[AsuRimArmouryPatches] Could not relocate the collapse button; rearm button order may be incorrect.");
			return codes;
		}

		codes.InsertRange(xCalculationIndex + 1, new[] {
			new CodeInstruction(OpCodes.Ldc_R4, 26f),
			new CodeInstruction(OpCodes.Sub)
		});
		return codes;
	}

	/// <summary>
	/// I have no clue what this is doing.
	/// </summary>
	/// <param name="__result"></param>
	/// <param name="__instance"></param>
	/// <returns></returns>
	private static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance) {
		foreach (Gizmo gizmo in __result) {
			yield return gizmo;
		}
		if (!NativeReArmAvailable && ShouldShowStandalone(__instance)) {
			yield return MakeCommand(__instance);
		}
	}

	/// <summary>
	/// Finds the correct index to add our rearm button in the gizmo.
	/// Since certain buttons can be disabled, we gotta account for that.
	/// Hopefully our button ends up to the right of collapse.
	/// </summary>
	/// <param name="__instance"></param>
	/// <param name="rect"></param>
	/// <param name="__result"></param>
	internal static void PanelButtonsPostfix(object __instance, Rect rect, ref bool __result) {
		if (NativeReArmAvailable) {
			return;
		}
		Pawn pawn = AccessTools.Field(__instance.GetType(), "pawn")?.GetValue(__instance) as Pawn;
		if (pawn == null || !CanRearm(pawn)) {
			return;
		}

		int buttonsBeforeCollapse = 1;
		if (ReadBoolean(__instance, "ShowsWalkFire")) {
			buttonsBeforeCollapse++;
		}
		if (ReadBoolean(__instance, "ShowsStance")) {
			buttonsBeforeCollapse++;
		}
		if (ReadBoolean(__instance, "ShowsOverwatch")) {
			buttonsBeforeCollapse++;
		}

		Rect buttonRect = new Rect(
			rect.x + rect.width - 6f - 26f * (buttonsBeforeCollapse + 1),
			rect.y + 6f,
			26f,
			26f);
		Widgets.DrawTextureFitted(buttonRect, StanceIcons.Get("UI/Stances/ASA_Rearm", TexCommand.RearmTrap), 1f);
		if (!Mouse.IsOver(buttonRect)) {
			return;
		}
		Widgets.DrawHighlight(buttonRect);
		TooltipHandler.TipRegion(buttonRect,
			"ASA_Rearm".Translate() + "\n\n" + "ASA_Rearm_Desc".Translate());
		MouseoverSounds.DoRegion(buttonRect, SoundDefOf.Mouseover_Command);
		if (Widgets.ButtonInvisible(buttonRect)) {
			Order(pawn);
		}
		__result = true;
	}

	/// <summary>
	/// Helper for checking if we're in a state where the rearm button should be standalone.
	/// </summary>
	/// <param name="pawn"></param>
	/// <returns></returns>
	private static bool ShouldShowStandalone(Pawn pawn) {
		bool integratedPanel = ArmourySettings.nativeLoadoutUIEnabled
			&& ArmourySettings.loadoutPanel
			&& PanelButtonPatchInstalled;
		return CanRearm(pawn) && !integratedPanel;
	}

	/// <summary>
	/// Returns whether or not our pawn is eligible to gain access to our rearm function. It might be too good for them.<br/> 
	/// There's some interesting checks in here. Why are checking if the sidearm system is enabled? No idea!
	/// </summary>
	/// <param name="pawn"></param>
	/// <returns></returns>
	private static bool CanRearm(Pawn pawn) {
		return pawn != null
			&& CompArsenal.For(pawn) != null
			&& ArmourySettings.sidearmSystemEnabled
			&& Arsenal.CanCarrySidearms(pawn)
			&& StanceRules.CanTakeOrders(pawn);
	}

	/// <summary>
	/// What the fuck?
	/// </summary>
	/// <param name="instance"></param>
	/// <param name="propertyName"></param>
	/// <returns></returns>
	private static bool ReadBoolean(object instance, string propertyName) {
		PropertyInfo property = AccessTools.Property(instance.GetType(), propertyName);
		return property != null && (bool)property.GetValue(instance);
	}

	/// <summary>
	/// The actual info and functionality the rearm button has regardless of it's standalone or in the gizmo.
	/// </summary>
	/// <param name="pawn"></param>
	/// <returns></returns>
	private static Command_Action MakeCommand(Pawn pawn) {
		return new Command_Action {
			defaultLabel = "ASA_Rearm".Translate(),
			defaultDesc = "ASA_Rearm_Desc".Translate(),
			icon = StanceIcons.Get("UI/Stances/ASA_Rearm", TexCommand.RearmTrap),
			action = () => Order(pawn)
		};
	}

	/// <summary>
	/// The method that gets called when issuing the order to rearm. 
	/// Adds the pawn to the command hashset, checks through the set, then creates the job, 
	///marks it as player forced, and tries to give it to the slected pawn(s).
	/// We even play a silly little sound as player feedback! How cute.
	/// </summary>
	/// <param name="pawn"></param>
	private static void Order(Pawn pawn) {
		HashSet<Pawn> commanded = new HashSet<Pawn>(Find.Selector.SelectedObjects.OfType<Pawn>());
		commanded.Add(pawn);
		foreach (Pawn selected in commanded) {
			if (!CanRearm(selected) || selected.jobs == null || PatchJobDefOf.ASA_Patches_ReArm == null) {
				continue;
			}
			Job job = JobMaker.MakeJob(PatchJobDefOf.ASA_Patches_ReArm);
			job.playerForced = true;
			selected.jobs.TryTakeOrderedJob(job);
		}
		SoundDefOf.Click.PlayOneShotOnCamera();
	}
}

/// <summary>
/// JobDriver for our Rearm.. job.
/// There are 3 steps to this job.<br/>
/// 1. Go fetch any needed items.<br/>
/// 2. Equip them.<br/>
/// 3. Dropoff any excess.<br/>
/// </summary>
public sealed class JobDriver_ReArmPatch : JobDriver {
	private enum Step {
		None,
		Fetch,
		Equip,
		Dropoff
	}

	private Step step;

	/// <summary>
	/// Kill
	/// </summary>
	/// <param name="errorOnFailed"></param>
	/// <returns></returns>
	public override bool TryMakePreToilReservations(bool errorOnFailed) {
		return true;
	}

	/// <summary>
	/// What is a "toil"? I'm so tired. I want this to be over. I shouldn't've tried to do all this in one day.
	/// </summary>
	/// <returns></returns>
	protected override IEnumerable<Toil> MakeNewToils() {
		Toil selectNext = ToilMaker.MakeToil("SelectNextReArmTask");
		selectNext.initAction = SelectNextTask;
		selectNext.defaultCompleteMode = ToilCompleteMode.Instant;
		yield return selectNext;
		yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.Touch);
		Toil perform = ToilMaker.MakeToil("PerformReArmTask");
		perform.initAction = PerformStep;
		perform.defaultCompleteMode = ToilCompleteMode.Instant;
		yield return perform;
		yield return Toils_Jump.Jump(selectNext);
	}

	/// <summary>
	/// Handles selecting the next task, or step rather.
	/// </summary>
	private void SelectNextTask() {
		Job task = NextJob();
		if (task == null || task.targetA.Thing == null) {
			EndJobWith(JobCondition.Succeeded);
			return;
		}

		step = task.def == JobDefOf.Equip ? Step.Equip
			: task.def == PatchJobDefOf.ASA_Patches_ReArm ? Step.Dropoff
			: Step.Fetch;
		job.targetA = task.targetA;
		job.targetB = step == Step.Dropoff
			? task.targetB
			: new LocalTargetInfo(task.targetA.Thing.Spawned ? task.targetA.Thing.Position : pawn.Position);
		job.count = task.count;
		if (step != Step.Dropoff && task.targetA.Thing.Spawned
			&& !pawn.Reserve(task.targetA, job, 1, task.count, null, false)) {
			EndJobWith(JobCondition.Incompletable);
		}
	}

	/// <summary>
	/// Actually does the next job, or something. Idk
	/// </summary>
	/// <returns></returns>
	private Job NextJob() {
		Job job = new WorkGiver_LoadoutGear().NonScanJob(pawn);
		if (job != null) {
			return job;
		}

		CompArsenal comp = CompArsenal.For(pawn);
		WeaponPolicy policy = comp?.policy;
		if (comp == null || pawn.Map == null) {
			return null;
		}

		if (policy != null) {
			PolicyLoadoutExtension extension = PolicyLoadoutExtension.For(policy);
			job = StockJob(extension.Grenades, policy);
			if (job != null) {
				return job;
			}
			job = StockJob(extension.Medicine, policy);
			if (job != null) {
				return job;
			}
		}
		return FindDropoffJob(comp);
	}

	/// <summary>
	/// The task that: locates missing stock, and picks it up.
	/// </summary>
	/// <param name="slots"></param>
	/// <param name="policy"></param>
	/// <returns></returns>
	private Job StockJob(List<LoadoutSlot> slots, WeaponPolicy policy) {
		for (int i = 0; i < slots.Count; i++) {
			LoadoutSlot slot = slots[i];
			if (slot?.def == null || slot.count <= 0) {
				continue;
			}
			int desired = DesiredStockCount(policy, slot.def);
			int carried = CountInInventory(slot.def);
			if (carried >= desired) {
				continue;
			}

			Thing target = GenClosest.ClosestThingReachable(
				pawn.Position,
				pawn.Map,
				ThingRequest.ForDef(slot.def),
				PathEndMode.ClosestTouch,
				TraverseParms.For(pawn),
				60f,
				thing => !thing.IsForbidden(pawn) && pawn.CanReserve(thing)
					&& MassUtility.CountToPickUpUntilOverEncumbered(pawn, thing) > 0);
			if (target != null) {
				Job job = JobMaker.MakeJob(JobDefOf.TakeInventory, target);
				job.count = Math.Min(desired - carried, target.stackCount);
				job.checkOverrideOnExpire = true;
				return job;
			}
		}
		return null;
	}

	/// <summary>
	/// Helper to fetch how many of whatever item we're dealing with we want/need.
	/// </summary>
	/// <param name="policy"></param>
	/// <param name="def"></param>
	/// <returns></returns>
	private static int DesiredStockCount(WeaponPolicy policy, ThingDef def) {
		long total = CountDesired(policy.ammo, def) + CountDesired(policy.meals, def);
		PolicyLoadoutExtension extension = PolicyLoadoutExtension.For(policy);
		total += CountDesired(extension.Grenades, def) + CountDesired(extension.Medicine, def);
		return (int)Math.Min(total, int.MaxValue);
	}

	/// <summary>
	/// Counts how many items we have set in our policy.
	/// I don't know how to count past 3.
	/// </summary>
	/// <param name="slots"></param>
	/// <param name="def"></param>
	/// <returns></returns>
	private static long CountDesired(List<LoadoutSlot> slots, ThingDef def) {
		long count = 0;
		if (slots == null) {
			return count;
		}
		for (int i = 0; i < slots.Count; i++) {
			if (slots[i]?.def == def && slots[i].count > 0) {
				count += slots[i].count;
			}
		}
		return count;
	}

	/// <summary>
	/// Counts how many of an item we have in our inventory, if applicable.
	/// </summary>
	/// <param name="def"></param>
	/// <returns></returns>
	private int CountInInventory(ThingDef def) {
		ThingOwner<Thing> inventory = pawn.inventory?.innerContainer;
		int count = 0;
		if (inventory != null) {
			for (int i = 0; i < inventory.Count; i++) {
				if (inventory[i].def == def) {
					count += inventory[i].stackCount;
				}
			}
		}
		return count;
	}

	/// <summary>
	/// If we have too many of something, this job should be triggered.
	/// Go find storage to drop off our excess.
	/// </summary>
	/// <param name="comp"></param>
	/// <returns></returns>
	private Job FindDropoffJob(CompArsenal comp) {
		ThingOwner<Thing> inventory = pawn.inventory?.innerContainer;
		if (inventory == null) {
			return null;
		}
		for (int i = 0; i < inventory.Count; i++) {
			Thing thing = inventory[i];
			int excess;
			if (ArsenalRules.IsWieldable(thing)) {
				if (LoadoutCoverage.Covers(pawn, thing)) {
					continue;
				}
				excess = thing.stackCount;
			} else {
				if (comp.policy == null) {
					continue;
				}
				int desired = DesiredStockCount(comp.policy, thing.def);
				if (desired == 0) {
					continue;
				}
				excess = CountInInventory(thing.def) - desired;
				if (excess <= 0) {
					continue;
				}
				excess = Math.Min(excess, thing.stackCount);
			}

			IntVec3 cell = pawn.Position;
			if (!StoreUtility.TryFindBestBetterStoreCellFor(
				thing, pawn, pawn.Map, StoragePriority.Unstored, pawn.Faction, out cell)) {
				cell = pawn.Position;
			}
			Job dropoff = JobMaker.MakeJob(PatchJobDefOf.ASA_Patches_ReArm, thing, cell);
			dropoff.count = excess;
			dropoff.checkOverrideOnExpire = true;
			return dropoff;
		}
		return null;
	}

	/// <summary>
	/// Triggers the job according to our current step.
	/// </summary>
	private void PerformStep() {
		Thing target = job.targetA.Thing;
		if (target == null || target.Destroyed) {
			EndJobWith(JobCondition.Incompletable);
			return;
		}
		switch (step) {
			case Step.Fetch:
				TakeToInventory(target);
				break;
			case Step.Equip:
				Equip(target as ThingWithComps);
				break;
			case Step.Dropoff:
				Dropoff(target);
				break;
			default:
				EndJobWith(JobCondition.Incompletable);
				break;
		}
	}

	/// <summary>
	/// Implementation of picking things of the ground crazy style.
	/// </summary>
	/// <param name="target"></param>
	private void TakeToInventory(Thing target) {
		ThingOwner<Thing> inventory = pawn.inventory?.innerContainer;
		if (inventory == null || !target.Spawned) {
			EndJobWith(JobCondition.Incompletable);
			return;
		}
		int count = job.count > 0 ? Math.Min(job.count, target.stackCount) : target.stackCount;
		Thing taken = count < target.stackCount ? target.SplitOff(count) : target;
		if (taken == target) {
			target.DeSpawn();
		}
		if (!inventory.TryAdd(taken) && !GenPlace.TryPlaceThing(taken, pawn.Position, pawn.Map, ThingPlaceMode.Near)) {
			Log.Error($"Unable to return {taken} to the map after {pawn} failed to pick it up.");
			EndJobWith(JobCondition.Incompletable);
		}
	}

	/// <summary>
	/// Implementation of equping weapons,,, crazy style.
	/// </summary>
	/// <param name="weapon"></param>
	private void Equip(ThingWithComps weapon) {
		if (weapon == null) {
			EndJobWith(JobCondition.Incompletable);
			return;
		}
		if (weapon.Spawned) {
			weapon.DeSpawn();
		}
		if (!ArsenalActions.EquipMain(pawn, weapon)) {
			if (!(pawn.inventory?.innerContainer.TryAdd(weapon) ?? false)
				&& !GenPlace.TryPlaceThing(weapon, pawn.Position, pawn.Map, ThingPlaceMode.Near)) {
				Log.Error($"Unable to return {weapon} to the map after {pawn} failed to equip it.");
			}
			EndJobWith(JobCondition.Incompletable);
		}
	}

	/// <summary>
	/// Implementation of dropping the thing off. How to let go of thing in hand google dot com.
	/// </summary>
	/// <param name="target"></param>
	private void Dropoff(Thing target) {
		ThingOwner<Thing> inventory = pawn.inventory?.innerContainer;
		if (inventory == null || !inventory.Contains(target)) {
			EndJobWith(JobCondition.Incompletable);
			return;
		}
		int count = job.count > 0 ? Math.Min(job.count, target.stackCount) : target.stackCount;
		Thing dropped = count < target.stackCount ? target.SplitOff(count) : target;
		if (dropped == target) {
			inventory.Remove(target);
		}
		if (!GenPlace.TryPlaceThing(dropped, job.targetB.Cell, pawn.Map, ThingPlaceMode.Direct)
			&& !GenPlace.TryPlaceThing(dropped, job.targetB.Cell, pawn.Map, ThingPlaceMode.Near)
			&& !inventory.TryAdd(dropped)) {
			Log.Error($"Unable to drop off {dropped} for {pawn} or return it to their inventory.");
			EndJobWith(JobCondition.Incompletable);
		}
	}
}
