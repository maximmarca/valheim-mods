# Changelog — mods MaxiFer Valheim

Registro completo de cambios. Detalle de jugabilidad en [JUGABILIDAD.md](JUGABILIDAD.md).

## TameableCreatures

### v0.17.0 — 2026-08-02 · Azada limpia hongos, tope por zona y brillo de aura
- **La azada limpia hongos**: cualquier operación de terreno (allanar, camino, cultivar) destruye los hongos (`HoeCleanPrefabs`, default `Pickable_Mushroom`) dentro de su radio — la forma pedida de recuperar zonas invadidas por la siembra. `HoeCleanEnabled` para apagarlo.
- **Tope de hongos por ZONA**: los topes eran por manchón de 4 m y los manchones adyacentes se solapaban sin límite total. Ahora además: con `MushroomMaxPerZone` (12) o más hongos del mismo tipo en `MushroomZoneRadius` (20 m), no se multiplican más.
- **Brillo del aura −10%**: `StarAuraBrightness` (0.9) multiplica el color del aura de todas las criaturas 3★+, salvajes y mascotas.

### v0.16.0 — 2026-08-02 · Habilidades por clase de criatura
- Las habilidades de 3★+ ya no van por tier fijo: **cada especie tiene una habilidad de su clase** (config `StarClassMap`, pares `criatura:habilidad`) y las estrellas escalan magnitud/duración (3★/4★/5★). Especies sin entrada conservan el sistema anterior (3★ fuego / 4★ escarcha / 5★ rayo + resist del 5★).
- **23 habilidades**: elementales (fuego, escarcha, rayo, veneno), robo de vida, raíz (inmoviliza 3-5 s), crítico, esquiva, celeridad, espinas, piel de hierro, inamovible, embestida aturdidora, aura de curación aliada, grito de guerra, escudo (burbuja vanilla), nova de fuego en área, desarme (te saca el arma y bloquea re-equipar 3-5 s), sangrado (DoT custom), arpón (te arrastra hacia la criatura, SE_Harpooned invertido — suelta al atacar/bloquear), náusea (te hace vomitar comidas, solo jugadores), empapar (mojado, sinergia con escarcha) y maldición (skills/stamina abajo).
- Mapa default: Greydwarfs raíz, Shaman curación, Troll/Berserker inamovible, Draugr veneno, Draugr Elite grito, Oozer/Tick náusea, Leech/Wraith/Ghost/Bat robo de vida, Abomination raíz, Surtling/Gjall nova, Wolf/Drake escarcha, Fenring desarme, Cultist fuego, Golem piel de hierro, Fulings crítico/maldición, Deathsquito rayo, Lox/Boar embestida, Seeker sangrado, Seeker Soldier arpón, serpiente empapar, Neck esquiva, Deer celeridad, Bjorn embestida.
- **El aura toma el color de la clase** (veneno verde, robo de vida carmesí, tanques ámbar, agilidad plateado, soporte dorado, maldición violeta oscuro…) y las estrellas escalan su tamaño. CC duro capado en 5 s. Todo verificado contra el vanilla descompilado (SEMan acepta instancias propias, Heal se auto-rutea por red, SE_Puke solo jugadores, SE_Harpooned acepta cualquier atacante).

### v0.15.1 — 2026-08-02 · Honguitos sembrados en blanco pleno
- Los hongos sembrados por caca, mientras crecen (30%/50%), ahora se ven **completamente blancos**: el `Lerp` al 85% de la 0.8.2 les dejaba un resto rosado del color original. Pedido de Maxi tras probar en juego.

### v0.15.0 — 2026-08-02 · Aura elemental para 3★+
- Diagnóstico del pendiente "los 3★+ no se distinguen": la extrapolación autoral (0.14.3) hereda deltas de tinte casi nulos en muchas especies y una escala de +5-15% por estrella — no había canal visual con margen. En vez de seguir con materiales, **aura de partículas** a juego con las habilidades 0.14.x: **3★ llama de fuego, 4★ llama de escarcha** (antorcha azul vanilla si existe, o llama teñida azul hielo), **5★ llama violeta "rayo"**.
- Implementación: componente `StarAura` en cada criatura con `LevelEffects`; clona las llamas de la antorcha vanilla (sin `ZNetView`, luces, audio ni humo), las centra en el cuerpo y las escala por el radio del collider de la especie. Se renueva en vivo al subir de estrella (mismo `m_onLevelSet` del vanilla). Solo visual y por cliente: **un cliente sin el mod no ve nada y no da errores**.
- Config nueva: `StarAuraEnabled` (true) y `StarAuraScale` (1). Los cuernos del ciervo y demás objetos por nivel quedan intactos.
- Nota de prueba descubierta en el camino: el caché estático de materiales del vanilla (`LevelEffects.m_materials`) y los setups extendidos sobreviven al relog — **para probar cambios de config visual hay que cerrar el juego por completo**, reloguear no alcanza.

