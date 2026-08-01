using HarmonyLib;
using UnityEngine;

namespace TameableCreatures;

[HarmonyPatch(typeof(Character), "SetLevel")]
internal static class Patch_Character_SetLevel
{
	private static void Prefix(Character __instance, ref int level)
	{
		Procreation current = Patch_Procreation_Procreate.Current;
		if (current == null || __instance == null || __instance.gameObject == current.gameObject)
		{
			return;
		}
		Character component = current.GetComponent<Character>();
		if (component == null)
		{
			return;
		}
		int num = Mathf.Max(1, component.GetLevel());
		int num2 = num;
		string prefabName = Utils.GetPrefabName(current.gameObject);
		float num3 = current.m_totalCheckRange * current.m_totalCheckRange;
		foreach (Character allCharacter in Character.GetAllCharacters())
		{
			if (!(allCharacter == component) && !(allCharacter == __instance) && allCharacter.IsTamed() && !(Utils.GetPrefabName(allCharacter.gameObject) != prefabName))
			{
				float sqrMagnitude = (allCharacter.transform.position - component.transform.position).sqrMagnitude;
				if (sqrMagnitude < num3)
				{
					num3 = sqrMagnitude;
					num2 = Mathf.Max(1, allCharacter.GetLevel());
				}
			}
		}
		float value = TameableCreaturesPlugin.MutationUpChance.Value;
		float value2 = TameableCreaturesPlugin.MutationDownChance.Value;
		float value3 = Random.value;
		if (value3 < value)
		{
			level = Mathf.Clamp(Mathf.Max(num, num2) + 1, 1, 6);
			TameableCreaturesPlugin.Log.LogInfo($"¡Mutación! Cría de {prefabName} nace con {level - 1} estrella(s) (padres {num - 1}/{num2 - 1})");
		}
		else if (value3 < value + value2)
		{
			level = Mathf.Clamp(Mathf.Min(num, num2) - 1, 1, 6);
			TameableCreaturesPlugin.Log.LogInfo($"Mutación regresiva: cría de {prefabName} nace con {level - 1} estrella(s) (padres {num - 1}/{num2 - 1})");
		}
	}

	// v0.5.0: marcar a la recién nacida como bebé (SetLevel solo se ejecuta sobre
	// la cría dentro de Procreate; Current lo delimita).
	private static void Postfix(Character __instance)
	{
		Procreation current = Patch_Procreation_Procreate.Current;
		if (current == null || __instance == null || __instance.gameObject == current.gameObject || !TameableCreaturesPlugin.BabyEnabled.Value || ZNet.instance == null)
		{
			return;
		}
		string prefabName = Utils.GetPrefabName(__instance.gameObject);
		bool flag = false;
		string[] array = TameableCreaturesPlugin.Creatures.Value.Split(',');
		foreach (string text in array)
		{
			if (text.Trim() == prefabName)
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			ZNetView component = __instance.GetComponent<ZNetView>();
			if (component != null && component.IsValid())
			{
				component.GetZDO().Set(BabyGrowth.ZdoKey, (float)ZNet.instance.GetTimeSeconds());
				TameableCreaturesPlugin.Log.LogInfo($"Cría bebé de {prefabName} nacida (crece en {TameableCreaturesPlugin.BabyGrowMinutes.Value:0} min)");
			}
		}
	}
}
