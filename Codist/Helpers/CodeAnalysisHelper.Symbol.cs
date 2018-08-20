﻿using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;

namespace Codist
{
	static partial class CodeAnalysisHelper
	{
		public static Location FirstSourceLocation(this ISymbol symbol) {
			return symbol?.Locations.FirstOrDefault(loc => loc.IsInSource);
		}

		public static string GetAccessibility(this ISymbol symbol) {
			switch (symbol.DeclaredAccessibility) {
				case Accessibility.Public: return "public ";
				case Accessibility.Private: return "private ";
				case Accessibility.ProtectedAndInternal: return "protected internal ";
				case Accessibility.Protected: return "protected ";
				case Accessibility.Internal: return "internal ";
				case Accessibility.ProtectedOrInternal: return "protected or internal ";
				default: return String.Empty;
			}
		}

		public static string GetAssemblyModuleName(this ISymbol symbol) {
			return symbol.ContainingAssembly?.Modules?.FirstOrDefault()?.Name
					?? symbol.ContainingAssembly?.Name;
		}

		public static StandardGlyphGroup GetGlyphGroup(this ISymbol symbol) {
			switch (symbol.Kind) {
				case SymbolKind.Alias: return StandardGlyphGroup.GlyphForwardType;
				case SymbolKind.Assembly: return StandardGlyphGroup.GlyphAssembly;
				case SymbolKind.DynamicType: return StandardGlyphGroup.GlyphGroupType;
				case SymbolKind.ErrorType: return StandardGlyphGroup.GlyphGroupError;
				case SymbolKind.Event: return StandardGlyphGroup.GlyphGroupEvent;
				case SymbolKind.Field:
					return (symbol as IFieldSymbol).IsConst
						? StandardGlyphGroup.GlyphGroupConstant
						: StandardGlyphGroup.GlyphGroupField;

				case SymbolKind.Label: return StandardGlyphGroup.GlyphArrow;
				case SymbolKind.Local: return StandardGlyphGroup.GlyphGroupVariable;
				case SymbolKind.Method:
					return (symbol as IMethodSymbol).IsExtensionMethod
						? StandardGlyphGroup.GlyphExtensionMethod
						: StandardGlyphGroup.GlyphGroupMethod;

				case SymbolKind.NetModule: return StandardGlyphGroup.GlyphGroupModule;
				case SymbolKind.NamedType:
					switch ((symbol as INamedTypeSymbol).TypeKind) {
						case TypeKind.Unknown: return StandardGlyphGroup.GlyphGroupUnknown;
						case TypeKind.Array:
						case TypeKind.Dynamic:
						case TypeKind.Class:
							return StandardGlyphGroup.GlyphGroupClass;

						case TypeKind.Delegate: return StandardGlyphGroup.GlyphGroupDelegate;
						case TypeKind.Enum: return StandardGlyphGroup.GlyphGroupEnum;
						case TypeKind.Error: return StandardGlyphGroup.GlyphGroupError;
						case TypeKind.Interface: return StandardGlyphGroup.GlyphGroupInterface;
						case TypeKind.Module: return StandardGlyphGroup.GlyphGroupModule;
						case TypeKind.Pointer:
						case TypeKind.Struct: return StandardGlyphGroup.GlyphGroupStruct;
					}
					return StandardGlyphGroup.GlyphGroupType;

				case SymbolKind.Namespace: return StandardGlyphGroup.GlyphGroupNamespace;
				case SymbolKind.Parameter: return StandardGlyphGroup.GlyphGroupVariable;
				case SymbolKind.Property: return StandardGlyphGroup.GlyphGroupProperty;
				case SymbolKind.TypeParameter: return StandardGlyphGroup.GlyphGroupType;
				default: return StandardGlyphGroup.GlyphGroupUnknown;
			}
		}

		public static StandardGlyphItem GetGlyphItem(this ISymbol symbol) {
			switch (symbol.DeclaredAccessibility) {
				case Accessibility.Private: return StandardGlyphItem.GlyphItemPrivate;
				case Accessibility.ProtectedAndInternal:
				case Accessibility.Protected: return StandardGlyphItem.GlyphItemProtected;
				case Accessibility.Internal: return StandardGlyphItem.GlyphItemInternal;
				case Accessibility.ProtectedOrInternal: return StandardGlyphItem.GlyphItemFriend;
				case Accessibility.Public: return StandardGlyphItem.GlyphItemPublic;
				default: return StandardGlyphItem.TotalGlyphItems;
			}
		}

