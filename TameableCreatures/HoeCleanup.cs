using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace TameableCreatures;

// v0.17.0: pasar la AZADA limpia los hongos de la zona. Pedido de Maxi: los
// hongos sembrados se expanden y no había forma de frenarlos. Las operaciones
// de terreno de la azada (allanar, caminos, elevar) destruyen los Pickables de
// HoeCleanPrefabs dentro de su radio. El cultivador queda excluido (v0.17.1).
// Se limpian marcados y maduros por igual (al madurar pierden la marca
// tc_planted y quedan indistinguibles de los silvestres; en suelo cultivado
// son todos sembrados en la práctica).
[HarmonyPatch(typeof(TerrainOp), "Awake")]
internal static class Patch_TerrainOp_Awake_HoeCleanup
{
	private static void Postfix(TerrainOp __instance)
	{
		if (!TameableCreaturesPlugin.HoeCleanEnabled.Value || TerrainOp.m_forceDisableTerrainOps || ZNetScene.instance == null)
		{
			return;
		}
		// v0.17.1: el cultivador NO limpia (sus operaciones son cultivate/replant)
		// — cultivar es justamente preparar el suelo donde se quiere sembrar.
		string opName = Utils.GetPrefabName(__instance.gameObject).ToLowerInvariant();
		string[] excluded = TameableCreaturesPlugin.HoeCleanExcludedOps.Value.Split(',');
		foreach (string text0 in excluded)
		{
			string e = text0.Trim().ToLowerInvariant();
			if (e.Length > 0 && opName.Contains(e))
			{
				return;
			}
		}
		HashSet<string> targets = new HashSet<string>();
		string[] array = TameableCreaturesPlugin.HoeCleanPrefabs.Value.Split(',');
		foreach (string text in array)
		{
			string text2 = text.Trim();
			if (text2.Length > 0)
			{
				targets.Add(text2);
			}
		}
		if (targets.Count == 0)
		{
			return;
		}
		float radius = __instance.GetRadius() + 0.5f;
		HashSet<Pickable> found = new HashSet<Pickable>();
		Collider[] colliders = Physics.OverlapSphere(__instance.transform.position + Vector3.up * 0.2f, radius);
		foreach (Collider collider in colliders)
		{
			Pickable pickable = collider.GetComponentInParent<Pickable>();
			if (pickable != null && targets.Contains(Utils.GetPrefabName(pickable.gameObject)))
			{
				found.Add(pickable);
			}
		}
		int cleaned = 0;
		foreach (Pickable pickable in found)
		{
			ZNetView znv = pickable.GetComponent<ZNetView>();
			if (znv != null && znv.IsValid())
			{
				znv.ClaimOwnership();
				ZNetScene.instance.Destroy(pickable.gameObject);
				cleaned++;
			}
		}
		if (cleaned > 0)
		{
			TameableCreaturesPlugin.Log.LogInfo($"Azada: limpiados {cleaned} hongos de la zona");
		}
	}
}
