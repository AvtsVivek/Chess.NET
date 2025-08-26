//-----------------------------------------------------------------------
// <copyright file="BoardVM.cs">
//     Copyright (c) Michael Szvetits. All rights reserved.
// </copyright>
// <author>Michael Szvetits</author>
//-----------------------------------------------------------------------
namespace Chess.ViewModel.Game
{
    using Chess.Model.Command;
    using Chess.Model.Data;
    using Chess.Model.Game;
    using Chess.ViewModel.Piece;
    using Chess.ViewModel.Visitor;
    using CommunityToolkit.Mvvm.ComponentModel;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    /// <summary>
    /// Represents the view model of a chess board.
    /// </summary>
    public class BoardVM : ObservableObject
    {
        /// <summary>
        /// Represents the visitor that can set potential targets for chess pieces on the board.
        /// </summary>
        private readonly TargetSetter targetSetter;

        /// <summary>
        /// Represents the currently presented fields of the chess board.
        /// </summary>
        private readonly FieldVM[,] fields;

        /// <summary>
        /// Represents the labels for columns and rows on all of the four sides of the board.
        /// </summary>
        private readonly List<RowColumnLabelVM> rowColumnLabels;

        /// <summary>
        /// Represents the mapping from target fields to potential game updates.
        /// </summary>
        private readonly Dictionary<FieldVM, List<Update>> targets;

        /// <summary>
        /// Represents the sequence of chess moves in the current game.
        /// </summary>
        /// <remarks>This property is used to track and manage the sequence of moves made during a chess
        /// game. It provides access to the move history and supports operations related to move analysis or
        /// replay.</remarks>
        private ChessMoveSequenceVM moveSequence;

        /// <summary>
        /// Represents the current index in the sequence of chess moves.
        /// </summary>
        /// <remarks>This field is used to track the position within a sequence of moves in a chess game.
        /// The value starts at 1 and increments as moves are processed.</remarks>
        private int chessMoveSequenceIndex = 0;

        /// <summary>
        /// Represents the collection of commands being executed for the current active player.
        /// </summary>
        /// <remarks>This list contains instances of commands that are currently being executed by the active player.
        /// Multiple commands are executed during an active players turn.
        /// For example, castling involves both the king and one rook, and here two move commands are executed in sequence.
        /// In a more common scenario, multiple commands are executed after a capture occurs, or a pawn was promoted.
        /// When a capture occurs, a move command and a remove command are executed. 
        /// </remarks>
        private List<ICommand> activePlayerCommands;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoardVM"/> class.
        /// </summary>
        /// <param name="board">The current chess board state.</param>
        public BoardVM(Board board)
        {
            this.activePlayerCommands = new List<ICommand>();
            this.moveSequence = new ChessMoveSequenceVM();
            var pieces = board.Select(p => new PlacedPieceVM(p));
            var fieldArray = new FieldVM[8, 8];
            var fieldVMs =
               from row in Enumerable.Range(0, 8)
               from column in Enumerable.Range(0, 8)
               select new FieldVM(row, column);

            foreach (var field in fieldVMs)
            {
                fieldArray[field.Row, field.Column] = field;
            }

            this.rowColumnLabels = new List<RowColumnLabelVM>();

            for (int row = 0; row < 8; row++)
            {
                var labelVM = new RowColumnLabelVM
                {
                    Column = 0,
                    Label = row.ToString(),
                    Row = row,
                    Height = 1,
                    Width = BoardConstants.BoardMarginForId,
                    DistanceFromBottom = BoardConstants.BoardMarginForId + row,
                    DistanceFromLeft = 0,
                    LabelResourceKey = "digit" + (row + 1).ToString()
                };

                if (row == 7)
                    labelVM.Height = labelVM.Height + BoardConstants.BoardMarginForId;

                rowColumnLabels.Add(labelVM);

                labelVM = new RowColumnLabelVM
                {
                    Column = 7,
                    Label = row.ToString(),
                    Row = row,
                    Height = 1,
                    Width = BoardConstants.BoardMarginForId,
                    DistanceFromBottom = BoardConstants.BoardMarginForId + row,
                    DistanceFromLeft = BoardConstants.BoardMarginForId + 8,
                    LabelResourceKey = "digit" + (row + 1).ToString()
                };

                if (row == 7)
                    labelVM.Height = labelVM.Height + BoardConstants.BoardMarginForId;

                rowColumnLabels.Add(labelVM);
            }

            for (int column = 0; column < 8; column++)
            {
                var labelVM = new RowColumnLabelVM
                {
                    Column = column,
                    Label = column.ToString(),
                    Row = 0,
                    Width = 1,
                    Height = BoardConstants.BoardMarginForId,
                    DistanceFromBottom = 0,
                    DistanceFromLeft = BoardConstants.BoardMarginForId + column,
                    LabelResourceKey = "char" + (column + 1).ToString()
                };

                if (column == 7)
                    labelVM.Width = labelVM.Width + BoardConstants.BoardMarginForId;

                if (column == 0)
                {
                    labelVM.Width = labelVM.Width + BoardConstants.BoardMarginForId;
                    labelVM.DistanceFromLeft = labelVM.DistanceFromLeft - BoardConstants.BoardMarginForId;
                }

                rowColumnLabels.Add(labelVM);

                labelVM = new RowColumnLabelVM
                {
                    Column = column,
                    Label = column.ToString(),
                    Row = 7,
                    Width = 1,
                    Height = BoardConstants.BoardMarginForId,
                    DistanceFromBottom = BoardConstants.BoardMarginForId + 8,
                    DistanceFromLeft = BoardConstants.BoardMarginForId + column,
                    LabelResourceKey = "char" + (column + 1).ToString()
                };

                rowColumnLabels.Add(labelVM);
            }

            this.fields = fieldArray;
            this.Pieces = new ObservableCollection<PlacedPieceVM>(pieces);
            this.targets = new Dictionary<FieldVM, List<Update>>();
            this.targetSetter = new TargetSetter();
        }

