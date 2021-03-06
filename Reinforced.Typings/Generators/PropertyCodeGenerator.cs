﻿using System;
using System.Reflection;
using Reinforced.Typings.Ast;
using Reinforced.Typings.Ast.TypeNames;
using Reinforced.Typings.Attributes;

namespace Reinforced.Typings.Generators
{
    /// <summary>
    ///     Default code generator for properties
    /// </summary>
    public class PropertyCodeGenerator : TsCodeGeneratorBase<MemberInfo, RtField>
    {
        /// <summary>
        ///     Main code generator method. This method should write corresponding TypeScript code for element (1st argument) to
        ///     WriterWrapper (3rd argument) using TypeResolver if necessary
        /// </summary>
        /// <param name="element">Element code to be generated to output</param>
        /// <param name="result">Resulting node</param>
        /// <param name="resolver">Type resolver</param>
        public override RtField GenerateNode(MemberInfo element, RtField result, TypeResolver resolver)
        {
            if (element.IsIgnored()) return null;
            result.IsStatic = element.IsStatic();
            result.Order = element.GetOrder();

            var doc = Context.Documentation.GetDocumentationMember(element);
            if (doc != null)
            {
                RtJsdocNode jsdoc = new RtJsdocNode { Description = doc.Summary.Text };
                result.Documentation = jsdoc;
            }

            var t = GetType(element);
            RtTypeName type = null;
            var propName = new RtIdentifier(element.Name);
            bool isNameOverridden = false;
            var tp = ConfigurationRepository.Instance.ForMember<TsPropertyAttribute>(element);
            if (tp != null)
            {
                if (tp.StrongType != null)
                {
                    type = resolver.ResolveTypeName(tp.StrongType);
                }
                else if (!string.IsNullOrEmpty(tp.Type))
                {
                    type = new RtSimpleTypeName(tp.Type);
                }
                
                type = tp.TypeInferers.Infer(element, resolver) ?? type;

                if (!string.IsNullOrEmpty(tp.Name))
                {
                    propName.IdentifierName = tp.Name;
                    isNameOverridden = true;
                }
                if (tp.NilForceNullable.HasValue && !Context.SpecialCase)
                {
                    propName.IsNullable = tp.NilForceNullable.Value;
                }
            }

            if (type == null) type = resolver.ResolveTypeName(t);
            if (tp != null && !tp.NilForceNullable.HasValue)
            {
                if (!propName.IsNullable && t.IsNullable() && !Context.SpecialCase)
                {
                    propName.IsNullable = true;
                }
            }
            if (!isNameOverridden)
            {
                if (element is PropertyInfo)
                {
                    propName.IdentifierName =
                        Context.ConditionallyConvertPropertyNameToCamelCase(propName.IdentifierName);
                }
                propName.IdentifierName = element.CamelCaseFromAttribute(propName.IdentifierName);
                propName.IdentifierName = element.PascalCaseFromAttribute(propName.IdentifierName);
            }
            result.Identifier = propName;
            result.AccessModifier = Context.SpecialCase ? AccessModifier.Public : element.GetModifier();
            result.Type = type;
            AddDecorators(result, ConfigurationRepository.Instance.DecoratorsFor(element));
            return result;
        }

        /// <summary>
        ///     Returns type of specified property. It is useful for overloads sometimes
        /// </summary>
        /// <param name="mi">Method Info</param>
        /// <returns>Property info type</returns>
        protected virtual Type GetType(MemberInfo mi)
        {
            var pi = (PropertyInfo)mi;
            return pi.PropertyType;
        }
    }
}