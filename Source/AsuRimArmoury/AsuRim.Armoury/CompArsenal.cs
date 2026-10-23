using System;
using System.Collections.Generic;
using Verse;

namespace AsuRim.Armoury;

public class CompArsenal : ThingComp, IThingHolder
{
	private ThingOwner<ThingWithComps> offHandHolder;

	public List<LoadoutSlot> stowed = new List<LoadoutSlot>();

	public List<ThingDef> weaponOrder;

	public int lastSquadId = -1;

	public ThingWithComps forced;

	public bool stanceLocked;

	public bool forcedUnarmed;

	public CombatStance stance;

	public WeaponPolicy policy;

	internal Pawn_StanceTracker offHandStances;

	public bool overwatch;

	public bool walkFire = ArmourySettings.walkFireDefaultOn;

	public LoadoutSlot beforeTool;

	public bool wasOutOfAction;

	public LoadoutSlot downMain;

	public LoadoutSlot downOff;

	internal ThingWithComps liveMain;

	internal ThingWithComps liveOff;

	public ThingWithComps downOffThing;

	public ThingWithComps downMainThing;

	public List<ThingWithComps> downSidearms;

	public List<ThingWithComps> declined;

	private Area overwatchArea;

	public Loadout primary = new Loadout();

	public Loadout secondary = new Loadout();

	public Loadout melee = new Loadout();

	public CompGuardPost onDutyPost;

	private Pawn guardingRaw;

	public Pawn ssEscort;

	public GuardFormation formation;

	public int patrolIndex;

	public Thing ambushTarget;

	public int nextAmbushSearchTick;

	public int nextStanceScanTick;

	public Thing cachedStanceScanTarget;

	public bool autoDrafted;

	public int combatProtectionUntil;

	public bool patrolReversing;

	public bool brawlActive;

	public bool brawlOffHand;

	public ThingWithComps brawlReturn;

	public ThingWithComps brawlReturnOff;

	public ThingWithComps meleeReturn;

	public ThingWithComps meleeReturnOff;

	internal bool directorActing;

	internal int lastSwitchTick = -99999;

	internal bool inMelee;

	internal int nextWalkScanTick;

	internal Thing inMeleeWith;

	private int sinceEvaluated;

	private static sbyte[] defHasArsenal = new sbyte[0];

	private ThingOwner<ThingWithComps> OffHandHolder
	{
		get
		{
			if (offHandHolder == null)
			{
				offHandHolder = new ThingOwner<ThingWithComps>(this);
			}
			return offHandHolder;
		}
	}

	public ThingWithComps offHand
	{
		get
		{
			if (OffHandHolder.Count <= 0)
			{
				return null;
			}
			return OffHandHolder[0];
		}
	}

	IThingHolder IThingHolder.ParentHolder => Pawn;

	public Area OverwatchArea
	{
		get
		{
			if (overwatchArea != null && !overwatchArea.Map.areaManager.AllAreas.Contains(overwatchArea))
			{
				overwatchArea = null;
			}
			return overwatchArea;
		}
		set
		{
			overwatchArea = value;
		}
	}

	public Pawn guarding
	{
		get
		{
			return guardingRaw;
		}
		set
		{
			if (guardingRaw != value)
			{
				Pawn pawn = guardingRaw;
				guardingRaw = value;
				Formations.Register(Pawn, pawn, value);
				Formations.Notify_DetailChanged(pawn);
				Formations.Notify_DetailChanged(value);
			}
		}
	}

	public bool OnDuty
	{
		get
		{
			if (onDutyPost?.parent != null)
			{
				return onDutyPost.parent.Spawned;
			}
			return false;
		}
	}

	public bool UnderCombatOrders
	{
		get
		{
			if (Pawn == null)
			{
				return false;
			}
			if (Pawn.Drafted)
			{
				return true;
			}
			if (Pawn.RaceProps != null && Pawn.RaceProps.IsMechanoid && Pawn.IsColonyMech)
			{
				return true;
			}
			if (OnDuty)
			{
				return Pawn.mindState?.enemyTarget != null;
			}
			return false;
		}
	}

	public CombatStance EffectiveStance
	{
		get
		{
			if (!OnDuty)
			{
				return stance;
			}
			return onDutyPost.stance;
		}
	}

	public Area EffectiveOverwatchArea
	{
		get
		{
			if (!OnDuty)
			{
				return OverwatchArea;
			}
			return onDutyPost.OverwatchArea ?? OverwatchArea;
		}
	}

	public WeaponPolicy EffectivePolicy => policy;

	public Pawn Pawn => parent as Pawn;

	ThingOwner IThingHolder.GetDirectlyHeldThings()
	{
		return OffHandHolder;
	}

