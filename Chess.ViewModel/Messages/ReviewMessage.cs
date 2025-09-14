//-----------------------------------------------------------------------
// <copyright file="ReviewMessage.cs">
//     Copyright (c) Michael Szvetits. All rights reserved.
// </copyright>
// <author>Michael Szvetits</author>
//-----------------------------------------------------------------------
using Chess.ViewModel.Game;

namespace Chess.ViewModel.Messages
{
    public record MessageFromRecordReviewModeVMToReviewModeHeaderDisplayVM(bool StartReviewLoop)
    {
        public bool StartReviewLoop { get; private set; } = StartReviewLoop;
    }

    public record MessageFromReviewModeHeaderDisplayVMToChessGameVM(ReviewMode reviewModeValue)
    {
        public ReviewMode ReviewModeValue { get; private set; } = reviewModeValue;
    }

    public record MessageFromStatusModeListViewVMToChessGameVM(AppMode appModeValue)
    {
        public AppMode AppModeValue { get; private set; } = appModeValue;
    }
}