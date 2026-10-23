using System.Collections.Generic;
using Verse;

namespace AsuRim.Armoury;

internal static class Commanded
{
	internal const int StanceGroup = 84610001;

	internal const int PolicyGroup = 84610002;

	internal const int OverwatchGroup = 84610003;

	internal const int WalkFireGroup = 84610004;

	private static readonly List<Pawn> buffer = new List<Pawn>();

	internal static List<Pawn> With(Pawn pawn)
	{
		buffer.Clear();
		List<object> selectedObjects = Find.Selector.SelectedObjects;
		for (int i = 0; i < selectedObjects.Count; i++)
		{
			if (selectedObjects[i] is Pawn pawn2 && StanceRules.CanTakeOrders(pawn2))
			{
				buffer.Add(pawn2);
			}
		}
		if (!buffer.Contains(pawn))
		{
			buffer.Add(pawn);
		}
		return buffer;
	}
}
