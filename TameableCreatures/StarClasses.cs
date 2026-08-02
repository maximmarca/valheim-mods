using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace TameableCreatures;

// v0.16.0: habilidades por CLASE de criatura. La clase (config StarClassMap,
// pares criatura:habilidad) define el sabor; las estrellas definen magnitud y
// duración (índice t: 0=3★, 1=4★, 2=5★). Especies sin entrada caen al sistema
// anterior por tier (3★ fuego / 4★ escarcha / 5★ rayo + resist del 5★).
// Todo verificado contra el vanilla descompilado: SEMan.AddStatusEffect acepta
// instancias propias, Character.Heal se auto-rutea por red, SE_Harpooned
// funciona con cualquier Character como atacante (suelta al atacar/bloquear),
// SE_Puke castea la víctima a Player (solo aplicar a jugadores).
internal static class StarClasses
{
	private static Dictionary<string, string> s_map;

	private static string s_mapSource;

	// escala del % elemental por tier (sobre StarElementalPercent)
	private static readonly float[] ElemScale = { 1f, 4f / 3f, 5f / 3f };

	private static readonly float[] LifeStealPct = { 0.3f, 0.5f, 0.8f };

	private static readonly float[] CritChance = { 0.1f, 0.15f, 0.25f };

	private static readonly float[] DodgeChance = { 0.1f, 0.2f, 0.3f };

	private static readonly float[] ThornsPct = { 0.15f, 0.25f, 0.4f };

	private static readonly float[] IronSkinPct = { 0.2f, 0.3f, 0.4f };

	private static readonly float[] UnstoppableResistPct = { 0.1f, 0.2f, 0.3f };

	private static readonly float[] RamPush = { 60f, 100f, 150f };

	private static readonly float[] RamStagger = { 1.5f, 2f, 3f };

	private static readonly float[] RootSeconds = { 3f, 4f, 5f };

	private static readonly float[] DisarmSeconds = { 3f, 4f, 5f };

	private static readonly float[] DisarmChance = { 0.35f, 0.5f, 0.65f };

	private static readonly float[] HarpoonSeconds = { 3f, 4f, 5f };

	private static readonly float[] PukeSeconds = { 3f, 5f, 8f };

	private static readonly float[] WetSeconds = { 4f, 7f, 10f };

	private static readonly float[] CurseSeconds = { 5f, 8f, 10f };

	private static readonly float[] BleedSeconds = { 4f, 7f, 10f };

	private static readonly float[] BleedTotalPct = { 0.6f, 0.9f, 1.2f };

	private static readonly float[] SpeedBuff = { 0.2f, 0.35f, 0.5f };

	private static readonly float[] HealAuraPctPerSec = { 0.02f, 0.04f, 0.06f };

	internal static readonly float[] WarCrySeconds = { 5f, 8f, 10f };

	private static readonly float[] WarCrySpeed = { 0.15f, 0.25f, 0.4f };

	private static readonly float[] WarCryDamage = { 1.15f, 1.25f, 1.4f };

	internal static readonly float[] ShieldAbsorb = { 50f, 100f, 200f };

	private static readonly Dictionary<string, Color> s_auraColors = new Dictionary<string, Color>
	{
		{ "fuego", new Color(1f, 0.55f, 0.15f) },
		{ "escarcha", new Color(0.35f, 0.75f, 1f) },
		{ "rayo", new Color(0.8f, 0.4f, 1f) },
		{ "veneno", new Color(0.35f, 1f, 0.3f) },
		{ "raiz", new Color(0.45f, 0.62f, 0.15f) },
		{ "robovida", new Color(1f, 0.15f, 0.25f) },
		{ "espinas", new Color(1f, 0.75f, 0.2f) },
		{ "pielhierro", new Color(1f, 0.75f, 0.2f) },
		{ "inamovible", new Color(1f, 0.75f, 0.2f) },
		{ "embestida", new Color(1f, 0.75f, 0.2f) },
		{ "critico", new Color(0.9f, 0.95f, 1f) },
		{ "esquiva", new Color(0.9f, 0.95f, 1f) },
		{ "celeridad", new Color(0.9f, 0.95f, 1f) },
		{ "sangrado", new Color(0.9f, 0.95f, 1f) },
		{ "desarme", new Color(0.9f, 0.95f, 1f) },
		{ "arpon", new Color(0.9f, 0.95f, 1f) },
		{ "curacion", new Color(1f, 0.9f, 0.4f) },
		{ "grito", new Color(1f, 0.9f, 0.4f) },
		{ "escudo", new Color(1f, 0.9f, 0.4f) },
		{ "nova", new Color(1f, 0.4f, 0.1f) },
		{ "nausea", new Color(0.6f, 0.8f, 0.2f) },
		{ "empapar", new Color(0.3f, 0.6f, 1f) },
		{ "maldicion", new Color(0.5f, 0.2f, 0.7f) }
	};

