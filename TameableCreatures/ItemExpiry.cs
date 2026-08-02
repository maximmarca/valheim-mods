using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace TameableCreatures;

// v0.18.1 (pedido de Fer): los ítems tirados DENTRO de una base (zona de banco
// de trabajo) en vanilla no expiran nunca — con las granjas 24/7 (ActiveZones,
// forrajeo, siembra) se acumulan sin límite. Ahora expiran a los
// BaseItemExpiryDays días de juego, haya o no jugadores cerca. Fuera de base:
// vanilla intacto (3 días, frenado por cercanía de jugador).
[HarmonyPatch(typeof(ItemDrop), "TimedDestruction")]
internal static class Patch_ItemDrop_TimedDestruction_BaseExpiry
{
	private static MethodInfo s_timeSince;

	private static bool Prefix(ItemDrop __instance, ZNetView ___m_nview)
	{
		float days = TameableCreaturesPlugin.BaseItemExpiryDays.Value;
		if (days <= 0f)
		{
			return true;
		}
		Vector3 position = __instance.transform.position;
		if (!(position.y > 28f) || !EffectArea.IsPointInsideArea(position, EffectArea.Type.PlayerBase))
		{
			return true; // fuera de base: comportamiento vanilla
		}
		if (__instance.IsPiece() || ___m_nview == null || !___m_nview.IsValid())
		{
			return false;
		}
		if (s_timeSince == null)
		{
			s_timeSince = AccessTools.Method(typeof(ItemDrop), "GetTimeSinceSpawned");
		}
		if ((double)s_timeSince.Invoke(__instance, null) > (double)(days * TamedAging.DayLength()))
		{
			___m_nview.Destroy();
		}
		return false; // dentro de base lo manejamos acá
	}
}
