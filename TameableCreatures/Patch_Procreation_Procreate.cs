using HarmonyLib;

namespace TameableCreatures;

[HarmonyPatch(typeof(Procreation), "Procreate")]
internal static class Patch_Procreation_Procreate
{
	internal static Procreation Current;

	private static bool Prefix(Procreation __instance)
	{
		// v0.5.0: los bebés no crían ni acumulan amor hasta crecer.
		if (TameableCreaturesPlugin.BabyEnabled.Value && __instance != null && BabyGrowth.IsBaby(__instance.GetComponent<ZNetView>()))
		{
			return false;
		}
		Current = __instance;
		return true;
	}

	private static void Finalizer()
	{
		Current = null;
	}
}
