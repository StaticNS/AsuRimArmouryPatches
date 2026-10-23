using Verse;

namespace AsuRim.Armoury;

public class Dialog_RenameSquad : Dialog_Rename<Squad>
{
	protected override int MaxNameLength
	{
		public get
		{
			return 56;
		}
	}

	public Dialog_RenameSquad(Squad squad)
		: base(squad)
	{
	}
}
