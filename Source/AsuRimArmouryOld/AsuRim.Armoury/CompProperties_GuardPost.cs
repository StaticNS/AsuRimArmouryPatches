using Verse;

namespace AsuRim.Armoury;

public class CompProperties_GuardPost : CompProperties
{
	public PostKind kind;

	public CompProperties_GuardPost()
	{
		compClass = typeof(CompGuardPost);
	}
}
