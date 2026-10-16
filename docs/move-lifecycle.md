# 3. A move from click to animation

[Documentation home](README.md) | [Previous: patterns](patterns.md) | [Next: reading and debugging](reading-and-debugging.md)

This walkthrough assumes normal Play mode and a standard initial position. Custom-board editing and Review mode take different branches in `ChessGameVM.Select`.

## 3.1 The complete route

```text
MainWindow.BoardMouseDown
	-> translate mouse coordinates
	-> ChessGameVM.Select(row, column)
		-> first click: select a friendly piece
			-> StandardRulebook.GetUpdates
				-> MovementRule generates candidates
				-> compose turn-end and history commands
				-> execute candidates into future states
				-> reject own-king check
			-> BoardVM.SetSource / SetTargets
			-> TargetSetter associates futures with squares
			-> WPF highlights destinations
		-> second click: select a destination
			-> get its previously generated updates
			-> choose one, prompting for promotion if necessary
			-> set forward link on old game
			-> adopt selected future game
			-> visit its command tree
			-> update BoardVM objects and turn status
			-> WPF animates the changed presentation
```

## 3.2 Initialization: finding the initial board

`MainWindow` constructs `ChessGameVM`, passing its `Choose` callback and a `WindowService`, then sets the window's `DataContext`.

The ViewModel constructs the rulebook, undo/redo commands, and supporting status/mode objects. In the current fork, initialization also involves a callback: `StatusModeListViewVM` invokes the supplied `StartNewGame` delegate during construction.

`StartNewGame` tries to obtain a serialized custom board through `BoardSerializationService`. If none is available, it uses `StandardRulebook.CreateGame` and refreshes the presentation.

`CreateGame` constructs the conventional piece layout with White active. `BoardVM` then constructs the displayed fields and pieces from the board.

For the walkthrough below, use a standard board rather than a previously saved custom position.

## 3.3 First click: select the pawn on e2

Source: `Chess.View\Window\MainWindow.xaml.cs`, `BoardMouseDown`.

The view obtains the mouse position relative to the board canvas. It accounts for the board-label margin and the difference between screen Y coordinates and board rows, then calls:

```text
Select(row: 1, column: 4)
```

This represents e2. The input handler performs coordinate translation, not chess validation.

Source: `Chess.ViewModel\Game\ChessGameVM.cs`, `Select`.

The method first handles application-level conditions:

- Review mode disallows ordinary move selection.
- Custom-board editing can route the click to `SetupCustomBoard`.
- Clicking an already selected source clears its selection.

In ordinary play, it first asks the board for updates associated with the clicked field. On the initial source click, there normally are none. The chooser returns no selected update.

Because e2 contains an active-player piece, the ViewModel asks the rulebook to generate updates for it, marks the source, and sets its targets.

Selecting a piece does not yet commit a chess move.

## 3.4 Generate candidate pawn actions

Source: `Chess.Model\Rule\MovementRule.cs`, `Visit(Pawn)`.

The rule generates several categories:

1. Diagonal captures where an enemy occupies an attacked square.
2. A one-square forward move if unoccupied.
3. A two-square forward move from the starting row if both squares are free.
4. En passant, if the immediately preceding turn qualifies.
5. Promotion variants when a forward move or capture reaches the final rank.

On the standard initial board, White's e2 pawn can generate forward candidates for e3 and e4. It has no ordinary diagonal capture at that moment.

For sliding pieces, `ThreatAnalyzer.Iterate` walks directions until it reaches a boundary, a friendly blocker, or the first enemy. For kings and knights, the applicable direction offsets are limited to one step. Pawns require special movement handling because their attacked squares are not their forward destinations.

A candidate respects the movement-generation logic, but it still needs final own-king safety evaluation.

## 3.5 Wrap a candidate into a complete turn

Source: `Chess.Model\Rule\StandardRulebook.cs`, `GetUpdates`.

Let:

```text
G0 = current game
M  = candidate movement command
T  = Sequence(M, EndTurn)
H  = Update(G0, T)                     history record
C  = Sequence(T, SetLastUpdate(H))     complete command
G1 = result of executing C against G0
F  = Update(G1, C)                     selectable future
```