        /// <summary>
        /// Gets the sequence of chess moves represented by the view model.
        /// </summary>
        public ChessMoveSequenceVM ChessMoveSequence
        {
            get
            {
                return moveSequence;
            }
        }

        /// <summary>
        /// Gets or sets the selected source field for which the <see cref="targets"/> were determined.
        /// </summary>
        /// <value>
        /// The field which is currently selected by a player before committing to a specific target field
        /// (i.e., before making the actual move of a chess piece).
        /// </value>
        public FieldVM Source { get; set; }

        /// <summary>
        /// Gets the current pieces on the chess board.
        /// </summary>
        /// <value>The current pieces on the chess board.</value>
        public ObservableCollection<PlacedPieceVM> Pieces { get; }

        public IEnumerable<RowColumnLabelVM> RowColumnLabels
        {
            get
            {
                return rowColumnLabels;
            }
        }

        /// <summary>
        /// Gets a sequence of the currently presented chess board fields.
        /// </summary>
        /// <value>A sequence of the chess board fields.</value>
        public IEnumerable<FieldVM> Fields
        {
            get
            {
                var rowCount = this.fields.GetLength(0);
                var columnCount = this.fields.GetLength(1);

                return
                    from row in Enumerable.Range(0, rowCount)
                    from column in Enumerable.Range(0, columnCount)
                    select this.fields[row, column];
            }
        }

        /// <summary>
        /// Clears the current sequence of chess moves.
        /// </summary>
        /// <remarks>This method removes all moves from the current chess move sequence, resetting it to
        /// an empty state.</remarks>
        public void ClearChessMoveSequence()
        {
            this.ChessMoveSequence.ChessMoves.Clear();
        }

        /// <summary>
        /// Removes all chess pieces from the board that are marked as removed.
        /// </summary>
        public void CleanUp()
        {
            foreach (var piece in this.Pieces.Where(p => p.Removed).ToList())
            {
                this.Pieces.Remove(piece);
            }
        }

        /// <summary>
        /// Gets the field of a specified position.
        /// </summary>
        /// <param name="position">The position of the field.</param>
        /// <returns>The field that corresponds to the specified position.</returns>
        public FieldVM GetField(Position position)
        {
            return this.fields[position.Row, position.Column];
        }

        /// <summary>
        /// Clears the selected source field and all of its corresponding targets.
        /// </summary>
        public void ClearUpdates()
        {
            if (this.Source != null)
            {
                this.Source.IsTarget = false;
                this.Source = null;
            }

            foreach (var target in this.targets.Keys)
            {
                target.IsTarget = false;
            }

            this.targets.Clear();
        }

        /// <summary>
        /// Sets the game state updates a chess player can choose from.
        /// </summary>
        /// <param name="updates">The game state updates a chess player can choose from.</param>
        public void SetTargets(IEnumerable<Update> updates)
        {
            foreach (var update in updates)
            {
                update.Command.Accept(this.targetSetter)(update, this);
            }
        }

