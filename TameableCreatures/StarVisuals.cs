using System.Collections;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace TameableCreatures;

// v0.10.0: visuales para 3-5 estrellas. Vanilla define aspecto (escala/tinte/
// objetos extra) solo para 1★ y 2★; de 3★ en adelante la criatura se veía
// común y el HUD no mostraba ninguna estrella. Acá: (a) se extrapola la
// progresión visual existente de cada especie (cada estrella más grande y de
// tinte más marcado; el ciervo conserva los cuernos), y (b) el HUD muestra
// "★N" junto al nombre para 3★+.
[HarmonyPatch(typeof(ZNetScene), "Awake")]
internal static class Patch_ZNetScene_Awake_StarVisuals
{
	private static void Postfix(ZNetScene __instance)
	{
		int num = Mathf.Clamp(TameableCreaturesPlugin.StarVisualsMaxStars.Value, 2, 10) + 1;
		int num2 = 0;
		foreach (GameObject prefab in __instance.m_prefabs)
		{
			if (prefab == null || prefab.GetComponent<Character>() == null)
			{
				continue;
			}
			LevelEffects componentInChildren = prefab.GetComponentInChildren<LevelEffects>(includeInactive: true);
			if (componentInChildren == null || componentInChildren.m_levelSetups == null || componentInChildren.m_levelSetups.Count != 2)
			{
				continue;
			}
			LevelEffects.LevelSetup levelSetup = componentInChildren.m_levelSetups[0];
			LevelEffects.LevelSetup levelSetup2 = componentInChildren.m_levelSetups[1];
			float num3 = levelSetup2.m_scale - levelSetup.m_scale;
			float num4 = levelSetup2.m_hue - levelSetup.m_hue;
			float num5 = levelSetup2.m_saturation - levelSetup.m_saturation;
			float num6 = levelSetup2.m_value - levelSetup.m_value;
			for (int i = 4; i <= num; i++)
			{
				int num7 = i - 3;
				componentInChildren.m_levelSetups.Add(new LevelEffects.LevelSetup
				{
					m_scale = Mathf.Min(levelSetup2.m_scale + num3 * (float)num7, levelSetup2.m_scale * 2f),
					m_hue = levelSetup2.m_hue + num4 * (float)num7,
					m_saturation = Mathf.Clamp(levelSetup2.m_saturation + num5 * (float)num7, -1f, 1f),
					m_value = Mathf.Clamp(levelSetup2.m_value + num6 * (float)num7, -1f, 1f),
					m_setEmissiveColor = levelSetup2.m_setEmissiveColor,
					m_emissiveColor = levelSetup2.m_emissiveColor,
					m_enableObject = levelSetup2.m_enableObject
				});
			}
			num2++;
		}
		if (num2 > 0)
		{
			TameableCreaturesPlugin.Log.LogInfo($"Visuales de estrellas extendidos hasta {num - 1}★ en {num2} criaturas");
		}
	}
}

// HUD: para 3★+ (nivel 4+) el marco vanilla no tiene íconos — se arma una
// fila con N íconos de estrella reales, clonando la estrellita vanilla y
// usando su mismo espaciado (v0.11.2; antes era texto "★N").
[HarmonyPatch(typeof(EnemyHud), "UpdateHuds")]
internal static class Patch_EnemyHud_UpdateHuds
{
	private static FieldInfo s_hudsField;

	private static FieldInfo s_level2Field;

	private static FieldInfo s_level3Field;

	private static void Postfix(EnemyHud __instance)
	{
		if (s_hudsField == null)
		{
			s_hudsField = AccessTools.Field(typeof(EnemyHud), "m_huds");
		}
		if (!(s_hudsField.GetValue(__instance) is IDictionary dictionary))
		{
			return;
		}
		foreach (DictionaryEntry item in dictionary)
		{
			Character character = item.Key as Character;
			if (character == null || item.Value == null)
			{
				continue;
			}
			if (s_level2Field == null)
			{
				s_level2Field = AccessTools.Field(item.Value.GetType(), "m_level2");
				s_level3Field = AccessTools.Field(item.Value.GetType(), "m_level3");
			}
			RectTransform level2 = s_level2Field?.GetValue(item.Value) as RectTransform;
			RectTransform level3 = s_level3Field?.GetValue(item.Value) as RectTransform;
			if (level2 == null || level3 == null)
			{
				continue;
			}
			int level = character.GetLevel();
			Transform parent = level3.parent;
			Transform container = parent.Find("tc_stars");
			if (level < 4)
			{
				if (container != null)
				{
					container.gameObject.SetActive(value: false);
				}
				continue;
			}
			if (container == null)
			{
				GameObject gameObject = new GameObject("tc_stars", typeof(RectTransform));
				container = gameObject.transform;
				container.SetParent(parent, worldPositionStays: false);
				RectTransform rectTransform = (RectTransform)container;
				rectTransform.anchorMin = level3.anchorMin;
				rectTransform.anchorMax = level3.anchorMax;
				rectTransform.pivot = level3.pivot;
				rectTransform.anchoredPosition = level3.anchoredPosition;
				rectTransform.localScale = level3.localScale;
			}
			int needed = level - 1;
			if (container.childCount != needed)
			{
				for (int num = container.childCount - 1; num >= 0; num--)
				{
					Object.Destroy(container.GetChild(num).gameObject);
				}
				GameObject template;
				float dx;
				float x0;
				if (level3.childCount >= 2 && level3.GetChild(0) is RectTransform star0 && level3.GetChild(1) is RectTransform star1)
				{
					template = star0.gameObject;
					dx = Mathf.Abs(star1.anchoredPosition.x - star0.anchoredPosition.x);
					x0 = star0.anchoredPosition.x;
					if (dx < 1f)
					{
						dx = 12f;
					}
				}
				else
				{
					template = level2.gameObject;
					dx = 14f;
					x0 = 0f;
				}
				for (int i = 0; i < needed; i++)
				{
					GameObject star = Object.Instantiate(template, container);
					star.SetActive(value: true);
					if (star.transform is RectTransform rt)
					{
						rt.anchoredPosition = new Vector2(x0 + dx * (float)i, rt.anchoredPosition.y);
					}
				}
			}
			container.gameObject.SetActive(value: true);
		}
	}
}
