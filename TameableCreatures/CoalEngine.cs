using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace TameableCreatures;

// v0.20.0 (spec de Fer): barcos a carbón. Si el barco tiene baulera (Container)
// con carbón, navega más rápido. Consumo según la velocidad del timón:
// remo (Slow) 1 carbón/5 min, media vela (Half) 1/2 min, vela llena (Full)
// 1/30 s. Sin carbón, velocidad normal. El boost lo aplica el dueño de la
// física del barco (server y todos los clientes tienen el mod). La balsa no
// tiene baulera, así que queda afuera sola.
// v0.20.1 (spec refinada de Fer): el boost y el consumo son POR SLOT de carbón
// con rendimiento decreciente: 1 slot +50%, 2 +80%, 3 +105%, 4 +120%, y +10%
// por cada slot extra (5→+130%, 6→+140%...). Cada intervalo consume 1 carbón
// de CADA slot.
[HarmonyPatch(typeof(ZNetScene), "Awake")]
internal static class Patch_ZNetScene_Awake_CoalEngine
{
	private static void Postfix(ZNetScene __instance)
	{
		if (!TameableCreaturesPlugin.CoalBoostEnabled.Value)
		{
			return;
		}
		int num = 0;
		foreach (GameObject prefab in __instance.m_prefabs)
		{
			if (!(prefab == null) && prefab.GetComponent<Ship>() != null && prefab.GetComponentInChildren<Container>(includeInactive: true) != null && prefab.GetComponent<CoalEngine>() == null)
			{
				prefab.AddComponent<CoalEngine>();
				num++;
			}
		}
		if (num > 0)
		{
			TameableCreaturesPlugin.Log.LogInfo($"Motor a carbón agregado a {num} barcos con baulera");
		}
	}
}

public class CoalEngine : MonoBehaviour
{
	internal static readonly Dictionary<Ship, CoalEngine> Engines = new Dictionary<Ship, CoalEngine>();

	private const string CoalName = "$item_coal";

	private Ship m_ship;

	private ZNetView m_nview;

	private Container m_container;

	private float m_baseRudderSpeed = -1f;

	private float m_burnTimer;

	internal bool Boosted;

	internal float Factor = 1f;

	// tabla de la spec: bonus total en % según slots con carbón
	internal static float TotalBonusPct(int slots)
	{
		switch (slots)
		{
		case 0:
			return 0f;
		case 1:
			return 50f;
		case 2:
			return 80f;
		case 3:
			return 105f;
		case 4:
			return 120f;
		default:
			return 120f + 10f * (float)(slots - 4);
		}
	}

	private void Awake()
	{
		m_ship = GetComponent<Ship>();
		m_nview = GetComponent<ZNetView>();
		m_container = GetComponentInChildren<Container>(includeInactive: true);
		if (m_ship != null)
		{
			Engines[m_ship] = this;
			m_baseRudderSpeed = m_ship.m_rudderSpeed;
		}
	}

	private void OnDestroy()
	{
		if (m_ship != null)
		{
			Engines.Remove(m_ship);
		}
	}

	private void Start()
	{
		InvokeRepeating("EngineUpdate", 1f, 1f);
	}

	private void EngineUpdate()
	{
		bool boosted = false;
		float factor = 1f;
		if (m_ship != null && m_nview != null && m_nview.IsValid() && m_nview.IsOwner() && m_container != null && TameableCreaturesPlugin.CoalBoostEnabled.Value)
		{
			float interval = 0f;
			switch (m_ship.GetSpeedSetting())
			{
			case Ship.Speed.Slow:
				interval = TameableCreaturesPlugin.CoalSecondsSlow.Value;
				break;
			case Ship.Speed.Half:
				interval = TameableCreaturesPlugin.CoalSecondsHalf.Value;
				break;
			case Ship.Speed.Full:
				interval = TameableCreaturesPlugin.CoalSecondsFull.Value;
				break;
			}
			Inventory inventory = m_container.GetInventory();
			if (interval > 0f && inventory != null)
			{
				// cada stack de carbón en la baulera es un "slot" de la spec
				List<ItemDrop.ItemData> coalStacks = new List<ItemDrop.ItemData>();
				foreach (ItemDrop.ItemData item in inventory.GetAllItems())
				{
					if (item != null && item.m_shared != null && item.m_shared.m_name == CoalName)
					{
						coalStacks.Add(item);
					}
				}
				if (coalStacks.Count > 0)
				{
					boosted = true;
					factor = 1f + TotalBonusPct(coalStacks.Count) / 100f * Mathf.Clamp(TameableCreaturesPlugin.CoalBonusMultiplier.Value, 0.1f, 3f);
					m_burnTimer += 1f;
					if (m_burnTimer >= interval)
					{
						m_burnTimer = 0f;
						// 1 carbón de CADA slot por intervalo
						foreach (ItemDrop.ItemData stack in coalStacks)
						{
							inventory.RemoveItem(stack, 1);
						}
					}
				}
				else
				{
					m_burnTimer = 0f;
				}
			}
		}
		Boosted = boosted;
		Factor = factor;
		// el remo (Slow) no usa la vela: el boost ahí va por la velocidad de remo
		if (m_ship != null && m_baseRudderSpeed > 0f)
		{
			m_ship.m_rudderSpeed = (boosted ? (m_baseRudderSpeed * factor) : m_baseRudderSpeed);
		}
	}
}

// Media vela y vela llena: la fuerza de la vela se multiplica con carbón.
[HarmonyPatch(typeof(Ship), "GetSailForce")]
internal static class Patch_Ship_GetSailForce_CoalBoost
{
	private static void Postfix(Ship __instance, ref Vector3 __result)
	{
		if (CoalEngine.Engines.TryGetValue(__instance, out var engine) && engine != null && engine.Boosted)
		{
			__result *= engine.Factor;
		}
	}
}
