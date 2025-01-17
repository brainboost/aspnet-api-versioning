﻿namespace Microsoft
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    // REF: https://raw.githubusercontent.com/aspnet/Common/dev/shared/Microsoft.Extensions.TypeNameHelper.Sources/TypeNameHelper.cs
    static class TypeExtensions
    {
        static readonly Dictionary<Type, string> BuiltInTypeNames = new Dictionary<Type, string>
        {
            [typeof( void )] = "void",
            [typeof( bool )] = "bool",
            [typeof( byte )] = "byte",
            [typeof( char )] = "char",
            [typeof( decimal )] = "decimal",
            [typeof( double )] = "double",
            [typeof( float )] = "float",
            [typeof( int )] = "int",
            [typeof( long )] = "long",
            [typeof( object )] = "object",
            [typeof( sbyte )] = "sbyte",
            [typeof( short )] = "short",
            [typeof( string )] = "string",
            [typeof( uint )] = "uint",
            [typeof( ulong )] = "ulong",
            [typeof( ushort )] = "ushort",
        };

        /// <summary>
        /// Pretty print a type name.
        /// </summary>
        /// <param name="type">The <see cref="Type"/>.</param>
        /// <param name="fullName"><c>true</c> to print a fully qualified name.</param>
        /// <param name="includeGenericParameterNames"><c>true</c> to include generic parameter names.</param>
        /// <returns>The pretty printed type name.</returns>
        internal static string GetTypeDisplayName( this Type type, bool fullName = true, bool includeGenericParameterNames = false )
        {
            var builder = new StringBuilder();
            ProcessType( builder, type, new DisplayNameOptions( fullName, includeGenericParameterNames ) );
            return builder.ToString();
        }

        static void ProcessType( StringBuilder builder, Type type, DisplayNameOptions options )
        {
            if ( type.IsGenericType )
            {
                var genericArguments = type.GetGenericArguments();
                ProcessGenericType( builder, type, genericArguments, genericArguments.Length, options );
            }
            else if ( type.IsArray )
            {
                ProcessArrayType( builder, type, options );
            }
            else if ( BuiltInTypeNames.TryGetValue( type, out var builtInName ) )
            {
                builder.Append( builtInName );
            }
            else if ( type.IsGenericParameter )
            {
                if ( options.IncludeGenericParameterNames )
                {
                    builder.Append( type.Name );
                }
            }
            else
            {
                builder.Append( options.FullName ? type.FullName : type.Name );
            }
        }

        static void ProcessArrayType( StringBuilder builder, Type type, DisplayNameOptions options )
        {
            var innerType = type;

            while ( innerType.IsArray )
            {
                innerType = innerType.GetElementType()!;
            }

            ProcessType( builder, innerType, options );

            while ( type.IsArray )
            {
                builder.Append( '[' );
                builder.Append( ',', type.GetArrayRank() - 1 );
                builder.Append( ']' );
                type = type.GetElementType()!;
            }
        }

        static void ProcessGenericType( StringBuilder builder, Type type, Type[] genericArguments, int length, DisplayNameOptions options )
        {
            var offset = 0;

            if ( type.IsNested )
            {
                offset = type.DeclaringType!.GetGenericArguments().Length;
            }

            if ( options.FullName )
            {
                if ( type.IsNested )
                {
                    ProcessGenericType( builder, type.DeclaringType!, genericArguments, offset, options );
                    builder.Append( '+' );
                }
                else if ( !string.IsNullOrEmpty( type.Namespace ) )
                {
                    builder.Append( type.Namespace );
                    builder.Append( '.' );
                }
            }

#if NETCOREAPP3_0
            var genericPartIndex = type.Name.IndexOf( '`', StringComparison.Ordinal );
#else
            var genericPartIndex = type.Name.IndexOf( '`' );
#endif

            if ( genericPartIndex <= 0 )
            {
                builder.Append( type.Name );
                return;
            }

            builder.Append( type.Name, 0, genericPartIndex );
            builder.Append( '<' );

            for ( var i = offset; i < length; i++ )
            {
                ProcessType( builder, genericArguments[i], options );

                if ( i + 1 == length )
                {
                    continue;
                }

                builder.Append( ',' );

                if ( options.IncludeGenericParameterNames || !genericArguments[i + 1].IsGenericParameter )
                {
                    builder.Append( ' ' );
                }
            }

            builder.Append( '>' );
        }

        readonly struct DisplayNameOptions
        {
            internal DisplayNameOptions( bool fullName, bool includeGenericParameterNames )
            {
                FullName = fullName;
                IncludeGenericParameterNames = includeGenericParameterNames;
            }

            public bool FullName { get; }

            public bool IncludeGenericParameterNames { get; }
        }
    }
}