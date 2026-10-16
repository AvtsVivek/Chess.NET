# 2. Patterns and functional building blocks

[Documentation home](README.md) | [Previous: architecture](architecture.md) | [Next: move lifecycle](move-lifecycle.md)

## 2.1 Pattern map

| Pattern or technique | Concrete example | Problem it solves |
| --- | --- | --- |
| MVVM | `ChessGameVM`, `BoardVM`, XAML bindings | Keep domain decisions separate from their presentation |
| Command | `MoveCommand`, `RemoveCommand` | Represent an action as data plus an executable transformation |
| Composite | `SequenceCommand` | Treat a multi-step action like a single command |
| Visitor / double dispatch | `MovementRule`, `ThreatAnalyzer`, `CommandNegator` | Supply different operations for a stable set of piece or command types |
| Immutable transformation | `Board.Add`, `ChessGame.SetBoard` | Calculate alternative states without overwriting the current board |
| Optional value / Maybe | `IMaybe<T>`, `Just<T>`, `Nothing<T>` | Compose operations that may have no result |
| Memento-like history | `LastUpdate.Game` | Retain the state needed to navigate backward |
| Strategy-like contract | `IRulebook` | Describe the operations expected of a rulebook |
| Callback injection | Promotion-selection delegate | Ask the UI to resolve ambiguity without putting a dialog into a rule |
| Publish/subscribe | `WeakReferenceMessenger` | Communicate application-mode and review messages between ViewModels |

The labels are explanations, not requirements to adopt every abstraction in a different chess program.

## 2.2 Command: an action becomes an object

Source directory: `Chess.Model\Command`.

A `MoveCommand` retains the source, destination, and piece. A `RemoveCommand` retains the piece and position to remove. These are inspectable descriptions, not just anonymous side effects.

The model command's execution shape is:

```text
ICommand.Execute(ChessGame) -> IMaybe<ChessGame>
```

This means a command can produce a new game, or no game if its primitive operation cannot be completed.

Keeping the description is useful because it can be interpreted in several ways:

- Execute it against a model state.
- Find its destination for highlighting.
- Update the displayed board.
- Construct its inverse for undo presentation.
- Inspect previous movement when evaluating a history-dependent rule.

A primitive command is not the complete chess validator. For example, executing `MoveCommand` removes a piece at the source and adds the stored piece at the destination; it does not on its own prove that a rook followed a legal path or that the player's king is safe. The rule-generation pipeline supplies those constraints.

### Two unrelated ICommand interfaces

| Interface | Used by | Contract |
| --- | --- | --- |
| `System.Windows.Input.ICommand` | `GenericCommand` | `CanExecute`, `Execute`, `CanExecuteChanged` for UI interaction |
| `Chess.Model.Command.ICommand` | `MoveCommand` and other model commands | Transform a game and accept command visitors |

A toolbar Undo button binds to a WPF command. That UI command then coordinates model-state navigation and model-command visitors. The toolbar does not bind directly to a `MoveCommand`.

## 2.3 Composite: a turn is a tree of commands

`SequenceCommand` has two children, `FirstCommand` and `SecondCommand`, each of which is another model `ICommand`.

That gives the recursive structure:

```text
Command = primitive operation OR sequence of commands
```

A capture followed by end-of-turn handling can be visualized as:

```text
Sequence
+-- Sequence
|   +-- Remove captured piece
|   +-- Move attacking piece
+-- End turn
```

Real turns also receive history recording, and can be nested more deeply. Code consuming a command can nevertheless start from one interface.

This is Composite because the container and leaves share the same contract. It is not simply a list of unrelated actions or a chain of handlers competing to process a request.

Execution processes the first child and passes its resulting game into the second. If the first yields nothing, the second is not run. If the second fails, the sequence yields nothing. Immutable board transformations preserve the original board even if an intermediate result was created before failure.

The composition provides board-state safety, not a transaction for arbitrary side effects. Do not assume it would roll back counters, logging, file writes, or mutable flags added to command execution.

## 2.4 Visitor: choosing behavior by both operation and type

Source interfaces:

- `Chess.Model\Piece\IChessPieceVisitor.cs`
- `Chess.Model\Command\ICommandVisitor.cs`

Suppose a piece variable has the static type `ChessPiece` but holds a `Rook`. We want one visitor to calculate attacks and another to choose a graphical template.

The call path is:

```text
piece.Accept(visitor)
	-> virtual dispatch reaches Rook.Accept
	-> Rook calls visitor.Visit(this)
	-> this is statically a Rook inside Rook.Accept
	-> the Visit(Rook) overload is selected
```

That two-step process is called double dispatch. It combines the runtime piece type with the supplied operation.

Why the `Accept` method matters: simply calling an overloaded `visitor.Visit(piece)` with a variable declared as `ChessPiece` does not dynamically select a `Visit(Rook)` overload in ordinary C# overload resolution.

### Operations are grouped by concern

| Visitor | What its piece/command-specific methods do |
| --- | --- |
| `ThreatAnalyzer` | Calculate attacked squares for each piece type |
| `MovementRule` | Generate movement commands for each piece type |
| `PieceSymbolSelector` | Produce template-resource keys for each piece type and color |
| `TargetSetter` | Inspect commands to associate an update with its clickable destination |
| `CommandNegator` | Transform a command into an inverse presentation command |
| `ChessGameVM` | Interpret a chosen command tree to update the ViewModel |

A piece can remain small because the operations are outside it.

### Trade-offs

Visitor works well when the set of visited types is stable and there are multiple operations over them. Chess's six conventional piece types fit that profile.

Adding another visitor can be straightforward. Adding a seventh piece type requires changes across visitor interfaces and implementations, not just adding one class.