This is a conceptual expansion of the nested LINQ pipeline. The actual constructors also receive description strings used in the current fork.

There are two distinct `Update` roles:

| Object | Stored game | Stored command | Purpose |
| --- | --- | --- | --- |
| History record H | Before-state G0 | Turn T | Navigate backward and inspect previous actions |
| Future F | After-state G1 | Complete command C | Select and present a legal result |

`G1.LastUpdate` points to H. H refers to G0 rather than recursively containing G1.

The command recorded in H is the turn command before the final history-setting wrapper. This distinction matters when tracing history or comparing commands.

### Why execute moves before the player chooses?

To know whether each proposed move is legal. Calculating a future is not committing it. The ViewModel's current game remains G0 until selection.

The implementation can therefore create many game instances for a single source click. The debugging instance counters count candidate evaluation and intermediate transformations too, not just moves played.

## 3.6 Filter unsafe futures

After executing each complete command, the rulebook drops absent results and checks the moving player's king.

Its filter uses the resulting game's `PassivePlayer`:

```text
Before White's turn: active White, passive Black
After EndTurn:      active Black, passive White
Safety question:    is the new passive player (White) in check?
```

A future is excluded if the answer is yes.

This handles constraints such as a pinned piece: the piece can generate a geometrically plausible move, but the simulated result exposes its king, so the move never becomes a selectable target.

`CheckRule.Check` finds the specified player's king and tests whether an enemy threat reaches its square.

Do not treat this as a general custom-position validator. For example, the current check implementation returns false if no king is found. A malformed custom board needs separate validity considerations.

## 3.7 Convert legal futures into highlights

Source: `Chess.ViewModel\Game\BoardVM.cs` and `Chess.ViewModel\Visitor\TargetSetter.cs`.

The board keeps a map shaped like:

```text
Destination FieldVM -> List<Update>
```

A destination can have multiple updates because a pawn reaching the last rank can become different pieces.

`SetTargets` visits each update's command tree with `TargetSetter`. That visitor locates a movement destination and calls `AddUpdate` for the destination field.

For a sequence, it searches the first child and only searches the second if the first did not identify a target. This is particularly useful for castling: the king's destination, not a second independently selectable rook move, represents the turn.

`AddUpdate` marks the field as a target. XAML's `IsTarget` data trigger animates the target indicator. The currently selected source also uses the target flag for highlighting, so a highlighted source is not an additional legal destination.

## 3.8 Second click: choose e4

`Select(3, 4)` looks up the previously stored updates for e4 and passes them to the selection callback supplied by `MainWindow`.

`MainWindow.Choose` handles three cases:

| Number of updates | Action |
| --- | --- |
| Zero | Return no selection |
| One | Return that update |
| More than one | Resolve promotion choice using `PromotionSelector` and `PieceWindow` |

The current chooser assumes multiple alternatives at one target mean promotion. Adding another kind of ambiguous action would require revisiting that assumption.

After a successful choice, the ViewModel:

1. Saves the relevant title/notes state.
2. Sets the old game's `NextUpdate` to the selected future.
3. Sets its current `Game` to the selected future's game.
4. Visits the selected command with `ChessGameVM`.

The board's temporary source/target selection is cleared during selection processing.

The key point: **the chosen command is not executed against the model again at this point**. Its model result was already calculated. Visiting it now applies its meaning to the presentation and associated application actions.

Clicking an unoccupied, non-target square does not generate an arbitrary illegal move. Clicking another friendly piece can generate a new set of targets instead.

## 3.9 Present the selected turn

`ChessGameVM` implements `ICommandVisitor`. Its sequence visitor processes the command tree in order and forwards primitive operations to `BoardVM.Execute` overloads.

| Command interpretation | Presentation effect |
| --- | --- |
| Move | Find the displayed piece at the source and replace its `PositionVM` |
| Remove | Mark the displayed piece `Removed = true` |
| Spawn | Add a new displayed piece |
| End turn | Finalize or undo the move-list entries and refresh status/mode handling |
| Set last update | Handle associated presentation/debug information |

