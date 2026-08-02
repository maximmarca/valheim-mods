using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace TameableCreatures;

// v0.15.0: aura elemental para 3★+. La extrapolación de LevelEffects (0.10.0)
// hereda la progresión autoral de cada especie, que en muchas es casi nula en
// tinte y sutil en escala — un 3★ quedaba como "un 2★ apenas más grande". El
// aura agrega un canal visual nuevo sin tocar materiales: partículas de llama
// vanilla clonadas y colgadas de la criatura, a juego con las habilidades
// 0.14.x (3★ fuego, 4★ escarcha, 5★ rayo). Clientes sin el mod no ven nada.
internal static class StarAuraTemplates
{
	private static GameObject s_root;

	internal static readonly GameObject[] Tiers = new GameObject[3];

	// Candidatos por tier; el primero que exista y tenga partículas gana.
	// 4★ prefiere la antorcha azul (color autoral); si no está, llama teñida.
	private static readonly string[] FireSources = { "piece_groundtorch_wood", "piece_groundtorch", "fire_pit" };

	private static readonly string[] FrostSources = { "piece_groundtorch_blue", "CastleKit_groundtorch_blue" };

	private static readonly Color FrostTint = new Color(0.35f, 0.75f, 1f);

	private static readonly Color StormTint = new Color(0.8f, 0.4f, 1f);

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
		GameObject fireSource = FindSource(scene, FireSources);
		if (fireSource == null)
		{
			TameableCreaturesPlugin.Log.LogWarning("StarAura: no se encontró ninguna fuente de llamas (" + string.Join(",", FireSources) + "), auras apagadas");
			return;
		}
		GameObject frostSource = FindSource(scene, FrostSources);
		Tiers[0] = BuildTemplate(fireSource, null, "tc_aura_fire");
		Tiers[1] = ((frostSource != null) ? BuildTemplate(frostSource, null, "tc_aura_frost") : BuildTemplate(fireSource, FrostTint, "tc_aura_frost"));
		Tiers[2] = BuildTemplate(fireSource, StormTint, "tc_aura_storm");
		TameableCreaturesPlugin.Log.LogInfo("StarAura: plantillas listas (fuego=" + fireSource.name + ", escarcha=" + ((frostSource != null) ? frostSource.name : (fireSource.name + " teñida")) + ", rayo=" + fireSource.name + " teñida)");
	}

	private static GameObject FindSource(ZNetScene scene, string[] names)
	{
		foreach (string name in names)
		{
			GameObject prefab = scene.GetPrefab(name);
			if (prefab != null && prefab.GetComponentInChildren<ParticleSystem>(includeInactive: true) != null)
			{
				return prefab;
			}
		}
		return null;
	}

	private static GameObject BuildTemplate(GameObject source, Color? tint, string name)
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
			if (tint.HasValue)
			{
				main.startColor = new ParticleSystem.MinMaxGradient(tint.Value);
			}
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
// el aura del tier al conocer el nivel y la renueva si sube en vivo (mismo
// mecanismo m_onLevelSet que usa el vanilla para el visual de estrellas).
public class StarAura : MonoBehaviour
{
	private Character m_character;

	private GameObject m_aura;

	private Transform m_attach;

	private Vector3 m_center = Vector3.up;

	private float m_scale = 1f;

	private void Start()
	{
		m_character = GetComponent<Character>();
		if (!(m_character == null))
		{
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

	private void Refresh(int level)
	{
		if (m_aura != null)
		{
			UnityEngine.Object.Destroy(m_aura);
			m_aura = null;
		}
		int tier = Mathf.Min(level - 4, 2);
		if (tier >= 0 && TameableCreaturesPlugin.StarAuraEnabled.Value)
		{
			GameObject template = StarAuraTemplates.Tiers[tier];
			if (!(template == null))
			{
				m_aura = UnityEngine.Object.Instantiate(template, m_attach);
				m_aura.name = "tc_aura";
				m_aura.transform.localPosition = m_center;
				m_aura.transform.localRotation = Quaternion.identity;
				m_aura.transform.localScale = Vector3.one * (m_scale * Mathf.Clamp(TameableCreaturesPlugin.StarAuraScale.Value, 0.2f, 3f));
				m_aura.SetActive(value: true);
			}
		}
	}
}
