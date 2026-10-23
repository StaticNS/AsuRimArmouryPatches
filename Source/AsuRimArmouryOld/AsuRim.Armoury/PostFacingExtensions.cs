using Verse;

namespace AsuRim.Armoury;

public static class PostFacingExtensions
{
	public static Rot4 ToRot(this PostFacing facing)
	{
		return facing switch
		{
			PostFacing.North => Rot4.North, 
			PostFacing.East => Rot4.East, 
			PostFacing.West => Rot4.West, 
			_ => Rot4.South, 
		};
	}

	public static string LabelKey(this PostFacing facing)
	{
		return "ASA_Facing_" + facing;
	}
}
