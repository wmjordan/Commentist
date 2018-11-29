﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Imaging;
using AppHelpers;

namespace Codist.Controls
{
	interface IMemberFilterable
	{
		bool Filter(MemberFilterTypes filterTypes);
	}

	sealed class MemberFilterBox : StackPanel
	{
		readonly ThemedTextBox _FilterBox;
		readonly MemberFilterButtonGroup _FilterButtons;
		readonly ItemCollection _Items;

		public MemberFilterBox(ItemCollection items) {
			Orientation = Orientation.Horizontal;
			Margin = WpfHelper.MenuItemMargin;
			Children.Add(ThemeHelper.GetImage(KnownImageIds.Filter).WrapMargin(WpfHelper.GlyphMargin));
			Children.Add(_FilterBox = new ThemedTextBox() {
				MinWidth = 150,
				ToolTip = new ThemedToolTip("Result Filter", "Filter items in this menu.\nUse space to separate keywords.")
			});
			Children.Add(_FilterButtons = new MemberFilterButtonGroup());
			_Items = items;
			_FilterButtons.FilterChanged += FilterChanged;
			_FilterBox.TextChanged += FilterChanged;
		}

		void FilterChanged(object sender, EventArgs e) {
			Filter(_FilterBox.Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), _FilterButtons.Filters);
		}
		void Filter(string[] keywords, MemberFilterTypes filters) {
			bool useModifierFilter = filters != MemberFilterTypes.All;
			if (keywords.Length == 0) {
				if (useModifierFilter) {
					foreach (UIElement item in _Items) {
						item.Visibility = item is ThemedMenuItem.MenuItemPlaceHolder == false
							&& item is IMemberFilterable menuItem && menuItem.Filter(filters)
							? Visibility.Visible
							: Visibility.Collapsed;
					}
				}
				else {
					foreach (UIElement item in _Items) {
						if (item is ThemedMenuItem.MenuItemPlaceHolder) {
							continue;
						}
						item.Visibility = Visibility.Visible;
					}
				}
				return;
			}
			IMemberFilterable filterable;
			foreach (UIElement item in _Items) {
				var menuItem = item as MenuItem;
				if (useModifierFilter) {
					filterable = item as IMemberFilterable;
					if (filterable != null) {
						if (filterable.Filter(filters) == false && (menuItem == null || menuItem.HasItems == false)) {
							item.Visibility = Visibility.Collapsed;
							continue;
						}
						item.Visibility = Visibility.Visible;
					}
				}
				if (menuItem == null) {
					item.Visibility = Visibility.Collapsed;
					continue;
				}
				var b = menuItem.Header as TextBlock;
				if (b == null) {
					continue;
				}
				if (FilterSignature(b.GetText(), keywords)) {
					menuItem.Visibility = Visibility.Visible;
					if (menuItem.HasItems) {
						foreach (MenuItem sub in menuItem.Items) {
							sub.Visibility = Visibility.Visible;
						}
					}
					continue;
				}
				var matchedSubItem = false;
				if (menuItem.HasItems) {
					foreach (MenuItem sub in menuItem.Items) {
						if (useModifierFilter) {
							filterable = sub as IMemberFilterable;
							if (filterable != null) {
								if (filterable.Filter(filters) == false) {
									sub.Visibility = Visibility.Collapsed;
									continue;
								}
								sub.Visibility = Visibility.Visible;
							}
						}
						b = sub.Header as TextBlock;
						if (b == null) {
							continue;
						}
						if (FilterSignature(b.GetText(), keywords)) {
							matchedSubItem = true;
							sub.Visibility = Visibility.Visible;
						}
						else {
							sub.Visibility = Visibility.Collapsed;
						}
					}
				}
				menuItem.Visibility = matchedSubItem ? Visibility.Visible : Visibility.Collapsed;
			}

			bool FilterSignature(string text, string[] words) {
				return words.All(p => text.IndexOf(p, StringComparison.OrdinalIgnoreCase) != -1);
			}
		}

