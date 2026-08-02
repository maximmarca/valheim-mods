using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace TameableCreatures;

// v0.15.0: aura elemental para 3★+ (llamas de antorcha vanilla clonadas).
// v0.16.0: el color del aura ahora lo define la CLASE de la especie (mapa
// StarClassMap) y las estrellas escalan el tamaño; especies sin clase usan la
// paleta por tier (3★ naranja / 4★ azul / 5★ violeta). El mismo componente
// ejecuta las habilidades por tick (celeridad/curación/grito/escudo) del lado
// del dueño de red. Clientes sin el mod no ven nada.
internal static class StarAuraTemplates
{
	private static GameObject s_root;

	internal static GameObject Template;

	private static readonly string[] FireSources = { "piece_groundtorch_wood", "piece_groundtorch", "fire_pit" };

	internal static void Build(ZNetScene scene)
	{
		if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
		{
			return; // server dedicado: no hay render, no hacen falta plantillas
		}
		if (s_root != null)
		{
			UnityEngine.Object.Destroy(s_root);
		}
		s_root = new GameObject("tc_aura_templates");
		s_root.SetActive(value: false); // los clones nacen inactivos: no corre ningún Awake (ZNetView)
		GameObject source = FindSource(scene);
		if (source == null)
		{
			TameableCreaturesPlugin.Log.LogWarning("StarAura: no se encontró ninguna fuente de llamas (" + string.Join(",", FireSources) + "), auras apagadas");
			return;
		}
		Template = BuildTemplate(source, "tc_aura");
		if (Template != null)
		{
			TameableCreaturesPlugin.Log.LogInfo("StarAura: plantilla lista (fuente=" + source.name + ", color por clase)");
		}
	}

	private static GameObject FindSource(ZNetScene scene)
	{
		foreach (string name in FireSources)
		{
			GameObject prefab = scene.GetPrefab(name);
			if (prefab != null && prefab.GetComponentInChildren<ParticleSystem>(includeInactive: true) != null)
			{
				return prefab;
			}
		}
		return null;
	}

	private static GameObject BuildTemplate(GameObject source, string name)
	{
		GameObject template = UnityEngine.Object.Instantiate(source, s_root.transform);
		template.name = name;
		// Dejar solo Transform + partículas: primero los scripts (ZNetView y cía,
		// que no deben tocar red), después luces, audio, colliders y mallas.
		for (int pass = 0; pass < 4; pass++)
		{
			bool any = false;
			foreach (Component component in template.GetComponentsInChildren<Component>(includeInactive: true))
			{
				if (component == null || component is Transform || component is ParticleSystem || component is ParticleSystemRenderer)
				{
					continue;
				}
				if (pass == 0 && !(component is MonoBehaviour))
				{
					continue;
				}
				try
				{
					UnityEngine.Object.DestroyImmediate(component);
					any = true;
				}
				catch
				{
				}
			}
			if (!any && pass > 0)
			{
				break;
			}
		}
		ParticleSystem[] systems = template.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
		int usable = 0;
		foreach (ParticleSystem system in systems)
		{
			if (system.name.IndexOf("smoke", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				if (system.gameObject != template)
				{
					system.gameObject.SetActive(value: false);
				}
				continue;
			}
			// Colgarlas directo de la raíz en el origen: la llama de la antorcha
			// vive a ~1 m de altura sobre el poste y hay que recentrarla.
			if (system.transform != template.transform)
			{
				system.transform.SetParent(template.transform, worldPositionStays: false);
				system.transform.localPosition = Vector3.zero;
				system.transform.localRotation = Quaternion.identity;
			}
			ParticleSystem.MainModule main = system.main;
			main.scalingMode = ParticleSystemScalingMode.Hierarchy;
			main.playOnAwake = true;
			usable++;
		}
		if (usable == 0)
		{
			UnityEngine.Object.Destroy(template);
			return null;
		}
		template.transform.localPosition = Vector3.zero;
		template.transform.localRotation = Quaternion.identity;
		template.transform.localScale = Vector3.one;
		return template;
	}
}

// Componente agregado al prefab de cada criatura con LevelEffects: instancia
// el aura al conocer el nivel (color por clase, tamaño por estrellas), la
// renueva al subir en vivo (m_onLevelSet, como el vanilla) y ejecuta las
// habilidades por tick del lado del dueño.
public class StarAura : MonoBehaviour
{
	private static readonly Color[] LegacyTier = new Color[3]
	{
		new Color(1f, 0.55f, 0.15f),
		new Color(0.35f, 0.75f, 1f),
		new Color(0.8f, 0.4f, 1f)
	};

