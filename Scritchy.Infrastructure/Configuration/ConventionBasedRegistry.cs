﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Scritchy.Domain;
using Scritchy.Infrastructure.Implementations;

namespace Scritchy.Infrastructure.Configuration
{
    public class ConventionBasedRegistry : HandlerRegistry
    {
        public ConventionBasedRegistry()
            : this(AppDomain.CurrentDomain.GetAssemblies())
        {
        }

        public ConventionBasedRegistry(IEnumerable<Assembly> assemblies)
        {
            ScanAssembliesAndRegisterAll(assemblies);
        }

        void ScanAssembliesAndRegisterAll(IEnumerable<Assembly> assemblies)
        {
            var ARTypes = new List<Type>();
            var EventHandlers = new List<Type>();
            var Validators = new List<Type>();
            var PossibleCommandNames = new List<string>();
            var PossibleEventNames = new List<string>();
            var PossibleValidatorNames = new List<string>();
            var CommandTypes = new List<Type>();
            var EventTypes = new List<Type>();

            var srctypes = new List<Type>();

            // scan AR's & store possible command & event names
            foreach (var asm in assemblies)
            {
                if (asm.ManifestModule != null && asm.ManifestModule.FullyQualifiedName == "<In Memory Module>")
                    continue;
                srctypes.AddRange(asm.GetTypes()
                    .Where(x => !x.IsAbstract && x.IsClass && !x.IsGenericType && x.IsPublic && !x.Namespace.StartsWith("System") && !x.Namespace.StartsWith("Microsoft")));
            }
            foreach (var t in srctypes)
            {
                if (typeof(AR).IsAssignableFrom(t))
                    ARTypes.Add(t);
                foreach (var methodname in t.GetMethods().Where(x => x.ReturnType == typeof(void)).Select(x => x.Name))
                {
                    if (methodname.StartsWith("On") && methodname.Length > 2 && methodname[2] == methodname.ToUpper()[2])
                    {
                        PossibleEventNames.Add(methodname.Substring(2));
                        if (!EventHandlers.Contains(t))
                            EventHandlers.Add(t);
                    }
                    if (methodname.StartsWith("Can") && methodname.Length > 3 && methodname[3] == methodname.ToUpper()[3])
                    {
                        PossibleCommandNames.Add(methodname.Substring(3));
                        if (!Validators.Contains(t))
                            Validators.Add(t);
                    }
                    PossibleCommandNames.Add(methodname);
                }
            }
            foreach (var t in srctypes)
            {
                if (PossibleEventNames.Any(x => t.Name.StartsWith(x)))
                {
                    EventTypes.Add(t);
                }
                if (PossibleCommandNames.Any(x => t.Name.StartsWith(x)))
                {
                    CommandTypes.Add(t);
                }
            }
            
            // ordering is important here
            RegisterHandlers(Validators, CommandTypes, "Can");
            RegisterHandlers(ARTypes, CommandTypes);
            RegisterHandlers(EventHandlers, EventTypes,"On");
        }

    }
}
