# Comet Foundry Technical Gap List

Last updated: 2026-08-26

This document records the current technical gaps found by comparing the project state against the Comet Foundry roadmap and GDD.

## Overall Assessment

Current implementation is past the pure planning stage. The runtime assembly builds successfully, and there are prototype systems for comet generation, grid placement, facilities, conveyors, drone logistics, market prices, exchange requests, research, and power.

The main gap is not the absence of isolated systems. The main gap is that the systems are not yet joined into one continuous playable loop.

Target loop:

1. Discover or select a comet.
2. Place extraction and production facilities.
3. Mine finite resources.
4. Move resources through conveyors or drones.
5. Process goods through recipes.
6. Store, sell, or fulfill requests.
7. Earn credits and trust.
8. Unlock expansion through research, power, and new facilities.

## Missing Or Incomplete Technologies

### 1. Unified Gameplay Loop

Status: high priority gap.

- World prototype and construction prototype are still separate scenes.
- Comet selection does not yet flow into comet interior construction.
- Facility production, logistics, economy, and requests are not yet experienced as one player-facing loop.
- `CometFoundryGame` advances market events, market prices, and requests, but does not yet coordinate full facility/resource/logistics simulation.

Needed:

- Scene or controller flow from world map to selected comet grid.
- Shared game state between world, construction, production, logistics, and economy.
- A minimal playable objective, such as produce iron, sell it, and fund the next facility.

### 2. Real Facility Data And Recipe Assets

Status: high priority gap.

- Facility categories exist in `FacilityCatalog`, but most are prototype definitions.
- Generated facility art exists for an ore drill and furnace, but these are not yet a complete asset pipeline.
- Production stages are planned, but the actual early chain still needs stable data.

Needed:

- Resource assets: iron ore, copper ore, ice, iron, steel, frame, machine parts, consumer goods.
- Recipe assets for mining, refining, processing, assembly, and sale preparation.
- Facility definitions connected to recipes, power use, capacity, footprint, inputs, and outputs.
- Unity-authored ScriptableObject assets rather than temporary runtime-only prototype data.

### 3. Resource Extraction And Depletion

Status: medium-high priority gap.

- Finite deposits and inventory primitives exist.
- The full mining process from resource node to facility output is not yet represented as a normal gameplay operation.

Needed:

- Mining facilities linked to comet deposits.
- Extraction rate, depletion, and blocked/empty deposit behavior.
- Rules for raw resources entering facility inventory or nearby logistics.
- Visual/debug feedback for remaining deposit amounts.

### 4. Production Chain Execution

Status: medium-high priority gap.

- `FacilityInstance` has production responsibilities, but the current construction prototype uses simplified test-item logic.
- The real recipe chain is not yet the primary gameplay path.

Needed:

- Facility input/output ports or directional rules.
- Recipe selection and active recipe state.
- Input consumption, production time, output creation, and stall conditions.
- Production tick integration into the main simulation loop.
- Tests that cover multi-step chains across multiple facilities.

### 5. Logistics Integration

Status: medium-high priority gap.

- Conveyor path placement and handoff behavior exist in the construction prototype.
- Drone logistics and route systems have tests.
- These systems are not yet fully joined to real inventories and production facilities in the playable scene.

Needed:

- Conveyor segments moving actual resource stacks.
- Facility input/output interaction using real inventory resources.
- Drone hangar assignment and drone haul requests connected to facility needs.
- Route priority, blocked path handling, and storage/market delivery as live gameplay.
- Visual representation for cargo, drones, and route state.

### 6. Storage And Warehouse Behavior

Status: medium priority gap.

- Inventory supports capacity and stack limits.
- Warehouse facility categories exist.
- Dedicated warehouse gameplay is not yet complete.

Needed:

- Warehouse input/output rules.
- Storage routing behavior.
- Capacity overflow and backpressure behavior.
- Player-facing storage inspection UI.

### 7. Economy, Market, And Request Fulfillment Integration

Status: medium priority gap.

- Market price, market event, trust, and request systems exist and have tests.
- They are not yet fully connected to produced goods in an end-to-end playable loop.

Needed:

- Selling terminal or market terminal behavior.
- Delivery of actual goods to exchange requests.
- Request completion UI and reward feedback.
- Market demand categories tied to item categories and production planning.
- Construction cost reduction from self-produced materials.

### 8. Power Network Gameplay

Status: medium priority gap.

- Power output and consumption rules have tests.
- Power is not yet a strong gameplay constraint in the prototype scenes.

Needed:

- Power plants as placeable facilities.
- Grid-level power report shown to the player.
- Underpowered facility behavior.
- Power connection or coverage rules if desired.

### 9. Research Unlock Flow

Status: medium priority gap.

- Research definitions and a research system exist.
- There is no complete research tree screen or unlock-driven facility/content progression yet.

Needed:

- Research point generation or lab behavior.
- Research tree data.
- Unlock effects for facilities, recipes, logistics rules, and efficiency upgrades.
- UI for choosing and completing research.

### 10. Player UI

Status: high practical gap.

- Current prototype UI uses temporary `OnGUI` debug panels.
- There is no production-grade HUD, build menu, inventory panel, logistics panel, market panel, or research panel.

Needed:

- Build menu with facility categories.
- Selected comet and selected facility panels.
- Inventory/resource readouts.
- Production status and stall reasons.
- Logistics route and cargo status views.
- Market/request/research screens.

### 11. Save And Load

Status: missing.

- Save/load is listed in the roadmap but not implemented.

Needed:

- Serializable company state.
- Serializable comets, grids, facilities, rails, inventories, routes, ships, requests, market state, and research.
- Versioned save data format.
- Manual save/load and eventually autosave.

### 12. Content Pipeline And Art Binding

Status: early.

- A few generated facility images exist.
- Most facilities, resources, effects, icons, and UI art are not yet bound into a consistent content workflow.

Needed:

- Asset naming and import conventions.
- Sprites linked to facility/resource definitions.
- Icons for resources and UI commands.
- Placement preview art and status overlays.
- Effects for extraction, production, transfer, and sale.

### 13. Scene And Playtest Validation

Status: incomplete.

- `dotnet build project_CF.sln` passes.
- Unity play mode behavior still needs editor-side confirmation.
- `dotnet test project_CF.sln` returns successfully but does not provide detailed NUnit execution output in the current shell.

Needed:

- Unity Editor playtest checklist for `WorldPrototypeScene` and `ConstructionTestScene`.
- Automated Unity EditMode/PlayMode test execution with readable results.
- Smoke test scene for the full gameplay loop.
- Regular verification that generated `.csproj` files include all new scripts.

## Recommended Next Technical Milestone

Build one vertical slice scene:

1. Generate or select one comet.
2. Place an ore drill, furnace, warehouse, and market terminal.
3. Extract iron ore from a finite deposit.
4. Move ore to the furnace.
5. Produce iron or steel.
6. Move output to warehouse or market terminal.
7. Sell goods or complete one exchange request.
8. Spend earned credits on another facility.

Completion of this milestone would move the project from isolated system prototype to an actual playable core loop.