	private static readonly float[] SizeByTier = new float[3] { 1f, 1.2f, 1.4f };

	private Character m_character;

	private ZNetView m_nview;

	private GameObject m_aura;

	private Transform m_attach;

	private Vector3 m_center = Vector3.up;

	private float m_scale = 1f;

	private float m_cooldown;

	private void Start()
	{
		m_character = GetComponent<Character>();
		if (!(m_character == null))
		{
			m_nview = GetComponent<ZNetView>();
			LevelEffects levelEffects = GetComponentInChildren<LevelEffects>(includeInactive: true);
			m_attach = ((levelEffects != null) ? levelEffects.transform : base.transform);
			CapsuleCollider capsule = GetComponent<CapsuleCollider>();
			if (capsule != null)
			{
				m_center = capsule.center;
				m_scale = Mathf.Clamp(capsule.radius * 2f, 0.6f, 3.5f);
			}
			m_character.m_onLevelSet = (Action<int>)Delegate.Combine(m_character.m_onLevelSet, new Action<int>(OnLevelSet));
			Refresh(m_character.GetLevel());
			InvokeRepeating("ClassTick", UnityEngine.Random.Range(2f, 3f), 2f);
		}
	}

	private void OnDestroy()
	{
		if (m_character != null)
		{
			m_character.m_onLevelSet = (Action<int>)Delegate.Remove(m_character.m_onLevelSet, new Action<int>(OnLevelSet));
		}
	}

	private void OnLevelSet(int level)
	{
		Refresh(level);
	}

	private void ClassTick()
	{
		if (m_character == null || m_character.IsDead() || m_nview == null || !m_nview.IsValid() || !m_nview.IsOwner())
		{
			return;
		}
		int tier = StarClasses.TierIndex(m_character.GetLevel() - 1);
		if (tier >= 0)
		{
			string ability = StarClasses.AbilityFor(m_character);
			if (ability != null)
			{
				float cooldown = m_cooldown;
				StarClasses.Tick(m_character, ability, tier, 2f, ref cooldown);
				m_cooldown = cooldown;
			}
		}
	}

	private void Refresh(int level)
	{
		if (m_aura != null)
		{
			UnityEngine.Object.Destroy(m_aura);
			m_aura = null;
		}
		int tier = StarClasses.TierIndex(level - 1);
		if (tier < 0 || !TameableCreaturesPlugin.StarAuraEnabled.Value)
		{
			return;
		}
		GameObject template = StarAuraTemplates.Template;
		if (template == null)
		{
			return;
		}
		Color color = StarClasses.AuraColor(StarClasses.AbilityFor(m_character)) ?? LegacyTier[tier];
		// v0.17.0: brillo del aura configurable (pedido: −10% por defecto)
		float brightness = Mathf.Clamp(TameableCreaturesPlugin.StarAuraBrightness.Value, 0.2f, 1.5f);
		color = new Color(color.r * brightness, color.g * brightness, color.b * brightness, color.a);
		m_aura = UnityEngine.Object.Instantiate(template, m_attach);
		m_aura.name = "tc_aura";
		m_aura.transform.localPosition = m_center;
		m_aura.transform.localRotation = Quaternion.identity;
		m_aura.transform.localScale = Vector3.one * (m_scale * SizeByTier[tier] * Mathf.Clamp(TameableCreaturesPlugin.StarAuraScale.Value, 0.2f, 3f));
		foreach (ParticleSystem system in m_aura.GetComponentsInChildren<ParticleSystem>(includeInactive: true))
		{
			ParticleSystem.MainModule main = system.main;
			main.startColor = new ParticleSystem.MinMaxGradient(color);
		}
		m_aura.SetActive(value: true);
	}
}
