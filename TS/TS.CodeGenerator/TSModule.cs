﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TS.CodeGenerator
{
    public class TSModule : IGenerateTS
    {
        private readonly Func<Type, string> _mapType;
        public string Name { get; set; }
        public List<TSInterface> Interfaces { get; set; }
        public List<TSConstEnumeration> Enumerations { get; set; }
        public List<TSModule> SubModules { get; set; }
        public bool IsDeclared { get; set; }

        public TSModule(string name, Func<Type, string> mapType)
        {
            _mapType = mapType;
            Name = name;
            Interfaces = new List<TSInterface>();
            Enumerations = new List<TSConstEnumeration>();
            SubModules = new List<TSModule>();
        }

        public void Initialize()
        {
            foreach (var tsModule in SubModules)
            {
                tsModule.Initialize();
            }
            foreach (var tsConstEnumeration in Enumerations)
            {
                tsConstEnumeration.Initialize();
            }
            foreach (var tsInterface in Interfaces)
            {
                tsInterface.Initialize();
            }
        }

        public string ToTSString()
        {
            var declarestring = IsDeclared ? " declare " : "export";
            var formats = declarestring + " module {0} {{"
                        + Settings.EndOfLine
                        + "{1}" + Settings.EndOfLine
                        + "{2}" + Settings.EndOfLine
                        + "{3}" + Settings.EndOfLine
                        + "}}"
                        + Settings.EndOfLine;

            var indentedEOL = Settings.EndOfLine + Settings.Indentation;
            var allInterfaces = string.Join(Settings.EndOfLine, Interfaces.Where(i => !Settings.IgnoreInterfaces.Contains(i.InterFaceName)).Select(i => i.ToTSString())).Replace(Settings.EndOfLine, indentedEOL);
            var allenums = string.Join(Settings.EndOfLine, Enumerations.Select(e => e.ToTSString())).Replace(Settings.EndOfLine, indentedEOL);
            var submods = string.Join(Settings.EndOfLine, SubModules.Select(m => m.ToTSString())).Replace(Settings.EndOfLine, indentedEOL);

            return string.Format(formats, Name, allenums, allInterfaces, submods).Replace(Settings.EndOfLine, indentedEOL);
        }

        public void AddSubNamespaceType(List<string> ns, Type type)
        {
            if (ns.Count == 0)
            {
                var ti = type.GetTypeInfo();
                if (ti.IsEnum)
                {
                    if (Settings.ConstEnumsEnabled)
                    {
                        var tsconstenum = new TSConstEnumeration(type)
                        {
                            IsExported = true
                        };
                        Enumerations.Add(tsconstenum);
                    }

                    return;
                }
                var tsinterface = new TSInterface(type, _mapType)
                {
                    IsExported = true
                };
                Interfaces.Add(tsinterface);

                return;
            }

            var root = ns.First();

            var mod = SubModules.FirstOrDefault(m => m.Name == root);
            if (mod == null)
            {
                mod = new TSModule(root, _mapType);
                SubModules.Add(mod);
            }

            ns.RemoveAt(0);

            mod.AddSubNamespaceType(ns, type);
        }
    }
}
