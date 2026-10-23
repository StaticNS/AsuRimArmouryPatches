using System.Xml;
using Verse;

namespace AsuRim.Armoury;

public class PatchOperationSurveillanceShift : PatchOperationSequence
{
	public override bool ApplyWorker(XmlDocument xml)
	{
		if (!GuardStandDown.Active)
		{
			return true;
		}
		return base.ApplyWorker(xml);
	}
}
