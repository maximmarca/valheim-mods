using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace TameableCreatures;

// v0.18.0: muerte por edad de los DOMESTICADOS (pedido de Maxi, opción B
// "vejez visible"). Cada animal domesticado marca su nacimiento (ZDO tc_birth;
// las crías heredan el tc_babyBorn real). Vida configurable en días de juego,
// +bonus por estrella. Al 80% de la vida entra en etapa "anciano": tinte gris,
// 50% más lento y deja de criar (lovePoints a cero). Al 100%, muere en paz:
// sin drops y con aviso. Los animales existentes al instalar arrancan a
// envejecer desde ese momento (vida completa por delante).
[HarmonyPatch(typeof(ZNetScene), "Awake")]
internal static class Patch_ZNetScene_Awake_Aging
{
	private static void Postfix(ZNetScene __instance)
	{
		if (!TameableCreaturesPlugin.AgingEnabled.Value)
		{
			return;
		}
		int num = 0;
		foreach (GameObject prefab in __instance.m_prefabs)
		{
			if (!(prefab == null) && prefab.GetComponent<Character>() != null && prefab.GetComponent<Player>() == null && prefab.GetComponent<TamedAging>() == null)
			{
				prefab.AddComponent<TamedAging>();
				num++;
			}
		}
		if (num > 0)
		{
			TameableCreaturesPlugin.Log.LogInfo($"Envejecimiento activo: {num} especies pueden envejecer (solo domesticadas)");
		}
	}
}

public class TamedAging : MonoBehaviour
{
	internal const string ZdoKey = "tc_birth";

	internal static bool DyingOfAge;

	private static int s_birthHash;

	private ZNetView m_nview;

	private Character m_character;

	private bool m_oldApplied;

	internal static int BirthHash
	{
		get
		{
			if (s_birthHash == 0)
			{
				s_birthHash = ZdoKey.GetStableHashCode();
			}
			return s_birthHash;
		}
	}

	internal static float DayLength()
	{
		if (!(EnvMan.instance != null))
		{
			return 1200f;
		}
		return EnvMan.instance.m_dayLengthSec;
	}

	private void Start()
	{
		m_nview = GetComponent<ZNetView>();
		m_character = GetComponent<Character>();
		InvokeRepeating("AgeUpdate", Random.Range(5f, 9f), 5f);
	}

	private void AgeUpdate()
	{
		if (!TameableCreaturesPlugin.AgingEnabled.Value || m_nview == null || !m_nview.IsValid() || ZNet.instance == null || m_character == null || m_character.IsDead() || !m_character.IsTamed())
		{
			return;
		}
		ZDO zdo = m_nview.GetZDO();
		double now = ZNet.instance.GetTimeSeconds();
		float birth = zdo.GetFloat(BirthHash, 0f);
		if (birth <= 0f)
		{
			// primera vez que se lo ve domesticado: nace el reloj (las crías
			// heredan su nacimiento real de BabyGrowth)
			if (m_nview.IsOwner())
			{
				float baby = zdo.GetFloat(BabyGrowth.ZdoKey.GetStableHashCode(), 0f);
				zdo.Set(BirthHash, (baby > 0f) ? baby : ((float)now));
			}
			return;
		}
		int stars = Mathf.Max(0, m_character.GetLevel() - 1);
		float lifespan = TameableCreaturesPlugin.AgingLifespanDays.Value * DayLength() * (1f + (float)stars * TameableCreaturesPlugin.AgingStarBonusPct.Value / 100f);
		if (lifespan <= 0f)
		{
			return;
		}
		float frac = (float)((now - (double)birth) / (double)lifespan);
		if (frac >= 1f)
		{
			if (m_nview.IsOwner())
			{
				DieOfAge();
			}
		}
		else if (frac >= TameableCreaturesPlugin.AgingOldPct.Value / 100f)
		{
			ApplyOldVisual();
			if (m_nview.IsOwner())
			{
				float slow = Mathf.Clamp(TameableCreaturesPlugin.AgingOldSpeedPct.Value / 100f, 0f, 0.9f);
				if (m_character.GetSEMan().GetStatusEffect("Anciano".GetStableHashCode()) == null)
				{
					m_character.GetSEMan().AddStatusEffect(StarClasses.MakeStats("Anciano", 12f, 0f - slow), resetTime: true);
				}
				// los ancianos no crían
				zdo.Set("lovePoints".GetStableHashCode(), 0);
			}
		}
	}

