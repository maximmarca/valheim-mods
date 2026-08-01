using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace TameableCreatures;

// v0.7.0: digestión y siembra. Cuando un animal come un ítem del mapa
// PoopMap, tras la digestión "hace caca" 1-3 semillas dispersas a su
// alrededor; cada semilla que cae sobre suelo cultivado (mismo chequeo
// Heightmap.IsCultivated que usan los cultivos) planta el prefab mapeado.
// Las que caen fuera de suelo cultivado se pierden.
[HarmonyPatch(typeof(Tameable), "OnConsumedItem")]
internal static class Patch_Tameable_OnConsumedItem
{
	private static void Postfix(Tameable __instance, ItemDrop item)
	{
		if (TameableCreaturesPlugin.PoopEnabled.Value && item != null)
		{
			SeedPooper component = __instance.GetComponent<SeedPooper>();
			if (component != null)
			{
				component.Schedule(Utils.GetPrefabName(item.gameObject));
			}
		}
	}
}

public class SeedPooper : MonoBehaviour
{
	private struct PendingPoop
	{
		public string ItemName;

		public float DueTime;
	}

	private ZNetView m_nview;

	private readonly List<PendingPoop> m_pending = new List<PendingPoop>();

	private void Start()
	{
		m_nview = GetComponent<ZNetView>();
		InvokeRepeating("PoopUpdate", 5f, 5f);
	}

	public void Schedule(string itemName)
	{
		float min = TameableCreaturesPlugin.DigestMinSeconds.Value;
		float max = Mathf.Max(min, TameableCreaturesPlugin.DigestMaxSeconds.Value);
		m_pending.Add(new PendingPoop
		{
			ItemName = itemName,
			DueTime = Time.time + Random.Range(min, max)
		});
	}

	private void PoopUpdate()
	{
		if (m_pending.Count == 0 || m_nview == null || !m_nview.IsValid() || !m_nview.IsOwner() || ZNetScene.instance == null)
		{
			return;
		}
		for (int num = m_pending.Count - 1; num >= 0; num--)
		{
			if (Time.time >= m_pending[num].DueTime)
			{
				string itemName = m_pending[num].ItemName;
				m_pending.RemoveAt(num);
				Poop(itemName);
			}
		}
	}

	private void Poop(string itemName)
	{
		string text = ResolvePlant(itemName);
		if (text == null)
		{
			return;
		}
		GameObject prefab = ZNetScene.instance.GetPrefab(text);
		if (prefab == null)
		{
			TameableCreaturesPlugin.Log.LogWarning("PoopMap: prefab '" + text + "' no existe, se ignora");
			return;
		}
		int num = Random.Range(TameableCreaturesPlugin.PoopSeedsMin.Value, TameableCreaturesPlugin.PoopSeedsMax.Value + 1);
		int num2 = 0;
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < 4; j++)
			{
				Vector2 vector = Random.insideUnitCircle * TameableCreaturesPlugin.PoopScatterRadius.Value;
				Vector3 vector2 = base.transform.position + new Vector3(vector.x, 0f, vector.y);
				if (ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(vector2, out var height))
				{
					vector2.y = height;
				}
				Heightmap heightmap = Heightmap.FindHeightmap(vector2);
				if (heightmap == null || !heightmap.IsCultivated(vector2) || SpotOccupied(vector2))
				{
					continue;
				}
				Quaternion quaternion = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
				Object.Instantiate(prefab, vector2, quaternion);
				Piece component = prefab.GetComponent<Piece>();
				if (component != null && component.m_placeEffect != null)
				{
					component.m_placeEffect.Create(vector2, quaternion);
				}
				num2++;
				break;
			}
		}
		if (num2 > 0)
		{
			TameableCreaturesPlugin.Log.LogInfo($"{Utils.GetPrefabName(base.gameObject)} sembró {num2}x {text} al hacer caca");
		}
	}

	private static bool SpotOccupied(Vector3 pos)
	{
		Collider[] array = Physics.OverlapSphere(pos + Vector3.up * 0.2f, 0.4f);
		foreach (Collider collider in array)
		{
			if (collider.GetComponentInParent<Plant>() != null || collider.GetComponentInParent<Pickable>() != null)
			{
				return true;
			}
		}
		return false;
	}

	private static string ResolvePlant(string itemName)
	{
		string[] array = TameableCreaturesPlugin.PoopMap.Value.Split(',');
		foreach (string text in array)
		{
			int num = text.IndexOf(':');
			if (num > 0 && text.Substring(0, num).Trim() == itemName)
			{
				return text.Substring(num + 1).Trim();
			}
		}
		return null;
	}
}
