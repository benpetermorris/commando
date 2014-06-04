using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;
using GalaSoft.MvvmLight;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.Engine;
using twomindseye.Commando.Engine.Extension;

namespace twomindseye.Commando.UI.ViewModels
{
    class EditCommandArgumentViewModel : ViewModelBase
    {
        readonly bool _allowUnspecifiedForNonOptional;
        readonly ReadOnlyCollection<FacetMonikerInfo> _monikers;
        FacetMonikerInfo _selectedMoniker;

        public EditCommandArgumentViewModel(CommandExecutor executor, int parameterIndex, 
            bool allowUnspecifiedForNonOptional)
        {
            _allowUnspecifiedForNonOptional = allowUnspecifiedForNonOptional;
            Executor = executor;
            ParameterIndex = parameterIndex;

            var monikers = new List<FacetMonikerInfo>();

            if (Argument.FacetMoniker == null || _allowUnspecifiedForNonOptional || Parameter.Optional)
            {
                var info = new FacetMonikerInfo();

                if (Argument.FacetMoniker == null)
                {
                    _selectedMoniker = info;
                }

                monikers.Add(info);
            }

            if (Argument.FacetMoniker != null)
            {
                _selectedMoniker = new FacetMonikerInfo(Argument.FacetMoniker);
                monikers.Add(_selectedMoniker);
            }

            if (Argument.Suggestions != null)
            {
                monikers.AddRange(Argument.Suggestions.Select(x => new FacetMonikerInfo(x)));
            }

            _monikers = new ReadOnlyCollection<FacetMonikerInfo>(monikers);
        }

        public CommandExecutor Executor { get; private set; }
        public int ParameterIndex { get; private set; }

        public CommandParameter Parameter
        {
            get
            {
                return Executor.Command.Parameters[ParameterIndex];
            }
        }

        public CommandArgument Argument
        {
            get
            {
                return Executor.Arguments[ParameterIndex];
            }
        }

        public ReadOnlyCollection<FacetMonikerInfo> Monikers
        {
            get
            {
                return _monikers;
            }
        }

        public FacetMonikerInfo SelectedMoniker
        {
            get
            {
                return _selectedMoniker;
            }
            set
            {
                if (!Monikers.Contains(value))
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                _selectedMoniker = value;

                RaisePropertyChanged("SelectedMoniker");
                RaisePropertyChanged("SelectedTuple");
            }
        }

        public Tuple<CommandParameter, FacetMoniker> SelectedTuple
        {
            get
            {
                return Tuple.Create(Parameter, SelectedMoniker.Moniker);
            }
        }

        public class FacetMonikerInfo
        {
            public FacetMonikerInfo()
            {
            }

            public FacetMonikerInfo(FacetMoniker moniker)
            {
                Moniker = moniker;
            }

            public FacetMoniker Moniker { get; private set; }

            public string DisplayName
            {
                get { return Moniker == null ? "Not Specified" : Moniker.DisplayName; }
            }
        }
    }
}
