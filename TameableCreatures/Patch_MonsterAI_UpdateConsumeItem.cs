using System;
using HarmonyLib;

namespace TameableCreatures;

// El ciervo no es Humanoid (único comedor no-Humanoid del juego): la línea
// vanilla humanoid.m_consumeItemEffects.Create(...) lanza NullReference justo
// DESPUÉS de comer (RemoveOne + OnConsumedItem) y ANTES de limpiar
// m_consumeTarget. La 0.4.0 tragaba la excepción dejando el target trabado, y
// como esa rama no chequea hambre, el ciervo vaciaba el stack entero mordida
// tras mordida (encadenando digestiones). v0.8.1: al tragar la excepción se
// limpia el target — come exactamente 1 ítem por ciclo de hambre, como el
// chancho.
[HarmonyPatch(typeof(MonsterAI), "UpdateConsumeItem")]
internal static class Patch_MonsterAI_UpdateConsumeItem
{
	private static Exception Finalizer(Exception __exception, Character ___m_character, ref ItemDrop ___m_consumeTarget)
	{
		if (__exception is NullReferenceException && !(___m_character is Humanoid))
		{
			___m_consumeTarget = null;
			return null;
		}
		return __exception;
	}
}
