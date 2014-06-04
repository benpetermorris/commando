using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using twomindseye.Commando.API1;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.API1.Parse;
using twomindseye.Commando.Engine.DB;
using twomindseye.Commando.Engine.Extension;
using twomindseye.Commando.Util;

namespace twomindseye.Commando.Engine
{
    /// <summary>
    /// Holds a command and a valid collection of arguments for execution. Immutable.
    /// </summary>
    public sealed class CommandExecutor : IEquatable<CommandExecutor>, IDisposable
    {
        readonly ReadOnlyCollection<CommandArgument> _arguments;
        readonly ReadOnlyCollection<string> _aliases;
        readonly Lazy<IFacet[]> _facets;
        readonly string _aliasedDescription;
        bool _disposed;

        internal CommandExecutor(ParseInput input, Command command, IEnumerable<ParseResult> arguments)
            : this(input, command, from a in arguments select new CommandArgument(a))
        {
            // OK that this check comes after chained constructor
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }
        }

        internal CommandExecutor(ParseInput input, Command command, IEnumerable<CommandArgument> arguments, 
            string aliasedDescription = null, IEnumerable<string> aliases = null)
        {
            _arguments = new ReadOnlyCollection<CommandArgument>(arguments.ToList());

            if (_arguments.Count != command.Parameters.Count)
            {
                throw new ArgumentException("Mismatched argument count");
            }

            _facets = new Lazy<IFacet[]>(InitFacets);
            Input = input;
            Command = command;

            CanInvoke = !ParametersAndArguments.Any(x => !x.Item1.Optional && !x.Item2.IsSpecified);

            if (aliasedDescription != null)
            {
                _aliasedDescription = aliasedDescription;
                Description = string.Format("{0} (aliased form of {1})", aliasedDescription, Command.Name);
            }
            else
            {
                Description = Command.Name;
            }

            if (aliases != null)
            {
                _aliases = new ReadOnlyCollection<string>(aliases.ToArray());
            }

            GC.SuppressFinalize(this);
        }

        ~CommandExecutor()
        {
            if (_disposed)
            {
                return;
            }

            Debug.WriteLine("~CommandExecutor: not disposed");
            Dispose();
        }

        /// <summary>
        /// Creates a new CommandExecutor using suggestions from the arguments in this instance. 
        /// </summary>
        public CommandExecutor CreateSuggestedVersion(IEnumerable<Tuple<CommandParameter, FacetMoniker>> newArguments)
        {
            CheckDisposed();

            return CreateSuggestedVersion(null, newArguments.Select(x => Tuple.Create(x.Item1, new CommandArgument(x.Item2))));
        }

        /// <summary>
        /// Creates a new CommandExecutor using suggestions from the arguments in this instance. 
        /// </summary>
        internal CommandExecutor CreateSuggestedVersion(ParseInput input, IEnumerable<Tuple<CommandParameter, CommandArgument>> newArguments)
        {
            CheckDisposed();

            if (newArguments == null)
            {
                throw new ArgumentNullException("newArguments");
            }

            var newArgumentsArray = newArguments.ToArray();

            if (newArgumentsArray.Length == 0)
            {
                if (input == null)
                {
                    return this;
                }

                return new CommandExecutor(input, Command, Arguments, _aliasedDescription, _aliases);
            }

            if (newArgumentsArray.Any(x => x.Item1 == null || x.Item2 == null))
            {
                throw new ArgumentNullException("newArguments");
            }

            if (newArgumentsArray.Any(x => Command.Parameters.FirstIndexWhere(y => ReferenceEquals(y, x.Item1)) == -1))
            {
                throw new ArgumentException("all CommandArguments must belong to this instance");
            }

            var finalArgs = new List<CommandArgument>();

            for (var argIndex = 0; argIndex < _arguments.Count; argIndex++)
            {
                var parameter = Command.Parameters[argIndex];
                var suggestion = newArgumentsArray.FirstOrDefault(x => ReferenceEquals(parameter, x.Item1));

                if (suggestion == null)
                {
                    // no suggestion indicated
                    finalArgs.Add(_arguments[argIndex]);

                    continue;
                }

                var newArgument = suggestion.Item2;

                if (!newArgument.IsSpecified && !parameter.Optional)
                {
                    throw new ArgumentException("parameter " + parameter.Name + " is not optional");
                }

                if (newArgument.IsSpecified && !parameter.IsUsableAsArgument(newArgument.FacetMoniker))
                {
                    throw new ArgumentOutOfRangeException("newArguments", "moniker is invalid for the parameter");
                }

                finalArgs.Add(new CommandArgument(newArgument.FacetMoniker,
                                                  newArgument.Suggestions.Where(parameter.IsUsableAsArgument)));
            }

            return new CommandExecutor(input ?? Input, Command, finalArgs, _aliasedDescription, _aliases);
        }

