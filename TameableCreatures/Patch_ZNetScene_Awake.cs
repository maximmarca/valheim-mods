using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace TameableCreatures;

[HarmonyPatch(typeof(ZNetScene), "Awake")]
internal static class Patch_ZNetScene_Awake
{
	private static void Postfix(ZNetScene __instance)
	{
		GameObject prefab = __instance.GetPrefab("Boar");
		if (prefab == null)
		{
			TameableCreaturesPlugin.Log.LogError("No se encontró el prefab Boar");
			return;
		}
		string[] array = TameableCreaturesPlugin.Creatures.Value.Split(',');
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim();
			if (text.Length != 0)
			{
				try
				{
					MakeTameable(__instance, text, prefab);
				}
				catch (Exception arg)
				{
					TameableCreaturesPlugin.Log.LogError($"Error modificando el prefab {text}: {arg}");
				}
			}
		}
		array = TameableCreaturesPlugin.MakeCommandable.Value.Split(',');
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = array[i].Trim();
			if (text2.Length != 0)
			{
				GameObject prefab2 = __instance.GetPrefab(text2);
				Tameable tameable = (prefab2 ? prefab2.GetComponent<Tameable>() : null);
				if (tameable == null)
				{
					TameableCreaturesPlugin.Log.LogWarning("MakeCommandable: '" + text2 + "' no existe o no es adiestrable");
				}
				else if (!tameable.m_commandable)
				{
					tameable.m_commandable = true;
					TameableCreaturesPlugin.Log.LogInfo(text2 + " domesticado ahora puede seguirte (E)");
				}
			}
		}
		// v0.11.0: dietas extra por config (ej. lobos comen carne de oso, que
		// vanilla excluye explícitamente de su lista).
		array = TameableCreaturesPlugin.ExtraFood.Value.Split(',');
		for (int j = 0; j < array.Length; j++)
		{
			string text3 = array[j].Trim();
			int num2 = text3.IndexOf(':');
			if (num2 <= 0)
			{
				continue;
			}
			string text4 = text3.Substring(0, num2).Trim();
			string text5 = text3.Substring(num2 + 1).Trim();
			GameObject prefab3 = __instance.GetPrefab(text4);
			GameObject prefab4 = __instance.GetPrefab(text5);
			MonsterAI monsterAI2 = ((prefab3 != null) ? prefab3.GetComponent<MonsterAI>() : null);
			ItemDrop itemDrop2 = ((prefab4 != null) ? prefab4.GetComponent<ItemDrop>() : null);
			if (monsterAI2 == null || itemDrop2 == null)
			{
				TameableCreaturesPlugin.Log.LogWarning("ExtraFood: '" + text3 + "' inválido (criatura sin MonsterAI o ítem inexistente), se ignora");
				continue;
			}
			if (monsterAI2.m_consumeItems == null)
			{
				monsterAI2.m_consumeItems = new List<ItemDrop>();
			}
			if (!monsterAI2.m_consumeItems.Contains(itemDrop2))
			{
				monsterAI2.m_consumeItems.Add(itemDrop2);
				TameableCreaturesPlugin.Log.LogInfo(text4 + " ahora también come " + text5);
			}
		}
		int value = TameableCreaturesPlugin.MaxCreaturesNearby.Value;
		if (value <= 0)
		{
			return;
		}
		int num = 0;
		foreach (GameObject prefab3 in __instance.m_prefabs)
		{
			Procreation procreation = (prefab3 ? prefab3.GetComponent<Procreation>() : null);
			if (procreation != null && procreation.m_maxCreatures != value)
			{
				procreation.m_maxCreatures = value;
				num++;
			}
		}
		if (num > 0)
		{
			TameableCreaturesPlugin.Log.LogInfo($"Densidad de cría: hasta {value} por especie en el radio de chequeo ({num} especies ajustadas)");
		}
	}

	private static void MakeTameable(ZNetScene scene, string name, GameObject boar)
	{
		GameObject prefab = scene.GetPrefab(name);
		if (prefab == null)
		{
			TameableCreaturesPlugin.Log.LogWarning("Prefab '" + name + "' no encontrado, se ignora");
		}
		else if (prefab.GetComponent<Character>() == null)
		{
			TameableCreaturesPlugin.Log.LogWarning("'" + name + "' no es una criatura, se ignora");
		}
		else
		{
			if (prefab.GetComponent<Tameable>() != null)
			{
				return;
			}
			MonsterAI component = boar.GetComponent<MonsterAI>();
			MonsterAI monsterAI = prefab.GetComponent<MonsterAI>();
			if (monsterAI == null)
			{
				AnimalAI component2 = prefab.GetComponent<AnimalAI>();
				EffectList ownIdleSound = null;
				EffectList ownAlertedEffects = null;
				float ownIdleSoundInterval = 5f;
				float ownIdleSoundChance = 0.5f;
				if (component2 != null)
				{
					ownIdleSound = component2.m_idleSound;
					ownAlertedEffects = component2.m_alertedEffects;
					ownIdleSoundInterval = component2.m_idleSoundInterval;
					ownIdleSoundChance = component2.m_idleSoundChance;
				}
				monsterAI = prefab.AddComponent<MonsterAI>();
				TameableCreaturesPlugin.CopyPublicFields(component, monsterAI);
				// v0.5.0: el copy de arriba trae también la voz del Boar (m_idleSound y
				// m_alertedEffects viven en BaseAI); restaurar la voz de la especie.
				monsterAI.m_idleSound = ownIdleSound ?? new EffectList();
				monsterAI.m_alertedEffects = ownAlertedEffects ?? new EffectList();
				monsterAI.m_idleSoundInterval = ownIdleSoundInterval;
				monsterAI.m_idleSoundChance = ownIdleSoundChance;
				if (component2 != null)
				{
					UnityEngine.Object.DestroyImmediate(component2, allowDestroyingAssets: true);
				}
			}
			else
			{
				if (monsterAI.m_consumeItems == null || monsterAI.m_consumeItems.Count == 0)
				{
					monsterAI.m_consumeItems = new List<ItemDrop>(component.m_consumeItems);
				}
				else
				{
					foreach (ItemDrop consumeItem in component.m_consumeItems)
					{
						if (!monsterAI.m_consumeItems.Contains(consumeItem))
						{
							monsterAI.m_consumeItems.Add(consumeItem);
						}
					}
				}
				if (monsterAI.m_consumeRange < component.m_consumeRange)
				{
					monsterAI.m_consumeRange = component.m_consumeRange;
				}
				if (monsterAI.m_consumeSearchRange < component.m_consumeSearchRange)
				{
					monsterAI.m_consumeSearchRange = component.m_consumeSearchRange;
				}
				if (monsterAI.m_consumeSearchInterval <= 0f || monsterAI.m_consumeSearchInterval > component.m_consumeSearchInterval)
				{
					monsterAI.m_consumeSearchInterval = component.m_consumeSearchInterval;
				}
			}
			string text = TameableCreaturesPlugin.FoodItems.Value.Trim();
			if (text.Length > 0)
			{
				List<ItemDrop> list = new List<ItemDrop>();
				string[] array = text.Split(',');
				foreach (string text2 in array)
				{
					GameObject prefab2 = scene.GetPrefab(text2.Trim());
					ItemDrop itemDrop = (prefab2 ? prefab2.GetComponent<ItemDrop>() : null);
					if (itemDrop != null)
					{
						list.Add(itemDrop);
					}
					else
					{
						TameableCreaturesPlugin.Log.LogWarning("FoodItems: prefab '" + text2.Trim() + "' no encontrado o no es un ítem");
					}
				}
				if (list.Count > 0)
				{
					monsterAI.m_consumeItems = list;
				}
			}
			Tameable tameable = prefab.AddComponent<Tameable>();
			TameableCreaturesPlugin.CopyPublicFields(boar.GetComponent<Tameable>(), tameable);
			tameable.m_tamingTime = TameableCreaturesPlugin.TamingTimeSeconds.Value;
			// v0.6.0: comen 1 ítem por ciclo de hambre; alargar la saciedad
			// hace que coman menos seguido (el copy traía los 600 s del Boar).
			if (TameableCreaturesPlugin.FedDurationSeconds.Value > 0f)
			{
				tameable.m_fedDuration = TameableCreaturesPlugin.FedDurationSeconds.Value;
			}
			// v0.5.0: los efectos copiados del Boar traen sus gruñidos; dejar los
			// visuales (corazones, humo) y usar la voz de la especie donde aplique.
			EffectList voice = monsterAI.m_idleSound;
			tameable.m_tamedEffect = TameableCreaturesPlugin.ReplaceSfx(tameable.m_tamedEffect, null);
			tameable.m_sootheEffect = TameableCreaturesPlugin.ReplaceSfx(tameable.m_sootheEffect, null);
			tameable.m_petEffect = TameableCreaturesPlugin.ReplaceSfx(tameable.m_petEffect, voice);
			if (TameableCreaturesPlugin.EnableBreeding.Value)
			{
				Procreation procreation = prefab.AddComponent<Procreation>();
				TameableCreaturesPlugin.CopyPublicFields(boar.GetComponent<Procreation>(), procreation);
				procreation.m_offspring = prefab;
				procreation.m_minOffspringLevel = 1;
				procreation.m_loveEffects = TameableCreaturesPlugin.ReplaceSfx(procreation.m_loveEffects, voice);
				procreation.m_birthEffects = TameableCreaturesPlugin.ReplaceSfx(procreation.m_birthEffects, voice);
				if (prefab.GetComponent<BabyGrowth>() == null)
				{
					prefab.AddComponent<BabyGrowth>();
				}
			}
			TameableCreaturesPlugin.Log.LogInfo(name + " ahora es adiestrable");
		}
	}
}
