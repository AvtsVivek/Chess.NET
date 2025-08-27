//-----------------------------------------------------------------------
// <copyright file="ReviewMessage.cs">
//     Copyright (c) Michael Szvetits. All rights reserved.
// </copyright>
// <author>Michael Szvetits</author>
//-----------------------------------------------------------------------
using Chess.ViewModel.Game;

namespace Chess.ViewModel.Messages
{
    using Chess.Model.Game;

    public class ReviewMessage 
    {
        public bool StartReviewLoop { get; private set; }
        public ReviewMessage(bool startReviewLoop) 
        {
            StartReviewLoop = startReviewLoop;
        }
    }
}