using Chess.ViewModel.Command;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Chess.ViewModel.StatusAndMode
{
    public class AutoReviewModeVM : ObservableObject
    {
        private const double autoReviewTimeIntervalLowerLimit = 0.1;
        private const double autoReviewTimeIntervalUpperLimit = 10;
        private readonly GenericCommand incrementLowCommand;
        private readonly GenericCommand incrementHighCommand;
        private readonly GenericCommand decrementLowCommand;
        private readonly GenericCommand decrementHighCommand;
        private readonly GenericCommand undoCommand;
        private readonly GenericCommand redoCommand;
        private CancellationTokenSource autoReviewCts;

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

        public void StartAutoReviewLoop()
        {
            autoReviewCts = new CancellationTokenSource();
            
            bool undoAvailable = true;
            bool redoAvailable = true;
            bool undoInProgress = true;

            Task.Run(async () =>
            {
                while (!autoReviewCts.IsCancellationRequested)
                {
                    if (undoAvailable && undoInProgress)
                    {
                       if (Application.Current.Dispatcher.CheckAccess())
                        {
                            this.undoCommand.Execute(null);
                        }
                        else
                        {
                            Application.Current.Dispatcher.Invoke(() => this.undoCommand.Execute(null));
                        }
                    }

                    undoAvailable = this.undoCommand.CanExecute(null);

                    if (redoAvailable && !undoInProgress)
                    {
                        if (Application.Current.Dispatcher.CheckAccess())
                        {
                            this.redoCommand.Execute(null);
                        }
                        else
                        {
                            Application.Current.Dispatcher.Invoke(() => this.redoCommand.Execute(null));
                        }
                    }

                    redoAvailable = this.redoCommand.CanExecute(null);

                    if(!undoAvailable)
                    {
                        undoInProgress = false;
                    }

                    if(!redoAvailable)
                    {
                        undoInProgress = true;
                    }

                    var intervalSeconds = Math.Round(autoReviewTimeInterval, 1);

                    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), autoReviewCts.Token);
                }
            }, autoReviewCts.Token);
        }

        public void StopAutoReviewLoop()
        {
            autoReviewCts?.Cancel();
        }

        public void ViewLoaded()
        {
            StartAutoReviewLoop();
        }

        public void ViewUnloaded()
        {
            StopAutoReviewLoop();
        }
    }
}