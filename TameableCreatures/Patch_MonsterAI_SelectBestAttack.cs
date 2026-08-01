using HarmonyLib;

namespace TameableCreatures;

[HarmonyPatch(typeof(MonsterAI), "SelectBestAttack")]
internal static class Patch_MonsterAI_SelectBestAttack
{
	private static bool Prefix(Character ___m_character, ref ItemDrop.ItemData __result)
	{
		if (___m_character is Humanoid)
		{
			return true;
		}
		__result = null;
		return false;
	}
}
