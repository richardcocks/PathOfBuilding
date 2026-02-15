# Path of Building .NET Port — Remaining Phases Roadmap

## Status: Phases 1-5 Complete (576 tests, 0 warnings)

The calculation engine core is functional: modifier system, parser, attributes/life/mana/charges, full defence, offence utilities (conversion, damage, speed, crit, hit damage), combat mechanics (conditions, shrines, fortification, rage, buffs, ailments, debuffs), and EHP.

What remains is the **data input layer** that feeds real builds into the calculation engine, plus the **full offence pipeline** that produces DPS numbers.

---

## Phase 6: Build Import — XML Foundation

**Goal:** Load Path of Building XML save files into C# data structures, so we can feed real builds into the calculation engine.

**Why first:** Everything else (items, tree, skills) is stored in the build XML. Having a loader means we can write integration tests against real exported builds.

### Lua Reference
- `src/Modules/Build.lua` (1,947 lines) — save/load orchestration
- `runtime/lua/xml.lua` (187 lines) — XML parser/composer
- `src/Modules/Common.lua` (976 lines) — XML utilities

### XML Schema (top-level)
```xml
<PathOfBuilding>
  <Build level="100" className="Witch" ascendClassName="Elementalist" bandit="None" .../>
  <Config><ConfigSet><Input name="..." boolean/number/string="..."/></ConfigSet></Config>
  <Notes>text</Notes>
  <Tree activeSpec="1"><Spec classId="3" nodes="7388,4184,..." treeVersion="3_22"/></Tree>
  <Items><Item id="1">[raw item text]</Item><ItemSet><Slot name="..." itemId="..."/></ItemSet></Items>
  <Skills><SkillSet><Skill slot="..." enabled="true"><Gem nameSpec="..." level="21" .../></Skill></SkillSet></Skills>
</PathOfBuilding>
```

### New Files
```
Data/
  BuildData.cs              — Top-level build container
  BuildMetadata.cs          — Level, class, ascendancy, bandit, pantheon
  ItemSetData.cs            — Item slot assignments
  SkillSetData.cs           — Socket groups + gem instances
  TreeSpecData.cs           — Allocated nodes, mastery selections, jewel sockets
  ConfigSetData.cs          — Config inputs (bool/number/string discriminated union)

Import/
  BuildXmlLoader.cs         — Parse XML → BuildData (using System.Xml.Linq)
  BuildXmlSaver.cs          — BuildData → XML (for round-trip)
  BuildCodec.cs             — Base64+Deflate encode/decode for build codes
```

### Key Design Decisions
- Use `System.Xml.Linq` (XDocument) — no need for custom XML parser
- `ConfigInput` as discriminated union: `bool | double | string`
- Raw item text stored as strings (parsed later by Item system)
- Gem instances stored as flat records (resolved later by Skill system)
- Tree nodes stored as `HashSet<int>` (resolved later by Tree system)
- Build codes: `Deflate(xml) → Base64 → URL-safe` (System.IO.Compression + Convert.ToBase64String)

### Tests (~40)
- Load minimal build XML → BuildData populated
- Load each section independently (Config, Items, Skills, Tree, Notes)
- Round-trip: save → load → compare
- Build code encode/decode round-trip
- Missing sections handled gracefully (defaults)
- Multiple ItemSets/SkillSets/ConfigSets
- Real build file integration test (from spec/TestBuilds/)

### Estimate
~8 files, ~1200 LOC source, ~600 LOC tests

---

## Phase 7: Items — Parsing, Slots, and Mod Application

**Goal:** Parse item text into structured data, calculate local mods, and apply item mods to ModDB.

**Why second:** Items are the simplest data-to-ModDB pipeline (text → parse → mods). The ModParser from Phase 2 already handles most mod text parsing.

