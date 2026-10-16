# 1. Architecture and domain model

[Documentation home](README.md) | [Next: patterns](patterns.md)

## 1.1 Think of chess as a domain, not an engine

For this application, chess is a set of rules governing transitions between discrete states.

```text
Input event
	-> ask the rules what is allowed
	-> choose an allowed result
	-> replace the current game state
	-> update presentation objects
	-> let WPF animate
```

There is no continuous chess simulation advancing each frame. While WPF performs animations over time, those animation frames are not intermediate chess positions.

A useful business-application analogy is an order workflow: the domain determines allowed transitions, the presentation layer exposes the choices, and the UI displays the selected result. Here the transitions happen to be chess moves.

## 1.2 Project responsibilities

| Project | Main responsibility | Representative types |
| --- | --- | --- |
| `Chess.Model` | Represent chess state and compute transformations and rule outcomes | `ChessGame`, `Board`, `Position`, `ChessPiece`, `StandardRulebook` |
| `Chess.ViewModel` | Coordinate interaction and expose observable presentation state | `ChessGameVM`, `BoardVM`, `FieldVM`, `PlacedPieceVM` |
| `Chess.View` | Render, translate pointer input, animate, and show windows | `MainWindow`, `PieceWindow`, template selectors, XAML resources |
| `Chess.Services` | Supporting operations and contracts, including persistence and window interaction | `BoardSerializationService`, `XmlFileService`, `IWindowService`, `PieceFactory` |

This is a responsibility map, not a claim of perfect dependency isolation. In particular, ViewModels in this fork use WPF types and dispatcher operations. They are not entirely UI-framework-independent objects.

The central flow is:

```text
MainWindow / XAML
	-> ChessGameVM.Select
		-> IRulebook / StandardRulebook
			-> movement and check rules
			-> model commands
			-> candidate ChessGame states
		-> BoardVM and its observable objects
	<- bindings, templates, triggers, storyboards
```

The model need not know how a pawn is drawn or how long its movement animation lasts.

## 1.3 The core domain objects

Source directory: `Chess.Model\Game`.

### Position

`Position` is a value-like object containing a row and column. Both are integers in the range 0 through 7. Equality and hashing use those coordinates, allowing positions to be dictionary keys.

The board's standard orientation maps coordinates as follows:

| Chess square | Row | Column |
| --- | --- | --- |
| a1 | 0 | 0 |
| e2 | 1 | 4 |
| e4 | 3 | 4 |
| h8 | 7 | 7 |

Rows increase toward Black's starting side; columns increase from a to h. These are board coordinates, not screen pixels.

Constructing an out-of-range `Position` fails validation. By contrast, `Offset` returns `Nothing<Position>` when stepping outside the board. That makes traversing outward from a valid square composable without constructing invalid coordinates.

`Position.ToString()` displays one-based numeric coordinates. Do not confuse that debugging representation with algebraic notation such as e4.

### ChessPiece and PlacedPiece

Source directory: `Chess.Model\Piece`.

A `ChessPiece` represents a kind of piece and its color. A `PlacedPiece` associates a piece with a position.

Conceptually:

```text
Rook(White)                       What is it?
PlacedPiece(a1, Rook(White))      Where is it?
```

Do not expect `Rook.cs` to contain all rook movement rules. Piece-specific operations largely live in visitors such as `MovementRule` and `ThreatAnalyzer`.

### Board

`Board` wraps an `IImmutableDictionary<Position, ChessPiece>`. Occupied squares have entries; an empty square has no entry. It also implements `IEnumerable<PlacedPiece>` so callers can enumerate occupied positions conveniently.

This differs from the presentation layer's 8-by-8 array of `FieldVM` objects. All 64 display squares exist even though many model positions are unoccupied.

Key operations:

| Operation | Result |
| --- | --- |
| `GetPiece` | An optional placed piece |
| `GetPieces(color)` | Pieces belonging to a color |
| `IsOccupied` | Whether a position is occupied, optionally by a specified color |
| `Add` | An optional new board |
| `Remove` | An optional new board |

`Board.Add` currently converts exceptions into `Nothing<Board>` using a broad catch. This is an implementation detail, not a general recommendation for exception handling: it can hide the reason an operation failed.

### ChessGame

`ChessGame` contains the current board, active player, passive player, and history information.

The names are relative to the current turn:

```text
Before White moves: active = White, passive = Black
After White moves:  active = Black, passive = White
```

`EndTurn()` creates another game with those player references swapped. It does not itself animate a piece or change a piece's square.

`SetBoard(...)` and `SetLastUpdate(...)` also create new game instances. Treat a `ChessGame` as the context around a board, rather than as the board itself.

