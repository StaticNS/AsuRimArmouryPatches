using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace AsuRim.Armoury;

[StaticConstructorOnStartup]
public static class SurveillanceShiftBridge
{
	private static Func<Pawn, bool> hasAnyDuty;

	private static Func<JobDef, bool> isGuardJob;

	private static Action<Pawn, Pawn> setProtectee;

	private static Func<Pawn, object> getAssignment;

	private static FieldInfo assignmentProtectee;

	internal static ThingDef guardPostDef;

	internal static ThingDef waypointDef;

	internal static Type guardingComponentType;

	internal static MethodInfo createRoute;

	internal static MethodInfo assignRoute;

	internal static FieldInfo routes;

	internal static FieldInfo postStance;

	internal static FieldInfo routeId;

	internal static FieldInfo routeLoopMode;

	internal static FieldInfo routeStance;

	internal static FieldInfo waypointRouteId;

	internal static FieldInfo waypointDwell;

	internal static Type guardStanceType;

	internal static Type loopModeType;

	public static readonly List<string> Missing;

	public static bool Bound { get; private set; }

	public static bool ConversionReady { get; private set; }

	public static bool EscortReady
	{
		get
		{
			if (setProtectee != null && getAssignment != null)
			{
				return assignmentProtectee != null;
			}
			return false;
		}
	}

	static SurveillanceShiftBridge()
	{
		Missing = new List<string>();
		if (GuardStandDown.Present)
		{
			Bind();
		}
	}

	public static void Bind()
	{
		Missing.Clear();
		hasAnyDuty = null;
		isGuardJob = null;
		setProtectee = null;
		getAssignment = null;
		assignmentProtectee = null;
		Type owner = FindType("SurveillanceShift.GuardUtility");
		Type type = FindType("SurveillanceShift.Building_GuardPost");
		Type type2 = FindType("SurveillanceShift.Building_PatrolWaypoint");
		Type type3 = FindType("SurveillanceShift.PatrolRoute");
		guardingComponentType = FindType("SurveillanceShift.MapComponent_Guarding");
		guardStanceType = FindType("SurveillanceShift.GuardStance");
		loopModeType = FindType("SurveillanceShift.RouteLoopMode");
		hasAnyDuty = StaticDelegate<Func<Pawn, bool>>(owner, "HasAnyDuty", new Type[1] { typeof(Pawn) });
		isGuardJob = StaticDelegate<Func<JobDef, bool>>(owner, "IsGuardJob", new Type[1] { typeof(JobDef) });
		setProtectee = StaticDelegate<Action<Pawn, Pawn>>(owner, "SetProtectee", new Type[2]
		{
			typeof(Pawn),
			typeof(Pawn)
		});
		getAssignment = StaticDelegate<Func<Pawn, object>>(owner, "GetGuardAssignment", new Type[1] { typeof(Pawn) });
		assignmentProtectee = Field(FindType("SurveillanceShift.GuardAssignment"), "protectee", typeof(Pawn));
		assignRoute = ((type3 == null) ? null : Method(owner, "AssignRoute", typeof(Pawn), type3));
		createRoute = Method(guardingComponentType, "CreateRoute", typeof(string));
		routes = Field(guardingComponentType, "routes", null);
		if (routes != null && !typeof(IList).IsAssignableFrom(routes.FieldType))
		{
			Missing.Add("MapComponent_Guarding.routes");
			routes = null;
		}
		postStance = Field(type, "stance", guardStanceType);
		routeId = Field(type3, "id", typeof(int));
		routeLoopMode = Field(type3, "loopMode", loopModeType);
		routeStance = Field(type3, "stance", guardStanceType);
		waypointRouteId = Field(type2, "routeId", typeof(int));
		waypointDwell = Field(type2, "dwellTicks", typeof(int));
		guardPostDef = Def("SS_GuardPost", type);
		waypointDef = Def("SS_PatrolWaypoint", type2);
		if (guardStanceType != null)
		{
			string[] array = new string[3] { "Hold", "Defend", "Pursue" };
			foreach (string text in array)
			{
				if (!Enum.IsDefined(guardStanceType, text))
				{
					Missing.Add("GuardStance." + text);
				}
			}
		}
		if (loopModeType != null)
		{
			string[] array = new string[2] { "Loop", "PingPong" };
			foreach (string text2 in array)
			{
				if (!Enum.IsDefined(loopModeType, text2))
				{
					Missing.Add("RouteLoopMode." + text2);
				}
			}
		}
		Bound = hasAnyDuty != null && isGuardJob != null;
		ConversionReady = Missing.Count == 0;
		if (Missing.Count > 0 && GuardStandDown.Active)
		{
			Log.Warning("[Rules of Engagement] Surveillance Shift is active, but some of its guarding code could not be found (" + string.Join(", ", Missing) + "). RoE guarding stays switched off and old posts are left in place, unconverted.");
		}
	}

