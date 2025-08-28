//-----------------------------------------------------------------------
// <copyright file="ReviewMessage.cs">
//     Copyright (c) Michael Szvetits. All rights reserved.
// </copyright>
// <author>Michael Szvetits</author>
//-----------------------------------------------------------------------
namespace Chess.ViewModel.Messages
{
    public class ReviewMessage 
    {
        public bool StartReviewLoop { get; private set; }
        public ReviewMessage(bool startReviewLoop) 
        {
            StartReviewLoop = startReviewLoop;
        }
    }
}