		public static int GetImageId(this ISymbol symbol) {
			switch (symbol.Kind) {
				case SymbolKind.Assembly: return KnownImageIds.Assembly;
				case SymbolKind.DynamicType: return KnownImageIds.Dynamic;
				case SymbolKind.Event:
					var ev = symbol as IEventSymbol;
					switch (ev.DeclaredAccessibility) {
						case Accessibility.Public: return KnownImageIds.EventPublic;
						case Accessibility.Protected: return KnownImageIds.EventProtected;
						case Accessibility.Private: return KnownImageIds.EventPrivate;
						case Accessibility.Internal: return KnownImageIds.EventInternal;
						default: return KnownImageIds.Event;
					}
				case SymbolKind.Field:
					var f = symbol as IFieldSymbol;
					if (f.IsConst) {
						switch (f.DeclaredAccessibility) {
							case Accessibility.Public: return KnownImageIds.ConstantPublic;
							case Accessibility.Protected: return KnownImageIds.ConstantProtected;
							case Accessibility.Private: return KnownImageIds.ConstantPrivate;
							case Accessibility.Internal: return KnownImageIds.ConstantInternal;
							default: return KnownImageIds.Constant;
						}
					}
					switch (f.DeclaredAccessibility) {
						case Accessibility.Public: return KnownImageIds.FieldPublic;
						case Accessibility.Protected: return KnownImageIds.FieldProtected;
						case Accessibility.Private: return KnownImageIds.FieldPrivate;
						case Accessibility.Internal: return KnownImageIds.FieldInternal;
						default: return KnownImageIds.Field;
					}
				case SymbolKind.Label: return KnownImageIds.Label;
				case SymbolKind.Local: return KnownImageIds.LocalVariable;
				case SymbolKind.Method:
					var m = symbol as IMethodSymbol;
					if (m.IsExtensionMethod) {
						return KnownImageIds.ExtensionMethod;
					}
					switch (m.DeclaredAccessibility) {
						case Accessibility.Public: return KnownImageIds.MethodPublic;
						case Accessibility.Protected: return KnownImageIds.MethodProtected;
						case Accessibility.Private: return KnownImageIds.MethodPrivate;
						case Accessibility.Internal: return KnownImageIds.MethodInternal;
						default: return KnownImageIds.Method;
					}
				case SymbolKind.NamedType:
					var t = symbol as INamedTypeSymbol;
					switch (t.TypeKind) {
						case TypeKind.Class:
							switch (t.DeclaredAccessibility) {
								case Accessibility.Public: return KnownImageIds.ClassPublic;
								case Accessibility.Protected: return KnownImageIds.ClassProtected;
								case Accessibility.Private: return KnownImageIds.ClassPrivate;
								case Accessibility.Internal: return KnownImageIds.ClassInternal;
								default: return KnownImageIds.Class;
							}
						case TypeKind.Delegate:
							switch (t.DeclaredAccessibility) {
								case Accessibility.Public: return KnownImageIds.DelegatePublic;
								case Accessibility.Protected: return KnownImageIds.DelegateProtected;
								case Accessibility.Private: return KnownImageIds.DelegatePrivate;
								case Accessibility.Internal: return KnownImageIds.DelegateInternal;
								default: return KnownImageIds.Delegate;
							}
						case TypeKind.Enum:
							switch (t.DeclaredAccessibility) {
								case Accessibility.Public: return KnownImageIds.EnumerationPublic;
								case Accessibility.Protected: return KnownImageIds.EnumerationProtected;
								case Accessibility.Private: return KnownImageIds.EnumerationPrivate;
								case Accessibility.Internal: return KnownImageIds.EnumerationInternal;
								default: return KnownImageIds.Enumeration;
							}
						case TypeKind.Interface:
							switch (t.DeclaredAccessibility) {
								case Accessibility.Public: return KnownImageIds.InterfacePublic;
								case Accessibility.Protected: return KnownImageIds.InterfaceProtected;
								case Accessibility.Private: return KnownImageIds.InterfacePrivate;
								case Accessibility.Internal: return KnownImageIds.InterfaceInternal;
								default: return KnownImageIds.Interface;
							}
						case TypeKind.Struct:
							switch (t.DeclaredAccessibility) {
								case Accessibility.Public: return KnownImageIds.StructurePublic;
								case Accessibility.Protected: return KnownImageIds.StructureProtected;
								case Accessibility.Private: return KnownImageIds.StructurePrivate;
								case Accessibility.Internal: return KnownImageIds.StructureInternal;
								default: return KnownImageIds.Structure;
							}
						case TypeKind.TypeParameter:
						default: return KnownImageIds.Type;
					}
				case SymbolKind.Namespace: return KnownImageIds.Namespace;
				case SymbolKind.Parameter: return KnownImageIds.Parameter;
				case SymbolKind.Property:
					switch ((symbol as IPropertySymbol).DeclaredAccessibility) {
						case Accessibility.Public: return KnownImageIds.PropertyPublic;
						case Accessibility.Protected: return KnownImageIds.PropertyProtected;
						case Accessibility.Private: return KnownImageIds.PropertyPrivate;
						case Accessibility.Internal: return KnownImageIds.PropertyInternal;
						default: return KnownImageIds.Property;
					}
				default: return KnownImageIds.Item;
			}
		}

