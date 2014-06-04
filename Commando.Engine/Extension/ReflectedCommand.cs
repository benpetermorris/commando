using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using twomindseye.Commando.API1;
using twomindseye.Commando.API1.Commands;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.Util;

namespace twomindseye.Commando.Engine.Extension
{
    public sealed class ReflectedCommand : Command
    {
        readonly MethodDescriptor _methodDescriptor;
        readonly Lazy<Action<IFacet[]>> _invoke;

        internal ReflectedCommand(CommandContainer container, MethodDescriptor method, CommandAttribute command)
            : base(container, command.Name, command.AliasesSplit)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }

            if (method == null)
            {
                throw new ArgumentNullException("method");
            }

            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

            _methodDescriptor = method;
            _invoke = new Lazy<Action<IFacet[]>>(CreateInvoke);

            for (var i = 0; i < method.Parameters.Count; i++)
            {
                AddParameterInfo(new ReflectedCommandParameter(this, method.Parameters[i], i));
            }
        }

        internal override void Invoke(IEnumerable<IFacet> facets)
        {
            _invoke.Value(facets.ToArray());
        }

        Action<IFacet[]> CreateInvoke()
        {
            // todo: faster
            return x =>
                   {
                       try
                       {
                           Container.Invoke(_methodDescriptor.Name, x);
                       }
                       catch (TargetInvocationException ex)
                       {
                           if (ex.InnerException is CommandExecutionException)
                           {
                               throw ex.InnerException;
                           }

                           throw new CommandExecutionException(ex.InnerException);
                       }
                       catch (Exception ex)
                       {
                           throw new CommandExecutionException(ex.InnerException);
                       }
                   };
        }

        class ReflectedCommandParameter : CommandParameter
        {
            readonly ParameterDescriptor _parameterInfo;

            internal ReflectedCommandParameter(ReflectedCommand info, ParameterDescriptor parameterInfo, int ordinal)
            {
                if (info == null)
                {
                    throw new ArgumentNullException("info");
                }

                _parameterInfo = parameterInfo;

                var optional = false;
                var name = parameterInfo.Name;

                var cpa = parameterInfo.APIAttributes.OfType<CommandParameterAttribute>().FirstOrDefault();

                if (cpa != null)
                {
                    optional = cpa.Optional;
                    name = cpa.Name;
                }

                Initialize(info, ordinal, name, _parameterInfo.ParameterType,
                           optional, parameterInfo.APIAttributes.OfType<FilterExtraDataAttribute>());
            }
        }
    }
}