using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace TameableCreatures;

// v0.12.0: Baúl de basura — pieza nueva del martillo (clon del cofre de
// madera, tintado oscuro). Todo lo que se guarde adentro se destruye a los
// pocos segundos. El prefab se registra en runtime en ZNetScene de cada
// máquina: server y TODOS los clientes necesitan el mod para verlo.
public class TrashChest : MonoBehaviour
{
	private ZNetView m_nview;

	private Container m_container;

	private void Start()
	{
		m_nview = GetComponent<ZNetView>();
		m_container = GetComponent<Container>();
		InvokeRepeating("TrashUpdate", 3f, Mathf.Max(1f, TameableCreaturesPlugin.TrashDelaySeconds.Value));
	}

	private void TrashUpdate()
	{
		if (m_nview != null && m_nview.IsValid() && m_nview.IsOwner() && m_container != null)
		{
			Inventory inventory = m_container.GetInventory();
			if (inventory != null && inventory.NrOfItems() > 0)
			{
				inventory.RemoveAll();
			}
		}
	}
}

[HarmonyPatch(typeof(ZNetScene), "Awake")]
internal static class Patch_ZNetScene_Awake_TrashChest
{
	internal const string PrefabName = "piece_trashchest_tc";

	private static GameObject s_root;

	private static GameObject s_prefab;

	private static void Postfix(ZNetScene __instance)
	{
		if (!TameableCreaturesPlugin.TrashChestEnabled.Value)
		{
			return;
		}
		if (s_prefab == null)
		{
			GameObject prefab = __instance.GetPrefab("piece_chest_wood");
			if (prefab == null)
			{
				TameableCreaturesPlugin.Log.LogWarning("TrashChest: no se encontró piece_chest_wood");
				return;
			}
			// clon inactivo (el padre desactivado evita que corra Awake/ZNetView)
			s_root = new GameObject("TC_TrashChestRoot");
			s_root.SetActive(value: false);
			Object.DontDestroyOnLoad(s_root);
			s_prefab = Object.Instantiate(prefab, s_root.transform);
			s_prefab.name = PrefabName;
			Piece piece = s_prefab.GetComponent<Piece>();
			if (piece != null)
			{
				piece.m_name = "Baúl de basura";
				piece.m_description = "Lo que guardes acá adentro se destruye a los pocos segundos. Sin vuelta atrás.";
				piece.m_category = Piece.PieceCategory.Furniture;
			}
			Container container = s_prefab.GetComponent<Container>();
			if (container != null)
			{
				container.m_name = "Basura";
				container.m_width = 2;
				container.m_height = 1;
			}
			// tinte oscuro para distinguirlo del cofre común
			Renderer[] componentsInChildren = s_prefab.GetComponentsInChildren<Renderer>(includeInactive: true);
			foreach (Renderer renderer in componentsInChildren)
			{
				Material[] sharedMaterials = renderer.sharedMaterials;
				for (int j = 0; j < sharedMaterials.Length; j++)
				{
					if (sharedMaterials[j] != null && sharedMaterials[j].HasProperty("_Color"))
					{
						Material material = new Material(sharedMaterials[j]);
						material.color = material.color * new Color(0.45f, 0.4f, 0.4f);
						sharedMaterials[j] = material;
					}
				}
				renderer.sharedMaterials = sharedMaterials;
			}
			if (s_prefab.GetComponent<TrashChest>() == null)
			{
				s_prefab.AddComponent<TrashChest>();
			}
		}
		// registrar en el ZNetScene de esta partida
		Dictionary<int, GameObject> dictionary = AccessTools.Field(typeof(ZNetScene), "m_namedPrefabs").GetValue(__instance) as Dictionary<int, GameObject>;
		int key = PrefabName.GetStableHashCode();
		if (dictionary != null && !dictionary.ContainsKey(key))
		{
			dictionary.Add(key, s_prefab);
			__instance.m_prefabs.Add(s_prefab);
		}
		// agregar al martillo
		GameObject prefab2 = __instance.GetPrefab("Hammer");
		ItemDrop itemDrop = ((prefab2 != null) ? prefab2.GetComponent<ItemDrop>() : null);
		PieceTable pieceTable = itemDrop?.m_itemData?.m_shared?.m_buildPieces;
		if (pieceTable != null && !pieceTable.m_pieces.Contains(s_prefab))
		{
			pieceTable.m_pieces.Add(s_prefab);
			TameableCreaturesPlugin.Log.LogInfo("Baúl de basura agregado al martillo (pestaña Muebles)");
		}
	}
}