### v0.14.3 — 2026-08-02 · Fix: colores mate/homogéneos en 3★+
- Los shifts fijos de saturación/valor aplanaban la paleta autoral de cada especie (resultado mate, homogéneo, desaturado). Se volvió a la extrapolación de la progresión de color propia de cada especie (deltas 1★→2★ continuados) y la emisión quedó **apagada por defecto** (`StarGlowIntensity = 0`; opcional para quien la quiera). Configs de server y cliente Maxi puestos en 0.

### v0.14.2 — 2026-08-02 · Fix: brillo "cámara térmica" en los 3★+
- La emisión con intensidad alta pintaba el cuerpo entero fullbright amarillo (los materiales de criatura no tienen máscara de emisión). Brillo bajado a tenue (0.25, escala leve por estrella) y configurable: `StarGlowIntensity` (0 = sin brillo, queda solo la base oscura+saturada). Reportado por Maxi en juego.

### v0.14.1 — 2026-08-02 · Estallidos elementales en el impacto
- Cada golpe de un 3★+ ahora **explota visiblemente** en el punto de impacto con el efecto vanilla de su elemento: fuego (`fx_DvergerMage_Fire_hit`), hielo (`vfx_frostarrow_hit`), rayo (`fx_lightningweapon_hit`). Configurable por `StarHitFx` (pares estrellas:prefab). Las víctimas además arden/se congelan por los estados del daño elemental.

### v0.14.0 — 2026-08-02 · Habilidades especiales por estrellas
- **3★ Ígneo**: sus golpes suman **daño de fuego** (30% del daño físico) → quemadura. **4★ Gélido**: **escarcha** → frena al objetivo (el brillo pasó de rojo a **celeste hielo** para coincidir). **5★ Tormenta**: **rayo** + **25% de resistencia física** pasiva.
- Aplica a salvajes y domesticados por igual. Config: `StarElementalPercent` (0.3; 0 = apagado) y `StarFiveResistPercent` (25). Hook: prefix en `Character.ApplyDamage`.

### v0.13.0 — 2026-08-02 · Daño exponencial + visuales dramáticos por estrella
- **Daño exponencial**: vanilla escala lineal (+50% por estrella; un 5★ pega 3,5×). Ahora `factor = StarDamagePerStar ^ estrellas` (default 1,5): 1★ igual vanilla, 3★ 3,4×, 5★ **7,6×**. Aplica a enemigos salvajes y mascotas por igual. Config `StarDamagePerStar` (1 = volver a vanilla).
- **Visuales dramáticos 3★+**: en vez de la extrapolación sutil, base **más oscura y saturada** por estrella con **emisión brillante creciente**: 3★ fuego naranja, 4★ rojo, 5★ violeta. El tamaño sigue la progresión de cada especie.

### v0.12.0 — 2026-08-02 · Baúl de basura
- Pieza nueva del martillo (pestaña **Muebles**): clon del cofre de madera, tintado oscuro, nombre "Baúl de basura". Todo lo que se guarde adentro **se destruye cada 4 s** (config `TrashDelaySeconds`; `TrashChestEnabled` para apagarlo).
- El prefab se registra en runtime (`piece_trashchest_tc`) en ZNetScene + tabla del martillo. **Requiere el mod en server y todos los clientes**: un cliente sin el mod que cargue la zona verá errores de "Missing prefab" — no colocar hasta que todos estén al día.

### v0.11.2 — 2026-08-02 · Estrellas de 3★+ con íconos reales
- El HUD de 3★–5★ ahora muestra una **fila de N íconos de estrella** (clonando la estrellita vanilla, mismo sprite y espaciado) en lugar del texto "★N" junto al nombre.

