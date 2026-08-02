using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace TameableCreatures;

// v0.18.0: "guerra viva" (pedido de Maxi, opciones A+B).
// A) Los nidos/spawners destruidos RENACEN: al morir un objeto con SpawnArea
//    (nido de greydwarf, pila de huesos, spawner de campamento), el dueño de la
//    destrucción deja un marcador de red invisible con el prefab y la fecha;
//    pasados NestRespawnDays días de juego, quien simule la zona lo re-instancia
//    (si no hay jugadores a <25 m). El marcador es un prefab de red nuevo:
//    IGUAL QUE EL BAÚL, un cliente sin el mod ve "Missing prefab" — todos
//    deben estar al día.
// B) Presión creciente: la chance de estrellas de los spawns sube con la edad
//    del mundo (PressurePerDayPct por día, tope PressureMaxPct) — el mundo se
//    embravece con el tiempo. Multiplica sobre MoreStars, con techo del 60%.
[HarmonyPatch(typeof(ZNetScene), "Awake")]
internal static class Patch_ZNetScene_Awake_LivingWorld
{
	internal const string MarkerName = "tc_respawn_marker";

	private static GameObject s_root;

	private static GameObject s_marker;

	private static void Postfix(ZNetScene __instance)
	{
		if (!TameableCreaturesPlugin.NestRespawnEnabled.Value)
		{
			return;
		}
		if (s_marker == null)
		{
			s_root = new GameObject("TC_LivingWorldRoot");
			s_root.SetActive(value: false);
			UnityEngine.Object.DontDestroyOnLoad(s_root);
			s_marker = new GameObject(MarkerName);
			s_marker.transform.SetParent(s_root.transform);
			ZNetView zNetView = s_marker.AddComponent<ZNetView>();
			zNetView.m_persistent = true;
			s_marker.AddComponent<RespawnMarker>();
		}
		Dictionary<int, GameObject> dictionary = AccessTools.Field(typeof(ZNetScene), "m_namedPrefabs").GetValue(__instance) as Dictionary<int, GameObject>;
		int key = MarkerName.GetStableHashCode();
		if (dictionary != null && !dictionary.ContainsKey(key))
		{
			dictionary.Add(key, s_marker);
			__instance.m_prefabs.Add(s_marker);
			TameableCreaturesPlugin.Log.LogInfo("Guerra viva: marcador de renacimiento de nidos registrado");
		}
	}
}

// Al destruirse un nido (objeto con SpawnArea), el dueño planta el marcador.
[HarmonyPatch(typeof(Destructible), "Destroy")]
internal static class Patch_Destructible_Destroy_NestMarker
{
	private static void Prefix(Destructible __instance)
	{
		if (!TameableCreaturesPlugin.NestRespawnEnabled.Value || ZNetScene.instance == null || ZNet.instance == null)
		{
			return;
		}
		if (__instance.GetComponent<SpawnArea>() == null)
		{
			return;
		}
		ZNetView component = __instance.GetComponent<ZNetView>();
		if (component == null || !component.IsValid() || !component.IsOwner())
		{
			return;
		}
		GameObject prefab = ZNetScene.instance.GetPrefab(Patch_ZNetScene_Awake_LivingWorld.MarkerName);
		if (prefab == null)
		{
			return;
		}
		string text = Utils.GetPrefabName(__instance.gameObject);
		GameObject gameObject = UnityEngine.Object.Instantiate(prefab, __instance.transform.position, __instance.transform.rotation);
		ZNetView component2 = gameObject.GetComponent<ZNetView>();
		if (component2 != null && component2.IsValid())
		{
			component2.GetZDO().Set("tc_prefab", text);
			component2.GetZDO().Set("tc_destroyed", (float)ZNet.instance.GetTimeSeconds());
			TameableCreaturesPlugin.Log.LogInfo($"Nido destruido registrado para renacer en {TameableCreaturesPlugin.NestRespawnDays.Value:0} días: {text}");
		}
	}
}

// Marcador invisible: espera los días configurados y re-instancia el nido.
public class RespawnMarker : MonoBehaviour
{
	private ZNetView m_nview;

	private void Start()
	{
		m_nview = GetComponent<ZNetView>();
		InvokeRepeating("Check", UnityEngine.Random.Range(10f, 20f), 30f);
	}

	private void Check()
	{
		if (m_nview == null || !m_nview.IsValid() || !m_nview.IsOwner() || ZNet.instance == null || ZNetScene.instance == null)
		{
			return;
		}
		ZDO zdo = m_nview.GetZDO();
		string text = zdo.GetString("tc_prefab", "");
		float num = zdo.GetFloat("tc_destroyed", 0f);
		if (text.Length == 0 || num <= 0f)
		{
			ZNetScene.instance.Destroy(base.gameObject);
			return;
		}
		if (ZNet.instance.GetTimeSeconds() - (double)num < (double)(TameableCreaturesPlugin.NestRespawnDays.Value * TamedAging.DayLength()))
		{
			return;
		}
		foreach (Player allPlayer in Player.GetAllPlayers())
		{
			if (allPlayer != null && Vector3.Distance(allPlayer.transform.position, base.transform.position) < 25f)
			{
				return;
			}
		}
		GameObject prefab = ZNetScene.instance.GetPrefab(text);
		if (prefab != null)
		{
			UnityEngine.Object.Instantiate(prefab, base.transform.position, base.transform.rotation);
			TameableCreaturesPlugin.Log.LogInfo("Guerra viva: renació un " + text);
		}
		ZNetScene.instance.Destroy(base.gameObject);
	}
}

// Presión creciente: multiplica la chance de estrella según la edad del mundo.
[HarmonyPatch(typeof(SpawnSystem), "GetLevelUpChance", new Type[] { typeof(float) })]
internal static class Patch_SpawnSystem_GetLevelUpChance_Pressure
{
	private static void Postfix(ref float __result)
	{
		__result = LivingWorldPressure.Apply(__result);
	}
}

[HarmonyPatch(typeof(SpawnArea), "GetLevelUpChance")]
internal static class Patch_SpawnArea_GetLevelUpChance_Pressure
{
	private static void Postfix(ref float __result)
	{
		__result = LivingWorldPressure.Apply(__result);
	}
}

internal static class LivingWorldPressure
{
	internal static float Apply(float chancePercent)
	{
		if (!TameableCreaturesPlugin.PressureEnabled.Value || ZNet.instance == null)
		{
			return chancePercent;
		}
		float days = (float)(ZNet.instance.GetTimeSeconds() / (double)TamedAging.DayLength());
		float bonus = Mathf.Min(days * TameableCreaturesPlugin.PressurePerDayPct.Value, TameableCreaturesPlugin.PressureMaxPct.Value) / 100f;
		return Mathf.Min(chancePercent * (1f + bonus), 60f);
	}
}
