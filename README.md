# valheim-mods — MaxiFer

Mods BepInEx del server **MaxiFer Valheim** (dserver, `maxi-ksp.duckdns.org:2456`).
Mantenido por Maxi + Claude; código original de TameableCreatures por Fer.

📖 **[JUGABILIDAD.md](JUGABILIDAD.md)** — cómo se juega con cada modificación, en detalle.
📋 **[CHANGELOG.md](CHANGELOG.md)** — registro completo de cambios por versión.

## Mods

### TameableCreatures (v0.12.0)
Ciervos y necks domesticables, con cría, mutaciones de estrellas y cuidado automático.

- **Domesticación**: Deer y Neck se doman como chanchos (1800 s, misma comida). Obedecen seguir/quedarse (E), igual que Boar.
- **Cría**: crían como los chanchos (hasta 20 por corral). Mutaciones: 2% de nacer con una estrella más que el mejor padre, 1% con una menos.
- **Crías bebé** *(v0.5.0)*: nacen a mitad de tamaño y color pastel, crecen solas a los 50 min reales. No crían hasta crecer. La escala/tinte lo aplica cada cliente leyendo el ZDO → **el mod debe estar en server y en todos los clientes**.
- **Voces propias** *(v0.5.0)*: fix — el ciervo y el neck heredaban los sonidos del Boar (`m_idleSound`/`m_alertedEffects` viven en BaseAI y el copy de campos públicos los traía). Ahora conservan su voz en idle, alerta, caricias, amor y parto; los efectos visuales (corazones, humo) quedan.
- **Regeneración** *(v0.6.0)*: todos los animales domesticados curan 5% de la vida máxima cada 10 s fuera de combate (mínimo 1 HP; con hambre, la mitad). Vanilla era vida completa en 1 hora y nada con hambre — por eso se veía "+0".
- **Forrajeo** *(v0.6.0)*: los domesticados con hambre cosechan solos arbustos/cultivos cuyo fruto esté en su lista de comida (radio 10 m) y comen lo que cae. Solo cosechan con hambre (1 ítem por ciclo de 10 min, igual que el chancho).
- **Siembra por caca** *(v0.7.0–0.8.2)*: al comer un ítem del mapa `PoopMap`, tras 1–3 min de digestión el animal hace caca 1–3 semillas; las que caen sobre **suelo cultivado** plantan. Arbustos y hongos nacen al 30% (verdosos / honguitos blancos) y maduran en 3 noches; los hongos se multiplican (tope 7 en 4 m); verduras = plantín vanilla. Tope de densidad general (10 en 4 m) contra plagas. Ciclo cerrado: corral cultivado → comen → siembran → crece → vuelven a comer.
- **Estrellas por combate** *(v0.9.0)*: 2% de subir estrella al rematar un enemigo, 1% por asistir; tope 5★.
- **Visuales 3★–5★** *(v0.10.0–0.11.2)*: cada estrella agranda al bicho y le intensifica el tinte, con fila de íconos de estrella reales en el HUD.
- **Dietas extra** *(v0.11.x)*: `ExtraFood` con pares criatura:ítem; default los lobos comen carne de oso (`BjornMeat`).
- **Baúl de basura** *(v0.12.0)*: pieza nueva del martillo (Muebles) que destruye lo que guardes adentro a los ~4 s.

Config: `BepInEx/config/fer.valheim.tameablecreatures.cfg` — secciones `[General]`, `[Comportamiento]`, `[Cria]`, `[Cuidado]`, `[Siembra]`, `[Combate]`.

### BuildTweaks (v0.1.0)
Las piezas de la pestaña **Building** del martillo no requieren mesa de trabajo para construirse (solo esa pestaña; Crafting/Furniture siguen igual). Efecto local de cada cliente — instalalo si lo querés. Config: `NoStationCategories` (se puede sumar `BuildingStonecutter` para la piedra).

## Instalación

**Cliente**: copiar `dist/TameableCreatures.dll` (y opcionalmente `dist/BuildTweaks.dll`) en `<Valheim>\BepInEx\plugins\`, reemplazando el existente. Requiere BepInEx ya instalado (el del pack del server).

**Server**: el de dserver se actualiza solo desde acá (lo hace Maxi/Claude); los DLLs van a `data/bepinex/BepInEx/plugins/`.

## Compilar

```
dotnet build TameableCreatures/TameableCreatures.csproj -c Release -p:ValheimDir="C:\ruta\a\Valheim"
dotnet build BuildTweaks/BuildTweaks.csproj -c Release -p:ValheimDir="C:\ruta\a\Valheim"
```

`ValheimDir` debe apuntar a una instalación de Valheim con BepInEx (usa sus DLLs como referencias). Default: `E:\Steam\steamapps\common\Valheim`.

## Changelog

Resumen — detalle completo en [CHANGELOG.md](CHANGELOG.md):

| Versión | Cambios |
|---|---|
| 0.14.0 | Habilidades por estrellas: 3★ fuego, 4★ escarcha (brillo celeste), 5★ rayo + resistencia |
| 0.13.0 | Daño exponencial por estrella (5★ = 7,6×) + visuales oscuro/brillante |
| 0.12.0 | Baúl de basura: pieza nueva que destruye lo que guardes adentro |
| 0.11.2 | Íconos de estrella reales en el HUD para 3★–5★ (antes texto "★N") |
| 0.11.1 | Fix prefab carne de oso: el ítem real es `BjornMeat` |
| 0.11.0 | Dietas extra por config (`ExtraFood`): los lobos ahora comen carne de oso |
| 0.10.0 | Visuales 3★–5★: más grandes y de tinte más intenso + "★N" en el HUD (antes eran invisibles) |
| 0.9.0 | Estrellas por combate: 2% al matar / 1% por asistir, tope 5★ (`[Combate]`) |
| 0.8.3 | Blindaje: los animales abandonan ítems fantasma (desync) en vez de morderlos eternamente |
| 0.8.2 | Fix: hongos en crecimiento eran invisibles — ahora se ven como honguitos blancos no cosechables |
| 0.8.1 | Fix: los ciervos comían el stack completo de un saque (NRE tragada dejaba el target de comida trabado) |
| 0.8.0 | Siembra por etapas: arbustos/hongos nacen al 30% sin fruto y crecen en 3 noches; hongos se multiplican (tope 7 en 4 m) |
| 0.7.1 | Tope de densidad de siembra (≥10 plantas en 4 m = la semilla se pierde) — corta la plaga exponencial |
| 0.7.0 | Siembra por caca: al comer, digestión 1–3 min y 1–3 semillas que plantan en suelo cultivado (`[Siembra]`) |
| 0.6.0 | Regeneración fuera de combate + forrajeo de arbustos/cultivos + config `FedDurationSeconds` (default: igual que el chancho) |
| 0.5.0 | Fix voces (no más sonidos de chancho en ciervo/neck) + crías bebé pastel + BuildTweaks |
| 0.4.0 | Versión original de Fer (base de este repo, reconstruida por decompilación) |


