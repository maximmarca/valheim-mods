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
- **Los lobos comen carne de oso** *(v0.11.0)* — vanilla la excluía. Se pueden sumar más combinaciones en el config `ExtraFood` (pares `criatura:ítem`).
- **Forrajeo**: un animal con hambre busca solo, en un radio de **10 m**, arbustos de frutas o cultivos cuyo fruto esté en su dieta, los cosecha y come lo que cae al piso. Lo que sobra queda en el suelo para los demás animales.
  - Solo cosechan **cuando tienen hambre** — no arrasan tu plantación por gusto.
  - Los lobos no forrajean (comen carne y los arbustos no dan carne).
- Alimentarlos a mano sigue funcionando igual que siempre (tirarles comida).

## Siembra por caca

- Cuando un animal come una fruta/verdura del mapa, la digiere **1–3 minutos** y después hace caca **1–3 semillas**, dispersas hasta 1,5 m a su alrededor.
- **Solo germinan las semillas que caen sobre suelo cultivado** (pasado con el cultivator). El resto se pierde.
- Qué siembra cada comida, en tres grupos *(v0.8.0)*:

  **Arbustos** (frambuesa → arbusto de frambuesas, arándano → arbusto de arándanos):
  el arbusto nace **chiquito (30%), verdoso y sin fruta**. Crece al 50% y al 100% en
  **3 noches de juego (~90 min reales)**. Recién al 100% recupera su color y da la
  **primera fruta**; de ahí en más el respawn de fruta es el normal del juego (~5 h).

  **Hongos** (hongo → hongo): crecen igual que los arbustos (30% → 50% → 100% en 3
  noches) — mientras crecen se ven como **honguitos blancos** chiquitos, no cosechables.
  Al madurar recuperan su color rojo, se pueden cosechar, y además **se multiplican
  solos**: +1 hongo (60%) o +2 (40%), hasta un máximo de **7 hongos por manchón**
  (radio 4 m). Los nuevos también nacen chiquitos y siguen el ciclo — una colonia que
  se expande hasta su tope natural. Cada hongo cosechado rebrota en el mismo lugar
  (~4 h), como los silvestres.

  **Verduras** (zanahoria/nabo/cebolla → plantín): rumbo 100% vanilla — el plantín
  crece ~75 min, se cosecha una vez y da 1 verdura.

- No planta encima de otra planta (respeta 0,4 m de espacio).
- **Tope de densidad** *(v0.7.1)*: si en 4 m alrededor del punto ya hay 10 plantas/arbustos, la semilla se pierde. Así el corral llega a un equilibrio natural en vez de volverse una plaga, y no te alfombran la huerta.
- Los arbustos/hongos **silvestres** (los del mundo) no cambian en nada — las etapas aplican solo a lo sembrado por animales.

### El corral autosustentable (combinando todo)

1. Cercá un corral y pasale el **cultivator a todo el piso**.
2. Plantá unos arbustos de frambuesa iniciales adentro (o tirá frutas).
3. Meté chanchos/ciervos/necks domesticados.
4. Ciclo: tienen hambre → cosechan el arbusto → comen → hacen caca semillas → el suelo cultivado las germina → más arbustos → más comida → crían solos y se curan solos.

## Estrellas por combate

- Un animal domesticado que **remata** a un enemigo tiene **2%** de subir una estrella en el acto; los que **asistieron** (le pegaron en el último minuto) tienen **1%** cada uno.
- Tope: 5 estrellas. Vale para cualquier domesticado — lobos de guerra, pero también un chancho valiente o tus ciervos.
- Combina con la cría: un animal que subió de estrellas peleando **hereda ese nivel a sus crías** — entrenar a la madre mejora la línea de sangre.
- **Se ven** *(v0.10.0)*: de 3★ en adelante cada estrella agranda al bicho y le intensifica el tinte (siguiendo la progresión natural de su especie), y al apuntarlo el nombre muestra "★3/★4/★5". Aplica también a los enemigos salvajes de estrellas altas — ojo con el greydwarf gigante oscuro.

## Construcción (BuildTweaks)

- Todo lo de la pestaña **Building** del martillo (muros, techos, vigas, escaleras… 108 piezas) se construye, repara y demuele **sin mesa de trabajo**, en cualquier lugar del mundo.
- Las demás pestañas no cambian: Crafting sigue pidiendo su estación, la piedra sigue pidiendo cantero (se puede liberar por config, `NoStationCategories`).
- Es un mod **de cliente**: cada uno elige si lo usa; no afecta al resto.

## Notas multijugador (importante)

- En Valheim, la IA de los animales la simula **el cliente del jugador más cercano**, no el server. Y los efectos de sonido son objetos de red: los escuchan todos.
- Por eso, **un cliente desactualizado contamina la partida**: si alguien juega con una versión vieja del mod, los ciervos que él simula gruñen como chanchos y sus bebés se ven adultos — para todos o para él según el efecto.
- Regla simple: **después de cada update, todos bajan el `dist/TameableCreatures.dll` del repo antes de jugar**. El server lo actualiza Maxi.
