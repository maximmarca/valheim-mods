using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace TameableCreatures;

// v0.6.0: regeneración útil para TODOS los animales domesticados.
// Vanilla regenera maxHP/m_regenAllHPTime (3600 s) => en bichos de poca vida
// el tick es ~0.003 HP y el cartel muestra "+0"; y con hambre no regenera nada.
// Acá: fuera de combate curan RegenPercent% de la vida máxima por intervalo
// (mínimo 1 HP); con hambre, a mitad de ritmo. Los salvajes siguen vanilla.
[HarmonyPatch(typeof(BaseAI), "UpdateRegeneration")]
internal static class Patch_BaseAI_UpdateRegeneration
{
	private static bool Prefix(BaseAI __instance, float dt, ref float ___m_regenTimer, Tameable ___m_tamable, Character ___m_character, float ___m_timeSinceHurt)
	{
		if (___m_tamable == null || ___m_character == null || !___m_character.IsTamed())
		{
			return true;
		}
		___m_regenTimer += dt;
		if (___m_regenTimer <= TameableCreaturesPlugin.RegenIntervalSeconds.Value)
		{
			return false;
		}
		___m_regenTimer = 0f;
		if (__instance.IsAlerted() || ___m_timeSinceHurt < TameableCreaturesPlugin.RegenCombatCooldownSeconds.Value)
		{
			return false;
		}
		if (___m_character.GetHealthPercentage() >= 1f)
		{
			return false;
		}
		float num = ___m_character.GetMaxHealth() * (TameableCreaturesPlugin.RegenPercent.Value / 100f);
		if (___m_tamable.IsHungry())
		{
			num *= 0.5f;
		}
		___m_character.Heal(Mathf.Max(1f, num));
		return false;
	}
}

// Registro liviano de Pickables cargados (arbustos, cultivos) para el forrajeo.
[HarmonyPatch(typeof(Pickable), "Awake")]
internal static class Patch_Pickable_Awake
{
	private static void Postfix(Pickable __instance)
	{
		TamedForager.Pickables.Add(__instance);
	}
}

// Todo animal domesticable recibe el forrajeador al instanciarse.
[HarmonyPatch(typeof(Tameable), "Awake")]
internal static class Patch_Tameable_Awake
{
	private static void Postfix(Tameable __instance)
	{
		if (__instance.GetComponent<TamedForager>() == null)
		{
			__instance.gameObject.AddComponent<TamedForager>();
		}
		if (__instance.GetComponent<SeedPooper>() == null)
		{
			__instance.gameObject.AddComponent<SeedPooper>();
		}
	}
}

// v0.8.3: los ítems "fantasma" (ZDO con dueño de red que no responde) no se
// pueden comer ni levantar; los animales quedaban imantados mordiendo en vano.
// Si el objetivo de comida no se consume en ~30 s estando al lado, se lo marca
// fantasma y el buscador lo ignora 2 minutos.
[HarmonyPatch(typeof(MonsterAI), "FindClosestConsumableItem")]
internal static class Patch_MonsterAI_FindClosestConsumableItem
{
	private static void Postfix(ref ItemDrop __result)
	{
		if (__result == null)
		{
			return;
		}
		if (TamedForager.GhostItems.TryGetValue(__result, out var value))
		{
			if (Time.time - value < 120f)
			{
				__result = null;
			}
			else
			{
				TamedForager.GhostItems.Remove(__result);
			}
		}
	}
}

// v0.6.0: los domesticados con hambre "cosechan" el arbusto/cultivo más cercano
// cuyo fruto esté en su lista de comida (RPC_Pick sin jugador). Los ítems caen
// al piso y la IA vanilla de comer hace el resto (comer, curarse, criar).
public class TamedForager : MonoBehaviour
{
	internal static readonly List<Pickable> Pickables = new List<Pickable>();

	internal static readonly Dictionary<ItemDrop, float> GhostItems = new Dictionary<ItemDrop, float>();

