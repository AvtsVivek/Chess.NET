# Understanding Chess.NET

A guided tour for developers who know C#, .NET, and WPF but do not have a game-development background.

## Start here

The central idea is:

> The model calculates legal future game states. The ViewModel lets a player choose one. WPF presents and animates the transition.

This is a turn-based, event-driven application, not a real-time simulation. You do not need to learn physics, collision detection, or a rendering engine to understand it.

Read the guides in this order:

| Guide | What you will learn |
| --- | --- |
| [1. Architecture and domain model](architecture.md) | Project responsibilities, board representation, immutable state, coordinates, and the boundary between chess and presentation |
| [2. Patterns and functional building blocks](patterns.md) | Command, Composite, Visitor, optional values, delegates, history, and their trade-offs |
| [3. A move from click to animation](move-lifecycle.md) | The actual selection pipeline, move generation, king-safety filtering, special moves, undo, and redo |
| [4. Reading and debugging the code](reading-and-debugging.md) | A manageable source-reading order, breakpoints, exercises, common misunderstandings, and guidance for extensions |

## Scope and accuracy

These documents describe the code in this fork as inspected when the guide was added. They are not a claim that the current upstream repository has the same implementation, nor a line-by-line attribution of which features came from upstream.

The walkthroughs focus on normal play from the standard starting position. Custom-board setup, recording, review, and notes are application features around that core. Their entry points are identified so they do not obscure the main design.

Important qualifications:

- The board uses an immutable collection, and core game transformations create new game objects. The current fork also contains mutable history links, metadata, counters, and command flags; it is not fully immutable throughout.
- Pattern names explain the implementation; they do not mean that every class is a textbook implementation or that every abstraction is necessary.
- Rule support should not be mistaken for proof of complete chess-rule correctness. For example, the inspected `EndRule` distinguishes checkmate and stalemate but does not implement every possible draw condition.
- Code fragments and diagrams in the guides are explanatory. Where a diagram flattens nested commands, it describes their order, not their exact object-tree shape.

## Quick vocabulary

| Term | Meaning in this project |
| --- | --- |
| State | A board, the active and passive players, and associated history information |
| Active player | The player whose turn it is |
| Passive player | The other player |
| Placed piece | A chess piece associated with a board position |
| Threat | A square attacked by a piece, as calculated by `ThreatAnalyzer`; not necessarily a legal move |
| Candidate move | A movement command before final own-king safety filtering |
| Legal update | A candidate future state that passed execution and the rulebook's safety filtering |
| Command | An object describing an operation; distinguish model commands from WPF commands |
| Visitor | An object supplying operations specialized for each piece or command type |
| Maybe | A value that is either present (`Just<T>`) or absent (`Nothing<T>`) |
| Turn | One player's action; potentially several primitive commands |
| History update | A command paired with its previous game state |
| Presentation state | Selection, highlights, displayed piece positions, and animation/removal state |

## If you remember only five things

1. `Board` is the chess position; `BoardVM` is its current presentation.
2. Model commands transform states; visitors can interpret the same commands for other purposes.
3. A complex turn is composed from small commands.
4. Legal moves are found by generating candidates, simulating them, and rejecting unsafe results.
5. Choosing a move adopts an already-calculated state, then updates the observable presentation objects.