### v0.11.1 — 2026-08-02 · Fix: prefab correcto de la carne de oso
- El ítem real se llama `BjornMeat` (el oso del Deep North es "Bjorn"); `BearMeat` es otro asset. Default de `ExtraFood` corregido y verificado en el server: "Wolf ahora también come BjornMeat".

### v0.11.0 — 2026-08-02 · Dietas extra (lobos comen carne de oso)
- Config nueva `ExtraFood` en `[General]`: pares `criatura:ítem` que se agregan a la lista de comida del animal. Default: `Wolf:BearMeat` — vanilla excluye explícitamente la carne de oso de la dieta del lobo.
- Sirve para cualquier combinación (ej. `Boar:Turnip`); avisa en el log si el prefab no existe.

### v0.10.0 — 2026-08-02 · Visuales para 3–5 estrellas
- Vanilla define aspecto (escala/tinte/objetos extra) solo para 1★–2★ y el HUD solo tiene íconos para esas dos: un 3★+ se veía como criatura común y sin estrellas — poder invisible.
- **Aspecto**: se extrapola la progresión visual de cada especie (`LevelEffects`): cada estrella extra es más grande (tope 2× el tamaño de 2★) y de tinte más marcado; el objeto extra de 2★ (los cuernos del ciervo) se conserva de ahí en adelante. Config `StarVisualsMaxStars` (5).
- **HUD**: para 3★+ se agrega "★N" al nombre de la criatura al apuntarla (1★–2★ conservan sus íconos vanilla).

### v0.9.0 — 2026-08-02 · Estrellas por combate
- Los animales domesticados pueden **subir una estrella peleando**: **2%** de probabilidad al rematar a un enemigo (`KillStarChance`) y **1%** por asistencia (`AssistStarChance`) — haber dañado al enemigo en los 60 s previos a su muerte (`AssistWindowSeconds`). Tope 5★. Los jugadores y los enemigos entre sí no cuentan.
- Implementación: postfix en `Character.ApplyDamage` (registro de asistentes domesticados por víctima) + `Character.OnDeath` (el `m_lastHit.GetAttacker()` es el que remata; se tiran los dados y se limpia el registro). Config nueva `[Combate]`.

### v0.8.3 — 2026-08-02 · Blindaje contra ítems fantasma
- Un ítem cuyo dueño de red no responde (desync, típico con clientes en versiones distintas) no se puede comer ni levantar; los animales quedaban imantados mordiéndolo eternamente. Diagnóstico: `RemoveOne()` → `CanPickup()` exige ser dueño; `RequestOwn()` nunca se resuelve si el dueño registrado está colgado.
- Fix: si un animal pasa ~30 s al lado de su comida objetivo sin lograr consumirla, la abandona y la marca fantasma; el buscador de comida la ignora 2 minutos (reintenta después por si la propiedad se liberó). El ítem fantasma en sí se limpia al reiniciar el server o relogueando.

### v0.8.2 — 2026-08-01 · Fix: hongos en crecimiento visibles (blancos)
- El estado "sin fruto" usa el mecanismo vanilla de "cosechado"; en el arbusto eso oculta solo las frutas, pero en el hongo oculta **el modelo entero** → el hongo sembrado era invisible durante los 90 min de crecimiento y aparecía de golpe.
- Fix: mientras crece se fuerza visible el modelo (`m_hideWhenPicked` activo; sigue sin poder cosecharse porque `m_picked` bloquea la interacción) y se ve como un **honguito blanco** (tinte hacia blanco 85%, pedido de Maxi) al 30%/50% de tamaño. Los arbustos conservan su tinte verdoso.

### v0.8.1 — 2026-08-01 · Fix: los ciervos vaciaban el stack de comida
- **Causa raíz (heredada de la 0.4.0)**: el ciervo es el único comedor no-Humanoid del juego; la línea vanilla `humanoid.m_consumeItemEffects.Create(...)` lanza NullReference justo después de comer y **antes** de limpiar `m_consumeTarget`. El Finalizer de la 0.4.0 tragaba la excepción sin limpiar: el objetivo quedaba trabado y, como esa rama no chequea hambre, el ciervo mordía el stack completo (5 zanahorias juntas = 5 comidas seguidas, con 5 digestiones encadenadas — parte de la "máquina de caca").
- **Fix**: al tragar la excepción se limpia `m_consumeTarget`. El ciervo come exactamente **1 ítem por ciclo de hambre**, como el chancho. Neck/chancho/lobo nunca tuvieron el problema (son Humanoid).