		public static ImmutableArray<IParameterSymbol> GetParameters(this ISymbol symbol) {
			switch (symbol.Kind) {
				case SymbolKind.Method: return (symbol as IMethodSymbol).Parameters;
				case SymbolKind.Event: return (symbol as IEventSymbol).AddMethod.Parameters;
				case SymbolKind.Property: return (symbol as IPropertySymbol).Parameters;
			}
			return default(ImmutableArray<IParameterSymbol>);
		}

		public static ITypeSymbol GetReturnType(this ISymbol symbol) {
			switch (symbol.Kind) {
				case SymbolKind.Field: return (symbol as IFieldSymbol).Type;
				case SymbolKind.Local: return (symbol as ILocalSymbol).Type;
				case SymbolKind.Method: return (symbol as IMethodSymbol).ReturnType;
				case SymbolKind.Parameter: return (symbol as IParameterSymbol).Type;
				case SymbolKind.Property: return (symbol as IPropertySymbol).Type;
			}
			return null;
		}

		public static string GetSignatureString(this ISymbol symbol) {
			if (symbol.Kind != SymbolKind.Method) {
				return symbol.Name;
			}
			using (var sbr = ReusableStringBuilder.AcquireDefault(100)) {
				var sb = sbr.Resource;
				var m = symbol as IMethodSymbol;
				sb.Append(m.Name);
				if (m.IsGenericMethod) {
					sb.Append('<');
					var s = false;
					foreach (var item in m.TypeParameters) {
						if (s) {
							sb.Append(", ");
						}
						else {
							s = true;
						}
						sb.Append(item.Name);
					}
					sb.Append('>');
				}
				sb.Append('(');
				var p = false;
				foreach (var item in m.Parameters) {
					if (p) {
						sb.Append(", ");
					}
					else {
						p = true;
					}
					GetTypeName(item.Type, sb);
				}
				sb.Append(')');
				return sb.ToString();

				void GetTypeName(ITypeSymbol type, StringBuilder output) {
					switch (type.TypeKind) {
						case TypeKind.Array:
							GetTypeName((type as IArrayTypeSymbol).ElementType, output);
							output.Append("[]");
							return;

						case TypeKind.Dynamic:
							output.Append('?'); return;
						case TypeKind.Module:
						case TypeKind.TypeParameter:
						case TypeKind.Enum:
						case TypeKind.Error:
							output.Append(type.Name); return;
						case TypeKind.Pointer:
							GetTypeName((type as IPointerTypeSymbol).PointedAtType, output);
							output.Append('*');
							return;
					}
					output.Append(type.Name);
					var nt = type as INamedTypeSymbol;
					if (nt == null) {
						return;
					}
					if (nt.IsGenericType == false) {
						return;
					}
					var s = false;
					output.Append('<');
					foreach (var item in nt.TypeArguments) {
						if (s) {
							output.Append(", ");
						}
						else {
							s = true;
						}
						GetTypeName(item, output);
					}
					output.Append('>');
				}
			}
		}

		public static ImmutableArray<SyntaxReference> GetSourceLocations(this ISymbol symbol) {
			return symbol == null || symbol.DeclaringSyntaxReferences.Length == 0
				? ImmutableArray<SyntaxReference>.Empty
				: symbol.DeclaringSyntaxReferences.RemoveAll(i => i.SyntaxTree.FilePath == null);
		}

		public static string GetSymbolKindName(this ISymbol symbol) {
			switch (symbol.Kind) {
				case SymbolKind.Event: return "event";
				case SymbolKind.Field:
					return "field";

				case SymbolKind.Label: return "label";
				case SymbolKind.Local:
					return (symbol as ILocalSymbol).IsConst
						? "local const"
						: "local";

				case SymbolKind.Method:
					return (symbol as IMethodSymbol).IsExtensionMethod
						? "extension"
						: "method";

				case SymbolKind.NamedType:
					switch ((symbol as INamedTypeSymbol).TypeKind) {
						case TypeKind.Array: return "array";
						case TypeKind.Dynamic: return "dynamic";
						case TypeKind.Class: return "class";
						case TypeKind.Delegate: return "delegate";
						case TypeKind.Enum: return "enum";
						case TypeKind.Interface: return "interface";
						case TypeKind.Struct: return "struct";
						case TypeKind.TypeParameter: return "type parameter";
					}
					return "type";

				case SymbolKind.Namespace: return "namespace";
				case SymbolKind.Parameter: return "parameter";
				case SymbolKind.Property: return "property";
				case SymbolKind.TypeParameter: return "type parameter";
				default:
					return symbol.Kind.ToString();
			}
		}

