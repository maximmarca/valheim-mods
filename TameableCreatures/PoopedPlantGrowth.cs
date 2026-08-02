using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace TameableCreatures;

// v0.8.0: crecimiento por etapas de lo sembrado por caca (grupos 1 y 2).
// El componente se agrega a los prefabs de StagedPlants, pero solo actúa si
// el ZDO tiene la marca tc_planted (lo sembró un animal) — los arbustos y
// hongos silvestres quedan intactos.
//
// Ciclo: nace al 30% de tamaño, con tinte verdoso y sin fruto → 50% → al
// cumplirse StagedGrowSeconds llega al 100%, recupera color, aparece la
// primera fruta y sigue el ciclo vanilla de respawn. Los hongos, al madurar,
// se multiplican (+1 con 60%, +2 con 40%) hasta un tope de 7 en 4 m.
[HarmonyPatch(typeof(ZNetScene), "Awake")]
internal static class Patch_ZNetScene_Awake_StagedPlants
{
	private static void Postfix(ZNetScene __instance)
	{
		string[] array = TameableCreaturesPlugin.StagedPlants.Value.Split(',');
		foreach (string text in array)
		{
			string text2 = text.Trim();
			if (text2.Length == 0)
			{
				continue;
			}
			GameObject prefab = __instance.GetPrefab(text2);
			if (prefab == null)
			{
				TameableCreaturesPlugin.Log.LogWarning("StagedPlants: prefab '" + text2 + "' no existe, se ignora");
			}
			else if (prefab.GetComponent<PoopedPlantGrowth>() == null)
			{
				prefab.AddComponent<PoopedPlantGrowth>();
			}
		}
	}
}

public class PoopedPlantGrowth : MonoBehaviour
{
	private struct TintEntry
	{
		public Material Mat;

		public Color Col;
	}

	internal const string ZdoKey = "tc_planted";

	private static int s_plantedHash;

	private ZNetView m_nview;

	private Pickable m_pickable;

	private Vector3 m_baseScale;

	private float m_appliedPct = -1f;

	private bool m_isMushroom;

	private readonly List<TintEntry> m_tinted = new List<TintEntry>();

	internal static int PlantedHash
	{
		get
		{
			if (s_plantedHash == 0)
			{
				s_plantedHash = ZdoKey.GetStableHashCode();
			}
			return s_plantedHash;
		}
	}

	private void Start()
	{
		m_nview = GetComponent<ZNetView>();
		m_pickable = GetComponent<Pickable>();
		m_baseScale = base.transform.localScale;
		string text = Utils.GetPrefabName(base.gameObject);
		string[] array = TameableCreaturesPlugin.MushroomPrefabs.Value.Split(',');
		foreach (string text2 in array)
		{
			if (text2.Trim() == text)
			{
				m_isMushroom = true;
				break;
			}
		}
		InvokeRepeating("GrowUpdate", Random.Range(2f, 5f), 5f);
	}

	private void GrowUpdate()
	{
		if (m_nview == null || !m_nview.IsValid() || ZNet.instance == null)
		{
			return;
		}
		float num = m_nview.GetZDO().GetFloat(PlantedHash, 0f);
		if (num <= 0f)
		{
			if (m_appliedPct > 0f)
			{
				RestoreVisuals();
			}
			return;
		}
		double num2 = ZNet.instance.GetTimeSeconds() - (double)num;
		float value = TameableCreaturesPlugin.StagedGrowSeconds.Value;
		if (num2 < (double)value)
		{
			float pct = ((num2 < (double)(value * 0.5f)) ? TameableCreaturesPlugin.StagedStartScale.Value : TameableCreaturesPlugin.StagedMidScale.Value);
			ApplyVisuals(pct);
			// sin fruto mientras crece (el dueño lo garantiza por si el respawn vanilla se adelanta)
			if (m_nview.IsOwner() && m_pickable != null && !m_pickable.GetPicked())
			{
				m_nview.GetZDO().Set(ZDOVars.s_pickedTime, ZNet.instance.GetTime().Ticks);
				m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetPicked", true);
			}
			// v0.8.2: en el hongo, "cosechado" oculta el modelo entero — forzarlo
			// visible mientras crece (no interactuable igual: m_picked lo bloquea).
			if (m_pickable != null && m_pickable.m_hideWhenPicked != null && !m_pickable.m_hideWhenPicked.activeSelf)
			{
				m_pickable.m_hideWhenPicked.SetActive(value: true);
			}
		}
		else
		{
			if (m_nview.IsOwner())
			{
				Mature();
			}
			RestoreVisuals();
		}
	}

