using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BuildTweaks;

[BepInPlugin("maxi.valheim.buildtweaks", "BuildTweaks", "0.1.0")]
public class BuildTweaksPlugin : BaseUnityPlugin
{
	internal static ManualLogSource Log;

	internal static ConfigEntry<string> NoStationCategories;

	private void Awake()
	{
		Log = base.Logger;
		NoStationCategories = base.Config.Bind("General", "NoStationCategories", "BuildingWorkbench", "Pestañas del martillo cuyas piezas NO requieren estación de crafteo (mesa/cantero) para construirse, separadas por coma. Valores: Misc, Crafting, BuildingWorkbench (pestaña Building), BuildingStonecutter (piedra), Furniture, Feasts, Food, Meads.");
		new Harmony("maxi.valheim.buildtweaks").PatchAll();
		Log.LogInfo("BuildTweaks 0.1.0 cargado");
	}
}

[HarmonyPatch(typeof(ZNetScene), "Awake")]
internal static class Patch_ZNetScene_Awake
{
	private static void Postfix(ZNetScene __instance)
	{
		HashSet<Piece.PieceCategory> hashSet = new HashSet<Piece.PieceCategory>();
		string[] array = BuildTweaksPlugin.NoStationCategories.Value.Split(',');
		foreach (string text in array)
		{
			string text2 = text.Trim();
			if (text2.Length != 0)
			{
				if (Enum.TryParse<Piece.PieceCategory>(text2, ignoreCase: true, out var result))
				{
					hashSet.Add(result);
				}
				else
				{
					BuildTweaksPlugin.Log.LogWarning("NoStationCategories: categoría desconocida '" + text2 + "'");
				}
			}
		}
		if (hashSet.Count == 0)
		{
			return;
		}
		int num = 0;
		foreach (GameObject prefab in __instance.m_prefabs)
		{
			if (!(prefab == null))
			{
				Piece component = prefab.GetComponent<Piece>();
				if (component != null && component.m_craftingStation != null && hashSet.Contains(component.m_category))
				{
					component.m_craftingStation = null;
					num++;
				}
			}
		}
		BuildTweaksPlugin.Log.LogInfo($"{num} piezas ya no requieren estación de crafteo ({BuildTweaksPlugin.NoStationCategories.Value})");
	}
}
