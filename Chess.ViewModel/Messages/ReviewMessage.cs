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
    using CommunityToolkit.Mvvm.Messaging.Messages;

    public class ReviewMessage : ValueChangedMessage<ReviewMode>
    {
        public bool StartReviewLoop { get; private set; }
        public ChessGame Game { get; set; }
        public ReviewMessage(ReviewMode newReviewMode, bool startReviewLoop) : base(newReviewMode)
        {
            StartReviewLoop = startReviewLoop;
        }
    }
}