	internal static int TierIndex(int stars)
	{
		if (stars < 3)
		{
			return -1;
		}
		return Mathf.Min(stars - 3, 2);
	}

	internal static string AbilityFor(Character c)
	{
		if (c == null)
		{
			return null;
		}
		EnsureMap();
		if (s_map.TryGetValue(Utils.GetPrefabName(c.gameObject), out var ability))
		{
			return ability;
		}
		return null;
	}

	internal static Color? AuraColor(string ability)
	{
		if (ability != null && s_auraColors.TryGetValue(ability, out var color))
		{
			return color;
		}
		return null;
	}

	private static void EnsureMap()
	{
		string value = TameableCreaturesPlugin.StarClassMap.Value;
		if (s_map != null && s_mapSource == value)
		{
			return;
		}
		s_map = new Dictionary<string, string>();
		s_mapSource = value;
		string[] array = value.Split(',');
		foreach (string text in array)
		{
			int num = text.IndexOf(':');
			if (num > 0)
			{
				string key = text.Substring(0, num).Trim();
				string text2 = text.Substring(num + 1).Trim().ToLowerInvariant();
				if (key.Length > 0 && text2.Length > 0)
				{
					s_map[key] = text2;
				}
			}
		}
		TameableCreaturesPlugin.Log.LogInfo($"Clases de estrellas: {s_map.Count} especies mapeadas");
	}

	// ===== helpers =====

	private static T FindSeAsset<T>() where T : StatusEffect
	{
		if (ObjectDB.instance == null)
		{
			return null;
		}
		foreach (StatusEffect statusEffect in ObjectDB.instance.m_StatusEffects)
		{
			if (statusEffect is T val)
			{
				return val;
			}
		}
		return null;
	}

	internal static SE_Stats MakeStats(string name, float ttl, float speed = 0f, float staminaRegen = 1f, float skillMod = 0f, float damageMod = 1f)
	{
		SE_Stats sE_Stats = ScriptableObject.CreateInstance<SE_Stats>();
		sE_Stats.name = name;
		sE_Stats.m_name = name;
		sE_Stats.m_ttl = ttl;
		sE_Stats.m_speedModifier = speed;
		sE_Stats.m_staminaRegenMultiplier = staminaRegen;
		sE_Stats.m_skillLevelModifier = skillMod;
		sE_Stats.m_damageModifier = damageMod;
		return sE_Stats;
	}

	private static void SpawnFx(string prefabName, Vector3 point)
	{
		if (!(ZNetScene.instance == null))
		{
			GameObject prefab = ZNetScene.instance.GetPrefab(prefabName);
			if (prefab != null)
			{
				UnityEngine.Object.Instantiate(prefab, point, Quaternion.identity);
			}
		}
	}

	internal static bool IsAlly(Character self, Character other)
	{
		return other != null && !other.IsPlayer() && other.IsTamed() == self.IsTamed() && !BaseAI.IsEnemy(self, other);
	}

	// ===== lado ofensivo (el atacante 3★+ tiene clase) =====