        /// <summary>
        /// Gets the game state updates for a specified field of the chess board.
        /// </summary>
        /// <param name="field">The field (i.e., target) to get the updates for.</param>
        /// <returns>A list of updates the user can choose from.</returns>
        public IList<Update> GetUpdates(FieldVM field)
        {
            if (this.targets.TryGetValue(field, out var updates))
            {
                return updates;
            }

            return Array.Empty<Update>();
        }

        /// <summary>
        /// Sets the selected source field for which <see cref="targets"/> will be determined.
        /// </summary>
        /// <param name="position">The position of the selected source field.</param>
        public void SetSource(Position position)
        {
            var field = this.fields[position.Row, position.Column];
            field.IsTarget = true;
            this.Source = field;
            this.activePlayerCommands.Clear();
        }

        /// <summary>
        /// Adds a selectable update to a specific field.
        /// </summary>
        /// <param name="position">The position of the field.</param>
        /// <param name="update">The update to be added to the field.</param>
        public void AddUpdate(Position position, Update update)
        {
            var field = this.fields[position.Row, position.Column];
            field.IsTarget = true;

            if (this.targets.TryGetValue(field, out var updates))
            {
                updates.Add(update);
            }
            else
            {
                this.targets.Add(field, new List<Update> { update });
            }
        }

        /// <summary>
        /// Executes the specified end-turn command, either finalizing or undoing the player's moves.
        /// </summary>
        /// <remarks>When finalizing a turn, this method processes all active player commands (e.g., move,
        /// remove, or spawn commands), converts them into chess moves, and adds them to the move sequence. If the
        /// command is an undo operation, the most recent moves are removed from the sequence.  After execution, the
        /// <see cref="ChessMoveSequence"/> property is updated to reflect the current state of the game on the UI.
        /// Note that the end turn command is triggered for regular moves. But for undo case, end turn command is extecuted
        /// at the beginning, then the individual commands are executed in reverse order.
        /// </remarks>
        /// <param name="endTurnCommand">The command representing the end of a turn. If <see cref="EndTurnCommand.IsUndo"/> is <see
        /// langword="true"/>, the method will undo the most recent moves. Otherwise, it will finalize the current
        /// player's active commands and update the move sequence.</param>
        public void Execute(EndTurnCommand endTurnCommand)
        {
            if (endTurnCommand.IsUndo)
            {
                UnPopulateChecssMoveSequence();
            }
            else
            {
                PopulateChecssMoveSequence();
            }

            OnPropertyChanged(nameof(ChessMoveSequence));
        }

        /// <summary>
        /// Executes a move command and repositions the chess piece.
        /// </summary>
        /// <param name="command">The move command to be executed.</param>
        public void Execute(MoveCommand command)
        {
            var piece = this.Pieces.FirstOrDefault
            (
                p => !p.Removed && p.Position.Equals(command.Source)
            );

            if (piece != null)
            {
                piece.Position = new PositionVM(command.Target);
            }

            UpdateMoveSequenceForMoveCommand(command);
        }

        /// <summary>
        /// Executes a remove command and marks the corresponding chess piece as removed.
        /// </summary>
        /// <param name="command">The remove command to be executed.</param>
        public void Execute(RemoveCommand command)
        {
            var piece = this.Pieces.FirstOrDefault
            (
                p => !p.Removed && p.Position.Equals(command.Position)
            );

            if (piece != null)
            {
                piece.Removed = true;
            }

            UpdateMoveSequenceForRemoveCommand(command);
        }

        /// <summary>
        /// Executes a spawn command and adds a new chess piece to the board.
        /// </summary>
        /// <param name="command">The spawn command to be executed.</param>
        public void Execute(SpawnCommand command)
        {
            this.Pieces.Add(new PlacedPieceVM(command.Position, command.Piece));

            UpdateMoveSequenceForSpawnCommand(command);
        }


