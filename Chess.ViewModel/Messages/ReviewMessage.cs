//-----------------------------------------------------------------------
// <copyright file="ReviewMessage.cs">
//     Copyright (c) Michael Szvetits. All rights reserved.
// </copyright>
// <author>Michael Szvetits</author>
//-----------------------------------------------------------------------
namespace Chess.ViewModel.Messages
{
    public record MessageFromRecordReviewModeVMToReviewModeHeaderDisplayVM(bool StartReviewLoop)
    {
        public bool StartReviewLoop { get; private set; } = StartReviewLoop;
    }
}