using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace TameableCreatures;

[BepInPlugin("fer.valheim.tameablecreatures", "TameableCreatures", "0.6.0")]
public class TameableCreaturesPlugin : BaseUnityPlugin
{
	public const string PluginGuid = "fer.valheim.tameablecreatures";

	public const string PluginName = "TameableCreatures";

	public const string PluginVersion = "0.6.0";

	internal static ManualLogSource Log;

	internal static ConfigEntry<string> Creatures;

	internal static ConfigEntry<float> TamingTimeSeconds;

	internal static ConfigEntry<string> FoodItems;

	internal static ConfigEntry<bool> EnableBreeding;

	internal static ConfigEntry<string> MakeCommandable;

	internal static ConfigEntry<float> FleeSeconds;

	internal static ConfigEntry<float> ScareRange;

	internal static ConfigEntry<int> MaxCreaturesNearby;

	internal static ConfigEntry<float> MutationUpChance;

	internal static ConfigEntry<float> MutationDownChance;

	internal static ConfigEntry<bool> BabyEnabled;

	internal static ConfigEntry<float> BabyGrowMinutes;

	internal static ConfigEntry<float> BabyScale;

	internal static ConfigEntry<float> BabySaturation;

	internal static ConfigEntry<float> BabyBrightness;

	internal static ConfigEntry<float> RegenPercent;

	internal static ConfigEntry<float> RegenIntervalSeconds;

	internal static ConfigEntry<float> RegenCombatCooldownSeconds;

	internal static ConfigEntry<bool> ForageEnabled;

	internal static ConfigEntry<float> ForageRange;

	internal static ConfigEntry<float> ForageIntervalSeconds;

	internal static ConfigEntry<float> FedDurationSeconds;

