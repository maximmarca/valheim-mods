using HarmonyLib;

namespace TameableCreatures;

[HarmonyPatch(typeof(MonsterAI), "DoAttack")]
internal static class Patch_MonsterAI_DoAttack
{
	private static bool Prefix(Character ___m_character, ref bool __result)
	{
		if (___m_character is Humanoid)
		{
			return true;
		}
		__result = false;
		return false;
	}
}
