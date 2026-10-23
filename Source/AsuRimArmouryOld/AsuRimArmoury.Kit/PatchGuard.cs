using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;
using LudeonTK;
using Verse;

namespace AsuRimArmoury.Kit;

public static class PatchGuard
{
	private static string tag = "AsuRimArmoury";

	private static readonly Dictionary<string, int> transpilerRun = new Dictionary<string, int>();

	private static readonly HashSet<string> transpilerWarned = new HashSet<string>();

	private static readonly HashSet<string> probed = new HashSet<string>();

	private static readonly HashSet<string> fired = new HashSet<string>();

	private static readonly List<string> installed = new List<string>();

	private static readonly List<string> failedClasses = new List<string>();

	private static bool reportQueued;

	private static bool reportDone;

	public static string Tag => tag;

	public static IReadOnlyList<string> Installed => installed;

	public static IReadOnlyList<string> FailedClasses => failedClasses;

	public static PatchCounts PatchAll(Harmony harmony, Assembly assembly, string modTag, Func<Type, bool> include = null)
	{
		if (!modTag.NullOrEmpty())
		{
			tag = modTag;
		}
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		Type[] typesFromAssembly = AccessTools.GetTypesFromAssembly(assembly);
		foreach (Type type in typesFromAssembly)
		{
			if (!type.IsDefined(typeof(HarmonyAttribute), inherit: true) || (include != null && !include(type)))
			{
				continue;
			}
			num++;
			try
			{
				List<MethodInfo> list = harmony.CreateClassProcessor(type).Patch();
				if (list == null || list.Count == 0)
				{
					num3++;
					continue;
				}
				num2 += list.Count;
				installed.Add(type.Name);
				if (HasProbe(type))
				{
					probed.Add(type.Name);
				}
			}
			catch (Exception ex)
			{
				num4++;
				failedClasses.Add(type.Name);
				Exception baseException = ex.GetBaseException();
				Log.Error($"[{tag}] Patch class {type.FullName} failed and is not active: {baseException.GetType().Name}: {baseException.Message}\n{ex}");
			}
		}
		int accessorsFailed = VerifyAccessors(assembly);
		PatchCounts patchCounts = new PatchCounts(num, num2, num3, num4, accessorsFailed);
		if (!patchCounts.Clean)
		{
			Log.Warning($"[{tag}] PatchGuard: {patchCounts}");
		}
		QueueReport();
		return patchCounts;
	}

	private static bool HasProbe(Type type)
	{
		FieldInfo field = type.GetField("fired", BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		if (field != null)
		{
			return field.FieldType == typeof(bool);
		}
		return false;
	}

	public static int VerifyAccessors(Assembly assembly)
	{
		int num = 0;
		Type[] typesFromAssembly = AccessTools.GetTypesFromAssembly(assembly);
		foreach (Type type in typesFromAssembly)
		{
			MethodInfo[] methods;
			try
			{
				methods = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			catch
			{
				continue;
			}
			MethodInfo[] array = methods;
			foreach (MethodInfo methodInfo in array)
			{
				if (!methodInfo.ContainsGenericParameters && methodInfo.IsDefined(typeof(UnsafeAccessorAttribute), inherit: false))
				{
					try
					{
						RuntimeHelpers.PrepareMethod(methodInfo.MethodHandle);
					}
					catch (MissingMemberException ex)
					{
						num++;
						Log.Error($"[{tag}] Accessor {type.FullName}.{methodInfo.Name} does not resolve: {ex.Message}");
					}
					catch (Exception ex2)
					{
						num++;
						Log.Warning($"[{tag}] Accessor {type.FullName}.{methodInfo.Name} could not load its target type: {ex2.GetBaseException().Message}");
					}
				}
			}
		}
		return num;
	}

	public static void Expect(string key)
	{
		if (reportDone && transpilerRun.TryGetValue(key, out var value) && value == 0)
		{
			WarnTranspiler(key);
		}
		transpilerRun[key] = 0;
	}

	public static void Matched(string key)
	{
		transpilerRun.TryGetValue(key, out var value);
		transpilerRun[key] = value + 1;
	}

	public static int Matches(string key)
	{
		if (!transpilerRun.TryGetValue(key, out var value))
		{
			return -1;
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static bool Fired(string key)
	{
		if (key != null)
		{
			fired.Add(key);
		}
		return true;
	}

	public static bool HasFired(string key)
	{
		return fired.Contains(key);
	}

	public static List<string> NeverFired()
	{
		List<string> list = new List<string>();
		foreach (string item in probed)
		{
			if (!fired.Contains(item))
			{
				list.Add(item);
			}
		}
		list.Sort(StringComparer.Ordinal);
		return list;
	}

	private static void QueueReport()
	{
		if (reportQueued)
		{
			return;
		}
		reportQueued = true;
		LongEventHandler.ExecuteWhenFinished(() =>
		{
			LongEventHandler.ExecuteWhenFinished(() =>
			{
				LongEventHandler.ExecuteWhenFinished(Report);
			});
		});
	}

	public static void Report()
	{
		reportDone = true;
		foreach (KeyValuePair<string, int> item in transpilerRun)
		{
			if (item.Value == 0)
			{
				WarnTranspiler(item.Key);
			}
		}
	}

	private static void WarnTranspiler(string key)
	{
		if (transpilerWarned.Add(key))
		{
			Log.Warning($"[{tag}] Transpiler {key} matched nothing. The patch is installed but changes no code.");
		}
	}

	public static string Describe()
	{
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
		handler.AppendLiteral("[");
		handler.AppendFormatted(tag);
		handler.AppendLiteral("] PatchGuard");
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(55, 4, stringBuilder2);
		handler.AppendLiteral("Installed classes: ");
		handler.AppendFormatted(installed.Count);
		handler.AppendLiteral(", failed: ");
		handler.AppendFormatted(failedClasses.Count);
		handler.AppendLiteral(", probed: ");
		handler.AppendFormatted(probed.Count);
		handler.AppendLiteral(", probes fired: ");
		handler.AppendFormatted(probed.Count - NeverFired().Count);
		stringBuilder4.AppendLine(ref handler);
		foreach (string failedClass in failedClasses)
		{
			stringBuilder.AppendLine("  failed: " + failedClass);
		}
		foreach (string item in NeverFired())
		{
			stringBuilder.AppendLine("  never fired: " + item);
		}
		foreach (KeyValuePair<string, int> item2 in transpilerRun)
		{
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder5 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(23, 2, stringBuilder2);
			handler.AppendLiteral("  transpiler ");
			handler.AppendFormatted(item2.Key);
			handler.AppendLiteral(": ");
			handler.AppendFormatted(item2.Value);
			handler.AppendLiteral(" matches");
			stringBuilder5.AppendLine(ref handler);
		}
		return stringBuilder.ToString().TrimEndNewlines();
	}

	[DebugAction("Mods", null, false, false, false, false, false, 0, false, allowedGameStates = (AllowedGameStates.Entry | AllowedGameStates.Playing))]
	private static DebugActionNode PatchGuardReport()
	{
		return new DebugActionNode(tag + ": patch report", DebugActionType.Action, () =>
		{
			Log.Message(Describe());
		});
	}
}
