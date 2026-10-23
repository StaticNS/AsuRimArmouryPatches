namespace AsuRimArmoury.Kit;

public readonly struct PatchCounts(int classes, int methods, int skipped, int failed, int accessorsFailed)
{
	public readonly int Classes = classes;

	public readonly int Methods = methods;

	public readonly int Skipped = skipped;

	public readonly int Failed = failed;

	public readonly int AccessorsFailed = accessorsFailed;

	public bool Clean
	{
		get
		{
			if (Failed == 0)
			{
				return AccessorsFailed == 0;
			}
			return false;
		}
	}

	public override string ToString()
	{
		return $"{Classes} classes, {Methods} methods patched, {Skipped} skipped, {Failed} failed, {AccessorsFailed} accessors missing";
	}
}
