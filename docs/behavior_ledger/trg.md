# Trigger (TRG) files

Implemented by tools/reference/export_trg_graph.py, which cross-checks every
field it relies on against the pinned Neversoft Multitool decode and refuses to
export on disagreement. Outputs: levels/<LEVEL>/trg_graph.json (private).

## Layout (little-endian)

| Offset | Field |
|---|---|
| 0 | magic `_TRG` |
| 4 | u16 version major, u16 version minor (2.1 in Level 1) |
| 8 | u32 node count |
| 12 | u32 offset of each node, from file start |

Each node starts with a u16 type. The link list is a u16 count followed by that many
u16 node indices, located at a type-specific u16 index inside the node:

| Node types | Link count at u16 index |
|---|---|
| 1 (BADDY) | 3 |
| 2, 3, 6, 8, 9, 10, 12, 13, 1000, 1001 | 1 |
| 5 (POWERUP), 20, 1002 | 2 |
| 4, 15 (AUTOEXEC), 255 (TERMINATOR) | none |

Evidence: `decomp:trig.cpp#Trig_GetLinksPointer@f8a0392 (@Ok)`, confirmed against raw
bytes for all 1,495 Level 1 nodes. Node positions are three i32 world-unit values
inside the node (found for 844/844 positioned Level 1 nodes). Confidence: verified.

Known decoder quirk: for CAMPT (13) nodes with zero links, Multitool reports the
following padding word as a link to node 0. The raw layout (zero links) is used;
63 such nodes in L1A3/L1A4.

## Semantics

| Concept | Behavior | Evidence |
|---|---|---|
| Links | A node's pulses, signals, suspends and kills go to its link list | `decomp:trig.cpp#Trig_SendPulse`, `#Trig_SendSignalToLinks`, `#SendKillFromNode` |
| Kill by target type | Baddy nodes kill baddies/controllers/environment objects by node; environment/crate nodes destroy or hide the matching environment item; powerup nodes remove powerups | `decomp:trig.cpp#SendKillFromNode@f8a0392 (@NotOk)` |
| Command points | Command lists addressed by checksum through a 256-bucket hash; each records collision, executed and pulse counts | `decomp:trig.h#SCommandPoint`, `decomp:trig.cpp#Trig_TriggerCommandPoint (@Ok)` |
| Pending lists | Up to 16 command lists queued and run later | `decomp:trig.cpp#Trig_AddCommandListToPending`, `#Trig_DoPendingCommandLists` |
| Autoexec | Runs once at level start | `decomp:trig.cpp#Trig_ExecuteAutoexec (@Ok)` |
| Restart | Named checkpoint: `SetRestart(name)` selects it, `Trig_ExecuteRestart` runs its command list after a death/restart | `decomp:trig.cpp#Trig_SetRestart`, `#Trig_ExecuteRestart (@Ok)` |
| Opcode interpreter | Not recovered (`ExecuteCommandList` is a stub). Opcode names come from Multitool; `Unknown_0x*` opcodes must not be given guessed meanings | - |

## Level 1 summary (2026-10-20)

| Act | Nodes | Restarts | Command points | Restart names (autoexec selects Re_Start_Death in every act) |
|---|---|---|---|---|
| L1A1 | 292 | 2 | 102 | Re_Start_Death, Re_Restart01 |
| L1A2 | 181 | 2 | 58 | Re_Start_Death, Re_Restart01 |
| L1A2A | 172 | 2 | 63 | Re_Start_Death, Re_Restart01 |
| L1A3 | 470 | 4 | 180 | Re_Start_Death, Re_Restart_L1A3, _2, _3 |
| L1A4 | 380 | 2 | 146 | Re_Start_Death, Re_Restart_L1A4_1 |

## Remake specification (Phase 4)

`MissionRuntime` loads trg_graph.json, runs autoexec at level start, treats restart
nodes as checkpoints, and delivers pulses/signals along links. Each supported opcode
is implemented from a ledger entry; unknown opcodes log and halt in debug builds.
