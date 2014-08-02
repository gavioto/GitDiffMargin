#region using

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.VisualStudio.Text.Editor;

#endregion

namespace GitDiffMargin.ViewModel
{
    internal class DiffMarginViewModel : ViewModelBase
    {
        private readonly IMarginCore _marginCore;
        private readonly DiffUpdateBackgroundParser _parser;
        private RelayCommand<DiffViewModel> _previousChangeCommand;
        private RelayCommand<DiffViewModel> _nextChangeCommand;

        internal DiffMarginViewModel(IWpfTextView textView, IMarginCore marginCore)
        {
            if (marginCore == null)
                throw new ArgumentNullException("marginCore");

            _marginCore = marginCore;

            DiffViewModels = new ObservableCollection<DiffViewModel>();

            _parser = new DiffUpdateBackgroundParser(textView.TextBuffer, textView.TextDataModel.DocumentBuffer, TaskScheduler.Default, _marginCore.TextDocumentFactoryService, _marginCore.GitCommands);
            _parser.ParseComplete += HandleParseComplete;
            _parser.RequestParse(false);
        }
       
        public ObservableCollection<DiffViewModel> DiffViewModels { get; private set; }

        public RelayCommand<DiffViewModel> PreviousChangeCommand
        {
            get { return _previousChangeCommand ?? (_previousChangeCommand = new RelayCommand<DiffViewModel>(PreviousChange, PreviousChangeCanExecute)); }
        }

        public RelayCommand<DiffViewModel> NextChangeCommand
        {
            get { return _nextChangeCommand ?? (_nextChangeCommand = new RelayCommand<DiffViewModel>(NextChange, NextChangeCanExecute)); }
        }

        private bool PreviousChangeCanExecute(DiffViewModel currentEditorDiffViewModel)
        {
            return DiffViewModels.IndexOf(currentEditorDiffViewModel) > 0;
        }

        private bool NextChangeCanExecute(DiffViewModel currentEditorDiffViewModel)
        {
            return DiffViewModels.IndexOf(currentEditorDiffViewModel) < (DiffViewModels.Count - 1);
        }

        private void PreviousChange(DiffViewModel currentEditorDiffViewModel)
        {
            MoveToChange(currentEditorDiffViewModel, -1);
        }

        private void NextChange(DiffViewModel currentEditorDiffViewModel)
        {
            MoveToChange(currentEditorDiffViewModel, +1);
        }

        private void MoveToChange(DiffViewModel currentDiffViewModel, int indexModifier)
        {
            var diffViewModelIndex = DiffViewModels.IndexOf(currentDiffViewModel) + indexModifier;
            var diffViewModel  = DiffViewModels[diffViewModelIndex];

            _marginCore.MoveToChange(diffViewModel.LineNumber);    

            //todo uncomment this because it is sued for the editor margin
            //currentDiffViewModel.ShowPopup = false;
        }

        public void RefreshDiffViewModelPositions()
        {
            foreach (var diffViewModel in DiffViewModels)
            {
                diffViewModel.RefreshPosition();
            }
        }

        public override void Cleanup()
        {
            _parser.Dispose();
            base.Cleanup();
        }

        private void HandleParseComplete(object sender, ParseResultEventArgs e)
        {
            _marginCore.CheckBeginInvokeOnUI(() =>
            {
                //todo do not clear if it the same collection returned

                //todo uncomment this because it is sued for the editor margin
                //if (DiffViewModels.Any(dvm => dvm.ShowPopup)) return;

                DiffViewModels.Clear();

                var diffResult = e as DiffParseResultEventArgs;
                if (diffResult == null) return;

                foreach (var diffViewModel in diffResult.Diff.Select(hunkRangeInfo => new EditorDiffViewModel(hunkRangeInfo, _marginCore)))
                {
                    DiffViewModels.Add(diffViewModel);
                }
            });
        }
    }
}