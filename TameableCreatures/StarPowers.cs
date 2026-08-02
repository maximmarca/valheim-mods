using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace TameableCreatures;

// v0.14.0: habilidades especiales por estrellas (salvajes y domesticados).
// 3★ Ígneo: golpes con fuego (quemadura). 4★ Gélido: escarcha (frena).
// 5★ Tormenta: rayo + resistencia física pasiva. El daño elemental es un %
// del daño físico del golpe, encima del exponencial de la 0.13.0.
[HarmonyPatch(typeof(Character), "ApplyDamage")]
internal static class Patch_Character_ApplyDamage_StarPowers
{
	private static readonly Dictionary<int, GameObject> s_fxCache = new Dictionary<int, GameObject>();

	// v0.14.1: estallido elemental visible en el punto de impacto
	private static void SpawnHitFx(int stars, Vector3 point)
	{
		if (ZNetScene.instance == null)
		{
			return;
		}
		if (!s_fxCache.TryGetValue(stars, out var value))
		{
			value = null;
			string[] array = TameableCreaturesPlugin.StarHitFx.Value.Split(',');
			foreach (string text in array)
			{
				int num = text.IndexOf(':');
				if (num > 0 && text.Substring(0, num).Trim() == stars.ToString())
				{
					value = ZNetScene.instance.GetPrefab(text.Substring(num + 1).Trim());
					break;
				}
			}
			s_fxCache[stars] = value;
		}
		if (value != null)
		{
			Object.Instantiate(value, point, Quaternion.identity);
		}
	}

	private static void Prefix(Character __instance, HitData hit)
	{
		if (hit == null || __instance == null)
		{
			return;
		}
		// v0.16.0: lado ofensivo — especies con clase usan su habilidad;
		// las demás conservan el sistema por tier (3★ fuego / 4★ escarcha / 5★ rayo).
		Character attacker = hit.GetAttacker();
		if (attacker != null && !attacker.IsPlayer())
		{
			int num = attacker.GetLevel() - 1;
			int tier = StarClasses.TierIndex(num);
			if (tier >= 0)
			{
				string ability = StarClasses.AbilityFor(attacker);
				if (ability != null)
				{
					StarClasses.OnHit(attacker, __instance, hit, ability, tier);
				}
				else
				{
					float value = TameableCreaturesPlugin.StarElementalPercent.Value;
					float num2 = hit.m_damage.GetTotalPhysicalDamage() * value;
					if (value > 0f && num2 > 0f)
					{
						if (num == 3)
						{
							hit.m_damage.m_fire += num2;
						}
						else if (num == 4)
						{
							hit.m_damage.m_frost += num2;
						}
						else
						{
							hit.m_damage.m_lightning += num2;
						}
						SpawnHitFx(Mathf.Min(num, 5), hit.m_point);
					}
				}
			}
		}
		// lado defensivo — clase de la víctima; sin clase, la resist del 5★ de siempre
		if (!__instance.IsPlayer())
		{
			int tier2 = StarClasses.TierIndex(__instance.GetLevel() - 1);
			if (tier2 >= 0)
			{
				string ability2 = StarClasses.AbilityFor(__instance);
				if (ability2 != null)
				{
					StarClasses.OnDamaged(__instance, hit, ability2, tier2);
				}
				else
				{
					float value2 = TameableCreaturesPlugin.StarFiveResistPercent.Value;
					if (value2 > 0f && __instance.GetLevel() - 1 >= 5)
					{
						float num3 = 1f - Mathf.Clamp01(value2 / 100f);
						hit.m_damage.m_blunt *= num3;
						hit.m_damage.m_slash *= num3;
						hit.m_damage.m_pierce *= num3;
					}
				}
			}
		}
	}
}