	private void DieOfAge()
	{
		string name = m_character.m_name;
		DyingOfAge = true;
		try
		{
			HitData hitData = new HitData();
			hitData.m_damage.m_damage = m_character.GetMaxHealth() * 10f;
			hitData.m_point = m_character.GetCenterPoint();
			m_character.ApplyDamage(hitData, showDamageText: false, triggerEffects: true);
		}
		finally
		{
			DyingOfAge = false;
		}
		if (Player.m_localPlayer != null)
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, name + " murió de viejo");
		}
		TameableCreaturesPlugin.Log.LogInfo(name + " murió de viejo");
	}

	// Tinte gris de anciano, por cliente (cada cliente lo aplica leyendo el ZDO)
	private void ApplyOldVisual()
	{
		if (m_oldApplied)
		{
			return;
		}
		m_oldApplied = true;
		Renderer[] componentsInChildren = GetComponentsInChildren<Renderer>(includeInactive: true);
		foreach (Renderer renderer in componentsInChildren)
		{
			if (!(renderer is SkinnedMeshRenderer) && !(renderer is MeshRenderer))
			{
				continue;
			}
			Material[] materials = renderer.materials;
			foreach (Material material in materials)
			{
				if (material == null)
				{
					continue;
				}
				if (material.HasProperty("_Saturation") && material.HasProperty("_Value"))
				{
					material.SetFloat("_Saturation", material.GetFloat("_Saturation") - 0.55f);
					material.SetFloat("_Value", material.GetFloat("_Value") - 0.1f);
				}
				else if (material.HasProperty("_Color"))
				{
					Color color = material.color;
					float gray = color.grayscale;
					material.color = Color.Lerp(color, new Color(gray, gray, gray, color.a), 0.6f);
				}
			}
		}
	}
}

// v0.18.1 (pregunta de Fer "¿dónde veo la edad?"): la edad aparece al apuntar
// al animal domesticado, junto con su esperanza de vida y la marca de anciano.
[HarmonyPatch(typeof(Tameable), "GetHoverText")]
internal static class Patch_Tameable_GetHoverText_Age
{
	private static void Postfix(Tameable __instance, ref string __result)
	{
		if (!TameableCreaturesPlugin.AgingEnabled.Value || ZNet.instance == null)
		{
			return;
		}
		Character component = __instance.GetComponent<Character>();
		ZNetView component2 = __instance.GetComponent<ZNetView>();
		if (component == null || component2 == null || !component2.IsValid() || !component.IsTamed())
		{
			return;
		}
		float birth = component2.GetZDO().GetFloat(TamedAging.BirthHash, 0f);
		if (birth <= 0f)
		{
			return;
		}
		int stars = Mathf.Max(0, component.GetLevel() - 1);
		float day = TamedAging.DayLength();
		float lifespan = TameableCreaturesPlugin.AgingLifespanDays.Value * day * (1f + (float)stars * TameableCreaturesPlugin.AgingStarBonusPct.Value / 100f);
		if (lifespan <= 0f)
		{
			return;
		}
		float aged = (float)(ZNet.instance.GetTimeSeconds() - (double)birth);
		bool old = aged / lifespan >= TameableCreaturesPlugin.AgingOldPct.Value / 100f;
		__result += string.Format("\n<color=grey>Edad: {0:0.#}/{1:0.#} días{2}</color>", aged / day, lifespan / day, old ? " (anciano)" : "");
	}
}

// Muerte por vejez sin drops: no deja carne/cuero — se fue en paz.
[HarmonyPatch(typeof(CharacterDrop), "GenerateDropList")]
internal static class Patch_CharacterDrop_GenerateDropList_Aging
{
	private static bool Prefix(ref List<KeyValuePair<GameObject, int>> __result)
	{
		if (TamedAging.DyingOfAge && TameableCreaturesPlugin.AgingNoDrops.Value)
		{
			__result = new List<KeyValuePair<GameObject, int>>();
			return false;
		}
		return true;
	}
}
