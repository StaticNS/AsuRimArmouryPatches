using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace AsuRim.Armoury;

[StaticConstructorOnStartup]
public static class DraftableAnimalsBridge
{
	public const string PackageId = "Wolfcub05.DraftableAnimals";

	private const string UtilityType = "DraftableAnimals.DraftableAnimalsUtility";

	private const string DraftableMethod = "IsTrainedToAttack";

	private static Func<Pawn, bool> canDraft;

	private static bool bound;

	private static bool failed;

	public static bool Present { get; private set; }

	public static bool Bound => canDraft != null;

	static DraftableAnimalsBridge()
	{
		Bind();
	}

	public static void Bind()
	{
		if (bound)
		{
			return;
		}
		bound = true;
		Type type = AccessTools.TypeByName("DraftableAnimals.DraftableAnimalsUtility");
		Present = ModsConfig.IsActive("Wolfcub05.DraftableAnimals") || type != null;
		if (!Present)
		{
			return;
		}
		MethodInfo methodInfo = ((type == null) ? null : AccessTools.Method(type, "IsTrainedToAttack", new Type[1] { typeof(Pawn) }));
		if (methodInfo != null && methodInfo.IsStatic && methodInfo.ReturnType == typeof(bool))
		{
			try
			{
				canDraft = (Func<Pawn, bool>)Delegate.CreateDelegate(typeof(Func<Pawn, bool>), methodInfo);
			}
			catch (Exception)
			{
				canDraft = null;
			}
		}
		if (canDraft == null)
		{
			Log.Warning("[Rules of Engagement] Draftable Animals is active, but DraftableAnimals.DraftableAnimalsUtility.IsTrainedToAttack could not be found. Drafted animals still get stances; undrafted ones without a master will not show them.");
		}
	}

	public static bool CanDraft(Pawn pawn)
	{
		if (canDraft == null || pawn == null)
		{
			return false;
		}
		try
		{
			return canDraft(pawn);
		}
		catch (Exception ex)
		{
			canDraft = null;
			if (!failed)
			{
				failed = true;
				Log.Warning("[Rules of Engagement] Draftable Animals check failed and is now off: " + ex.GetBaseException().Message);
			}
			return false;
		}
	}
}
