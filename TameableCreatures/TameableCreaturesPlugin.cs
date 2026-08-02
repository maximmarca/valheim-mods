using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace TameableCreatures;

[BepInPlugin("fer.valheim.tameablecreatures", "TameableCreatures", "0.14.0")]
public class TameableCreaturesPlugin : BaseUnityPlugin
{
	public const string PluginGuid = "fer.valheim.tameablecreatures";

	public const string PluginName = "TameableCreatures";

	public const string PluginVersion = "0.14.0";

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

	internal static ConfigEntry<bool> PoopEnabled;

	internal static ConfigEntry<string> PoopMap;

	internal static ConfigEntry<float> DigestMinSeconds;

	internal static ConfigEntry<float> DigestMaxSeconds;

	internal static ConfigEntry<int> PoopSeedsMin;

	internal static ConfigEntry<int> PoopSeedsMax;

	internal static ConfigEntry<float> PoopScatterRadius;

	internal static ConfigEntry<int> PoopMaxPlantsNearby;

	internal static ConfigEntry<float> PoopDensityRadius;

	internal static ConfigEntry<string> StagedPlants;

	internal static ConfigEntry<float> StagedGrowSeconds;

	internal static ConfigEntry<float> StagedStartScale;

	internal static ConfigEntry<float> StagedMidScale;

	internal static ConfigEntry<string> MushroomPrefabs;

	internal static ConfigEntry<float> MushroomChanceOne;

	internal static ConfigEntry<int> MushroomMaxInPatch;

	internal static ConfigEntry<float> MushroomPatchRadius;

	internal static ConfigEntry<float> KillStarChance;

	internal static ConfigEntry<float> AssistStarChance;

	internal static ConfigEntry<float> AssistWindowSeconds;

	internal static ConfigEntry<int> StarVisualsMaxStars;

	internal static ConfigEntry<float> StarDamagePerStar;

	internal static ConfigEntry<float> StarElementalPercent;

	internal static ConfigEntry<float> StarFiveResistPercent;

	internal static ConfigEntry<string> ExtraFood;

	internal static ConfigEntry<bool> TrashChestEnabled;

	internal static ConfigEntry<float> TrashDelaySeconds;