		public static void GoToSource(this ISymbol symbol) {
			symbol.FirstSourceLocation().GoToSource();
		}

		public static void GoToSource(this Location loc) {
			if (loc != null) {
				var pos = loc.GetLineSpan().StartLinePosition;
				CodistPackage.DTE.OpenFile(loc.SourceTree.FilePath, pos.Line + 1, pos.Character + 1);
			}
		}

		public static void GoToSource(this SyntaxReference loc) {
			var pos = loc.SyntaxTree.GetLineSpan(loc.Span).StartLinePosition;
			CodistPackage.DTE.OpenFile(loc.SyntaxTree.FilePath, pos.Line + 1, pos.Character + 1);
		}

		public static bool IsAccessible(this ISymbol symbol) {
			return symbol.DeclaredAccessibility == Accessibility.Public || symbol.DeclaredAccessibility == Accessibility.NotApplicable || symbol.Locations.Any(l => l.IsInSource);
		}

		public static bool CanConvertTo(this ITypeSymbol symbol, ITypeSymbol target) {
			if (symbol.Equals(target)) {
				return true;
			}
			foreach (var item in symbol.Interfaces) {
				if (item.CanConvertTo(target)) {
					return true;
				}
			}
			while ((symbol = symbol.BaseType) != null) {
				if (symbol.CanConvertTo(target)) {
					return true;
				}
			}
			return false;
		}

		public static bool IsCommonClass(this ISymbol symbol) {
			if (symbol.Kind == SymbolKind.NamedType) {
				var name = symbol.Name;
				return name == "Object" || name == "ValueType" || name == "Enum" || name == "MulticastDelegate";
			}
			return false;
		}

		public static bool IsMemberOrType(this ISymbol symbol) {
			switch (symbol.Kind) {
				case SymbolKind.Event:
				case SymbolKind.Field:
				case SymbolKind.Method:
				case SymbolKind.Property:
				case SymbolKind.NamedType:
					return true;
			}
			return false;
		}

		/// <summary>Checks whether the given symbol has the given <paramref name="kind"/>, <paramref name="returnType"/> and <paramref name="parameters"/>.</summary>
		/// <param name="symbol">The symbol to be checked.</param>
		/// <param name="kind">The <see cref="SymbolKind"/> the symbol should have.</param>
		/// <param name="returnType">The type that the symbol should return.</param>
		/// <param name="parameters">The parameters the symbol should take.</param>
		public static bool MatchSignature(this ISymbol symbol, SymbolKind kind, ITypeSymbol returnType, ImmutableArray<IParameterSymbol> parameters) {
			if (symbol.Kind != kind
				|| returnType == null && symbol.GetReturnType() != null
				|| returnType != null && returnType.Equals(symbol.GetReturnType()) == false) {
				return false;
			}
			var method = kind == SymbolKind.Method ? symbol as IMethodSymbol
				: kind == SymbolKind.Event ? (symbol as IEventSymbol).RaiseMethod
				: null;
			if (method != null && parameters.IsDefault == false) {
				var memberParameters = method.Parameters;
				if (memberParameters.Length != parameters.Length) {
					return false;
				}
				for (var i = parameters.Length - 1; i >= 0; i--) {
					var pi = parameters[i];
					var mi = memberParameters[i];
					if (pi.Type.Equals(mi.Type) == false
						|| pi.RefKind != mi.RefKind) {
						return false;
					}
				}
			}
			return true;
		}

		/// <summary>Returns whether a symbol could have an override.</summary>
		public static bool MayHaveOverride(this ISymbol symbol) {
			var type = symbol.ContainingType;
			return type != null && symbol.IsStatic == false
				&& symbol.IsSealed == false
				&& symbol.DeclaredAccessibility != Accessibility.Private
				&& (symbol.IsAbstract || symbol.IsVirtual || symbol.IsOverride)
				&& (symbol.Kind == SymbolKind.Method && (symbol as IMethodSymbol).MethodKind == MethodKind.Ordinary || symbol.Kind == SymbolKind.Property || symbol.Kind == SymbolKind.Event)
				&& type.TypeKind == TypeKind.Class
				&& type.DeclaredAccessibility != Accessibility.Private
				&& type.IsSealed == false;
		}
	}
}