	void IThingHolder.GetChildHolders(List<IThingHolder> outChildren)
	{
		ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, OffHandHolder);
	}

	public bool SetOffHand(ThingWithComps weapon)
	{
		if (weapon == null)
		{
			return true;
		}
		if (offHand != null)
		{
			TakeOffHand();
		}
		if (!OffHandHolder.TryAdd(weapon))
		{
			return false;
		}
		ArmouryTracked.OffHand.Add(Pawn);
		CompEquippable comp = weapon.GetComp<CompEquippable>();
		if (comp != null)
		{
			foreach (Verb allVerb in comp.AllVerbs)
			{
				allVerb.caster = Pawn;
				allVerb.Notify_PickedUp();
			}
		}
		weapon.Notify_Equipped(Pawn);
		return true;
	}

	public ThingWithComps TakeOffHand()
	{
		ThingWithComps thingWithComps = offHand;
		if (thingWithComps == null)
		{
			return null;
		}
		CompEquippable comp = thingWithComps.GetComp<CompEquippable>();
		if (comp != null)
		{
			foreach (Verb allVerb in comp.AllVerbs)
			{
				allVerb.caster = null;
			}
		}
		thingWithComps.Notify_Unequipped(Pawn);
		OffHandHolder.Remove(thingWithComps);
		ArmouryTracked.OffHand.Remove(Pawn);
		return thingWithComps;
	}

	public void DestroyOffHand()
	{
		TakeOffHand()?.Destroy();
	}

	public override void CompTick()
	{
		base.CompTick();
		if (offHandStances == null && (offHandHolder == null || offHandHolder.Count == 0))
		{
			return;
		}
		Pawn pawn = Pawn;
		if (pawn != null)
		{
			if (offHandHolder != null && offHandHolder.Count > 0 && pawn.equipment != null && pawn.equipment.Primary == null)
			{
				ArsenalActions.CheckOffHand(pawn, this);
			}
			if (pawn.Spawned)
			{
				DualWield.Tick(pawn, this);
			}
		}
	}

	public override void CompTickInterval(int delta)
	{
		base.CompTickInterval(delta);
		if (Pawn == null || !Pawn.Spawned)
		{
			return;
		}
		WalkFire.Tick(Pawn, this, delta);
		sinceEvaluated += delta;
		if (sinceEvaluated >= 60)
		{
			sinceEvaluated = 0;
			if (offHand != null)
			{
				ArsenalActions.CheckOffHand(Pawn, this);
			}
			KitRestore.Check(Pawn, this);
			RangeDirector.Evaluate(Pawn, this);
			CombatMobilization.Check(Pawn, this);
		}
	}

	public override void PostDestroy(DestroyMode mode, Map previousMap)
	{
		base.PostDestroy(mode, previousMap);
		if (mode != DestroyMode.KillFinalize)
		{
			DestroyOffHand();
		}
	}

	private static bool DefHasArsenal(ThingDef def)
	{
		int index = def.index;
		if (index >= defHasArsenal.Length)
		{
			sbyte[] destinationArray = new sbyte[Math.Max(index + 1, DefDatabase<ThingDef>.DefCount)];
			Array.Copy(defHasArsenal, destinationArray, defHasArsenal.Length);
			defHasArsenal = destinationArray;
		}
		if (defHasArsenal[index] == 0)
		{
			bool flag = false;
			if (def.comps != null)
			{
				for (int i = 0; i < def.comps.Count; i++)
				{
					if (def.comps[i] is CompProperties_Arsenal)
					{
						flag = true;
						break;
					}
				}
			}
			defHasArsenal[index] = (sbyte)(flag ? 1 : (-1));
		}
		return defHasArsenal[index] > 0;
	}

	public static CompArsenal For(Pawn pawn)
	{
		if (pawn == null || !DefHasArsenal(pawn.def))
		{
			return null;
		}
		return pawn.GetComp<CompArsenal>();
	}

	public override void PostDraw()
	{
		base.PostDraw();
		if (offHandStances != null && Pawn != null && Pawn.Spawned && Arsenal.OffHand(Pawn) != null)
		{
			offHandStances.StanceTrackerDraw();
		}
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		if (Pawn != null)
		{
			sinceEvaluated = Pawn.thingIDNumber % 60;
			if (respawningAfterLoad && stowed.Count == 0)
			{
				AdoptCarried();
			}
			if (respawningAfterLoad)
			{
				CarrierRepair.Run(Pawn);
			}
		}
	}

	private void AdoptCarried()
	{
		ThingOwner<Thing> thingOwner = Pawn.inventory?.innerContainer;
		if (thingOwner == null)
		{
			return;
		}
		for (int i = 0; i < thingOwner.Count; i++)
		{
			Thing thing = thingOwner[i];
			if (ArsenalRules.IsWieldable(thing))
			{
				ArsenalActions.RememberQuietly(Pawn, thing);
			}
		}
	}

	public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
	{
		base.PostDeSpawn(map, mode);
	}

	public Loadout SlotFor(LoadoutKind kind)
	{
		return kind switch
		{
			LoadoutKind.Primary => primary, 
			LoadoutKind.Secondary => secondary, 
			_ => melee, 
		};
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		if (Scribe.mode == LoadSaveMode.Saving)
		{
			KitRecovery.Prune(Pawn, this);
		}
		Scribe_Values.Look(ref stance, "stance", CombatStance.Hold);
		Scribe_References.Look(ref policy, "policy");
		Scribe_References.Look(ref forced, "forced");
		Scribe_Values.Look(ref overwatch, "overwatch", defaultValue: false);
		Scribe_Values.Look(ref walkFire, "walkFire", defaultValue: false);
		Scribe_Deep.Look(ref beforeTool, "beforeTool");
		Scribe_Values.Look(ref wasOutOfAction, "wasOutOfAction", defaultValue: false);
		Scribe_Deep.Look(ref downMain, "downMain");
		Scribe_Deep.Look(ref downOff, "downOff");
		Scribe_References.Look(ref downOffThing, "downOffThing");
		Scribe_References.Look(ref downMainThing, "downMainThing");
		Scribe_Values.Look(ref brawlActive, "brawlActive", defaultValue: false);
		Scribe_Values.Look(ref brawlOffHand, "brawlOffHand", defaultValue: false);
		Scribe_References.Look(ref brawlReturn, "brawlReturn");
		Scribe_References.Look(ref brawlReturnOff, "brawlReturnOff");
		Scribe_References.Look(ref meleeReturn, "meleeReturn");
		Scribe_References.Look(ref meleeReturnOff, "meleeReturnOff");
		Scribe_Collections.Look(ref downSidearms, "downSidearms", LookMode.Reference);
		Scribe_Collections.Look(ref declined, "declined", LookMode.Reference);
		Scribe_References.Look(ref guardingRaw, "guarding");
		Scribe_References.Look(ref ssEscort, "ssEscort");
		Scribe_References.Look(ref ambushTarget, "ambushTarget");
		Scribe_Values.Look(ref autoDrafted, "autoDrafted", defaultValue: false);
		Scribe_Values.Look(ref formation, "formation", GuardFormation.Ring);
		Scribe_Values.Look(ref patrolIndex, "patrolIndex", 0);
		Scribe_Values.Look(ref patrolReversing, "patrolReversing", defaultValue: false);
		Scribe_References.Look(ref overwatchArea, "overwatchArea");
		Scribe_Deep.Look(ref offHandHolder, "offHandHolder", this);
		Scribe_Values.Look(ref lastSwitchTick, "lastSwitchTick", -99999);
		Scribe_Values.Look(ref inMelee, "inMelee", defaultValue: false);
		Scribe_Values.Look(ref sinceEvaluated, "sinceEvaluated", 0);
		Scribe_Collections.Look(ref stowed, "stowed", LookMode.Deep);
		Scribe_Collections.Look(ref weaponOrder, "weaponOrder", LookMode.Def);
		Scribe_Values.Look(ref lastSquadId, "lastSquadId", -1);
		Scribe_Values.Look(ref stanceLocked, "stanceLocked", defaultValue: false);
		Scribe_Values.Look(ref forcedUnarmed, "forcedUnarmed", defaultValue: false);
		Scribe_Deep.Look(ref primary, "primary");
		Scribe_Deep.Look(ref secondary, "secondary");
		Scribe_Deep.Look(ref melee, "melee");
		if (Scribe.mode != LoadSaveMode.PostLoadInit)
		{
			return;
		}
		stance = stance.Normalise();
		if (stowed == null)
		{
			stowed = new List<LoadoutSlot>();
		}
		if (primary == null)
		{
			primary = new Loadout();
		}
		if (secondary == null)
		{
			secondary = new Loadout();
		}
		if (melee == null)
		{
			melee = new Loadout();
		}
		stowed.RemoveAll((LoadoutSlot s) => s == null || s.def == null);
		downSidearms?.RemoveAll((ThingWithComps t) => t == null);
		declined?.RemoveAll((ThingWithComps t) => t == null);
		weaponOrder?.RemoveAll((ThingDef d) => d == null);
		Formations.Register(Pawn, null, guardingRaw);
		Formations.Notify_DetailChanged(guardingRaw);
		if (offHandHolder != null && offHandHolder.Count > 0)
		{
			ArmouryTracked.OffHand.Add(Pawn);
		}
		if (offHandHolder == null)
		{
			return;
		}
		for (int num = 0; num < offHandHolder.Count; num++)
		{
			CompEquippable comp = offHandHolder[num].GetComp<CompEquippable>();
			if (comp == null)
			{
				continue;
			}
			foreach (Verb allVerb in comp.AllVerbs)
			{
				allVerb.caster = Pawn;
			}
		}
	}
}
