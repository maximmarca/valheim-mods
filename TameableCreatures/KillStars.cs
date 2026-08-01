using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace TameableCreatures;

// v0.9.0: los animales domesticados pueden subir una estrella en combate.
// 2% al rematar a un enemigo, 1% por asistencia (haberlo dañado dentro de la
// ventana previa a su muerte). Tope 5 estrellas. Config en [Combate].
internal static class KillStars
{
	internal struct Assist
	{
		public Character Pet;

		public float Time;
	}

	internal static readonly Dictionary<Character, List<Assist>> Assists = new Dictionary<Character, List<Assist>>();

	internal static bool IsTamedAnimal(Character c)
	{
		return c != null && !c.IsPlayer() && c.IsTamed();
	}

	internal static void TryLevelUp(Character pet, float chance, string reason, string victim)
	{
		if (pet == null || pet.GetHealth() <= 0f)
		{
			return;
		}
		ZNetView component = pet.GetComponent<ZNetView>();
		if (component == null || !component.IsValid() || !component.IsOwner())
		{
			return;
		}
		int level = pet.GetLevel();
		if (level < 6 && Random.value < chance)
		{
			pet.SetLevel(level + 1);
			TameableCreaturesPlugin.Log.LogInfo($"¡{Utils.GetPrefabName(pet.gameObject)} subió a {level} estrella(s) por {reason} {victim}!");
		}
	}

	internal static void Prune()
	{
		if (Assists.Count <= 100)
		{
			return;
		}
		List<Character> list = new List<Character>();
		float num = TameableCreaturesPlugin.AssistWindowSeconds.Value * 2f;
		foreach (KeyValuePair<Character, List<Assist>> assist in Assists)
		{
			if (assist.Key == null || assist.Value.Count == 0 || Time.time - assist.Value[assist.Value.Count - 1].Time > num)
			{
				list.Add(assist.Key);
			}
		}
		foreach (Character item in list)
		{
			Assists.Remove(item);
		}
	}
}

// Registro de asistencias: cada golpe de un domesticado a un enemigo.
[HarmonyPatch(typeof(Character), "ApplyDamage")]
internal static class Patch_Character_ApplyDamage_Assists
{
	private static void Postfix(Character __instance, HitData hit)
	{
		if (__instance == null || hit == null || __instance.IsPlayer() || __instance.IsTamed())
		{
			return;
		}
		Character attacker = hit.GetAttacker();
		if (KillStars.IsTamedAnimal(attacker))
		{
			if (!KillStars.Assists.TryGetValue(__instance, out var value))
			{
				value = new List<KillStars.Assist>();
				KillStars.Assists[__instance] = value;
			}
			value.RemoveAll((KillStars.Assist a) => a.Pet == attacker);
			value.Add(new KillStars.Assist
			{
				Pet = attacker,
				Time = Time.time
			});
			KillStars.Prune();
		}
	}
}

// Al morir un enemigo: 2% para el que remató, 1% para cada asistente.
[HarmonyPatch(typeof(Character), "OnDeath")]
internal static class Patch_Character_OnDeath_KillStars
{
	private static void Postfix(Character __instance, HitData ___m_lastHit)
	{
		if (__instance == null || __instance.IsPlayer() || __instance.IsTamed())
		{
			return;
		}
		Character character = ___m_lastHit?.GetAttacker();
		string victim = Utils.GetPrefabName(__instance.gameObject);
		if (KillStars.Assists.TryGetValue(__instance, out var value))
		{
			float value2 = TameableCreaturesPlugin.AssistWindowSeconds.Value;
			foreach (KillStars.Assist item in value)
			{
				if (item.Pet != null && item.Pet != character && Time.time - item.Time <= value2)
				{
					KillStars.TryLevelUp(item.Pet, TameableCreaturesPlugin.AssistStarChance.Value, "asistir en la muerte de", victim);
				}
			}
			KillStars.Assists.Remove(__instance);
		}
		if (KillStars.IsTamedAnimal(character))
		{
			KillStars.TryLevelUp(character, TameableCreaturesPlugin.KillStarChance.Value, "matar a", victim);
		}
	}
}
