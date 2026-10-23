using Verse;

namespace AsuRimArmoury.Kit;

public sealed class CacheResetComponent : GameComponent
{
	public CacheResetComponent(Game game)
	{
		CacheReset.RunResets();
	}

	public override void FinalizeInit()
	{
		CacheReset.RunRebuilds();
	}
}
