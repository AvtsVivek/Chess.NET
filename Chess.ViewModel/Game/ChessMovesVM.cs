using System.Collections.Generic;
using System.Collections.ObjectModel;

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
        /// <summary>
        /// Initializes a new instance of the <see cref="ChessMoveSequenceVM"/> class.
        /// </summary>
        /// <remarks>This constructor initializes the <see cref="ChessMoves"/> collection to an empty 
        /// <see cref="ObservableCollection{T}"/> of <see cref="ChessMoveVM"/> objects.</remarks>
        public ChessMoveSequenceVM() 
        {
            this.ChessMoves = new ObservableCollection<ChessMoveVM>();
        }

        /// <summary>
        /// Gets or sets the collection of chess moves in the current game.
        /// </summary>
        public ObservableCollection<ChessMoveVM> ChessMoves { get; set; }

    }
}