	private void Awake()
	{
		Log = base.Logger;
		Creatures = base.Config.Bind("General", "Creatures", "Deer,Neck", "Criaturas a volver adiestrables, por nombre de prefab separadas por coma.");
		TamingTimeSeconds = base.Config.Bind("General", "TamingTimeSeconds", 1800f, "Segundos de convivencia (alimentada y tranquila) para domesticar una criatura. 1800 = igual que el chancho.");
		FoodItems = base.Config.Bind("General", "FoodItems", "", "Comidas que aceptan, por nombre de prefab separadas por coma (ej: Raspberry,Blueberries,Carrot). Vacío = las mismas que el chancho.");
		EnableBreeding = base.Config.Bind("General", "EnableBreeding", defaultValue: true, "Las criaturas domesticadas pueden criar (como los chanchos). Las crías nacen mansas.");
		MakeCommandable = base.Config.Bind("General", "MakeCommandable", "Deer,Neck,Boar", "Criaturas domesticadas que obedecen el comando de seguir/quedarse (E), como los lobos. Por nombre de prefab, separadas por coma.");
		ExtraFood = base.Config.Bind("General", "ExtraFood", "Wolf:BjornMeat", "Comidas extra por criatura, pares criatura:ítem separados por coma (ej: Wolf:BjornMeat,Boar:Turnip). Vanilla excluye la carne de oso de la dieta del lobo.");
		TrashChestEnabled = base.Config.Bind("General", "TrashChestEnabled", defaultValue: true, "Agrega el Baúl de basura al martillo (pestaña Muebles): lo que se guarda adentro se destruye. Requiere el mod en server y todos los clientes.");
		TrashDelaySeconds = base.Config.Bind("General", "TrashDelaySeconds", 4f, new ConfigDescription("Cada cuántos segundos el baúl destruye su contenido.", new AcceptableValueRange<float>(1f, 60f)));
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
		PoopEnabled = base.Config.Bind("Siembra", "PoopEnabled", defaultValue: true, "Al comer un ítem del mapa PoopMap, el animal hace caca semillas tras la digestión; las que caen sobre suelo cultivado plantan el prefab mapeado.");
		PoopMap = base.Config.Bind("Siembra", "PoopMap", "Raspberry:RaspberryBush,Blueberries:BlueberryBush,Mushroom:Pickable_Mushroom,Carrot:sapling_carrot,Turnip:sapling_turnip,Onion:sapling_onion", "Mapa comida:planta separado por comas (nombre de prefab del ítem comido : prefab a plantar).");
		DigestMinSeconds = base.Config.Bind("Siembra", "DigestMinSeconds", 60f, new ConfigDescription("Digestión mínima en segundos antes de hacer caca.", new AcceptableValueRange<float>(5f, 600f)));
		DigestMaxSeconds = base.Config.Bind("Siembra", "DigestMaxSeconds", 180f, new ConfigDescription("Digestión máxima en segundos antes de hacer caca.", new AcceptableValueRange<float>(5f, 900f)));
		PoopSeedsMin = base.Config.Bind("Siembra", "PoopSeedsMin", 1, new ConfigDescription("Mínimo de semillas por caca.", new AcceptableValueRange<int>(0, 10)));
		PoopSeedsMax = base.Config.Bind("Siembra", "PoopSeedsMax", 3, new ConfigDescription("Máximo de semillas por caca.", new AcceptableValueRange<int>(1, 10)));
		PoopScatterRadius = base.Config.Bind("Siembra", "PoopScatterRadius", 1.5f, new ConfigDescription("Radio en metros en el que se dispersan las semillas alrededor del animal.", new AcceptableValueRange<float>(0.5f, 5f)));
		PoopMaxPlantsNearby = base.Config.Bind("Siembra", "PoopMaxPlantsNearby", 10, new ConfigDescription("Tope de densidad: si ya hay esta cantidad de plantas/arbustos en el radio PoopDensityRadius, la semilla se pierde. Evita la plaga exponencial. 0 = sin tope.", new AcceptableValueRange<int>(0, 100)));
		PoopDensityRadius = base.Config.Bind("Siembra", "PoopDensityRadius", 4f, new ConfigDescription("Radio en metros del chequeo de densidad.", new AcceptableValueRange<float>(1f, 15f)));
		StagedPlants = base.Config.Bind("Siembra", "StagedPlants", "RaspberryBush,BlueberryBush,Pickable_Mushroom", "Prefabs sembrados por caca que crecen por etapas (30%→50%→100%, con tinte y sin fruto hasta madurar). Los silvestres no se tocan.");
		StagedGrowSeconds = base.Config.Bind("Siembra", "StagedGrowSeconds", 5400f, new ConfigDescription("Segundos totales de crecimiento por etapas. 5400 = 3 noches de juego (~90 min reales); mitad en 30% y mitad en 50%.", new AcceptableValueRange<float>(60f, 36000f)));
		StagedStartScale = base.Config.Bind("Siembra", "StagedStartScale", 0.3f, new ConfigDescription("Tamaño inicial de lo sembrado (primera etapa).", new AcceptableValueRange<float>(0.1f, 1f)));
		StagedMidScale = base.Config.Bind("Siembra", "StagedMidScale", 0.5f, new ConfigDescription("Tamaño de la etapa intermedia.", new AcceptableValueRange<float>(0.1f, 1f)));
		MushroomPrefabs = base.Config.Bind("Siembra", "MushroomPrefabs", "Pickable_Mushroom", "Prefabs que al madurar se multiplican solos (grupo hongos).");
		MushroomChanceOne = base.Config.Bind("Siembra", "MushroomChanceOne", 0.6f, new ConfigDescription("Probabilidad de multiplicarse por 1 (el resto de las veces, por 2).", new AcceptableValueRange<float>(0f, 1f)));
		MushroomMaxInPatch = base.Config.Bind("Siembra", "MushroomMaxInPatch", 7, new ConfigDescription("Tope de hongos del mismo tipo en el manchón (radio MushroomPatchRadius).", new AcceptableValueRange<int>(1, 50)));
		MushroomPatchRadius = base.Config.Bind("Siembra", "MushroomPatchRadius", 4f, new ConfigDescription("Radio en metros del manchón de hongos.", new AcceptableValueRange<float>(1f, 15f)));
		KillStarChance = base.Config.Bind("Combate", "KillStarChance", 0.02f, new ConfigDescription("Probabilidad de que un domesticado suba una estrella al rematar a un enemigo.", new AcceptableValueRange<float>(0f, 1f)));
		AssistStarChance = base.Config.Bind("Combate", "AssistStarChance", 0.01f, new ConfigDescription("Probabilidad de subir una estrella por asistir en la muerte (haberlo dañado en la ventana previa).", new AcceptableValueRange<float>(0f, 1f)));
		AssistWindowSeconds = base.Config.Bind("Combate", "AssistWindowSeconds", 60f, new ConfigDescription("Segundos previos a la muerte en los que un golpe cuenta como asistencia.", new AcceptableValueRange<float>(5f, 300f)));
		StarVisualsMaxStars = base.Config.Bind("Combate", "StarVisualsMaxStars", 5, new ConfigDescription("Hasta cuántas estrellas extender los visuales (tamaño/tinte) extrapolando la progresión vanilla de 1-2 estrellas.", new AcceptableValueRange<int>(2, 10)));
		StarDamagePerStar = base.Config.Bind("Combate", "StarDamagePerStar", 1.5f, new ConfigDescription("Daño exponencial: factor = este valor elevado a la cantidad de estrellas (vanilla es lineal +50%/estrella). 1.5 => 1★ igual vanilla, 3★ 3.4x, 5★ 7.6x. 1 = dejar vanilla.", new AcceptableValueRange<float>(1f, 3f)));
		StarElementalPercent = base.Config.Bind("Combate", "StarElementalPercent", 0.3f, new ConfigDescription("Habilidades: fracción del daño físico del golpe que se suma como elemental según estrellas (3★ fuego/quemadura, 4★ escarcha/frena, 5★ rayo). 0 = apagado.", new AcceptableValueRange<float>(0f, 2f)));
		StarFiveResistPercent = base.Config.Bind("Combate", "StarFiveResistPercent", 25f, new ConfigDescription("Resistencia física pasiva (%) de las criaturas 5★. 0 = apagado.", new AcceptableValueRange<float>(0f, 90f)));
		new Harmony("fer.valheim.tameablecreatures").PatchAll();
		Log.LogInfo("TameableCreatures 0.14.0 cargado (todo lo anterior + habilidades por estrellas)");
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














