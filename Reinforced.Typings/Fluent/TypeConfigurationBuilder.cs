﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Reinforced.Typings.Attributes;
using Reinforced.Typings.Fluent.Interfaces;

namespace Reinforced.Typings.Fluent
{
    /// <summary>
    ///     Configuration builder for type
    /// </summary>
    /// <typeparam name="TType"></typeparam>
    public abstract class TypeConfigurationBuilder<TType> : ITypeConfigurationBuilder
    {
        private readonly Dictionary<MemberInfo, IExportConfiguration<TsAttributeBase>> _membersConfiguration =
            new Dictionary<MemberInfo, IExportConfiguration<TsAttributeBase>>();

        private readonly Dictionary<ParameterInfo, IExportConfiguration<TsParameterAttribute>> _parametersConfiguration
            = new Dictionary<ParameterInfo, IExportConfiguration<TsParameterAttribute>>();

        private readonly ICollection<TsAddTypeReferenceAttribute> _references = new List<TsAddTypeReferenceAttribute>();

        Type ITypeConfigurationBuilder.Type
        {
            get { return typeof (TType); }
        }

        Dictionary<ParameterInfo, IExportConfiguration<TsParameterAttribute>> ITypeConfigurationBuilder.
            ParametersConfiguration
        {
            get { return _parametersConfiguration; }
        }

        Dictionary<MemberInfo, IExportConfiguration<TsAttributeBase>> ITypeConfigurationBuilder.MembersConfiguration
        {
            get { return _membersConfiguration; }
        }

        ICollection<TsAddTypeReferenceAttribute> IReferenceConfigurationBuilder.References
        {
            get { return _references; }
        }

        string IReferenceConfigurationBuilder.PathToFile { get; set; }

        public abstract double MemberOrder { get; set; }
    }
}