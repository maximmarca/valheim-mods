# Changelog — mods MaxiFer Valheim

Registro completo de cambios. Detalle de jugabilidad en [JUGABILIDAD.md](JUGABILIDAD.md).

## TameableCreatures

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
