using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace TameableCreatures;

[HarmonyPatch(typeof(MonsterAI), "UpdateAI")]
internal static class Patch_MonsterAI_UpdateAI
{
	private static readonly MethodInfo s_flee = AccessTools.Method(typeof(BaseAI), "Flee");

	private static readonly MethodInfo s_setAlerted = AccessTools.Method(typeof(BaseAI), "SetAlerted");

	private static readonly Func<BaseAI, float, bool> s_baseUpdateAI = AccessTools.MethodDelegate<Func<BaseAI, float, bool>>(AccessTools.Method(typeof(BaseAI), "UpdateAI"), null, virtualCall: false);

	private static readonly Dictionary<int, float> s_fleeTimers = new Dictionary<int, float>();

	private static bool Prefix(MonsterAI __instance, float dt, Character ___m_character, ref Character ___m_targetCreature, ref bool __result)
	{
		if (___m_character == null || ___m_character is Humanoid || ___m_character.IsTamed())
		{
			return true;
		}
		int instanceID = __instance.GetInstanceID();
		float value = TameableCreaturesPlugin.ScareRange.Value;
		if (!__instance.IsAlerted() && value > 0f)
		{
			Player closestPlayer = Player.GetClosestPlayer(__instance.transform.position, value);
			if (closestPlayer != null)
			{
				___m_targetCreature = closestPlayer;
				s_setAlerted.Invoke(__instance, new object[1] { true });
			}
		}
		if (!__instance.IsAlerted() || ___m_targetCreature == null)
		{
			s_fleeTimers.Remove(instanceID);
			return true;
		}
		s_fleeTimers.TryGetValue(instanceID, out var value2);
		value2 += dt;
		if (value2 >= TameableCreaturesPlugin.FleeSeconds.Value)
		{
			s_fleeTimers.Remove(instanceID);
			___m_targetCreature = null;
			s_setAlerted.Invoke(__instance, new object[1] { false });
			return true;
		}
		s_fleeTimers[instanceID] = value2;
		if (s_fleeTimers.Count > 1000)
		{
			s_fleeTimers.Clear();
		}
		if (!s_baseUpdateAI(__instance, dt))
		{
			__result = false;
			return false;
		}
		s_flee.Invoke(__instance, new object[2]
		{
			dt,
			___m_targetCreature.transform.position
		});
		__result = true;
		return false;
	}
}