	private static readonly AccessTools.FieldRef<MonsterAI, ItemDrop> s_consumeTarget = AccessTools.FieldRefAccess<MonsterAI, ItemDrop>("m_consumeTarget");

	private ZNetView m_nview;

	private Character m_character;

	private MonsterAI m_monsterAI;

	private Tameable m_tameable;

	private ItemDrop m_lastTarget;

	private int m_stuckTicks;

	private void Start()
	{
		m_nview = GetComponent<ZNetView>();
		m_character = GetComponent<Character>();
		m_monsterAI = GetComponent<MonsterAI>();
		m_tameable = GetComponent<Tameable>();
		if (m_monsterAI != null && m_tameable != null)
		{
			InvokeRepeating("ForageUpdate", Random.Range(5f, 10f), Mathf.Max(2f, TameableCreaturesPlugin.ForageIntervalSeconds.Value));
		}
	}

	private void ForageUpdate()
	{
		if (m_nview == null || !m_nview.IsValid() || !m_nview.IsOwner())
		{
			return;
		}
		CheckStuckTarget();
		if (!TameableCreaturesPlugin.ForageEnabled.Value)
		{
			return;
		}
		if (m_character == null || !m_character.IsTamed() || m_monsterAI == null || m_monsterAI.IsAlerted() || m_tameable == null || !m_tameable.IsHungry())
		{
			return;
		}
		float value = TameableCreaturesPlugin.ForageRange.Value;
		float num = value * value;
		Pickable pickable = null;
		ZNetView znetView = null;
		for (int num2 = Pickables.Count - 1; num2 >= 0; num2--)
		{
			Pickable pickable2 = Pickables[num2];
			if (pickable2 == null)
			{
				Pickables.RemoveAt(num2);
				continue;
			}
			if (!pickable2.isActiveAndEnabled)
			{
				continue;
			}
			float sqrMagnitude = (pickable2.transform.position - base.transform.position).sqrMagnitude;
			if (sqrMagnitude >= num)
			{
				continue;
			}
			ZNetView component = pickable2.GetComponent<ZNetView>();
			if (component == null || !component.IsValid() || pickable2.GetPicked() || !IsFoodForMe(pickable2))
			{
				continue;
			}
			num = sqrMagnitude;
			pickable = pickable2;
			znetView = component;
		}
		if (pickable != null)
		{
			znetView.InvokeRPC("RPC_Pick", 0);
		}
	}

	// v0.8.3: si lleva ~30 s al lado de su comida sin poder consumirla
	// (dueño de red que no responde), abandonarla y marcarla fantasma.
	private void CheckStuckTarget()
	{
		if (m_monsterAI == null)
		{
			return;
		}
		ItemDrop itemDrop = s_consumeTarget(m_monsterAI);
		if (itemDrop != null && itemDrop == m_lastTarget)
		{
			if (Vector3.Distance(itemDrop.transform.position, base.transform.position) < m_monsterAI.m_consumeRange + 1.5f)
			{
				m_stuckTicks++;
				if (m_stuckTicks >= 3)
				{
					GhostItems[itemDrop] = Time.time;
					s_consumeTarget(m_monsterAI) = null;
					m_stuckTicks = 0;
					TameableCreaturesPlugin.Log.LogWarning(Utils.GetPrefabName(base.gameObject) + " no pudo comer un ítem fantasma; lo ignora 2 min");
				}
			}
		}
		else
		{
			m_stuckTicks = 0;
		}
		m_lastTarget = itemDrop;
	}

	private bool IsFoodForMe(Pickable pickable)
	{
		if (pickable.m_itemPrefab == null || m_monsterAI.m_consumeItems == null)
		{
			return false;
		}
		ItemDrop component = pickable.m_itemPrefab.GetComponent<ItemDrop>();
		if (component == null)
		{
			return false;
		}
		string name = component.m_itemData.m_shared.m_name;
		foreach (ItemDrop consumeItem in m_monsterAI.m_consumeItems)
		{
			if (consumeItem != null && consumeItem.m_itemData.m_shared.m_name == name)
			{
				return true;
			}
		}
		return false;
	}
}