	internal static void OnHit(Character attacker, Character victim, HitData hit, string ability, int t)
	{
		float elemPct = TameableCreaturesPlugin.StarElementalPercent.Value * ElemScale[t];
		float phys = hit.m_damage.GetTotalPhysicalDamage();
		switch (ability)
		{
		case "fuego":
			hit.m_damage.m_fire += phys * elemPct;
			SpawnFx("fx_DvergerMage_Fire_hit", hit.m_point);
			break;
		case "escarcha":
			hit.m_damage.m_frost += phys * elemPct;
			SpawnFx("vfx_frostarrow_hit", hit.m_point);
			break;
		case "rayo":
			hit.m_damage.m_lightning += phys * elemPct;
			SpawnFx("fx_lightningweapon_hit", hit.m_point);
			break;
		case "veneno":
			hit.m_damage.m_poison += phys * elemPct * 2f;
			break;
		case "robovida":
			if (phys > 0f)
			{
				attacker.Heal(hit.GetTotalDamage() * LifeStealPct[t], showText: true);
			}
			break;
		case "critico":
			if (UnityEngine.Random.value < CritChance[t])
			{
				hit.ApplyModifier(2.5f);
				SpawnFx("fx_crit", hit.m_point);
			}
			break;
		case "embestida":
			hit.m_pushForce = Mathf.Max(hit.m_pushForce, RamPush[t]);
			hit.m_staggerMultiplier *= RamStagger[t];
			break;
		case "raiz":
			if (victim != null && !victim.IsBoss())
			{
				SE_Stats root = MakeStats("Raíz", RootSeconds[t], -0.95f);
				victim.GetSEMan().AddStatusEffect(root, resetTime: true);
				victim.Message(MessageHud.MessageType.Center, "¡Enraizado!");
			}
			break;
		case "desarme":
			if (victim is Player playerD && UnityEngine.Random.value < DisarmChance[t])
			{
				ItemDrop.ItemData currentWeapon = playerD.GetCurrentWeapon();
				if (currentWeapon != null)
				{
					playerD.UnequipItem(currentWeapon);
					playerD.GetSEMan().AddStatusEffect(MakeStats("Desarmado", DisarmSeconds[t]), resetTime: true);
					playerD.Message(MessageHud.MessageType.Center, "¡Desarmado!");
				}
			}
			break;
		case "arpon":
			if (victim != null && !victim.IsBoss())
			{
				SE_Harpooned asset = FindSeAsset<SE_Harpooned>();
				if (asset != null)
				{
					SE_Harpooned clone = UnityEngine.Object.Instantiate(asset);
					clone.name = asset.name;
					clone.m_ttl = HarpoonSeconds[t];
					StatusEffect added = victim.GetSEMan().AddStatusEffect(clone, resetTime: true);
					added?.SetAttacker(attacker);
				}
			}
			break;
		case "nausea":
			if (victim is Player playerN)
			{
				SE_Puke pukeAsset = FindSeAsset<SE_Puke>();
				SE_Puke puke = ((pukeAsset != null) ? UnityEngine.Object.Instantiate(pukeAsset) : ScriptableObject.CreateInstance<SE_Puke>());
				puke.name = "TC_Nausea";
				puke.m_name = "Náusea";
				puke.m_ttl = PukeSeconds[t];
				puke.m_removeInterval = 2f;
				playerN.GetSEMan().AddStatusEffect(puke, resetTime: true);
			}
			break;
		case "empapar":
			if (victim != null)
			{
				StatusEffect wet = victim.GetSEMan().AddStatusEffect("Wet".GetStableHashCode(), resetTime: true);
				if (wet != null)
				{
					wet.m_ttl = WetSeconds[t];
				}
			}
			break;
		case "maldicion":
			if (victim != null && !victim.IsBoss())
			{
				SE_Stats curse = MakeStats("Maldición", CurseSeconds[t], -0.05f * (float)(t + 3), 0.6f - 0.15f * (float)t, -5f * (float)(t + 1));
				victim.GetSEMan().AddStatusEffect(curse, resetTime: true);
			}
			break;
		case "sangrado":
			if (victim != null && phys > 0f)
			{
				TC_SE_Bleed bleed = ScriptableObject.CreateInstance<TC_SE_Bleed>();
				bleed.name = "TC_Sangrado";
				bleed.m_name = "Sangrado";
				bleed.m_ttl = BleedSeconds[t];
				bleed.m_damagePerTick = phys * BleedTotalPct[t] / BleedSeconds[t];
				victim.GetSEMan().AddStatusEffect(bleed, resetTime: true);
			}
			break;
		case "nova":
		{
			float novaDamage = phys * elemPct;
			if (!(novaDamage > 0f))
			{
				break;
			}
			SpawnFx("fx_DvergerMage_Fire_hit", hit.m_point);
			foreach (Character allCharacter in Character.GetAllCharacters())
			{
				if (!(allCharacter == attacker) && !(allCharacter == victim) && !allCharacter.IsDead() && Vector3.Distance(allCharacter.transform.position, hit.m_point) < 4f && BaseAI.IsEnemy(attacker, allCharacter))
				{
					HitData hitData = new HitData();
					hitData.m_damage.m_fire = novaDamage;
					hitData.m_point = allCharacter.GetCenterPoint();
					allCharacter.Damage(hitData);
				}
			}
			break;
		}
		}
	}

	// ===== lado defensivo (la víctima 3★+ tiene clase) =====

	internal static void OnDamaged(Character victim, HitData hit, string ability, int t)
	{
		switch (ability)
		{
		case "esquiva":
			if (UnityEngine.Random.value < DodgeChance[t])
			{
				hit.ApplyModifier(0f);
				hit.m_pushForce = 0f;
				hit.m_staggerMultiplier = 0f;
			}
			break;
		case "pielhierro":
			ReducePhysical(hit, IronSkinPct[t]);
			break;
		case "inamovible":
			hit.m_pushForce = 0f;
			hit.m_staggerMultiplier = 0f;
			ReducePhysical(hit, UnstoppableResistPct[t]);
			break;
		case "espinas":
		{
			Character attacker = hit.GetAttacker();
			float phys = hit.m_damage.GetTotalPhysicalDamage();
			if (attacker != null && !attacker.IsDead() && phys > 0f)
			{
				// sin atacante en el hit devuelto: corta la cadena espinas↔espinas
				HitData hitData = new HitData();
				hitData.m_damage.m_blunt = phys * ThornsPct[t];
				hitData.m_point = attacker.GetCenterPoint();
				attacker.Damage(hitData);
			}
			break;
		}
		}
	}

