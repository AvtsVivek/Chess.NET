# 4. Reading and debugging the code

[Documentation home](README.md) | [Previous: move lifecycle](move-lifecycle.md)

## 4.1 Read by concept, not by folder order

Do not start by reading the entire `ChessGameVM` constructor or every status-mode message handler. Those include substantial application coordination that is not needed to understand a basic move.

Follow this route instead:

| Step | Files under the repository root | Question to answer |
| --- | --- | --- |
| 1 | `Chess.Model\Game\Position.cs`, `Board.cs`, `ChessGame.cs` | What exactly is a game state? |
| 2 | `Chess.Model\Piece\ChessPiece.cs`, `PlacedPiece.cs`, `Rook.cs` | How is piece identity separated from placement? |
| 3 | `Chess.Model\Data\IMaybe.cs`, `Just.cs`, `Nothing.cs` | How are optional results represented and chained? |
| 4 | `Chess.Model\Command\MoveCommand.cs`, `SequenceCommand.cs` | How does an operation create a new state? |
| 5 | `Chess.Model\Visitor\ThreatAnalyzer.cs` | How are reachable attacked squares enumerated? |
| 6 | `Chess.Model\Rule\MovementRule.cs` | How are square destinations turned into commands? |
| 7 | `Chess.Model\Rule\StandardRulebook.cs` | How do candidates become selectable legal futures? |
| 8 | `Chess.Model\Rule\CheckRule.cs`, `EndRule.cs` | How are king safety and game outcome evaluated? |
| 9 | `Chess.ViewModel\Game\ChessGameVM.cs`, method `Select` | How does the UI choose between futures? |
| 10 | `Chess.ViewModel\Visitor\TargetSetter.cs`, `Chess.ViewModel\Game\BoardVM.cs` | How are highlights and displayed pieces updated? |
| 11 | `Chess.View\Window\MainWindow.xaml.cs`, `MainWindow.xaml` | How are clicks translated and changes animated? |
| 12 | `Chess.ViewModel\Visitor\CommandNegator.cs` | How is the visual effect of a turn reversed? |

After that, investigate special rules, serialization, and application modes as separate features.

## 4.2 A practical Visual Studio debugging session

Use a standard board in Play mode. A saved custom board changes starting assumptions.

### Exercise A: follow e2 to e4

Place breakpoints at:

1. `MainWindow.BoardMouseDown`.
2. `ChessGameVM.Select`.
3. `StandardRulebook.GetUpdates`.
4. `MovementRule.Visit(Pawn)` or the returned lambda's body.
5. `MoveCommand.Execute`.
6. `ChessGameVM.Visit(MoveCommand)`.
7. `BoardVM.Execute(MoveCommand)`.

Click the pawn on e2.

Expected observations:

- Board coordinates identify row 1, column 4.
- Candidate commands include e3 and e4 on an unobstructed standard opening board.
- Model execution breakpoints can fire before any destination is chosen.
- The current ViewModel game is still the pre-move game.
- Target fields are populated after futures are generated.

Click e4.

Expected observations:

- An update is retrieved from the target map.
- The ViewModel adopts the update's existing future state.
- Command visiting reaches the presentation move overload.
- The displayed piece's `Position` changes to row 3, column 4.
- WPF then animates the displayed coordinates.

You may see additional rule evaluation when status is refreshed. Do not assume every later `MoveCommand.Execute` breakpoint represents execution of the chosen move a second time.

### Exercise B: inspect one capture

In a position with a legal capture, inspect the chosen command tree.

Identify the remove operation and the move operation. Step through the presentation visitor and observe that removal is marked before the captured visual object actually leaves `BoardVM.Pieces`.

Place a breakpoint in `MainWindow.RemoveCompleted` or `BoardVM.CleanUp` to see the final cleanup stage after the fade animation.

### Exercise C: inspect undo and redo

After playing one move, inspect:

- The current game's `LastUpdate`.
- The game stored in that history entry.
- The previous game's `NextUpdate`.

Invoke Undo and step through `CommandNegator`. Confirm that the model changes by adopting the saved game, while inverse commands update the presentation.

Invoke Redo. Confirm that the forward update supplies both the destination game and the forward command.

### Exercise D: understand king-safety filtering

Use a known valid position where a piece shields its king from an attacking sliding piece. Compare movement candidates with the final updates returned by the rulebook.

A candidate that exposes the king should be removed by final safety filtering, so its destination is not offered as a legal target.

If using custom-board editing, ensure the position has the necessary kings and otherwise meaningful chess state. The rulebook is not a comprehensive validator for arbitrary edited positions.

### Exercise E: inspect promotion ambiguity

In a valid promotion position, inspect the destination's update list. It should contain alternatives for promotion piece types produced by `PromotionRule`.

Step into `MainWindow.Choose`. The dialog resolves a choice between future states; it is not the rule engine deciding which piece is strongest.

These exercises describe observations to verify while debugging, not an automated regression suite.

## 4.3 Watch expressions and useful state

| Inspect | What it explains |
| --- | --- |
| Current game's `ActivePlayer` / `PassivePlayer` | Whose turn the current state represents |
| `Board.GetPiece(position)` | Actual domain occupancy |
| `BoardVM.Source` | Which field is selected in the UI |
| `BoardVM` target dictionary | Which future updates belong to each clickable square |
| `Update.Game` and `Update.Command` | The state paired with an action in this context |
| `FirstCommand` / `SecondCommand` | Composition and ordering inside a sequence |
| Displayed piece's `Position` / `Removed` | Presentation changes and pending visual cleanup |
| `LastUpdate` / `NextUpdate` | Navigation links |