        /// <summary>
        /// Executes the specified command to update the last update.
        /// </summary>
        /// <remarks>This method processes the provided <see cref="SetLastUpdateCommand"/> to update the
        /// relevant state. Ensure that the <paramref name="command"/> contains valid data before invoking this
        /// method.</remarks>
        /// <param name="command">The command containing the necessary data to set the last update timestamp. Cannot be null.</param>
        public void Execute(SetLastUpdateCommand command)
        {
            // Need to understand more. 
            if (command.Update.HasValue)
            {
                var update = command.Update.Yield().FirstOrDefault();

                // The following is for debugging purposes only.
                // Comment it out in production code.

                //LastUpdateInfo = $"Update: {update.UpdateId} " +
                //    $"GameId: {update.Game.GameId} LastUpdateId: {update.Game.LastUpdateId} " +
                //    $"NextUpdateId: {update.Game.NextUpdateId}";

                this.OnPropertyChanged(nameof(this.LastUpdateInfo));

                var moves = this.ChessMoveSequence.ChessMoves
                    .Where(move => move.MoveNumber == chessMoveSequenceIndex);

                foreach (var move in moves)
                {
                    move.GameAndUpdateInfo = LastUpdateInfo;
                }
            }
        }

        public string LastUpdateInfo { get; set; }

        /// <summary>
        /// Updates the active players command list with the specified move command.
        /// </summary>
        /// <remarks>If the move command is not an undo operation, the method creates a new chess move
        /// representation and adds the command to the active player commands list.</remarks>
        /// <param name="moveCommand">The move command to process. This command represents a chess move and contains details such as the source
        /// and target positions, the piece being moved, and whether the move is an undo operation.</param>
        private void UpdateMoveSequenceForMoveCommand(MoveCommand moveCommand)
        {
            if (!moveCommand.IsUndo)
            {
                this.activePlayerCommands.Add(moveCommand);
            }
        }

        /// <summary>
        /// Updates the move sequence to account for a remove command.
        /// </summary>
        /// <remarks>If the <paramref name="removeCommand"/> is not an undo operation, this method adds a
        /// new move  to the sequence representing the removal of a piece and includes the command 
        /// in the current active players command list </remarks>
        /// <param name="removeCommand">The remove command to process. Must not be null.</param>
        private void UpdateMoveSequenceForRemoveCommand(RemoveCommand removeCommand)
        {
            if (!removeCommand.IsUndo)
            {
                this.activePlayerCommands.Add(removeCommand);
            }
        }

        /// <summary>
        /// Updates the move sequence to include a spawn command for a chess piece.
        /// </summary>
        /// <remarks>This method processes the provided <paramref name="spawnCommand"/> and adds it to the
        /// active player commands if it is not marked as an undo operation. </remarks>
        /// <param name="spawnCommand">The spawn command containing the details of the piece to be added to the move sequence.</param>
        private void UpdateMoveSequenceForSpawnCommand(SpawnCommand spawnCommand)
        {
            if (!spawnCommand.IsUndo)
            {
                this.activePlayerCommands.Add(spawnCommand);
            }
        }

        private void UnPopulateChecssMoveSequence()
        {
            var movesToBeRemoved = this.ChessMoveSequence.ChessMoves
                .Where(move => move.MoveNumber == chessMoveSequenceIndex)
                .ToList();

            foreach (var move in movesToBeRemoved)
                this.ChessMoveSequence.ChessMoves.Remove(move);

            chessMoveSequenceIndex--;
        }

        private void PopulateChecssMoveSequence()
        {
            chessMoveSequenceIndex++;
            foreach (var command in this.activePlayerCommands)
            {
                ChessMoveVM chessMove = null;
                if (command is MoveCommand moveCommand)
                {
                    chessMove = new ChessMoveVM
                    (
                        new PositionVM(moveCommand.Source),
                        new PositionVM(moveCommand.Target),
                        moveCommand.Piece,
                        chessMoveSequenceIndex,
                        "Moved"
                    );
                }
                else if (command is RemoveCommand removeCommand)
                {
                    chessMove = new ChessMoveVM
                    (
                        new PositionVM(removeCommand.Position),
                        null,
                        removeCommand.Piece,
                        chessMoveSequenceIndex,
                        removeCommand.IsPromotion? "Promoted" : "Captured"
                    );
                }
                else if (command is SpawnCommand spawnCommand)
                {
                    chessMove = new ChessMoveVM
                    (
                        new PositionVM(spawnCommand.Position),
                        null,
                        spawnCommand.Piece,
                        chessMoveSequenceIndex,
                        "Appeared"
                    );
                }

                if (chessMove != null)
                {
                    // Insert the move at the beginning of the sequence to
                    // ensure the most recent move is at the top.
                    this.ChessMoveSequence.ChessMoves.Insert(0, chessMove);
                }
            }
            activePlayerCommands.Clear();
        }
    }
}