		internal static bool FilterByImageId(MemberFilterTypes filterTypes, int imageId) {
			switch (imageId) {
				case KnownImageIds.ClassPublic:
				case KnownImageIds.InterfacePublic:
				case KnownImageIds.StructurePublic:
				case KnownImageIds.EnumerationPublic:
					return filterTypes.MatchFlags(MemberFilterTypes.Public | MemberFilterTypes.NestedType);
				case KnownImageIds.ClassPrivate:
				case KnownImageIds.InterfacePrivate:
				case KnownImageIds.StructurePrivate:
				case KnownImageIds.EnumerationPrivate:
					return filterTypes.MatchFlags(MemberFilterTypes.Private | MemberFilterTypes.NestedType);
				case KnownImageIds.ClassProtected:
				case KnownImageIds.InterfaceProtected:
				case KnownImageIds.StructureProtected:
				case KnownImageIds.EnumerationProtected:
					return filterTypes.MatchFlags(MemberFilterTypes.Protected | MemberFilterTypes.NestedType);
				case KnownImageIds.ClassInternal:
				case KnownImageIds.InterfaceInternal:
				case KnownImageIds.StructureInternal:
				case KnownImageIds.EnumerationInternal:
					return filterTypes.MatchFlags(MemberFilterTypes.Internal | MemberFilterTypes.NestedType);
				case KnownImageIds.ClassShortcut:
				case KnownImageIds.InterfaceShortcut:
				case KnownImageIds.StructureShortcut:
					return filterTypes.MatchFlags(MemberFilterTypes.NestedType);
				case KnownImageIds.MethodPublic:
				case KnownImageIds.TypePublic: // constructor
				case KnownImageIds.OperatorPublic:
					return filterTypes.MatchFlags(MemberFilterTypes.Public | MemberFilterTypes.Method);
				case KnownImageIds.MethodProtected:
				case KnownImageIds.TypeProtected: // constructor
				case KnownImageIds.OperatorProtected:
					return filterTypes.MatchFlags(MemberFilterTypes.Protected | MemberFilterTypes.Method);
				case KnownImageIds.MethodInternal:
				case KnownImageIds.TypeInternal: // constructor
				case KnownImageIds.OperatorInternal:
					return filterTypes.MatchFlags(MemberFilterTypes.Internal | MemberFilterTypes.Method);
				case KnownImageIds.MethodPrivate:
				case KnownImageIds.TypePrivate: // constructor
				case KnownImageIds.OperatorPrivate:
					return filterTypes.MatchFlags(MemberFilterTypes.Private | MemberFilterTypes.Method);
				case KnownImageIds.DeleteListItem: // deconstructor
					return filterTypes.MatchFlags(MemberFilterTypes.Method);
				case KnownImageIds.FieldPublic:
				case KnownImageIds.ConstantPublic:
				case KnownImageIds.PropertyPublic:
				case KnownImageIds.EventPublic:
					return filterTypes.MatchFlags(MemberFilterTypes.Public | MemberFilterTypes.FieldAndProperty);
				case KnownImageIds.FieldProtected:
				case KnownImageIds.ConstantProtected:
				case KnownImageIds.PropertyProtected:
				case KnownImageIds.EventProtected:
					return filterTypes.MatchFlags(MemberFilterTypes.Protected | MemberFilterTypes.FieldAndProperty);
				case KnownImageIds.FieldInternal:
				case KnownImageIds.ConstantInternal:
				case KnownImageIds.PropertyInternal:
				case KnownImageIds.EventInternal:
					return filterTypes.MatchFlags(MemberFilterTypes.Internal | MemberFilterTypes.FieldAndProperty);
				case KnownImageIds.FieldPrivate:
				case KnownImageIds.ConstantPrivate:
				case KnownImageIds.PropertyPrivate:
				case KnownImageIds.EventPrivate:
					return filterTypes.MatchFlags(MemberFilterTypes.Private | MemberFilterTypes.FieldAndProperty);
				case KnownImageIds.Numeric: // #region
					return filterTypes == MemberFilterTypes.All;
			}
			return true;
		}

		sealed class MemberFilterButtonGroup : UserControl
		{
			static readonly Thickness _Margin = new Thickness(3, 0, 3, 0);
			readonly ThemedToggleButton _FieldFilter, _MethodFilter, _TypeFilter, _PublicFilter, _PrivateFilter;
			bool _uiLock;