Use care when expanding lazy enumerables in the debugger. Evaluation can create candidates or invoke property code. In particular, this fork's `History` getter assigns update IDs while enumerating.

The IDs and counters are debugging/navigation metadata, not a reliable count of committed moves. One hypothetical action can create several intermediate games; one source selection can evaluate many actions.

## 4.4 Common misunderstandings

### "Why does a move execute when I only select a piece?"

The model is simulating candidate futures. The ViewModel has not adopted one yet.

### "Why does Rook.cs not contain the rook rules?"

Operations are grouped into visitors. Read `ThreatAnalyzer.Visit(Rook)` and `MovementRule.Visit(Rook)`.

### "Does Bind update a WPF binding?"

No. `IMaybe<T>.Bind` composes operations returning optional values. WPF bindings are a separate presentation mechanism.

### "Is a threat always a legal destination?"

No. Pawn attacks differ from pawn forward movement, and own-king safety can reject candidate movement. Threat generation and final legal-update generation answer different questions.

### "Why is PassivePlayer checked after a move?"

The full turn already swapped players. The new passive player is the player who just moved. `EndRule` has a different evaluation context because it does not append that swap when checking for possible escapes.

### "Does the ViewModel replay the command into the model after selection?"

Not in the normal selection path. It adopts the precomputed future and visits the command to update presentation and application effects.

### "Does an Update always contain the next state?"

No. Selectable futures hold after-states; history records hold before-states. Trace where the update was created.

### "Does undo just execute inverse commands against the current model?"

No. It adopts the retained previous game, then interprets inverse commands for presentation.

### "Why is the captured piece still in the collection?"

The presentation retains it temporarily so it can fade out. Model occupancy and animation lifetime are different concerns.

### "Does immutable mean thread-safe everywhere?"

No. The board collection is immutable, but this fork also has mutable navigation links, flags, collections, shared metadata, and WPF objects. UI collection updates remain subject to WPF threading rules.

### "Is this an AI chess engine?"

The inspected core calculates legal choices and game status. It does not choose an opponent move through minimax, alpha-beta search, or another computer-player search mechanism. Simulating one move for legality is not, by itself, AI.

## 4.5 Safely extending the design

The following are navigation suggestions, not features already supplied by the project.

### Change appearance or animation

Start in `Chess.View\Window\MainWindow.xaml`, the piece templates/resources, and template selectors. Keep domain occupancy independent of animation progress.

Changes to labels, margins, or rotation may also involve `PositionVM`, `BoardConstants`, coordinate translation, and transform converters. Inspect both input and rendering paths so visual positions and clicked squares continue to agree.

### Change an existing chess rule

Start with the relevant rule and examine its interaction with `StandardRulebook.GetUpdates` and `EndRule`.

For example, a move-generation change can affect both selectable targets and checkmate/stalemate detection. Verify those caller-visible outcomes, not just whether a new candidate appears.

Preserve the distinction between attacked squares and movement destinations, especially for pawns and kings.

### Add a piece type

Visitor makes this a cross-cutting change. Anticipate changes to:

- Piece types and piece-visitor interfaces.
- Movement and threat visitors.
- Other piece-type predicates implemented as visitors.
- Template selection and graphics.
- Piece creation/serialization surfaces and custom-board setup if the type is exposed there.

Adding a subclass alone is not sufficient.

### Add another compound action

First see whether existing move/remove/spawn commands can express it. A new primitive command requires considering execution, visitor contracts, target identification, inverse presentation, move-list handling, and persistence surfaces.

The existing chooser assumes multiple updates at a destination mean promotion. Another ambiguous action needs a corresponding selection UX.

### Add a computer opponent

Treat legal-update generation as a potential input to a separate decision-making component. Search and evaluation would choose among legal states; they should not be embedded in WPF animations or replace the rulebook's legality checks.

Do not assume the whole current game graph is safe to share and mutate across worker threads simply because boards use immutable dictionaries.

## 4.6 Further application-feature entry points

| Feature | Starting points |
| --- | --- |
| Play / Record / Review mode changes | `ChessGameVM.DoMessengerRegistration`, `AppModeChangedHandler`, `StatusModeListViewVM` |
| Manual / automatic navigation | `ReviewModeHeaderDisplayVM`, `ManualReviewModeVM`, `AutoReviewModeVM` |
| Notes and recording hooks | `StatusModeListViewVM`, `ChessGameVM.Visit(EndTurnCommand)` |
| Custom board | `ChessGameVM.SetupCustomBoard`, `BuildCustomBoardVM`, `BoardSerializationService` |
| View selection for status/mode objects | `Chess.View\Resources\MvvmDataTemplates.xaml` |
| Piece graphics | `PieceSymbolSelector`, `Chess.View\Resources\PiecesDataTemplates.xaml`, `Chess.View\Piece` |
| Dialog abstraction | `Chess.Services\IWindowService.cs`, `Chess.View\Window\WindowService.cs` |

Explore one feature by tracing a concrete user action rather than reading all message types at once.

## 4.7 Final mental model

```text
Domain state:      What is true on the board?
Rules:             Which next states are allowed?
Model commands:    What operations describe a transition?
Visitors:          How should this operation/type be interpreted here?
ViewModels:        What is selected, displayed, highlighted, or being removed?
WPF:               How should those presentation changes look over time?
History:           Which retained state and action support navigation?
```

Once those questions are separate, most of the apparent game-development complexity becomes recognizable .NET application design.
