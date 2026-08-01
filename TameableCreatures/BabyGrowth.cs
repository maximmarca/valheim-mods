using System.Collections.Generic;
using UnityEngine;

namespace TameableCreatures;

// Cría bebé: mientras el ZDO tenga tc_babyBorn vigente, la criatura se ve
// reducida y con tinte pastel. Cada cliente lo aplica localmente leyendo el
// ZDO (la escala no se sincroniza por red), por eso el mod debe estar en
// server y en todos los clientes. Al cumplirse BabyGrowMinutes vuelve a la
// normalidad y el dueño limpia la marca.
public class BabyGrowth : MonoBehaviour
{
	internal const string ZdoKey = "tc_babyBorn";

	private struct TintEntry
	{
		public Material Mat;

		public bool Hsv;

		public float Sat;

		public float Val;

		public Color Col;
	}

	private ZNetView m_nview;

	private bool m_applied;

	private float m_scaleFactor = 1f;

	private Vector3 m_appliedScale;

	private readonly List<TintEntry> m_tinted = new List<TintEntry>();

	internal static bool IsBaby(ZNetView nview)
	{
		if (nview == null || !nview.IsValid() || ZNet.instance == null)
		{
			return false;
		}
		float num = nview.GetZDO().GetFloat(ZdoKey);
		if (num <= 0f)
		{
			return false;
		}
		return ZNet.instance.GetTimeSeconds() - (double)num < (double)(TameableCreaturesPlugin.BabyGrowMinutes.Value * 60f);
	}

	private void Start()
	{
		m_nview = GetComponent<ZNetView>();
		InvokeRepeating("BabyUpdate", Random.Range(1f, 2f), 2f);
	}

	private void BabyUpdate()
	{
		if (m_nview == null || !m_nview.IsValid() || ZNet.instance == null)
		{
			return;
		}
		float num = m_nview.GetZDO().GetFloat(ZdoKey);
		bool flag = num > 0f && ZNet.instance.GetTimeSeconds() - (double)num < (double)(TameableCreaturesPlugin.BabyGrowMinutes.Value * 60f);
		if (flag && !m_applied)
		{
			Apply();
		}
		else if (!flag && m_applied)
		{
			Remove();
		}
		else if (flag && m_applied && (base.transform.localScale - m_appliedScale).sqrMagnitude > 1E-06f)
		{
			// LevelEffects (estrellas) pisa localScale al setear nivel: re-componer.
			base.transform.localScale *= m_scaleFactor;
			m_appliedScale = base.transform.localScale;
		}
		if (!flag && num > 0f && m_nview.IsOwner())
		{
			m_nview.GetZDO().Set(ZdoKey, 0f);
		}
	}

	private void Apply()
	{
		m_scaleFactor = Mathf.Clamp(TameableCreaturesPlugin.BabyScale.Value, 0.2f, 1f);
		base.transform.localScale *= m_scaleFactor;
		m_appliedScale = base.transform.localScale;
		m_tinted.Clear();
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
					m_tinted.Add(new TintEntry
					{
						Mat = material,
						Hsv = true,
						Sat = material.GetFloat("_Saturation"),
						Val = material.GetFloat("_Value")
					});
					material.SetFloat("_Saturation", material.GetFloat("_Saturation") + TameableCreaturesPlugin.BabySaturation.Value);
					material.SetFloat("_Value", material.GetFloat("_Value") + TameableCreaturesPlugin.BabyBrightness.Value);
				}
				else if (material.HasProperty("_Color"))
				{
					m_tinted.Add(new TintEntry
					{
						Mat = material,
						Hsv = false,
						Col = material.color
					});
					material.color = Color.Lerp(material.color, Color.white, 0.45f);
				}
			}
		}
		m_applied = true;
	}

	private void Remove()
	{
		if (m_scaleFactor > 0f)
		{
			base.transform.localScale /= m_scaleFactor;
		}
		foreach (TintEntry tintEntry in m_tinted)
		{
			if (!(tintEntry.Mat == null))
			{
				if (tintEntry.Hsv)
				{
					tintEntry.Mat.SetFloat("_Saturation", tintEntry.Sat);
					tintEntry.Mat.SetFloat("_Value", tintEntry.Val);
				}
				else
				{
					tintEntry.Mat.color = tintEntry.Col;
				}
			}
		}
		m_tinted.Clear();
		m_applied = false;
		m_scaleFactor = 1f;
	}
}
