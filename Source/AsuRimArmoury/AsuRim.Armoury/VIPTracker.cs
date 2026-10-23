using UnityEngine;
using Verse;

namespace AsuRim.Armoury;

public class VIPTracker : MapComponent
{
	private Pawn vip;

	public Pawn VIP
	{
		get
		{
			if (vip != null && (vip.Dead || vip.Destroyed))
			{
				vip = null;
			}
			else if (vip != null && vip.Spawned && vip.Map != map)
			{
				vip = null;
			}
			return vip;
		}
		set
		{
			vip = value;
		}
	}

	public VIPTracker(Map map)
		: base(map)
	{
	}

	public static VIPTracker For(Map map)
	{
		return map?.GetComponent<VIPTracker>();
	}

	public static Pawn Of(Map map)
	{
		return For(map)?.VIP;
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_References.Look(ref vip, "vip");
	}

	public override void MapComponentUpdate()
	{
		base.MapComponentUpdate();
		Pawn vIP = VIP;
		if (vIP != null && vIP.Spawned && Find.CurrentMap == map)
		{
			Vector3 drawPos = vIP.DrawPos;
			drawPos.y = AltitudeLayer.MetaOverlays.AltitudeFor();
			float num = 1f + Mathf.Sin(Time.realtimeSinceStartup * 2.4f) * 0.08f;
			Matrix4x4 matrix = default;
			matrix.SetTRS(drawPos, Quaternion.identity, new Vector3(1.9f * num, 1f, 1.9f * num));
			Graphics.DrawMesh(MeshPool.plane10, matrix, VIPMat.Ring, 0);
		}
	}
}