The sequence overload on `BoardVM` also recognizes certain compositions and sets flags used for the move list, such as castling and capture markers.

For movement, the view observes the new position and animates the presenter's canvas coordinates. For capture, the view fades the removed object; its completion handler calls `Board.CleanUp` to remove it from the observable collection.

The model state is already at the destination while the animation is running. The animation is a visual interpolation, not a sequence of legal chess states.

`Visit(EndTurnCommand)` also invokes status/mode refresh and recording-related processing. These are presentation/application effects, not part of the model's primitive board transformation.

## 3.10 Special moves use the same pipeline

The following lists flatten command order for clarity; the actual structures are nested `SequenceCommand` objects. The standard rulebook then appends end-of-turn and history handling.

### Capture

```text
Remove opponent at destination
Move attacker to destination
```

Removing first matters because the board add operation expects an available destination.

### Castling

```text
Move king two squares
Move rook to the square next to the king
```

`CastlingRule` consults the position, available rook, threats, and movement history to generate candidates. This demonstrates why board occupancy alone is not enough: a king that moved away and returned is not equivalent, for castling, to a king that never moved.

This explanation describes the rule's mechanism, not a certification of every castling edge case. Consult the implementation when investigating a particular position.

### En passant

```text
Remove the neighboring opponent pawn
Move the capturing pawn to the diagonal destination
```

`EnPassantRule` inspects `LastUpdate` to recognize the preceding two-square pawn move. The captured piece is not on the destination square, but primitive remove-plus-move composition still works.

### Promotion

```text
Move pawn to final rank
Remove pawn
Spawn chosen queen, rook, bishop, or knight
```

`PromotionRule` generates separate command alternatives for the promotion choices. The destination map retains all of them; the promotion dialog chooses one future rather than modifying an already-committed pawn afterward.

## 3.11 Status, checkmate, and stalemate

`ChessGameVM.Status` delegates to `rulebook.GetStatus`, which uses `EndRule`.

`EndRule` asks:

1. Is the active player's king checked?
2. Does that player have any candidate that executes successfully and leaves their king safe?

| Checked | Safe move exists | Result |
| --- | --- | --- |
| Yes | No | Opponent wins by checkmate |
| No | No | Draw by stalemate |
| Either | Yes | Active player's turn continues |

During this search, `EndRule` evaluates movement commands without appending a player swap, so it checks the original active player. This differs from the final-future filtering in `StandardRulebook`, where the turn has already ended.

The inspected implementation does not additionally evaluate repetition, the fifty-move rule, or insufficient material. Do not infer all draw rules from the `Status.Draw` value.

## 3.12 Undo: restore the model, reverse the presentation

The WPF Undo command is available when `Game.LastUpdate` has a value.

For history entry H:

```text
Current model G1
	-> set Game to H.Game (G0)
	-> clear selection
	-> negate H.Command
	-> visit the inverse command to update the displayed board
```

`CommandNegator` applies these general transformations:

| Original | Inverse presentation action |
| --- | --- |
| Move A to B | Move B to A |
| Remove piece | Spawn piece |
| Spawn piece | Remove piece |
| Sequence(A, B) | Sequence(inverse B, inverse A) |
| End turn | End-turn command marked as undo |

There is additional pawn-at-final-rank handling to get promotion undo animations right.

For a capture, undo must move the attacker back before restoring the captured piece. That is why sequence inversion reverses order, not just direction.

The inverse commands are visited to update presentation; they are not used here to reconstruct the old model by executing against G1. The old state reference already supplies the model.

## 3.13 Redo and forward links

On move selection, G0 receives `NextUpdate = F`, and F contains G1 and the complete forward command.

After undo returns to G0, redo can:

1. Obtain `G0.NextUpdate`.
2. Adopt its game G1.
3. Visit its forward command to present the move again.

The current implementation also clears `NextUpdate` when a friendly piece is selected for fresh move generation. This is not a branching analysis tree that retains every explored variation.

Manual and automatic review build on navigation behavior, but include additional mode restrictions, callbacks, and timing. Do not confuse replay timing with the domain's move-generation process.

Continue with [Reading and debugging the code](reading-and-debugging.md).