### v0.8.0 — 2026-08-01 · Siembra por etapas (3 grupos)
Rediseño de la siembra por caca en tres grupos (spec de Maxi):
- **Grupo 1 — Arbustos (frambuesa, arándano)**: la semilla ya no planta un arbusto completo. Nace al **30% de tamaño, con tinte verdoso y sin fruto**, pasa al **50%** y al **100%** en un total de `StagedGrowSeconds` = **5400 s (3 noches de juego, ~90 min reales; mitad en cada etapa)**. Al madurar recupera el color, aparece la **primera fruta** y sigue el ciclo vanilla de respawn (intacto, ~5 h).
- **Grupo 2 — Hongos**: mismo crecimiento por etapas. Además, al madurar **se multiplican**: +1 hongo (60%) o +2 (40%), con tope de **7 por manchón en radio de 4 m**. Los nuevos también nacen al 30% y crecen (colonia progresiva hasta el tope).
- **Grupo 3 — Zanahoria/nabo/cebolla**: sin cambios — plantín vanilla real (~75 min, cosecha única). 
- Implementación: componente `PoopedPlantGrowth` agregado a los prefabs de `StagedPlants`; **solo actúa si el ZDO tiene la marca `tc_planted`** (sembrado por animal) — arbustos y hongos silvestres intactos. El estado sin-fruto usa el sistema vanilla (`s_picked`/`s_pickedTime` + `RPC_SetPicked`); al spawnear se fija `pickedTime = ahora` porque si queda en 0 el respawn vanilla lo retro-data aleatorio y daría fruta antes de tiempo. Escala/tinte los aplica cada cliente leyendo el ZDO (requiere mod en todos los clientes para verse).
- Config nueva en `[Siembra]`: `StagedPlants`, `StagedGrowSeconds`, `StagedStartScale`, `StagedMidScale`, `MushroomPrefabs`, `MushroomChanceOne`, `MushroomMaxInPatch`, `MushroomPatchRadius`.

### v0.7.1 — 2026-08-01 · Tope de densidad de siembra
- **Fix de balance de la 0.7.0**: cada comida plantaba 1–3 semillas (factor de reproducción ~2 por ítem comido) → crecimiento **exponencial** de plantas; verificado en juego: cadena de ciervos comiendo zanahorias y replantándolas sin freno (32 siembras en una sesión).
- **Tope de densidad**: antes de plantar, cada semilla cuenta las plantas/arbustos/pickables en un radio de `PoopDensityRadius` (4 m); si ya hay `PoopMaxPlantsNearby` (10) o más, la semilla se pierde. El corral llega a un equilibrio y la siembra se frena sola. También evita que alfombren las plantaciones del jugador.
- Config nueva en `[Siembra]`: `PoopMaxPlantsNearby` (10; 0 = sin tope), `PoopDensityRadius` (4 m).

### v0.7.0 — 2026-08-01 · Siembra por caca
- **Nuevo `SeedPooper`** (en todos los animales con `Tameable`): al comer un ítem del mapa `PoopMap`, digestión aleatoria de 60–180 s y después "caca" de 1–3 semillas dispersas en un radio de 1,5 m.
- Cada semilla que cae sobre **suelo cultivado** (chequeo `Heightmap.IsCultivated`, el mismo que usan los cultivos del juego) instancia el prefab mapeado con el efecto vanilla de plantado. Las que caen en suelo sin cultivar se pierden.
- Chequeo de espacio: no planta si ya hay una planta o pickable a menos de 0,4 m del punto.
- Mapa por defecto: `Raspberry→RaspberryBush`, `Blueberries→BlueberryBush`, `Mushroom→Pickable_Mushroom`, `Carrot→sapling_carrot`, `Turnip→sapling_turnip`, `Onion→sapling_onion`.
- Config nueva `[Siembra]`: `PoopEnabled`, `PoopMap`, `DigestMinSeconds`, `DigestMaxSeconds`, `PoopSeedsMin`, `PoopSeedsMax`, `PoopScatterRadius`.
- Nota técnica: no existen ítems de semilla vanilla para frutos rojos/hongos, así que la semilla no queda como objeto agarrable — la caca planta directo en el punto de caída.