The current code also uses visitors for predicates such as identifying a king or pawn. Modern C# type patterns could express some of those checks more directly. Understand the existing choice without assuming every visitor is essential.

## 2.5 Reading the Func-heavy signatures

`MovementRule` implements a visitor whose result is:

```csharp
Func<ChessGame, Position, IEnumerable<ICommand>>
```

Read from left to right:

```text
Inputs:  a ChessGame and a Position
Output:  a sequence of model commands
```

Therefore a call shaped like this:

```csharp
piece.Accept(this)(game, position)
```

performs two operations:

1. `Accept(this)` selects and returns a piece-specific function.
2. `(game, position)` invokes that function.

Conceptually, you can expand it to:

```csharp
var generateCommands = piece.Accept(this);
var commands = generateCommands(game, position);
```

The returned function can capture the piece and its color. The later invocation supplies the board context and starting square.

Other visitors return different things: booleans, strings, commands, or functions taking other arguments. Visitor does not require a particular return type.

## 2.6 Maybe: explicit absence instead of repeated null checks

Source files: `Chess.Model\Data\IMaybe.cs`, `Just.cs`, and `Nothing.cs`.

```text
Just<T>(value)       A value exists
Nothing<T>.Instance No value exists
```

This resembles an `Option<T>` abstraction. It is useful for reference types as well as other values and is more composable than a `bool` plus an `out` parameter.

Examples of expected absence:

- There is no piece on a square.
- An offset would leave the board.
- A removal has no piece to remove.
- There is no previous turn for en passant analysis.

### Map versus Bind

| Method | Function it accepts | Result |
| --- | --- | --- |
| `Map` | `T -> V` | `IMaybe<V>` |
| `Bind` | `T -> IMaybe<V>` | `IMaybe<V>` |

`Map` transforms a present value and wraps the result. `Bind` invokes a transformation that already returns a Maybe, avoiding an extra layer such as `IMaybe<IMaybe<V>>`.

For `Nothing<T>`, neither transformation is invoked; the absence propagates.

`MoveCommand.Execute` can be read as this pipeline:

```text
Board.Remove(source)           Maybe<Board>
	-> Bind Add(target, piece) Maybe<Board>
	-> Map game.SetBoard       Maybe<ChessGame>
```

The `Add` step can fail, so it uses `Bind`. `SetBoard` returns a plain game, so it uses `Map`.

### Other helpers

| Helper | Meaning |
| --- | --- |
| `Guard(predicate)` | Keep a present value only if the predicate passes |
| `GetOrElse(...)` | Extract or transform a present value, otherwise use an alternative |
| `Do(action)` | Perform an action only when a value is present |
| `Yield()` on a Maybe | Produce a sequence of zero or one values |
| `FilterMaybes()` | Flatten a sequence of optional values, dropping absent ones |

Be aware that there is also a general `Yield` helper for ordinary values in `Data\Extension.cs`. The receiver type determines which extension method is being used.

### What Maybe does not do

- It is not a `Task`, and `Bind` does not imply asynchronous execution.
- It is unrelated to WPF data binding.
- It does not contain a detailed error reason.
- It does not universally catch exceptions. `Just<T>.Map` and `Bind` invoke their functions directly; exceptions can still propagate.
- `Just<T>` validates its value against null, so absence should be represented as `Nothing<T>`, not a null wrapped in `Just<T>`.

Use Maybe for expected lack of a result; do not interpret it as a substitute for every validation, diagnostic, or exception-handling policy.

## 2.7 LINQ and deferred enumeration

Move generation uses `Select`, `SelectMany`, `Where`, `Union`, `Any`, and iterator methods. Many of these return lazily evaluated sequences.

This means calculating a variable named `commands` may not yet execute all the generation logic. Enumeration by `foreach`, `ToList`, or `Any` triggers evaluation; `Any` can stop early.

`StandardRulebook.GetUpdates` materializes the returned legal updates into a list near the end. Earlier parts of its pipeline are composed as sequences.

Debugger watches can also enumerate sequences. In this fork, `History` enumeration assigns IDs, so merely inspecting or counting history is not entirely free of side effects. Do not infer that one constructor call or one breakpoint hit corresponds to one committed player move: candidate simulation creates many objects too.

## 2.8 History: state retention plus inverse commands

Undo combines two mechanisms:

1. **State retention:** the history update contains the previous `ChessGame`.
2. **Inverse interpretation:** `CommandNegator` constructs commands to bring the displayed board back into alignment.

This resembles Memento because an earlier state is retained, but there is no need to force every role of the classic pattern onto a separate class.

It is not pure event sourcing: the program does not restore every previous position solely by replaying persisted domain events. An in-memory history entry retains a state reference.

The current `NextUpdate` link provides forward navigation. It is mutable, so this is not a purely persistent, immutable history data structure.

## 2.9 Rulebook abstraction and callback injection

`IRulebook` exposes creating a game, getting its status, and getting updates for a position. This is a strategy-like seam for a rules implementation.

However, `ChessGameVM` currently constructs `StandardRulebook` directly. The interface does not mean the application already supports selecting arbitrary rulebooks at runtime.

The promotion-selection callback is another seam:

```text
Func<IList<Update>, Update>
```

`ChessGameVM` receives it from `MainWindow`. When several futures share a destination, the window can display the promotion dialog and return the selected update. The chess model itself need not open a WPF window.

Similarly, `IWindowService` is a service contract for window-related interactions. These mechanisms are distinct from a full dependency-injection container; the code uses manual construction and callbacks.

Continue with [A move from click to animation](move-lifecycle.md).