	private static void ReducePhysical(HitData hit, float pct)
	{
		float num = 1f - pct;
		hit.m_damage.m_blunt *= num;
		hit.m_damage.m_slash *= num;
		hit.m_damage.m_pierce *= num;
	}

	// ===== habilidades por tick (llamadas desde StarAura, solo owner) =====

	internal static void Tick(Character self, string ability, int t, float dt, ref float cooldown)
	{
		switch (ability)
		{
		case "celeridad":
			if (self.GetSEMan().GetStatusEffect("Celeridad".GetStableHashCode()) == null)
			{
				self.GetSEMan().AddStatusEffect(MakeStats("Celeridad", 5f, SpeedBuff[t]), resetTime: true);
			}
			break;
		case "curacion":
			foreach (Character allCharacter in Character.GetAllCharacters())
			{
				if (IsAlly(self, allCharacter) && !(allCharacter == self) && Vector3.Distance(allCharacter.transform.position, self.transform.position) < 10f)
				{
					allCharacter.Heal(allCharacter.GetMaxHealth() * HealAuraPctPerSec[t] * dt, showText: true);
				}
			}
			break;
		case "grito":
		{
			cooldown -= dt;
			if (cooldown > 0f)
			{
				break;
			}
			BaseAI baseAI = self.GetComponent<BaseAI>();
			if (baseAI == null || !baseAI.IsAlerted())
			{
				break;
			}
			cooldown = 20f;
			int count = 0;
			foreach (Character allCharacter2 in Character.GetAllCharacters())
			{
				if (IsAlly(self, allCharacter2) && Vector3.Distance(allCharacter2.transform.position, self.transform.position) < 10f)
				{
					allCharacter2.GetSEMan().AddStatusEffect(MakeStats("Grito de guerra", WarCrySeconds[t], WarCrySpeed[t], 1f, 0f, WarCryDamage[t]), resetTime: true);
					count++;
				}
			}
			if (count > 0)
			{
				SpawnFx("fx_DvergerMage_Support_start", self.GetCenterPoint());
			}
			break;
		}
		case "escudo":
		{
			cooldown -= dt;
			if (cooldown > 0f)
			{
				break;
			}
			SE_Shield asset = FindSeAsset<SE_Shield>();
			if (asset == null)
			{
				cooldown = 120f;
				break;
			}
			if (self.GetSEMan().GetStatusEffect(asset.m_name.GetStableHashCode()) == null)
			{
				cooldown = 30f;
				SE_Shield clone = UnityEngine.Object.Instantiate(asset);
				clone.name = asset.name;
				clone.m_ttl = 15f;
				clone.m_absorbDamage = ShieldAbsorb[t];
				clone.m_absorbDamagePerSkillLevel = 0f;
				clone.m_absorbDamageWorldLevel = 0f;
				StatusEffect added = self.GetSEMan().AddStatusEffect(clone, resetTime: true);
				added?.SetLevel(1, 0f);
			}
			break;
		}
		}
	}
}

// Desarme: mientras dure el estado "Desarmado", el jugador no puede volver a
// empuñar un arma (el desarme sin esto sería cosmético: re-equipás al instante).
[HarmonyPatch(typeof(Humanoid), "EquipItem")]
internal static class Patch_Humanoid_EquipItem_Disarm
{
	private static bool Prefix(Humanoid __instance, ItemDrop.ItemData item, ref bool __result)
	{
		if (__instance is Player player && item != null && item.IsWeapon() && player.GetSEMan().HaveStatusEffect("Desarmado".GetStableHashCode()))
		{
			player.Message(MessageHud.MessageType.Center, "¡Desarmado!");
			__result = false;
			return false;
		}
		return true;
	}
}

// Sangrado: DoT físico puro (sin armadura), tick de 1 s. Custom porque el
// vanilla no tiene bleed; mismo patrón que SE_Poison pero vía ApplyDamage.
public class TC_SE_Bleed : StatusEffect
{
	public float m_damagePerTick;

	private float m_timer;

	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		m_timer += dt;
		if (m_timer >= 1f && m_character != null && !m_character.IsDead())
		{
			m_timer = 0f;
			HitData hitData = new HitData();
			hitData.m_damage.m_damage = m_damagePerTick;
			hitData.m_point = m_character.GetCenterPoint();
			m_character.ApplyDamage(hitData, showDamageText: true, triggerEffects: false);
		}
	}
}
