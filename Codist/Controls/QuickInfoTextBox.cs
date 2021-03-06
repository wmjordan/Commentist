﻿using System;
using System.Windows.Input;
using System.Windows.Controls;
using Codist;

namespace Codist.Controls
{
	sealed class QuickInfoTextBox : TextBox
	{
		public QuickInfoTextBox() {
			IsReadOnly = true;
			this.ReferenceStyle(Microsoft.VisualStudio.Shell.VsResourceKeys.TextBoxStyleKey);
		}

		protected override void OnMouseRightButtonUp(MouseButtonEventArgs e) {
			base.OnMouseRightButtonUp(e);
			QuickInfo.QuickInfoOverrider.HoldQuickInfo(e.Source as System.Windows.DependencyObject, true);
			if (ContextMenu == null) {
				ContextMenu = new ContextMenu {
					Resources = SharedDictionaryManager.ContextMenu,
					Foreground = ThemeHelper.ToolWindowTextBrush,
					IsOpen = true,
					Items = {
						new ThemedMenuItem(IconIds.Copy, Properties.Resources.CMD_CopySelection, (s, args)=> {
							Copy();
						})
					}
				};
				ContextMenu.SetBackgroundForCrispImage(ThemeHelper.TitleBackgroundColor);
			}
		}
	}
}
