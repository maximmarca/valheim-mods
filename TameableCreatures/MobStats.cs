using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace TameableCreatures;

// v0.19.0 (pedido de Fer, ok de Maxi): al apuntar a cualquier mob, línea de
// stats bajo el nombre: vida en números, daño estimado de su mejor ataque (con
// el multiplicador de estrellas aplicado) y resistencias/debilidades por tipo
// ("defensa" no existe en Valheim: son modificadores de daño). Jefes excluidos
// (su banner propio no tiene lugar). Solo visual, por cliente.
[HarmonyPatch(typeof(EnemyHud), "UpdateHuds")]
internal static class Patch_EnemyHud_UpdateHuds_MobStats
{
	private struct CacheEntry
	{
		public float Time;

		public string Text;
	}

	private static FieldInfo s_hudsField;

	private static FieldInfo s_nameField;

	private static readonly Dictionary<Character, float> s_damage = new Dictionary<Character, float>();

	private static readonly Dictionary<Character, CacheEntry> s_text = new Dictionary<Character, CacheEntry>();

	private static void Postfix(EnemyHud __instance)
	{
		if (!TameableCreaturesPlugin.MobStatsEnabled.Value)
		{
			return;
		}
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
			if (character == null || item.Value == null || character.IsPlayer() || character.IsBoss())
			{
				continue;
			}
			if (s_nameField == null)
			{
				s_nameField = AccessTools.Field(item.Value.GetType(), "m_name");
			}
			TextMeshProUGUI nameText = s_nameField?.GetValue(item.Value) as TextMeshProUGUI;
			if (nameText == null)
			{
				continue;
			}
			Transform parent = nameText.transform.parent;
			Transform statsTransform = parent.Find("tc_mobstats");
			TextMeshProUGUI statsText;
			if (statsTransform == null)
			{
				GameObject gameObject = Object.Instantiate(nameText.gameObject, parent);
				gameObject.name = "tc_mobstats";
				statsText = gameObject.GetComponent<TextMeshProUGUI>();
				statsText.fontSize = nameText.fontSize * 0.7f;
				RectTransform rectTransform = (RectTransform)gameObject.transform;
				RectTransform nameRect = (RectTransform)nameText.transform;
				rectTransform.anchoredPosition = nameRect.anchoredPosition + new Vector2(0f, 0f - nameText.fontSize * 1.15f);
			}
			else
			{
				statsText = statsTransform.GetComponent<TextMeshProUGUI>();
			}
			if (statsText != null)
			{
				statsText.text = GetText(character);
			}
		}
	}

	// El texto se rearma cada medio segundo por criatura (UpdateHuds corre por frame)
	private static string GetText(Character c)
	{
		if (s_text.TryGetValue(c, out var entry) && Time.time - entry.Time < 0.5f)
		{
			return entry.Text;
		}
		if (s_text.Count > 100)
		{
			s_text.Clear();
		}
		string text = BuildText(c);
		s_text[c] = new CacheEntry
		{
			Time = Time.time,
			Text = text
		};
		return text;
	}

	private static string BuildText(Character c)
	{
		string text = $"<color=#ff9d9d>♥{Mathf.CeilToInt(c.GetHealth())}/{Mathf.CeilToInt(c.GetMaxHealth())}</color>";
		float damage = EstimateDamage(c);
		if (damage > 0f)
		{
			text += $" <color=#ffd08a>⚔~{Mathf.RoundToInt(damage)}</color>";
		}
		string mods = DescribeModifiers(c.m_damageModifiers);
		if (mods.Length > 0)
		{
			text += mods;
		}
		return text;
	}

	// Mejor ataque entre los ítems por defecto del Humanoid, × factor de estrellas
	private static float EstimateDamage(Character c)
	{
		if (!s_damage.TryGetValue(c, out var baseDamage))
		{
			if (s_damage.Count > 100)
			{
				s_damage.Clear();
			}
			baseDamage = 0f;
			if (c is Humanoid humanoid)
			{
				baseDamage = Mathf.Max(BestItemDamage(humanoid.m_defaultItems), BestItemDamage(humanoid.m_randomWeapon));
			}
			s_damage[c] = baseDamage;
		}
		if (baseDamage <= 0f)
		{
			return 0f;
		}
		int stars = Mathf.Max(0, c.GetLevel() - 1);
		float perStar = TameableCreaturesPlugin.StarDamagePerStar.Value;
		float factor = ((stars >= 1 && perStar > 1f) ? Mathf.Pow(perStar, stars) : (1f + (float)stars * 0.5f));
		return baseDamage * factor;
	}

	private static float BestItemDamage(GameObject[] items)
	{
		float best = 0f;
		if (items == null)
		{
			return 0f;
		}
		foreach (GameObject item in items)
		{
			ItemDrop itemDrop = (item ? item.GetComponent<ItemDrop>() : null);
			if (itemDrop == null || itemDrop.m_itemData == null)
			{
				continue;
			}
			HitData.DamageTypes d = itemDrop.m_itemData.m_shared.m_damages;
			// solo daño que le pega a jugadores/criaturas (sin chop/pickaxe, que inflan a los trolls)
			float total = d.m_damage + d.m_blunt + d.m_slash + d.m_pierce + d.m_fire + d.m_frost + d.m_lightning + d.m_poison + d.m_spirit;
			if (total > best)
			{
				best = total;
			}
		}
		return best;
	}

	private static string DescribeModifiers(HitData.DamageModifiers m)
	{
		List<string> weak = new List<string>();
		List<string> resist = new List<string>();
		Classify(m.m_blunt, "contund", weak, resist);
		Classify(m.m_slash, "corte", weak, resist);
		Classify(m.m_pierce, "perf", weak, resist);
		Classify(m.m_fire, "fuego", weak, resist);
		Classify(m.m_frost, "escarcha", weak, resist);
		Classify(m.m_lightning, "rayo", weak, resist);
		Classify(m.m_poison, "veneno", weak, resist);
		Classify(m.m_spirit, "espíritu", weak, resist);
		string text = "";
		if (weak.Count > 0)
		{
			text += "\n<color=#a8e6a1>Débil: " + string.Join(", ", weak) + "</color>";
		}
		if (resist.Count > 0)
		{
			text += "\n<color=#9fb8c9>Resiste: " + string.Join(", ", resist) + "</color>";
		}
		return text;
	}

	private static void Classify(HitData.DamageModifier mod, string name, List<string> weak, List<string> resist)
	{
		switch (mod)
		{
		case HitData.DamageModifier.Weak:
		case HitData.DamageModifier.VeryWeak:
			weak.Add(name);
			break;
		case HitData.DamageModifier.Resistant:
		case HitData.DamageModifier.VeryResistant:
			resist.Add(name);
			break;
		case HitData.DamageModifier.Immune:
		case HitData.DamageModifier.Ignore:
			resist.Add(name + "✕");
			break;
		}
	}
}
