# Changelog — mods MaxiFer Valheim

Registro completo de cambios. Detalle de jugabilidad en [JUGABILIDAD.md](JUGABILIDAD.md).

## TameableCreatures

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