			public event EventHandler FilterChanged;

			public MemberFilterButtonGroup() {
				_FieldFilter = CreateButton(KnownImageIds.Field, "Fields and properties");
				_MethodFilter = CreateButton(KnownImageIds.Method, "Methods, delegates and events");
				_TypeFilter = CreateButton(KnownImageIds.EntityContainer, "Nested types");

				_PublicFilter = CreateButton(KnownImageIds.ModulePublic, "Public and protected members");
				_PrivateFilter = CreateButton(KnownImageIds.ModulePrivate, "Internal and private members");

				Margin = _Margin;
				Content = new Border {
					BorderThickness = WpfHelper.TinyMargin,
					BorderBrush = ThemeHelper.TextBoxBorderBrush,
					CornerRadius = new CornerRadius(3),
					Child = new StackPanel {
						Children = {
							_PublicFilter, _PrivateFilter,
							new Border{ Width = 1, BorderThickness = WpfHelper.TinyMargin, BorderBrush = ThemeHelper.TextBoxBorderBrush },
							_FieldFilter, _MethodFilter, _TypeFilter,
							new Border{ Width = 1, BorderThickness = WpfHelper.TinyMargin, BorderBrush = ThemeHelper.TextBoxBorderBrush },
							new ThemedButton(KnownImageIds.StopFilter, "Clear filter switches", ClearFilter) { Margin = WpfHelper.NoMargin, BorderThickness = WpfHelper.NoMargin },
						},
						Orientation = Orientation.Horizontal
					}
				};
			}

			public MemberFilterTypes Filters { get; private set; } = MemberFilterTypes.All;

			void UpdateFilterValue(object sender, RoutedEventArgs eventArgs) {
				if (_uiLock) {
					return;
				}
				var f = MemberFilterTypes.None;
				if (_FieldFilter.IsChecked == true) {
					f |= MemberFilterTypes.FieldAndProperty;
				}
				if (_MethodFilter.IsChecked == true) {
					f |= MemberFilterTypes.Method;
				}
				if (_TypeFilter.IsChecked == true) {
					f |= MemberFilterTypes.NestedType;
				}
				if (f.HasAnyFlag(MemberFilterTypes.AllMembers) == false) {
					f |= MemberFilterTypes.AllMembers;
				}
				if (_PublicFilter.IsChecked == true) {
					f |= MemberFilterTypes.Public | MemberFilterTypes.Protected;
				}
				if (_PrivateFilter.IsChecked == true) {
					f |= MemberFilterTypes.Internal | MemberFilterTypes.Private;
				}
				if (f.HasAnyFlag(MemberFilterTypes.AllAccessibility) == false) {
					f |= MemberFilterTypes.AllAccessibility;
				}
				if (Filters != f) {
					Filters = f;
					FilterChanged?.Invoke(this, EventArgs.Empty);
				}
			}

			void ClearFilter() {
				_uiLock = true;
				_FieldFilter.IsChecked = _MethodFilter.IsChecked = _TypeFilter.IsChecked
					= _PublicFilter.IsChecked = _PrivateFilter.IsChecked = false;
				_uiLock = false;
				if (Filters != MemberFilterTypes.All) {
					Filters = MemberFilterTypes.All;
					FilterChanged?.Invoke(this, EventArgs.Empty);
				}
			}

			ThemedToggleButton CreateButton(int imageId, string toolTip) {
				var b = new ThemedToggleButton(imageId, toolTip) { BorderThickness = WpfHelper.NoMargin };
				b.Checked += UpdateFilterValue;
				b.Unchecked += UpdateFilterValue;
				return b;
			}
		}
	}

	[Flags]
	enum MemberFilterTypes
	{
		None,
		FieldAndProperty = 1,
		Method = 1 << 1,
		NestedType = 1 << 2,
		AllMembers = FieldAndProperty | Method | NestedType,
		Public = 1 << 3,
		Protected = 1 << 4,
		Internal = 1 << 5,
		Private = 1 << 6,
		AllAccessibility = Public | Protected | Private | Internal,
		All = AllMembers | AllAccessibility
	}
}