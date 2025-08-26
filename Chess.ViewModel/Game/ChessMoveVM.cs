using Chess.Model.Piece;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Chess.ViewModel.Game
{
    /// <summary>
    /// Represents a view model for a chess move, including the source and target positions,  as well as the chess piece
    /// being moved.
    /// </summary>
    /// <remarks>This class is typically used to encapsulate the details of a single chess move in a user
    /// interface or application logic. It includes the starting position, the destination position, and the piece
    /// being moved.</remarks>
    public partial class ChessMoveVM: ObservableObject
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

        public ChessMoveVM(PositionVM source, PositionVM target, ChessPiece chessPiece, int moveNumber, string shortDescription)
        {
            this.Source = source;
            this.Target = target;
            this.Piece = chessPiece;
            this.MoveNumber = moveNumber;
            this.ShortDescription = shortDescription;
        }

        public PositionVM Source
        {
            get => source;
            set
            {
                SetProperty(ref source, value);
                this.OnPropertyChanged(nameof(this.SourceString));
            }
        }

        /// <summary>
        /// Gets or sets a brief description or summary of this move.
        /// </summary>
        public string ShortDescription { get; set; }

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
                if (this.target == null)
                {
                    return "-";
                }
                return GetRowColumnId(target.Row, target.Column);
            }
        }

        public PositionVM Target
        {
            get => target;
            set
            {
                SetProperty(ref target, value);
                this.OnPropertyChanged(nameof(this.TargetString));
            }
        }

        /// <summary>
        /// Gets the chess piece thats currently being moved.
        /// </summary>
        public ChessPiece Piece
        {
            get => this.piece;

            private set
            {
                SetProperty(ref piece, value);
                this.OnPropertyChanged(nameof(this.PieceName));
                this.OnPropertyChanged(nameof(this.PieceColor));
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

        [ObservableProperty]
        private int moveNumber;

        [ObservableProperty]
        private string gameAndUpdateInfo;

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
    }
}