### Lua Reference
- `src/Classes/Item.lua` (1,831 lines) — item parsing, BuildModList, local mod calculation
- `src/Modules/ItemTools.lua` (169 lines) — utilities (influence, range application)
- `src/Data/Bases/` (22 files, ~10,937 lines) — item base definitions
- `src/Data/ModItem.lua` (11,475 lines) — affix database (optional, for crafting)

### Core Data Structures

**ItemBase** — base type definition:
```csharp
record ItemBase(string Name, string Type, int SocketLimit,
    Dictionary<string, bool> Tags, string? Implicit,
    WeaponData? Weapon, ArmourData? Armour, FlaskData? Flask,
    ItemRequirements Req);
```

**Item** — parsed item:
```csharp
class Item {
    int Id; string Name; string BaseName; ItemBase Base;
    ItemRarity Rarity; int Quality;
    List<ModLine> ImplicitMods, ExplicitMods, EnchantMods, CraftedMods;
    List<ItemSocket> Sockets;
    ModList ModList;                    // Global mods (for non-weapons)
    ModList[]? SlotModList;             // Per-slot mods (weapons, rings)
}
```

**ItemSlot** — equipment slots:
```csharp
enum ItemSlotId { Weapon1, Weapon2, Helmet, BodyArmour, Gloves, Boots,
    Belt, Amulet, Ring1, Ring2, Flask1..Flask5, Jewel1..Jewel21 }
```

### New Files
```
Data/
  ItemBase.cs               — Base type records (WeaponData, ArmourData, FlaskData)
  ItemBaseData.cs           — Static dictionary of all base types (from Bases/*.lua)
  ItemRarity.cs             — Enum: Normal, Magic, Rare, Unique, Relic
  ItemSlotId.cs             — Enum for all equipment slots

Items/
  Item.cs                   — Item class with ParseRaw(), BuildModList()
  ItemParser.cs             — Text parsing: rarity, base detection, mod lines, sockets
  ItemLocalMods.cs          — Extract local weapon/armour mods, apply quality
  ItemSlotManager.cs        — Maps items to slots, handles weapon swap
  ModLine.cs                — Parsed mod line (text, ModList, range, flags)

Calculation/
  CalcItems.cs              — Apply item mods to actor ModDB (from CalcSetup.lua ~665-1000)
```

### Key Challenges
- **Raw text parsing:** Items come as multi-line text with sections separated by `--------`
- **Local vs global mods:** Local mods (e.g., "+% physical damage" on a weapon) modify the item's base stats, not the global ModDB
- **Quality scaling:** Different quality types scale different stats
- **Weapon dual-slot:** Weapons produce separate ModLists for MainHand/OffHand
- **Template replacement:** `{SlotName}` → "MainHand"/"OffHand" in mod stat names

### Item Base Data Strategy
Convert the 22 Lua base files to a single C# static class with a `Dictionary<string, ItemBase>`. This is ~400 base types. Can be auto-generated or hand-ported (the data is stable).

### Tests (~60)
- Parse raw item text (Normal/Magic/Rare/Unique) → correct fields
- Socket parsing ("R-R-G B" → 4 sockets, 2 groups)
- Implicit/explicit/crafted mod line categorization
- Quality application (weapon physical, armour defences)
- Local mod extraction (weapon damage, armour values)
- Global mod application to ModDB
- Weapon slot mod lists (MainHand vs OffHand)
- Template replacement in stat names
- Range application (0.0-1.0 → min/max values)
- Base type lookup by name
- Item from BuildXmlLoader integration

### Estimate
~10 files, ~2500 LOC source, ~1200 LOC tests

---

## Phase 8: Passive Tree — Node Data, Allocation, Mod Application

**Goal:** Load passive tree data, represent allocated nodes, and apply passive mods to ModDB.

### Lua Reference
- `src/Classes/PassiveTree.lua` (988 lines) — tree loading, node processing
- `src/Classes/PassiveSpec.lua` (2,123 lines) — allocation, cluster jewels
- `src/TreeData/3_22/tree.lua` (136K+ lines) — full tree data
- `src/Data/ClusterJewels.lua` (1,050 lines) — cluster jewel templates

