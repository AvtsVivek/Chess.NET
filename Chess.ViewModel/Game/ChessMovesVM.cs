using Chess.Model.Piece;
using Chess.Model.Rule;
using Chess.ViewModel.Piece;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Chess.ViewModel.Game
{
    /// <summary>
    /// Represents a sequence of chess moves in a game.
    /// </summary>
    /// <remarks>This class provides a stack-based structure to manage a sequence of chess moves.  Moves are
    /// stored in a <see cref="Stack{T}"/>, allowing for efficient addition and  removal of moves in a last-in,
    /// first-out (LIFO) order.</remarks>
    public class ChessMoveSequenceVM
    {
        public ObservableCollection<ChessMoveVM> ChessMoves { get; set; }
                
        public ChessMoveSequenceVM() 
        {
            this.ChessMoves = new ObservableCollection<ChessMoveVM>();
        }
    }

    /// <summary>
    /// Represents a view model for a chess move, including the source and target positions,  as well as the chess piece
    /// being moved.
    /// </summary>
    /// <remarks>This class is typically used to encapsulate the details of a single chess move in a user
    /// interface or application logic. It includes the starting position, the destination position, and the piece
    /// being moved.</remarks>
    public class ChessMoveVM: INotifyPropertyChanged
    {
        /// <summary>
        /// Represents the source of the move.
        /// </summary>
        private PositionVM source;

        /// <summary>
        /// Represents the target of the move.
        /// </summary>
        private PositionVM target;

        /// <summary>
        /// Represents the chess piece being moved.
        /// </summary>
        private ChessPiece piece;

        public ChessMoveVM() { }

        public ChessMoveVM(PositionVM source, PositionVM target, ChessPiece chessPiece) 
        {
            this.Source = source;
            this.Target = target;
            this.Piece = chessPiece;
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        public PositionVM Source
        {
            get
            {
                return this.source;
            }

            private set
            {
                if (this.source != value)
                {
                    this.source = value ?? throw new ArgumentNullException(nameof(this.Source));
                    this.OnPropertyChanged(nameof(this.Source));
                    this.OnPropertyChanged(nameof(this.SourceString));
                }
            }
        }

        public string SourceString
        {
            get
            {
                return GetRowColumnId(source.Row, source.Column);
            }
        }

        public string TargetString
        {
            get
            {
                return GetRowColumnId(target.Row, target.Column);
            }
        }

        public PositionVM Target 
        {
            get
            {
                return this.target;
            }

            private set
            {
                if (this.target != value)
                {
                    this.target = value ?? throw new ArgumentNullException(nameof(this.Target));
                    this.OnPropertyChanged(nameof(this.Target));
                    this.OnPropertyChanged(nameof(this.TargetString));
                }
            }
        }

        /// <summary>
        /// Gets the chess piece thats currently being moved.
        /// </summary>
        public ChessPiece Piece
        {
            get
            {
                return this.piece;
            }

            private set
            {
                if (this.piece != value)
                {
                    this.piece = value ?? throw new ArgumentNullException(nameof(this.Piece));
                    this.OnPropertyChanged(nameof(this.Piece));
                    this.OnPropertyChanged(nameof(this.PieceName));
                    this.OnPropertyChanged(nameof(this.PieceColor));
                }
            }
        }

        /// <summary>
        /// Gets the color of the piece.
        /// </summary>
        public Color PieceColor
        {
            get
            {
                return this.piece.Color;
            }
        }

        public string PieceName
        {
            get
            {
                return this.piece.GetType().Name;
            }
        }

        public string PieceId
        {
            get
            {
                return this.PieceColor.ToString().ToLower() + this.piece.GetType().Name;
            }
        }

        private int moveNumber;

        /// <summary>
        /// Gets the chess piece thats currently being moved.
        /// </summary>
        public int MoveNumber
        {
            get
            {
                return this.moveNumber;
            }

            set
            {
                if (this.moveNumber != value)
                {
                    // this.moveNumber = value ?? throw new ArgumentNullException(nameof(this.MoveNumber));
                    this.moveNumber = value;
                    this.OnPropertyChanged(nameof(this.MoveNumber));
                }
            }
        }

        private string GetRowColumnId(int row, int column)
        {
            var columnId = string.Empty;
            switch(column)
            {
                case 0:
                    {
                        columnId = "A";
                    }
                    break;
                case 1:
                    {
                        columnId = "B";
                    }
                    break;
                case 2:
                    {
                        columnId = "C";
                    }
                    break;
                case 3:
                    {
                        columnId = "D";
                    }
                    break;
                case 4:
                    {
                        columnId = "E";
                    }
                    break;
                case 5:
                    {
                        columnId = "F";
                    }
                    break;
                case 6:
                    {
                        columnId = "G";
                    }
                    break;
                case 7:
                    {
                        columnId = "H";
                    }
                    break;
            }
            return (row + 1).ToString() + columnId;
        }


        /// <summary>
        /// Fires the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">The name of the property that has been changed.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}

