# Guía de jugabilidad — mods MaxiFer

Cómo se juega con cada modificación, en detalle. Cambios técnicos en [CHANGELOG.md](CHANGELOG.md).

## Domesticar ciervos y necks

Funcionan **igual que domesticar un chancho**:

1. Tirales comida cerca: aceptan lo mismo que el chancho (frambuesas, arándanos, hongos, zanahorias...).
2. Alejate para que no estén asustados. Ojo: un ciervo/neck salvaje se asusta si te acercás a **menos de 12 m** (config `ScareRange`) y corre ~5 s (`FleeSeconds`). Escondido o detrás de una pared no te detectan.
3. Con comida y tranquilidad, la domesticación tarda **30 min de convivencia** (misma que el chancho). Apuntándolos ves el estado: asustado / hambriento / domesticándose / feliz.
4. Domesticado: con **E** le das seguir/quedarse, como a un lobo (el chancho también obedece ahora).

**Consejo corral**: cerco + comida adentro; el forrajeo y la siembra (abajo) lo vuelven autosuficiente.

## Cría y bebés

- Dos adultos domesticados de la misma especie, **alimentados** y cerca uno del otro → corazones → cría. Como los chanchos.
- Límite por corral: hasta **20** de la misma especie en ~10 m (vanilla corta en ~5).
- **La cría nace bebé**: mitad de tamaño y color pastel clarito. Crece sola a los **50 minutos reales**. Mientras es bebé **no puede criar**.
- **Estrellas y mutaciones**: la cría normalmente hereda el nivel de los padres, pero hay **2%** de que nazca con **una estrella más** que el mejor padre y **1%** de una menos que el peor. Con MoreStars el techo es **5 estrellas**: criando en masa y seleccionando, se puede escalar de a poco una línea de sangre.
- Para **ver** a los bebés chiquitos y pastel, cada jugador necesita el mod actualizado (el server los achica igual, pero tu cliente es el que dibuja).

## Vida y curación de los animales

- **Fuera de combate** (10 s sin recibir daño y sin estar alertados), los domesticados curan **5% de su vida máxima cada 10 s** (mínimo +1, así el número flotante se ve). A ese ritmo: de casi muerto a full en ~3–4 minutos.
- **Con hambre curan la mitad** — mantenelos alimentados si venís de una pelea.
- **En combate no regeneran**: si un lobo tuyo pelea, la vida que pierde no vuelve hasta que termine el combate.
- Esto reemplaza el "+0" viejo: vanilla tardaba UNA HORA en llenar la vida y no curaba nada con hambre.

## Comida y forrajeo

- Cada animal come **1 ítem por ciclo de hambre** y queda saciado **10 minutos** (todas las especies por igual).
- **Forrajeo**: un animal con hambre busca solo, en un radio de **10 m**, arbustos de frutas o cultivos cuyo fruto esté en su dieta, los cosecha y come lo que cae al piso. Lo que sobra queda en el suelo para los demás animales.
  - Solo cosechan **cuando tienen hambre** — no arrasan tu plantación por gusto.
  - Los lobos no forrajean (comen carne y los arbustos no dan carne).
- Alimentarlos a mano sigue funcionando igual que siempre (tirarles comida).

## Siembra por caca

- Cuando un animal come una fruta/verdura del mapa, la digiere **1–3 minutos** y después hace caca **1–3 semillas**, dispersas hasta 1,5 m a su alrededor.
- **Solo germinan las semillas que caen sobre suelo cultivado** (pasado con el cultivator). El resto se pierde.
- Qué siembra cada comida:

  | Come | Planta |
  |---|---|
  | Frambuesa | Arbusto de frambuesas |
  | Arándano | Arbusto de arándanos |
  | Hongo | Hongo |
  | Zanahoria | Plantín de zanahoria (crece normal) |
  | Nabo | Plantín de nabo |
  | Cebolla | Plantín de cebolla |

- No planta encima de otra planta (respeta 0,4 m de espacio).

### El corral autosustentable (combinando todo)

1. Cercá un corral y pasale el **cultivator a todo el piso**.
2. Plantá unos arbustos de frambuesa iniciales adentro (o tirá frutas).
3. Meté chanchos/ciervos/necks domesticados.
4. Ciclo: tienen hambre → cosechan el arbusto → comen → hacen caca semillas → el suelo cultivado las germina → más arbustos → más comida → crían solos y se curan solos.

## Construcción (BuildTweaks)

- Todo lo de la pestaña **Building** del martillo (muros, techos, vigas, escaleras… 108 piezas) se construye, repara y demuele **sin mesa de trabajo**, en cualquier lugar del mundo.
- Las demás pestañas no cambian: Crafting sigue pidiendo su estación, la piedra sigue pidiendo cantero (se puede liberar por config, `NoStationCategories`).
- Es un mod **de cliente**: cada uno elige si lo usa; no afecta al resto.

## Notas multijugador (importante)

- En Valheim, la IA de los animales la simula **el cliente del jugador más cercano**, no el server. Y los efectos de sonido son objetos de red: los escuchan todos.
- Por eso, **un cliente desactualizado contamina la partida**: si alguien juega con una versión vieja del mod, los ciervos que él simula gruñen como chanchos y sus bebés se ven adultos — para todos o para él según el efecto.
- Regla simple: **después de cada update, todos bajan el `dist/TameableCreatures.dll` del repo antes de jugar**. El server lo actualiza Maxi.
