using System;
using System.Collections.Generic;
using AsuRimArmoury.Kit;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public static class Formations
{
	private const float Radius = 2.2f;

	private static readonly List<Pawn> buffer;

	private static readonly List<Pawn> roleBuffer;

	private static readonly List<Pawn> rangedBuffer;

	private static readonly List<Pawn> meleeBuffer;

	private static readonly List<int> rankBuffer;

	private static readonly Dictionary<Pawn, List<Pawn>> guardsByCharge;

	private static readonly Comparison<Pawn> ByThingId;

	private const float RoomSearchRadius = 8f;

	public static void Notify_DetailChanged(Pawn charge)
	{
		if (charge != null && Scribe.mode == LoadSaveMode.Inactive)
		{
			List<Pawn> list = DetailFor(charge);
			for (int i = 0; i < list.Count; i++)
			{
				list[i].jobs?.EndCurrentJob(JobCondition.InterruptForced);
			}
		}
	}

	static Formations()
	{
		buffer = new List<Pawn>();
		roleBuffer = new List<Pawn>();
		rangedBuffer = new List<Pawn>();
		meleeBuffer = new List<Pawn>();
		rankBuffer = new List<int>();
		guardsByCharge = new Dictionary<Pawn, List<Pawn>>();
		ByThingId = (Pawn a, Pawn b) => a.thingIDNumber.CompareTo(b.thingIDNumber);
		CacheReset.Register(guardsByCharge.Clear);
	}

	public static void Register(Pawn guard, Pawn oldCharge, Pawn newCharge)
	{
		if (guard == null)
		{
			return;
		}
		if (oldCharge != null && guardsByCharge.TryGetValue(oldCharge, out var value))
		{
			value.Remove(guard);
			if (value.Count == 0)
			{
				guardsByCharge.Remove(oldCharge);
			}
		}
		if (newCharge != null)
		{
			if (!guardsByCharge.TryGetValue(newCharge, out var value2))
			{
				value2 = new List<Pawn>();
				guardsByCharge[newCharge] = value2;
			}
			if (!value2.Contains(guard))
			{
				value2.Add(guard);
			}
		}
	}

	public static List<Pawn> DetailFor(Pawn charge)
	{
		buffer.Clear();
		Map map = charge?.Map;
		if (map == null)
		{
			return buffer;
		}
		if (!guardsByCharge.TryGetValue(charge, out var value))
		{
			return buffer;
		}
		for (int num = value.Count - 1; num >= 0; num--)
		{
			Pawn pawn = value[num];
			if (pawn == null || pawn.Destroyed)
			{
				value.RemoveAt(num);
			}
			else if (pawn.Spawned && pawn.Map == map && !pawn.Dead && !pawn.Downed && CompArsenal.For(pawn)?.guarding == charge)
			{
				buffer.Add(pawn);
			}
		}
		if (buffer.Count > 1)
		{
			buffer.Sort(ByThingId);
		}
		return buffer;
	}

	public static IntVec3 SlotFor(Pawn guard, Pawn charge)
	{
		if (guard == null || charge == null || !charge.Spawned)
		{
			return IntVec3.Invalid;
		}
		List<Pawn> list = DetailFor(charge);
		int count = Mathf.Max(list.Count, 1);
		GuardFormation formation = CompArsenal.For(charge)?.formation ?? GuardFormation.Ring;
		int index = AssignedIndex(guard, list, formation, count);
		Vector3 vector = Offset(formation, index, count);
		vector = Quaternion.AngleAxis(charge.Rotation.AsAngle, Vector3.up) * vector;
		IntVec3 intVec = charge.Position + new IntVec3(Mathf.RoundToInt(vector.x), 0, Mathf.RoundToInt(vector.z));
		Map map = charge.Map;
		Room room = charge.GetRoom();
		if (Usable(intVec, map, room))
		{
			return intVec;
		}
		float magnitude = vector.magnitude;
		IntVec3 result = IntVec3.Invalid;
		float num = float.MaxValue;
		foreach (IntVec3 item in GenRadial.RadialCellsAround(intVec, 8f, useCenter: false))
		{
			if (Usable(item, map, room))
			{
				float num2 = Mathf.Abs(item.DistanceTo(charge.Position) - magnitude);
				if (!(num2 >= num))
				{
					num = num2;
					result = item;
				}
			}
		}
		if (!result.IsValid)
		{
			return charge.Position;
		}
		return result;
	}

	private static bool Usable(IntVec3 cell, Map map, Room room)
	{
		if (!cell.InBounds(map) || !cell.Standable(map))
		{
			return false;
		}
		if (room == null)
		{
			return true;
		}
		return cell.GetRoom(map) == room;
	}

	private static int AssignedIndex(Pawn guard, List<Pawn> detail, GuardFormation formation, int count)
	{
		int num = RoleOrdered(detail, formation).IndexOf(guard);
		if (num < 0)
		{
			return 0;
		}
		if (!UsesFrontnessPriority(formation))
		{
			return num;
		}
		List<int> list = FrontnessRank(formation, count);
		if (num >= list.Count)
		{
			return num;
		}
		return list[num];
	}

	private static bool UsesFrontnessPriority(GuardFormation formation)
	{
		if (formation != GuardFormation.Ring && formation != GuardFormation.Bastion && formation != GuardFormation.Box)
		{
			return formation == GuardFormation.Diamond;
		}
		return true;
	}

	private static List<int> FrontnessRank(GuardFormation formation, int count)
	{
		rankBuffer.Clear();
		for (int i = 0; i < count; i++)
		{
			rankBuffer.Add(i);
		}
		bool outerFirst = formation == GuardFormation.Ring || formation == GuardFormation.Box || formation == GuardFormation.Diamond;
		rankBuffer.Sort((int a, int b) =>
		{
			if (outerFirst)
			{
				int num = RingOf(formation, a);
				int num2 = RingOf(formation, b);
				if (num != num2)
				{
					return num2.CompareTo(num);
				}
			}
			Vector3 vector = Offset(formation, a, count);
			Vector3 vector2 = Offset(formation, b, count);
			int num3 = vector2.z.CompareTo(vector.z);
			return (num3 == 0) ? Mathf.Abs(vector.x).CompareTo(Mathf.Abs(vector2.x)) : num3;
		});
		return rankBuffer;
	}

	private static int RingOf(GuardFormation formation, int index)
	{
		return formation switch
		{
			GuardFormation.Box => RingFor(index, (int n) => 8 * (2 * n - 1)), 
			GuardFormation.Diamond => RingFor(index, (int n) => 4 * (2 * n - 1)), 
			_ => index / 8 + 1, 
		};
	}

	private static List<Pawn> RoleOrdered(List<Pawn> detail, GuardFormation formation)
	{
		roleBuffer.Clear();
		rangedBuffer.Clear();
		meleeBuffer.Clear();
		for (int i = 0; i < detail.Count; i++)
		{
			(IsRanged(detail[i]) ? rangedBuffer : meleeBuffer).Add(detail[i]);
		}
		switch (formation)
		{
		case GuardFormation.Screen:
		{
			int num = meleeBuffer.Count / 2;
			for (int j = 0; j < num; j++)
			{
				roleBuffer.Add(meleeBuffer[j]);
			}
			roleBuffer.AddRange(rangedBuffer);
			for (int k = num; k < meleeBuffer.Count; k++)
			{
				roleBuffer.Add(meleeBuffer[k]);
			}
			return roleBuffer;
		}
		case GuardFormation.Wedge:
		case GuardFormation.Column:
		case GuardFormation.Echelon:
			roleBuffer.AddRange(rangedBuffer);
			roleBuffer.AddRange(meleeBuffer);
			return roleBuffer;
		default:
			if (UsesFrontnessPriority(formation))
			{
				roleBuffer.AddRange(meleeBuffer);
				roleBuffer.AddRange(rangedBuffer);
				return roleBuffer;
			}
			roleBuffer.AddRange(detail);
			return roleBuffer;
		}
	}

	private static bool IsRanged(Pawn pawn)
	{
		Pawn_EquipmentTracker equipment = pawn.equipment;
		if (equipment == null)
		{
			return false;
		}
		return equipment.Primary?.def.IsRangedWeapon == true;
	}

	private static Vector3 Offset(GuardFormation formation, int index, int count)
	{
		switch (formation)
		{
		case GuardFormation.Screen:
		{
			float num14 = 140f;
			float num15 = ((count > 1) ? (num14 / (float)(count - 1)) : 0f);
			float f2 = ((count > 1) ? ((0f - num14) / 2f + num15 * (float)index) : 0f) * ((float)Math.PI / 180f);
			return new Vector3(Mathf.Sin(f2) * 2.2f, 0f, Mathf.Cos(f2) * 2.2f);
		}
		case GuardFormation.Wedge:
		{
			int num16 = index / 2 + 1;
			return new Vector3((float)(((index % 2 != 0) ? 1 : (-1)) * num16) * 1.4f, 0f, (float)num16 * 0.9f);
		}
		case GuardFormation.Column:
		{
			int num9 = index / 2 + 1;
			float z = ((index % 2 == 0) ? ((float)num9 * 1.1f) : ((float)(-num9) * 1.1f));
			return new Vector3(0f, 0f, z);
		}
		case GuardFormation.Box:
		{
			int num10 = RingFor(index, (int n) => 8 * (2 * n - 1));
			int layer2 = 2 * num10 - 1;
			int num11 = CellsBefore(num10, (int n) => 8 * (2 * n - 1));
			var (num12, num13) = SquareRingCell(index - num11, layer2);
			return new Vector3(num12, 0f, num13);
		}
		case GuardFormation.Echelon:
			return new Vector3((float)(index + 1) * 1.1f, 0f, (float)(-(index + 1)) * 0.9f);
		case GuardFormation.Bastion:
		{
			float num17 = 1.21f;
			float f3 = 360f / (float)count * (float)index * ((float)Math.PI / 180f);
			return new Vector3(Mathf.Sin(f3) * num17, 0f, Mathf.Cos(f3) * num17);
		}
		case GuardFormation.Diamond:
		{
			int num5 = RingFor(index, (int n) => 4 * (2 * n - 1));
			int layer = 2 * num5 - 1;
			int num6 = CellsBefore(num5, (int n) => 4 * (2 * n - 1));
			var (num7, num8) = DiamondRingCell(index - num6, layer);
			return new Vector3(num7, 0f, num8);
		}
		default:
		{
			int num = index / 8 + 1;
			int num2 = index % 8;
			int num3 = Mathf.Min(8, count - (num - 1) * 8);
			float num4 = 2.2f * (float)(2 * num - 1);
			float f = 360f / (float)num3 * (float)num2 * ((float)Math.PI / 180f);
			return new Vector3(Mathf.Sin(f) * num4, 0f, Mathf.Cos(f) * num4);
		}
		}
	}

	private static int RingFor(int index, Func<int, int> capacityForRing)
	{
		int i = 1;
		for (int num = 0; index >= num + capacityForRing(i); i++)
		{
			num += capacityForRing(i);
		}
		return i;
	}

	private static int CellsBefore(int ring, Func<int, int> capacityForRing)
	{
		int num = 0;
		for (int i = 1; i < ring; i++)
		{
			num += capacityForRing(i);
		}
		return num;
	}

	private static (int x, int z) DiamondRingCell(int localIndex, int layer)
	{
		int num = localIndex;
		if (num < layer)
		{
			return (x: num, z: layer - num);
		}
		num -= layer;
		if (num < layer)
		{
			return (x: layer - num, z: -num);
		}
		num -= layer;
		if (num < layer)
		{
			return (x: -num, z: -(layer - num));
		}
		num -= layer;
		return (x: -(layer - num), z: num);
	}

	private static (int x, int z) SquareRingCell(int localIndex, int layer)
	{
		int num = localIndex;
		int num2 = layer;
		if (num < num2)
		{
			return (x: num, z: layer);
		}
		num -= num2;
		num2 = 2 * layer;
		if (num < num2)
		{
			return (x: layer, z: layer - num);
		}
		num -= num2;
		num2 = 2 * layer;
		if (num < num2)
		{
			return (x: layer - num, z: -layer);
		}
		num -= num2;
		num2 = 2 * layer;
		if (num < num2)
		{
			return (x: -layer, z: -layer + num);
		}
		num -= num2;
		return (x: -layer + num, z: layer);
	}

	public static string LabelKey(this GuardFormation formation)
	{
		return "ASA_Formation_" + formation;
	}

	public static string DescKey(this GuardFormation formation)
	{
		return "ASA_Formation_" + formation.ToString() + "_desc";
	}

	public static bool Offerable(this GuardFormation formation)
	{
		return true;
	}
}