	private void Awake()
	{
		Log = base.Logger;
		Creatures = base.Config.Bind("General", "Creatures", "Deer,Neck", "Criaturas a volver adiestrables, por nombre de prefab separadas por coma.");
		TamingTimeSeconds = base.Config.Bind("General", "TamingTimeSeconds", 1800f, "Segundos de convivencia (alimentada y tranquila) para domesticar una criatura. 1800 = igual que el chancho.");
		FoodItems = base.Config.Bind("General", "FoodItems", "", "Comidas que aceptan, por nombre de prefab separadas por coma (ej: Raspberry,Blueberries,Carrot). Vacío = las mismas que el chancho.");
		EnableBreeding = base.Config.Bind("General", "EnableBreeding", defaultValue: true, "Las criaturas domesticadas pueden criar (como los chanchos). Las crías nacen mansas.");
		MakeCommandable = base.Config.Bind("General", "MakeCommandable", "Deer,Neck,Boar", "Criaturas domesticadas que obedecen el comando de seguir/quedarse (E), como los lobos. Por nombre de prefab, separadas por coma.");
		FleeSeconds = base.Config.Bind("Comportamiento", "FleeSeconds", 5f, "Cuántos segundos corre una criatura no-Humanoid salvaje (ciervo) al asustarse.");
		ScareRange = base.Config.Bind("Comportamiento", "ScareRange", 12f, "A cuántos metros de un jugador se asusta una criatura no-Humanoid salvaje (0 = solo se asusta al ser golpeada).");
		MaxCreaturesNearby = base.Config.Bind("Cria", "MaxCreaturesNearby", 20, new ConfigDescription("Cuántos animales de la misma especie puede haber en el radio de chequeo (~10 m) antes de que dejen de criar. Vanilla ~5. 0 = no tocar.", new AcceptableValueRange<int>(0, 100)));
		MutationUpChance = base.Config.Bind("Cria", "MutationUpChance", 0.02f, new ConfigDescription("Probabilidad de que la cría nazca con 1 estrella MÁS que el mejor de sus padres.", new AcceptableValueRange<float>(0f, 1f)));
		MutationDownChance = base.Config.Bind("Cria", "MutationDownChance", 0.01f, new ConfigDescription("Probabilidad de que la cría nazca con 1 estrella MENOS que el peor de sus padres.", new AcceptableValueRange<float>(0f, 1f)));
		BabyEnabled = base.Config.Bind("Cria", "BabyEnabled", defaultValue: true, "Las crías nacen bebés: tamaño reducido y color pastel, crecen solas con el tiempo. Requiere el mod actualizado en server y en todos los clientes.");
		BabyGrowMinutes = base.Config.Bind("Cria", "BabyGrowMinutes", 50f, new ConfigDescription("Minutos reales que tarda un bebé en crecer (vanilla: el lechón tarda 50).", new AcceptableValueRange<float>(1f, 600f)));
		BabyScale = base.Config.Bind("Cria", "BabyScale", 0.5f, new ConfigDescription("Tamaño del bebé respecto del adulto.", new AcceptableValueRange<float>(0.2f, 1f)));
		BabySaturation = base.Config.Bind("Cria", "BabySaturation", -0.35f, new ConfigDescription("Corrimiento de saturación del color del bebé (negativo = más pastel).", new AcceptableValueRange<float>(-1f, 1f)));
		BabyBrightness = base.Config.Bind("Cria", "BabyBrightness", 0.25f, new ConfigDescription("Corrimiento de brillo del color del bebé (positivo = más claro).", new AcceptableValueRange<float>(-1f, 1f)));
		RegenPercent = base.Config.Bind("Cuidado", "RegenPercent", 5f, new ConfigDescription("Porcentaje de la vida máxima que recupera un animal domesticado por intervalo, fuera de combate (mínimo 1 HP). Con hambre cura la mitad. Vanilla: vida completa en 1 hora y nada con hambre.", new AcceptableValueRange<float>(0f, 100f)));
		RegenIntervalSeconds = base.Config.Bind("Cuidado", "RegenIntervalSeconds", 10f, new ConfigDescription("Cada cuántos segundos cura el tick de regeneración.", new AcceptableValueRange<float>(2f, 120f)));
		RegenCombatCooldownSeconds = base.Config.Bind("Cuidado", "RegenCombatCooldownSeconds", 10f, new ConfigDescription("Segundos sin recibir daño (y sin estar alertado) para considerarse fuera de combate.", new AcceptableValueRange<float>(0f, 120f)));
		ForageEnabled = base.Config.Bind("Cuidado", "ForageEnabled", defaultValue: true, "Los domesticados con hambre cosechan solos arbustos y cultivos cuyo fruto esté en su lista de comida; los ítems caen al piso y se los comen.");
		ForageRange = base.Config.Bind("Cuidado", "ForageRange", 10f, new ConfigDescription("Radio en metros en el que buscan arbustos/cultivos para comer.", new AcceptableValueRange<float>(1f, 30f)));
		ForageIntervalSeconds = base.Config.Bind("Cuidado", "ForageIntervalSeconds", 10f, new ConfigDescription("Cada cuántos segundos revisan si hay algo para cosechar (solo con hambre).", new AcceptableValueRange<float>(2f, 120f)));
		FedDurationSeconds = base.Config.Bind("Cuidado", "FedDurationSeconds", 0f, new ConfigDescription("Segundos que las criaturas de la lista (ciervo/neck) quedan saciadas tras comer 1 ítem. Comen 1 ítem por ciclo de hambre. 0 = igual que el chancho (600 s, 10 min).", new AcceptableValueRange<float>(0f, 7200f)));
		new Harmony("fer.valheim.tameablecreatures").PatchAll();
		Log.LogInfo("TameableCreatures 0.6.0 cargado (voces propias + crías bebé pastel + regen + forrajeo)");
	}

	internal static void CopyPublicFields<T>(T source, T target) where T : Component
	{
		FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public);
		foreach (FieldInfo fieldInfo in fields)
		{
			fieldInfo.SetValue(target, fieldInfo.GetValue(source));
		}
	}

	// Copia una EffectList quitando los prefabs de sonido (sfx*) heredados del Boar
	// y agregando, si se pasa, la voz propia de la especie.
	internal static EffectList ReplaceSfx(EffectList source, EffectList ownVoice)
	{
		List<EffectList.EffectData> list = new List<EffectList.EffectData>();
		if (source != null && source.m_effectPrefabs != null)
		{
			foreach (EffectList.EffectData effectData in source.m_effectPrefabs)
			{
				if (effectData == null || effectData.m_prefab == null || !effectData.m_prefab.name.StartsWith("sfx", StringComparison.OrdinalIgnoreCase))
				{
					list.Add(effectData);
				}
			}
		}
		if (ownVoice != null && ownVoice.m_effectPrefabs != null)
		{
			foreach (EffectList.EffectData effectData2 in ownVoice.m_effectPrefabs)
			{
				if (effectData2 != null && effectData2.m_prefab != null)
				{
					list.Add(effectData2);
				}
			}
		}
		return new EffectList
		{
			m_effectPrefabs = list.ToArray()
		};
	}
}