### Update

`Update` pairs a `ChessGame` with a model command. Its meaning depends on where it is used:

- A selectable future update contains the game after the command has executed.
- A history update contains the game before the corresponding turn.

This dual use is important when reading `StandardRulebook.GetUpdates`. An `Update` is not inherently an event notification, an animation frame, or always an after-state.

## 1.4 Immutable transformation as the central design

The core transformation style is:

```text
Old state + command -> new state, or no result
```

Instead of changing a board's existing dictionary, `Add` and `Remove` produce new board wrappers around resulting immutable dictionaries. Instead of setting the old game's board field, `SetBoard` produces another game.

This enables:

1. **Safe hypothetical evaluation.** Generate a possible move without replacing the actual current game.
2. **Retained history.** Keep references to previous states rather than reconstructing them solely from inverse operations.
3. **Local reasoning.** A candidate's result can be inspected separately from its starting position.
4. **Composable execution.** A failed later command does not partially mutate the original board.

Immutable collections generally share unchanged internal structure. A new board wrapper is not equivalent to a deep copy of every piece and every history entry.

### The current fork's mutability boundary

Do not extend the immutable-state explanation beyond what the code guarantees:

- `ChessGame.NextUpdate` is settable and is used for forward navigation.
- Game identifiers and update metadata have setters.
- `History` enumeration assigns `Update.Id` values.
- `TitleNotesConcurrentDictionary` is shared mutable state.
- Commands contain flags such as `IsCastlingMove` and `IsKillerMove` that presentation processing can set.

Consequently, a game and all reachable objects do not constitute a deeply immutable object graph. Nor does using an immutable board alone make every ViewModel operation thread-safe.

The accurate description is: **immutable board storage and mostly functional core state transformations, surrounded by mutable navigation, metadata, and presentation state**.

## 1.5 Model state and presentation state are different

Source files:

- `Chess.ViewModel\Game\BoardVM.cs`
- `Chess.ViewModel\Game\FieldVM.cs`
- `Chess.ViewModel\Piece\PlacedPieceVM.cs`

| Model concern | Presentation counterpart |
| --- | --- |
| Position contains a piece | A `PlacedPieceVM` appears in `BoardVM.Pieces` |
| Piece moves to another square | The displayed piece's `Position` changes |
| Piece no longer occupies a square | Its displayed object can remain temporarily with `Removed = true` |
| A future move is legal | A destination `FieldVM` has `IsTarget = true` |
| Player has not committed a move | `BoardVM.Source` and its target map represent selection |

`BoardVM.Pieces` is an `ObservableCollection<PlacedPieceVM>`. CommunityToolkit MVVM observable properties notify WPF about changes such as piece position and removal.

A model board should not gain flags such as `IsHighlighted` or `FadeOutProgress`. Those describe interaction or rendering, not chess.

## 1.6 Rendering with ordinary WPF

Source file: `Chess.View\Window\MainWindow.xaml`.

The board uses layered `ItemsControl` elements for labels, fields, and pieces, with `Canvas` panels for positioning.

- A square is represented using a logical size of one unit.
- `Viewbox` scales the board to the available space.
- Fields bind to `Board.Fields`.
- Pieces bind to `Board.Pieces`.
- `PieceSymbolSelector` chooses a template using the piece type and color.
- Highlight triggers respond to `IsTarget`.
- Movement storyboards animate `Canvas.Left` and `Canvas.Bottom`.
- Removal triggers animate opacity before cleanup removes the displayed object.

`PositionVM` translates board coordinates into presentation distances. The current fork includes label margins and board-inversion transforms, so its offsets should not be treated as a universal coordinate formula.

A board rotation is a presentation concern: it does not change e2 into a different model position. `MainWindow.BoardMouseDown` translates pointer coordinates back into row and column values.

## 1.7 Application features around the core

The current fork adds a larger interaction surface around normal play:

- Play, Record, and Review application modes.
- Manual and automatic review controls.
- Title/notes handling.
- Board serialization and custom-board construction.
- Row/column labels and board inversion.

`ChessGameVM` coordinates with status/mode ViewModels through direct references, callbacks, and `WeakReferenceMessenger` messages. XAML data templates map various status/mode ViewModels to their views.

This is a messenger-based form of in-process publish/subscribe. It is separate from the command pipeline used to calculate chess positions.

When learning the rules engine, temporarily set these features aside. Once a normal move makes sense, the extra modes are easier to understand as restrictions, navigation, and persistence around the same game and board.

Continue with [Patterns and functional building blocks](patterns.md).