	public static bool HasDuty(Pawn pawn)
	{
		if (hasAnyDuty == null || pawn == null)
		{
			return false;
		}
		try
		{
			return hasAnyDuty(pawn);
		}
		catch (Exception)
		{
			return false;
		}
	}

	public static bool OnGuardJob(Pawn pawn)
	{
		if (isGuardJob == null)
		{
			return false;
		}
		JobDef jobDef = pawn?.CurJobDef;
		if (jobDef == null)
		{
			return false;
		}
		return isGuardJob(jobDef);
	}

	internal static bool SetProtectee(Pawn guard, Pawn charge)
	{
		if (setProtectee == null)
		{
			return false;
		}
		setProtectee(guard, charge);
		return true;
	}

	internal static Pawn ProtecteeOf(Pawn guard)
	{
		if (guard == null || getAssignment == null || assignmentProtectee == null)
		{
			return null;
		}
		object obj = getAssignment(guard);
		if (obj != null)
		{
			return assignmentProtectee.GetValue(obj) as Pawn;
		}
		return null;
	}

	internal static object StanceFor(CombatStance stance)
	{
		string value;
		switch (stance)
		{
		case CombatStance.Hold:
		case CombatStance.Hide:
			value = "Hold";
			break;
		case CombatStance.Brawl:
		case CombatStance.Hunt:
		case CombatStance.Breach:
			value = "Pursue";
			break;
		default:
			value = "Defend";
			break;
		}
		return Enum.Parse(guardStanceType, value);
	}

	internal static object LoopModeFor(bool loops)
	{
		return Enum.Parse(loopModeType, loops ? "Loop" : "PingPong");
	}

	internal static MapComponent GuardingFor(Map map)
	{
		if (map == null || guardingComponentType == null)
		{
			return null;
		}
		List<MapComponent> components = map.components;
		for (int i = 0; i < components.Count; i++)
		{
			if (guardingComponentType.IsInstanceOfType(components[i]))
			{
				return components[i];
			}
		}
		return null;
	}

	private static Type FindType(string name)
	{
		Type type = AccessTools.TypeByName(name);
		if (type == null)
		{
			Missing.Add(name);
		}
		return type;
	}

	private static MethodInfo Method(Type owner, string name, params Type[] args)
	{
		if (owner == null)
		{
			return null;
		}
		MethodInfo methodInfo = AccessTools.Method(owner, name, args);
		if (methodInfo == null)
		{
			Missing.Add(owner.Name + "." + name);
		}
		return methodInfo;
	}

	private static T StaticDelegate<T>(Type owner, string name, params Type[] args) where T : Delegate
	{
		MethodInfo methodInfo = Method(owner, name, args);
		if (methodInfo == null)
		{
			return null;
		}
		try
		{
			return (T)Delegate.CreateDelegate(typeof(T), methodInfo);
		}
		catch (Exception)
		{
			Missing.Add(owner.Name + "." + name + " (signature)");
			return null;
		}
	}

	private static FieldInfo Field(Type owner, string name, Type fieldType)
	{
		if (owner == null)
		{
			return null;
		}
		FieldInfo fieldInfo = AccessTools.Field(owner, name);
		if (fieldInfo == null || fieldInfo.IsStatic || (fieldType != null && fieldInfo.FieldType != fieldType))
		{
			Missing.Add(owner.Name + "." + name);
			return null;
		}
		return fieldInfo;
	}

	private static ThingDef Def(string defName, Type thingClass)
	{
		ThingDef namedSilentFail = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
		if (namedSilentFail == null || thingClass == null || !thingClass.IsAssignableFrom(namedSilentFail.thingClass))
		{
			Missing.Add(defName);
			return null;
		}
		return namedSilentFail;
	}
}
