using System;
using HarmonyLib;

namespace TameableCreatures;

[HarmonyPatch(typeof(MonsterAI), "UpdateConsumeItem")]
internal static class Patch_MonsterAI_UpdateConsumeItem
{
	private static Exception Finalizer(Exception __exception, Character ___m_character)
	{
		if (__exception is NullReferenceException && !(___m_character is Humanoid))
		{
			return null;
		}
		return __exception;
	}
}