        public Command Command { get; private set; }

        public ParseInput Input { get; private set; }

        public string Description { get; private set; }

        public bool CanInvoke { get; private set; }

        public ReadOnlyCollection<string> Aliases
        {
            get
            {
                return _aliases ?? Command.Aliases;
            }
        }

        public ReadOnlyCollection<CommandArgument> Arguments
        {
            get { return _arguments; }
        }

        public IEnumerable<CommandParameter> UnspecifiedParameters
        {
            get
            {
                return ParametersAndArguments.Where(x => !x.Item2.IsSpecified).Select(x => x.Item1);
            }
        }

        public IEnumerable<Tuple<CommandParameter, CommandArgument>> ParametersAndArguments
        {
            get
            {
                return Command.Parameters.Zip(_arguments, Tuple.Create);
            }
        }

        public void Invoke()
        {
            CheckDisposed();

            if (!CanInvoke)
            {
                throw new InvalidOperationException("CanInvoke is false");
            }

            CommandHistory.SaveExecutedCommand(this);

            try
            {
                Command.Invoke(_facets.Value);
            }
            catch (CommandExecutionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new CommandExecutionException(ex);
            }
        }

        public override string ToString()
        {
            return string.Format(
                "{0}: {1}",
                Command.Name,
                Arguments.Select(x => x.ToString()).JoinStrings(", "));
        }

        public double ScoreSimilarity(CommandExecutor other)
        {
            CheckDisposed();

            var score = 0.0;

            var otherMonikers = other.Arguments
                .Where(x => x.IsSpecified)
                .Select(x => x.FacetMoniker)
                .ToList();

            foreach (var moniker in Arguments.Where(x => x.IsSpecified).Select(x => x.FacetMoniker))
            {
                var otherMoniker = otherMonikers.Where(x => x.FacetType == moniker.FacetType).FirstOrDefault();

                if (otherMoniker != null)
                {
                    score += CommandGenerator.ScoreSimilarity(moniker, otherMoniker);
                    otherMonikers.Remove(otherMoniker);
                }
            }

            score = score/Arguments.Count/2;

            if (other.Command == Command)
            {
                score += 0.5;
            }

            return score;
        }

        public bool Equals(CommandExecutor other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Equals(other.Command, Command) && Arguments.SequenceEqual(other.Arguments);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != typeof (CommandExecutor))
            {
                return false;
            }
            return Equals((CommandExecutor) obj);
        }

        public override int GetHashCode()
        {
            return Command.GetHashCode();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_facets.IsValueCreated)
            {
                foreach (var facet in _facets.Value.Where(x => x != null))
                {
                    facet.Dispose();
                }
            }

            _disposed = true;

            GC.SuppressFinalize(this);
        }

        public static bool operator ==(CommandExecutor left, CommandExecutor right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(CommandExecutor left, CommandExecutor right)
        {
            return !Equals(left, right);
        }

        void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("This instance is disposed");
            }
        }
    
        IFacet[] InitFacets()
        {
            GC.ReRegisterForFinalize(this);
            return _arguments.Select(x => x.CreateFacet()).ToArray();
        }
    }
}