	private void Mature()
	{
		m_nview.GetZDO().Set(PlantedHash, 0f);
		if (m_pickable != null)
		{
			m_nview.GetZDO().Set(ZDOVars.s_pickedTime, ZNet.instance.GetTime().Ticks);
			m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetPicked", false);
		}
		string text = Utils.GetPrefabName(base.gameObject);
		TryMultiply(text);
		TameableCreaturesPlugin.Log.LogInfo(text + " sembrado llegó al 100%: primera fruta disponible");
	}

	// Grupo 2: los hongos maduros se multiplican hasta el tope del manchón.
	private void TryMultiply(string prefabName)
	{
		bool flag = false;
		string[] array = TameableCreaturesPlugin.MushroomPrefabs.Value.Split(',');
		foreach (string text in array)
		{
			if (text.Trim() == prefabName)
			{
				flag = true;
				break;
			}
		}
		if (!flag || ZNetScene.instance == null)
		{
			return;
		}
		float value = TameableCreaturesPlugin.MushroomPatchRadius.Value;
		int num = CountSameNearby(prefabName, value);
		int num2 = TameableCreaturesPlugin.MushroomMaxInPatch.Value - num;
		if (num2 <= 0)
		{
			return;
		}
		int num3 = ((Random.value < TameableCreaturesPlugin.MushroomChanceOne.Value) ? 1 : 2);
		num3 = Mathf.Min(num3, num2);
		GameObject prefab = ZNetScene.instance.GetPrefab(prefabName);
		if (prefab == null)
		{
			return;
		}
		int num4 = 0;
		for (int i = 0; i < num3; i++)
		{
			for (int j = 0; j < 6; j++)
			{
				Vector2 vector = Random.insideUnitCircle * value;
				Vector3 vector2 = base.transform.position + new Vector3(vector.x, 0f, vector.y);
				if (ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(vector2, out var height))
				{
					vector2.y = height;
				}
				Heightmap heightmap = Heightmap.FindHeightmap(vector2);
				if (heightmap == null || !heightmap.IsCultivated(vector2) || SeedPooper.SpotOccupied(vector2))
				{
					continue;
				}
				GameObject go = Object.Instantiate(prefab, vector2, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
				SeedPooper.MarkPlanted(go);
				num4++;
				break;
			}
		}
		if (num4 > 0)
		{
			TameableCreaturesPlugin.Log.LogInfo($"{prefabName} se multiplicó: +{num4} (manchón {num + num4}/{TameableCreaturesPlugin.MushroomMaxInPatch.Value})");
		}
	}

	private int CountSameNearby(string prefabName, float radius)
	{
		HashSet<GameObject> hashSet = new HashSet<GameObject>();
		Collider[] array = Physics.OverlapSphere(base.transform.position + Vector3.up * 0.2f, radius);
		foreach (Collider collider in array)
		{
			Pickable componentInParent = collider.GetComponentInParent<Pickable>();
			if (componentInParent != null && Utils.GetPrefabName(componentInParent.gameObject) == prefabName)
			{
				hashSet.Add(componentInParent.gameObject);
			}
		}
		return hashSet.Count;
	}

	private void ApplyVisuals(float pct)
	{
		if (Mathf.Approximately(pct, m_appliedPct))
		{
			return;
		}
		if (m_appliedPct < 0f)
		{
			CaptureTints();
		}
		base.transform.localScale = m_baseScale * pct;
		m_appliedPct = pct;
	}

	private void CaptureTints()
	{
		// arbustos: tinte verdoso (multiplicativo); hongos: blanco pleno
		// (v0.15.1; el Lerp 0.85 de la 0.8.2 les dejaba un resto rosado)
		Color color = new Color(0.72f, 1f, 0.68f);
		Renderer[] componentsInChildren = GetComponentsInChildren<Renderer>(includeInactive: true);
		foreach (Renderer renderer in componentsInChildren)
		{
			Material[] materials = renderer.materials;
			foreach (Material material in materials)
			{
				if (material != null && material.HasProperty("_Color"))
				{
					m_tinted.Add(new TintEntry
					{
						Mat = material,
						Col = material.color
					});
					material.color = (m_isMushroom ? new Color(1f, 1f, 1f, material.color.a) : (material.color * color));
				}
			}
		}
	}

	private void RestoreVisuals()
	{
		base.transform.localScale = m_baseScale;
		foreach (TintEntry tintEntry in m_tinted)
		{
			if (!(tintEntry.Mat == null))
			{
				tintEntry.Mat.color = tintEntry.Col;
			}
		}
		m_tinted.Clear();
		m_appliedPct = -1f;
	}
}
