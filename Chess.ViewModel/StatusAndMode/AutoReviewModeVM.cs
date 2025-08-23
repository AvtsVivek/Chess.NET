using Chess.ViewModel.Command;
using System;
using System.ComponentModel;

namespace Chess.ViewModel.StatusAndMode
{
    public class AutoReviewModeVM : INotifyPropertyChanged
    {
        private const double autoReviewTimeIntervalLowerLimit = 0.1;
        private const double autoReviewTimeIntervalUpperLimit = 10;

        private readonly GenericCommand incrementLowCommand;
        private readonly GenericCommand incrementHighCommand;
        private readonly GenericCommand decrementLowCommand;
        private readonly GenericCommand decrementHighCommand;

        private readonly GenericCommand undoCommand;

        private readonly GenericCommand redoCommand;

        public AutoReviewModeVM(GenericCommand undoCommand, GenericCommand redoCommand)
        {
            this.undoCommand = undoCommand;

            this.redoCommand = redoCommand;

            this.incrementLowCommand = new GenericCommand
            (
                () => true,
                () =>
                {
                    IncrementDecrementTimeInterval(IncrementLowValue);
                }
            );

            this.incrementHighCommand = new GenericCommand
            (
                () => true,
                () =>
                {
                    IncrementDecrementTimeInterval(IncrementHighValue);
                }
            );

            this.decrementLowCommand = new GenericCommand
            (
                () => true,
                () =>
                {
                    IncrementDecrementTimeInterval(DecrementLowValue, "decrement");
                }
            );

            this.decrementHighCommand = new GenericCommand
            (
                () => true,
                () =>
                {
                    IncrementDecrementTimeInterval(DecrementHighValue, "decrement");
                }
            );

            if (ChessAppSettings.Default.AutoReviewTimeInterval < autoReviewTimeIntervalLowerLimit)
            {
                autoReviewTimeInterval = autoReviewTimeIntervalLowerLimit;
            }
            else if (ChessAppSettings.Default.AutoReviewTimeInterval > autoReviewTimeIntervalUpperLimit)
            {
                autoReviewTimeInterval = autoReviewTimeIntervalUpperLimit;
            }
            else
                autoReviewTimeInterval = ChessAppSettings.Default.AutoReviewTimeInterval;
        }

        //public GenericCommand UndoCommand => this.undoCommand;

        //public GenericCommand RedoCommand => this.redoCommand;

        public GenericCommand IncrementLowCommand => this.incrementLowCommand;
        public GenericCommand IncrementHighCommand => this.incrementHighCommand;
        public GenericCommand DecrementLowCommand => this.decrementLowCommand;
        public GenericCommand DecrementHighCommand => this.decrementHighCommand;

        public double DecrementLowValue { get; set; } = -0.1;
        public double DecrementHighValue { get; set; } = -1;
        public double IncrementLowValue { get; set; } = 0.1;
        public double IncrementHighValue { get; set; } = 1;


        private double autoReviewTimeInterval;

        public string AutoReviewTimeIntervalStringValue
        {
            get => autoReviewTimeInterval.ToString("F1");
        }

        private void IncrementDecrementTimeInterval(double value, string incrementDecrement = "increment")
        {
            autoReviewTimeInterval = Math.Round(autoReviewTimeInterval, 1);

            if (autoReviewTimeInterval < autoReviewTimeIntervalLowerLimit && incrementDecrement == "decrement")
            {
                return;
            }

            if (autoReviewTimeInterval > autoReviewTimeIntervalUpperLimit && incrementDecrement == "increment")
            {
                return;
            }

            var newValue = Math.Round(autoReviewTimeInterval + value, 1);

            if (newValue < autoReviewTimeIntervalLowerLimit && incrementDecrement == "decrement")
            {
                return;
            }

            if (newValue > autoReviewTimeIntervalUpperLimit && incrementDecrement == "increment")
            {
                return;
            }

            autoReviewTimeInterval = newValue;
            OnPropertyChanged(nameof(AutoReviewTimeIntervalStringValue));

            var roundedValue = Math.Round(autoReviewTimeInterval, 1);

            ChessAppSettings.Default.AutoReviewTimeInterval = roundedValue;
            ChessAppSettings.Default.Save();
        }


        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Fires the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">The name of the property that has been changed.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void ViewLoaded()
        {
            // this.redoCommand.Execute(null);
        }

        public void ViewUnloaded()
        {
            // No implementation needed currently.
        }
    }
}