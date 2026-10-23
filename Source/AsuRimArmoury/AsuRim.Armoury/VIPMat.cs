using UnityEngine;
using Verse;

namespace AsuRim.Armoury;

[StaticConstructorOnStartup]
internal static class VIPMat
{
	internal static readonly Material Ring = MaterialPool.MatFrom("Things/Posts/VIPRing", ShaderDatabase.MoteGlow, new Color(1f, 0.15f, 0.15f));
}
