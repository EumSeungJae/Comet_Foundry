# Comet Foundry Framework Overview v0.1

## Core Direction

Comet Foundry is an industrial management simulation. The player operates a company, not a single mine, factory, ship, or comet. Every system should answer one question:

> How efficiently is the company using its comet assets?

The first framework separates the game into stable simulation concepts:

- Comets: permanent company assets with changing roles.
- Resources: finite deposits and movable goods.
- Facilities: mines, refineries, factories, warehouses, hubs, labs, and terminals.
- Comet grids: XCOM-like rectangular tile layouts for internal comet development.
- Production: recipe-based transformation over simulation time.
- Logistics: route, priority, cargo, and dispatch rules.
- Economy: selling output and later market pressure.
- Research: unlock and efficiency progression.

## Folder Layout

- `Assets/CometFoundry/Scripts/Core`: small primitives shared by every system.
- `Assets/CometFoundry/Scripts/Data`: ScriptableObject definitions edited in Unity.
- `Assets/CometFoundry/Scripts/Simulation`: runtime state and system logic.
- `Assets/CometFoundry/Tests/EditMode`: low-level simulation tests.

## Main Model

`CompanyState` owns the current company-wide state:

- credits
- discovered or owned comets
- transport routes
- cargo ships

`CometState` is permanent. Deposits can be depleted, but the comet remains available for later roles such as refinery, assembly base, logistics hub, or research base.

Each comet owns a `CometGrid`. The grid is the internal development board where facilities and rails are placed.

`FacilityInstance` represents a built facility on a comet. It has local inventory and can run one active recipe.

`TransportRoute` connects two facilities. The route does not mean a ship is always moving. It is a standing logistics rule that cargo ship AI can evaluate.

## Next Implementation Steps

1. Create initial resource assets: iron ore, iron, steel, frame, machine parts, consumer goods.
2. Create initial facility assets: mine, refinery, component factory, assembly plant, warehouse, logistics hub.
3. Add a simple prototype scene with `CometFoundryGame`.
4. Add a debug UI for company credits, comet list, selected comet inventory, and route status.
5. Implement cargo ship dispatch ticks from route automation rules.
6. Add comet discovery and evaluation as a separate loop before ownership investment.

## Design Guardrails

- Do not treat mined resources as infinite.
- Do not remove depleted comets.
- Prefer route rules and priorities over direct manual shipping.
- Keep production and logistics measurable in days, tons, distance, operating cost, and revenue.
- Let factories matter because they fit an industrial network, not because more buildings automatically win.