### v0.6.0 — 2026-08-01 · Regeneración + forrajeo
- **Fix "+0" de vida**: vanilla regenera `vidaMáx / 3600 s` (una hora para llenarse) y **nada** si el animal tiene hambre; en bichos de poca vida el tick es ~0,003 HP y el cartel flotante lo redondea a "+0". Ahora los **domesticados** (todas las especies, también chancho/lobo/lox) curan `RegenPercent` (5%) de la vida máxima cada `RegenIntervalSeconds` (10 s) **fuera de combate** (sin daño recibido ni alerta por 10 s), mínimo 1 HP por tick; con hambre curan la mitad. Los salvajes siguen vanilla. (Patch: `BaseAI.UpdateRegeneration`.)
- **Forrajeo** (`TamedForager`, en todos los animales con `Tameable`): un domesticado **con hambre** y sin alerta busca cada 10 s el arbusto/cultivo (`Pickable`) más cercano en 10 m cuyo fruto esté en su lista de comida, y lo cosecha (RPC_Pick sin jugador). Los ítems caen al piso y la IA vanilla de comer hace el resto.
- **`FedDurationSeconds`** (default 0 = sin cambio): permite ajustar cuánto quedan saciados ciervo/neck. Confirmado por pedido: comen igual que el chancho (1 ítem cada 10 min).

### v0.5.0 — 2026-08-01 · Voces propias + crías bebé
- **Fix sonidos de chancho en ciervo/neck**: la causa era `CopyPublicFields<MonsterAI>` — `m_idleSound`, `m_alertedEffects`, `m_idleSoundInterval` y `m_idleSoundChance` viven en `BaseAI` (clase base) y el copy de campos públicos los traía del Boar. Ahora el ciervo conserva los de su `AnimalAI` original y el neck los suyos. Además se quitan los prefabs `sfx*` del Boar de los efectos copiados de `Tameable` (domesticar/calmar/acariciar) y `Procreation` (amor/parto), conservando los visuales (corazones, humo) y agregando la voz de la especie donde aplica.
- **Crías bebé** (`BabyGrowth`): la recién nacida se marca con timestamp en su ZDO (`tc_babyBorn`). Mientras es bebé: escala ×0,5 y tinte pastel (corrimiento `_Saturation`/`_Value` del shader de criaturas — el mismo mecanismo que usan las estrellas vía `LevelEffects`). Crece sola a los `BabyGrowMinutes` (50 min reales). Los bebés **no crían** hasta crecer. La escala/tinte los aplica cada cliente leyendo el ZDO (Unity no sincroniza `localScale` por red) → **el mod debe estar en server y todos los clientes**.
- Config nueva `[Cria]`: `BabyEnabled`, `BabyGrowMinutes`, `BabyScale`, `BabySaturation`, `BabyBrightness`.

### v0.4.0 — base de Fer (2026-08-01, pack TestServer)
- Ciervos y necks domesticables (componentes copiados del Boar), cría habilitada con crías adultas, mutaciones de estrellas (2% sube / 1% baja), hasta 20 animales por corral, comando seguir/quedarse (E) para Deer/Neck/Boar, huida configurable de criaturas salvajes.
- Este repo reconstruyó el código por decompilación (ilspycmd) del DLL 0.4.0.

## BuildTweaks

### v0.1.0 — 2026-08-01
- Las piezas de la pestaña **Building** del martillo (categoría `BuildingWorkbench`, 108 piezas) no requieren estación de crafteo para construirse, repararse ni demolerse. Solo esa pestaña: Crafting, Furniture y piedra siguen pidiendo su estación.
- Config `NoStationCategories`: lista de categorías (se puede sumar `BuildingStonecutter` para liberar también la piedra).
- Efecto **client-side** (la validación de colocación es del cliente que construye): cada uno decide si lo instala.

## Infraestructura
- **2026-08-01**: server dserver migrado a BepInEx nativo (`BEPINEX=true`, imagen lloesche — sobrevive autoupdates). 8 plugins activos: ActiveZones, BuildTweaks, DeathTweaks, MoreStars, PortalTweaks, ServerRules, ServerWelcome, TameableCreatures. Mundo MaxiFer conservado (backup pre-mods `worlds_local.pre-bepinex-20260801` + zips automáticos cada 6 h).