### Tree Data Loading Strategy
The tree data is a massive JSON (from GGG's API). Options:
1. **Ship as embedded JSON resource** — parse at startup (~2MB compressed)
2. **Pre-convert to C# static data** — faster load, larger binary
3. **Download on first use** — smallest distribution

Recommend option 1: embed JSON, deserialize with `System.Text.Json` into typed records.

### Core Data Structures

**PassiveNode:**
```csharp
record PassiveNode(int Id, string Name, PassiveNodeType Type,
    string[] Stats, int GroupId, int Orbit, int OrbitIndex,
    string? AscendancyName, int[]? LinkedNodes,
    MasteryEffect[]? MasteryEffects);
```

**PassiveTree:**
```csharp
class PassiveTree {
    Dictionary<int, PassiveNode> Nodes;
    Dictionary<string, int> KeystoneMap;      // name → nodeId
    Dictionary<int, PassiveGroup> Groups;
    PassiveClass[] Classes;
    string Version;
}
```

**PassiveSpec (allocation):**
```csharp
class PassiveSpec {
    PassiveTree Tree;
    HashSet<int> AllocatedNodes;
    Dictionary<int, int> MasterySelections;   // nodeId → effectId
    Dictionary<int, int> JewelSockets;        // nodeId → itemId
    int ClassId, AscendClassId;
}
```

### New Files
```
Data/
  PassiveNode.cs            — Node record + PassiveNodeType enum
  PassiveTree.cs            — Tree container, JSON deserialization
  PassiveSpec.cs            — Allocation state
  PassiveTreeLoader.cs      — Load tree from embedded JSON resource
  MasteryEffect.cs          — Mastery effect record (id, stats)

Calculation/
  CalcTree.cs               — BuildModListForNodeList(): iterate allocated nodes,
                               parse stats via ModParser, add to ModDB
                               Keystone handling (LIST mod pattern)
                               Source tagging ("Tree:<nodeId>")
```

### Key Design Points
- **Mod parsing reuse:** Node stat lines (`node.sd`) are plain text, parsed by existing `ModParser` from Phase 2
- **Keystones:** Added as `ModType.List` mods with the keystone name, then merged recursively (enables conditional mod chains)
- **Masteries:** When a mastery is selected, replace the node's stats with the selected effect's stats
- **Source tracking:** All tree mods tagged with `"Tree:<nodeId>"` source for breakdowns
- **Cluster jewels deferred:** Synthetic subgraph generation is complex and can be Phase 8b. Basic tree works without them.

### Deferred to Phase 8b
- Cluster jewel subgraph generation
- Timeless jewel transformations
- Tattoo passives
- Jewel radius effects (affecting unallocated nodes)
- Anoint support

### Tests (~45)
- Load tree JSON → correct node count, groups, classes
- Keystone map populated correctly
- Allocate nodes → mods added to ModDB (spot-check known nodes)
- Mastery selection → correct stats applied
- Ascendancy nodes gated on ascendancy class
- BuildModListForNodeList integration with real tree data
- Keystone adds LIST mod, mergeKeystones resolves it
- Source tags present on all tree mods
- From TreeSpecData (Phase 6) → allocated nodes resolved
- Empty allocation → no mods added

### Estimate
~7 files, ~1500 LOC source, ~800 LOC tests. Tree JSON resource: ~2MB embedded.

---

## Phase 9: Skill System — Gems, Socket Groups, Active Skills

**Goal:** Create active skills from socket groups, link support gems, build skill mod lists, and set up the skill configuration needed by the offence pipeline.

**Why after items+tree:** Skills reference item slots (for socket groups) and need the ModDB populated with item/tree mods for proper support gem filtering.

### Lua Reference
- `src/Modules/CalcActiveSkill.lua` (892 lines) — createActiveSkill, buildActiveSkillModList
- `src/Modules/CalcSetup.lua` (1,760 lines) — skill setup portion (~lines 1281-1729)
- `src/Data/Gems.lua` (14,786 lines) — gem database (~800+ gems)
- `src/Classes/SkillsTab.lua` (1,345 lines) — socket group management
- `src/Data/Skills/` — per-gem granted effect definitions

### Gem Data Strategy
`Gems.lua` has ~800 gem entries with metadata (name, tags, requirements). The actual skill mechanics (statMap, baseMods, skillTypes) are in separate per-gem files under `src/Data/Skills/`. Strategy:
1. Convert `Gems.lua` to a C# static dictionary (gem metadata)
2. Convert key skill definitions (top ~50 popular skills) to C# data
3. Defer exhaustive skill coverage — focus on the framework

### Core Data Structures

**GemDefinition:**
```csharp
record GemDefinition(string Id, string Name, string BaseTypeName,
    string GrantedEffectId, Dictionary<string, bool> Tags,
    int ReqStr, int ReqDex, int ReqInt, int NaturalMaxLevel);
```

**GrantedEffect (skill mechanics):**
```csharp
class GrantedEffect {
    string Id, Name;
    bool IsSupport;
    HashSet<SkillType> SkillTypes;
    SkillFlags BaseFlags;
    Dictionary<string, StatMapEntry> StatMap;
    ModList BaseMods;
    SkillPart[]? Parts;                 // Multi-part skills
    LevelStats[] Levels;                // Per-level stat values
}
```

**ActiveSkill:**
```csharp
class ActiveSkill {
    GrantedEffect ActiveEffect;
    List<GrantedEffect> SupportList;
    Actor Actor;
    SocketGroupData SocketGroup;
    SkillData SkillData;                // Calculated parameters
    ModList SkillModList;               // Merged mods from gem+supports+items
    ModConfig SkillCfg;                 // Config for mod queries
    SkillFlags SkillFlags;
    int SkillPart;
    string? SlotName;
}
```

**SocketGroup:**
```csharp
class SocketGroup {
    bool Enabled;
    string? Slot;
    string? Label;
    int MainActiveSkill;
    List<GemInstance> GemList;
    bool IncludeInFullDPS;
}
```

### New Files
```
Data/
  GemDefinition.cs          — Gem metadata record
  GemData.cs                — Static dictionary of all gems (from Gems.lua)
  GrantedEffect.cs          — Skill mechanics (statMap, baseMods, skillTypes)
  SkillFlags.cs             — [Flags] enum: Hit, Attack, Spell, Projectile, Area, etc.
  SkillPart.cs              — Multi-part skill definition
  LevelStats.cs             — Per-level/quality stat values

Skills/
  ActiveSkill.cs            — Active skill class
  SocketGroup.cs            — Socket group with gem instances
  GemInstance.cs            — Individual gem in a socket group
  SkillData.cs              — Calculated skill parameters (cast time, cooldown, etc.)
  SupportValidator.cs       — canGrantedEffectSupportActiveSkill logic

Calculation/
  CalcSkills.cs             — CreateActiveSkill, BuildActiveSkillModList,
                               MergeSkillInstanceMods (stat scaling from level/quality)
  CalcSkillSetup.cs         — Iterate socket groups, resolve gems, create active skills,
                               determine mainSkill
```

### Key Challenges
- **Two-pass support validation:** First pass propagates skill types from supports, second pass builds the final effect list. Must iterate until stable.
- **Stat scaling:** Gem level/quality → stats → statMap lookup → mod scaling with mult/div/base
- **Skill configuration:** Each active skill gets its own `ModConfig` with flags and keyword flags for proper mod filtering
- **Weapon configs:** Attack skills need per-weapon configs (weapon1Cfg, weapon2Cfg) with weapon-specific flags
- **Granted effect data:** The per-gem skill files are large and varied. Start with a framework + a handful of concrete skills.

### Deferred
- Exhaustive skill definitions (only port framework + ~50 popular skills initially)
- Minion skills (recursive skill lists)
- Trigger mechanics (Cast on Crit, CWDT, etc.)
- Vaal skill variants
- Awakened support gems

### Tests (~55)
- GemData lookup by ID and name
- CreateActiveSkill with active + supports
- Support validation: compatible support added, incompatible rejected
- Two-pass skill type propagation
- MergeSkillInstanceMods: level scaling applied correctly
- SkillFlags computed from granted effect + supports
- SkillCfg flags match skill type (attack/spell/projectile)
- SocketGroup → ActiveSkill creation
- MainSkill selection from socket group index
- Weapon config generation for attack skills
- BuildActiveSkillModList: gem + support + item mods merged
- Disabled gem excluded from active skill list
- Multi-part skill: correct part selected
- Integration: SocketGroup → ActiveSkill → SkillModList queryable

### Estimate
~14 files, ~3000 LOC source, ~1200 LOC tests. Gem data: ~2000 LOC (auto-generated).

---

## Phase 10: Full Offence Calculation

**Goal:** Port the complete DPS pipeline from `CalcOffence.lua` (5,873 lines), producing hit damage, attack/cast speed, crit, ailment DPS, and combined DPS.

**Why last:** Depends on ActiveSkill, skill mod lists, weapon data, and the full ModDB from items+tree+skills.

### Lua Reference
- `src/Modules/CalcOffence.lua` (5,873 lines) — single `calcs.offence()` function

### Pipeline Sections

| Section | Lua Lines | Description |
|---------|-----------|-------------|
| Input validation | 320-339 | Check disabled, early return |
| AoE + Flask | 341-820 | Area of effect, flask scaling, special conversions |
| Weapon/Pass setup | 1895-2070 | MainHand/OffHand/Skill pass configuration |
| Hit chance | 2082-2163 | Accuracy vs evasion, enemy block |
| Attack/Cast speed | 2165-2700 | Base time, speed mods, triggers, seals |
| Crit chance + mult | 2826-3046 | Crit calculation, crit multiplier |
| Base hit damage | 3050-3087 | Per-type base + added damage |
| Hit damage calc | 3100-3468 | Crit/non-crit passes, conversions, resist, pen |
| Average damage + DPS | 3469-3482 | Weighted average, speed multiplier |
| MH/OH combination | 1963-2070 | Dual wield stat merging |
| Leech | 3751-3805 | Life/mana/ES leech from damage |
| Ailments: Bleed | 4058-4330 | Bleed chance, damage, duration, stacks |
| Ailments: Poison | 4331-4608 | Poison with 8-stage DoT |
| Ailments: Ignite | 4609-4950 | Ignite DPS, weighted stacks |
| Ailments: Other | 4951-5214 | Freeze, chill, shock, brittle, scorch |
| Misc effects | 5132-5319 | Knockback, stun, impale, decay |
| DoT components | 5388-5721 | Skill DoT, ground effects |
| Combined DPS | 5722-5872 | Sum all sources, cull, reservation |

### Incremental Approach
This is too large for a single phase. Split into sub-phases:

**Phase 10a: Hit Damage + Speed + Crit (~1500 LOC)**
- Pass setup (MainHand/OffHand/Skill)
- Hit chance (accuracy vs evasion)
- Attack/cast speed
- Critical strike chance and multiplier
- Base hit damage per type
- Hit damage with conversion, resist, pen
- Average damage and TotalDPS
- Dual wield combination

**Phase 10b: Ailments + DoT (~1500 LOC)**
- Bleed DPS (chance, damage, duration, stacks)
- Poison DPS (8-stage, stack potential)
- Ignite DPS (weighted stacks)
- Non-damaging ailments (freeze, chill, shock, brittle, scorch)
- Skill-specific DoT
- Ground effect DoT

**Phase 10c: Combined DPS + Misc (~800 LOC)**
- Leech calculations
- Impale DPS
- Knockback, stun
- Combined DPS summation
- Cull multiplier
- FullDPS across multiple skills
- AoE calculations

### New Files
```
Calculation/
  CalcOffence.cs            — Main calcs.offence() port
  CalcOffenceHit.cs         — Hit damage, speed, crit (Phase 10a)
  CalcOffenceAilment.cs     — Ailments, DoT (Phase 10b)
  CalcOffenceMisc.cs        — Leech, impale, combined DPS (Phase 10c)
  OffencePassConfig.cs      — MainHand/OffHand/Skill pass data
  DualWieldCombiner.cs      — combineStat() logic (OR/ADD/AVERAGE/HARMONIC)
```

### Key Algorithms
- **Damage conversion chain:** Recursive Physical → Lightning → Cold → Fire → Chaos
- **Two-pass crit/non-crit:** Calculate damage twice (crit and non-crit), weight by crit chance
- **Dual wield combination:** Multiple modes (OR, ADD, AVERAGE, HARMONICMEAN, CHANCE)
- **Ailment stack potential:** `HitChance × AilmentChance × Speed × Duration / MaxStacks`
- **Ignite weighted average:** Top-N stacks weighted by roll position
- **Lucky/Unlucky rolls:** E[(max of 2 rolls)] vs E[(min of 2 rolls)]

### Already Ported (Phase 4)
These helper functions are already available:
- `CalcDamage.CalcDamageMinMax` — recursive damage with conversion
- `CalcSpeed.CalcSkillCooldown` — cooldown with server tick rounding
- `CalcSpeed.CalcSkillDuration` — duration with modifiers
- `CalcHitDamage.ApplyResistAndPen` — resistance penetration
- `CalcHitDamage.EffectiveCritChance` — crit chance calculation
- `CalcHitDamage.CritMultiplier` — crit multiplier
- `CalcHitDamage.LuckyDamage` — lucky/unlucky damage rolls
- `CalcHitDamage.DoubleDamageEffect` — double damage multiplier
- `ConversionTable` — damage conversion/gain table builder

### Tests (~120)
**Phase 10a (~50):**
- Pass setup: spell (single pass), attack (MH+OH), two-hand (MH only)
- Hit chance: accuracy vs evasion formula, enemy block
- Speed: attack from weapon, cast from skill, trigger rate
- Crit: base + modifiers, crit multiplier, crit effect
- Base damage: weapon + added, spell + added, damage effectiveness
- Hit damage: conversion chain, resist + pen, more/less modifiers
- Average damage: weighted crit/non-crit
- DPS: damage × speed
- Dual wield: all combination modes

**Phase 10b (~40):**
- Bleed: chance, physical base, duration, stacks, moving bonus
- Poison: chance, multi-type base, duration, 8-stage calc
- Ignite: chance, fire base, duration, weighted stacks
- Non-damaging: freeze/chill/shock thresholds and effects
- Skill DoT: base damage, duration, area overlap

**Phase 10c (~30):**
- Leech: life/mana/ES amounts, instant vs over-time, caps
- Impale: chance, stored damage, stack multiplier
- Combined DPS: sum of all sources
- Cull multiplier application
- FullDPS across multiple active skills
- Integration: full skill → DPS pipeline

### Estimate
~6 files, ~3500 LOC source, ~2000 LOC tests

---

## Phase 11: Integration — End-to-End Build Calculation

**Goal:** Wire everything together: load a build XML → parse items/tree/skills/config → populate ModDB → run full calculation pipeline → produce all output numbers.

### New Files
```
Calculation/
  BuildCalculator.cs        — Orchestrator: load build → calculate → return outputs
  CalcEnvironment.cs        — Calculation environment (replaces Lua's env table)
```

### CalcEnvironment
Replaces Lua's `env` table — holds all state for a single calculation run:
```csharp
class CalcEnvironment {
    BuildData Build;
    Actor Player;
    Actor Enemy;
    List<ActiveSkill> ActiveSkillList;
    ActiveSkill? MainSkill;
    Dictionary<string, Item> EquippedItems;
    PassiveSpec Spec;
    string Mode;  // "MAIN" or "CALCS"
}
```

### Calculation Pipeline (full)
```
BuildCalculator.Calculate(BuildData build)
  1. InitEnvironment(build)
     - Create Player + Enemy actors
     - InitModDB for both
  2. CalcItems.ApplyItems(env)
     - Parse items from raw text
     - BuildModList for each
     - Merge into player.ModDB
  3. CalcTree.ApplyTree(env)
     - Resolve allocated nodes
     - Parse node stats → mods
     - Merge into player.ModDB
     - Merge keystones
  4. CalcSkillSetup.SetupSkills(env)
     - Resolve gems from SkillSets
     - Create active skills
     - Build skill mod lists
     - Determine main skill
  5. CalcPerform pipeline:
     - DoActorAttributes()
     - CombatConditions() + ShrineBuffs()
     - DoActorLifeMana() + DoActorLifeManaReservation()
     - DoActorCharges()
     - DoActorMisc()
     - Defence()
     - BuildDefenceEstimations()
  6. CalcOffence pipeline:
     - For each active skill: calcs.offence()
     - Aggregate FullDPS
  7. Return player.Output
```

### Tests (~30)
- End-to-end: load test build XML → TotalDPS > 0, Life > 0, EHP > 0
- Compare outputs against known Lua values for specific builds
- Config changes propagate (e.g., enemy boss → lower DPS from curses)
- Multiple skill sets → correct main skill selected
- Item swap → recalculate correctly

### Estimate
~2 files, ~500 LOC source, ~600 LOC tests

---

## Summary Table

| Phase | Name | New Files | Est. Source LOC | Est. Test LOC | Cumulative Tests |
|-------|------|-----------|-----------------|---------------|------------------|
| 6 | Build Import (XML) | ~8 | ~1,200 | ~600 | ~616 |
| 7 | Items | ~10 | ~2,500 | ~1,200 | ~876 |
| 8 | Passive Tree | ~7 | ~1,500 | ~800 | ~921 |
| 9 | Skill System | ~14 | ~3,000 | ~1,200 | ~976 |
| 10a | Offence: Hit+Speed+Crit | ~3 | ~1,500 | ~800 | ~1,026 |
| 10b | Offence: Ailments+DoT | ~2 | ~1,500 | ~700 | ~1,096 |
| 10c | Offence: Combined DPS | ~1 | ~800 | ~500 | ~1,126 |
| 11 | Integration | ~2 | ~500 | ~600 | ~1,156 |
| **Total** | | **~47** | **~12,500** | **~6,400** | **~1,156** |

---

## Dependency Graph

```
Phase 6 (XML Import) ──→ Phase 7 (Items) ──→ Phase 11 (Integration)
                     ──→ Phase 8 (Tree)  ──→ Phase 11
                     ──→ Phase 9 (Skills) ──→ Phase 10 (Offence) ──→ Phase 11
```

Phases 7, 8, 9 can be developed in parallel after Phase 6.
Phase 10 requires Phase 9 (active skills).
Phase 11 requires all previous phases.

---

## Deferred (Future Phases)

These are intentionally out of scope for the core port:

- **Cluster jewel subgraph generation** — dynamic synthetic nodes
- **Timeless jewel transformations** — per-seed node replacement
- **Tattoo passives** — node replacement effects
- **Jewel radius effects** — affecting unallocated nodes near sockets
- **Minion recursive skills** — minions with their own skill lists
- **Trigger mechanics** — CWDT, CWC, Cast on Crit
- **Exhaustive skill definitions** — all ~800 gems (start with ~50)
- **Guard skills / aegis detail** in EHP
- **Ally-taken-before-you** — frost shield, spectres, soul link
- **PvP damage formula** — special non-linear scaling
- **UI (Avalonia)** — visual tree, item editor, skill config, DPS display
- **Character import from GGG API** — OAuth + API integration
- **Item crafting simulation** — affix rolling, fossil crafting
