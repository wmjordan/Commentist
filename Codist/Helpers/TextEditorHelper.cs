﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Codist.SyntaxHighlight;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using VsTextView = Microsoft.VisualStudio.TextManager.Interop.IVsTextView;
using VsUserData = Microsoft.VisualStudio.TextManager.Interop.IVsUserData;

namespace Codist
{
	/// <summary>
	/// This class assumes that the <see cref="IClassificationFormatMap"/> is shared among document editor instances and the "default" classification format map contains all needed formatting.
	/// </summary>
	static class TextEditorHelper
	{
		static /*readonly*/ Guid guidIWpfTextViewHost = new Guid("8C40265E-9FDB-4f54-A0FD-EBB72B7D0476");

		public static bool AnyTextChanges(ITextVersion oldVersion, ITextVersion currentVersion) {
			while (oldVersion != currentVersion) {
				if (oldVersion.Changes.Count > 0) {
					return true;
				}
				oldVersion = oldVersion.Next;
			}
			return false;
		}
		public static bool Contains(this TextSpan span, int position, bool inclusive) {
			return span.Contains(position) || (inclusive && span.End == position);
		}
		public static bool Contains(this TextSpan span, ITextSelection selection, bool inclusive) {
			var start = selection.Start.Position.Position;
			var end = selection.End.Position.Position;
			return span.Contains(start) && (span.Contains(end) || inclusive && span.End == end);
		}

		public static SnapshotSpan CreateSnapshotSpan(this TextSpan span, ITextSnapshot snapshot) {
			if (span.End < snapshot.Length) {
				return new SnapshotSpan(snapshot, span.Start, span.Length);
			}
			return default;
		}
		public static Span ToSpan(this TextSpan span) {
			return new Span(span.Start, span.Length);
		}
		public static TextSpan ToTextSpan(this SnapshotSpan span) {
			return new TextSpan(span.Start, span.Length);
		}
		public static Span ToSpan(this SnapshotSpan span) {
			return new Span(span.Start, span.Length);
		}
		public static TextSpan ToTextSpan(this Span span) {
			return new TextSpan(span.Start, span.Length);
		}

		public static Span GetLineSpan(this SnapshotSpan span) {
			return Span.FromBounds(span.Snapshot.GetLineNumberFromPosition(span.Start),
				span.Snapshot.GetLineNumberFromPosition(span.End));
		}

		public static Span GetLineSpan(this ITextSnapshot snapshot, TextSpan span) {
			return Span.FromBounds(snapshot.GetLineNumberFromPosition(span.Start),
				snapshot.GetLineNumberFromPosition(span.End));
		}

		public static TextFormattingRunProperties GetRunProperties(this IClassificationFormatMap formatMap, string classificationType) {
			var t = ServicesHelper.Instance.ClassificationTypeRegistry.GetClassificationType(classificationType);
			return t == null ? null : formatMap.GetTextProperties(t);
		}

		/// <summary>
		/// Begins an edit operation to the <paramref name="view"/>.
		/// </summary>
		/// <typeparam name="TView">The type of the view.</typeparam>
		/// <param name="view">The <see cref="ITextView"/> to be edited.</param>
		/// <param name="action">The edit operation.</param>
		/// <returns>Returns a new <see cref="ITextSnapshot"/> if <see cref="ITextEdit.HasEffectiveChanges"/>  returns <see langword="true"/>, otherwise, returns <see langword="null"/>.</returns>
		public static ITextSnapshot Edit<TView>(this TView view, Action<TView, ITextEdit> action)
			where TView : ITextView {
			using (var edit = view.TextSnapshot.TextBuffer.CreateEdit()) {
				action(view, edit);
				if (edit.HasEffectiveChanges) {
					return edit.Apply();
				}
				return null;
			}
		}
		/// <summary>
		/// Performs edit operation to each selected spans in the <paramref name="view"/>.
		/// </summary>
		/// <typeparam name="TView">The type of the view.</typeparam>
		/// <param name="view">The <see cref="ITextView"/> to be edited.</param>
		/// <param name="action">The edit operation against each selected span.</param>
		/// <returns>Returns a new <see cref="SnapshotSpan"/> if <see cref="ITextEdit.HasEffectiveChanges"/> returns <see langword="true"/> and <paramref name="action"/> returns a <see cref="Span"/>, otherwise, returns <see langword="null"/>.</returns>
		public static SnapshotSpan? EditSelection<TView>(this TView view, Func<TView, ITextEdit, SnapshotSpan, Span?> action)
			where TView : ITextView {
			using (var edit = view.TextSnapshot.TextBuffer.CreateEdit()) {
				Span? s = null;
				foreach (var item in view.Selection.SelectedSpans) {
					if (s == null) {
						s = action(view, edit, item);
					}
					else {
						action(view, edit, item);
					}
				}
				if (edit.HasEffectiveChanges) {
					var shot = edit.Apply();
					if (s.HasValue) {
						return view.TextSnapshot.CreateTrackingSpan(s.Value, SpanTrackingMode.EdgeInclusive).GetSpan(shot);
					}
				}
				return null;
			}
		}

		public static void ExpandSelectionToLine(this ITextView view) {
			view.ExpandSelectionToLine(true);
		}
		public static void ExpandSelectionToLine(this ITextView view, bool includeLineBreak) {
			var start = view.TextSnapshot.GetLineFromPosition(view.Selection.Start.Position).Start;
			var end = view.Selection.End.Position;
			var endLine = view.TextSnapshot.GetLineFromPosition(end);
			if (endLine.Start != end) {
				// if selection not ended in line break, expand to line break
				end = includeLineBreak ? endLine.EndIncludingLineBreak : endLine.End;
			}
			view.Selection.Select(new SnapshotSpan(start, end), false);
		}
		public static string GetFirstSelectionText(this ITextSelection selection) {
			return selection.IsEmpty ? String.Empty : selection.SelectedSpans[0].GetText();
		}
		public static string GetFirstSelectionText(this ITextView view) {
			return view.TryGetFirstSelectionSpan(out var span) ? span.GetText() : String.Empty;
		}
		public static bool TryGetFirstSelectionSpan(this ITextView view, out SnapshotSpan span) {
			if (view.Selection.IsEmpty || view.Selection.SelectedSpans.Count < 1) {
				span = new SnapshotSpan();
				return false;
			}
			else {
				span = view.Selection.SelectedSpans[0];
				return true;
			}
		}

		public static TokenType GetSelectedTokenType(this ITextView view) {
			if (view.Selection.IsEmpty || view.Selection.SelectedSpans.Count > 1) {
				return TokenType.None;
			}
			var selection = view.Selection.SelectedSpans[0];
			if (selection.Length >= 128) {
				return TokenType.None;
			}
			string s = null;
			if ((selection.Length == 36 || selection.Length == 38) && Guid.TryParse(s = selection.GetText(), out var result)) {
				return TokenType.Guid;
			}
			if (selection.Length == 4 && (s = selection.GetText()).Equals("Guid", StringComparison.OrdinalIgnoreCase)) {
				return TokenType.GuidPlaceHolder;
			}
			var t = TokenType.None;
			foreach (var c in s ?? (s = selection.GetText())) {
				if (c >= '0' && c <= '9') {
					t |= TokenType.Digit;
				}
				else if (c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z') {
					t |= TokenType.Letter;
				}
				else if (c == '_') {
					t |= TokenType.Underscore;
				}
				else if (c == '.') {
					t |= TokenType.Dot;
				}
				else {
					return TokenType.None;
				}
			}
			return t;
		}

		public static SnapshotPoint GetCaretPosition(this ITextView textView) {
			return textView.Caret.Position.BufferPosition;
		}

		public static bool IsCaretInReadOnlyRegion(this ITextView textView) {
			return textView.TextBuffer.IsReadOnly(textView.Caret.Position.BufferPosition);
		}

		public static bool IsMultilineSelected(this ITextView textView) {
			var s = textView.Selection;
			if (s.IsEmpty || s.SelectedSpans.Count < 1) {
				return false;
			}
			return textView.GetTextViewLineContainingBufferPosition(s.Start.Position) != textView.GetTextViewLineContainingBufferPosition(s.End.Position);
		}

		public static void SelectNode(this ITextView view, SyntaxNode node, bool includeTrivia) {
			var span = includeTrivia ? node.FullSpan : node.Span;
			if (view.TextSnapshot.Length > span.End) {
				var ss = includeTrivia
					? new SnapshotSpan(view.TextSnapshot, node.GetSematicSpan())
					: new SnapshotSpan(view.TextSnapshot, span.Start, span.Length);
				view.SelectSpan(ss);
			}
		}

		public static void SelectSpan(this ITextView view, SnapshotSpan span) {
			if (view.TextSnapshot != span.Snapshot) {
				// should not be here
				span = new SnapshotSpan(view.TextSnapshot, span.Span);
			}
			view.ViewScroller.EnsureSpanVisible(span, EnsureSpanVisibleOptions.ShowStart);
			view.Selection.Select(span, false);
			view.Caret.MoveTo(span.End);
		}

		public static void SelectSpan(this ITextView view, int start, int end) {
			if (end < 0 || start < 0 || start + end > view.TextSnapshot.Length) {
				return;
			}
			var span = new SnapshotSpan(view.TextSnapshot, start, end);
			view.ViewScroller.EnsureSpanVisible(span, EnsureSpanVisibleOptions.ShowStart);
			view.Selection.Select(span, false);
			view.Caret.MoveTo(span.End);
		}

		public static void CopyOrMoveSyntaxNode(this IWpfTextView view, SyntaxNode sourceNode, SyntaxNode targetNode, bool copy, bool before) {
			var tSpan = (targetNode.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.VariableDeclarator) ? targetNode.Parent.Parent : targetNode).GetSematicSpan();
			var sNode = sourceNode.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.VariableDeclarator) ? sourceNode.Parent.Parent : sourceNode;
			var sSpan = sNode.GetSematicSpan();
			var target = before ? tSpan.Start : tSpan.End;

			if (targetNode.SyntaxTree.FilePath == sourceNode.SyntaxTree.FilePath) {
				using (var edit = view.TextBuffer.CreateEdit()) {
					edit.Insert(target, view.TextSnapshot.GetText(sSpan));
					if (copy == false) {
						edit.Delete(sSpan.Start, sSpan.Length);
					}
					if (edit.HasEffectiveChanges) {
						edit.Apply();
						view.SelectSpan(sSpan.Start > tSpan.Start ? target : target - sSpan.Length, sSpan.Length);
					}
				}
			}
			else {
				using (var edit = view.TextBuffer.CreateEdit()) {
					if (copy == false) {
						edit.Delete(sSpan.Start, sSpan.Length);
					}
					if (edit.HasEffectiveChanges) {
						edit.Apply();
					}
				}
				CodistPackage.DTE.OpenFile(targetNode.SyntaxTree.FilePath, d => {
					view = CodistPackage.Instance.GetActiveWpfDocumentView();
					using (var edit = view.TextBuffer.CreateEdit()) {
						edit.Insert(target, sNode.ToFullString());
						if (edit.HasEffectiveChanges) {
							edit.Apply();
							view.SelectSpan(target, sSpan.Length);
						}
					}
				});
			}
		}

		public static void TryExecuteCommand(this EnvDTE.DTE dte, string command, string args = "") {
			ThreadHelper.ThrowIfNotOnUIThread();
			try {
				if (dte.Commands.Item(command).IsAvailable) {
					dte.ExecuteCommand(command, args);
				}
			}
			catch (System.Runtime.InteropServices.COMException ex) {
				System.Windows.Forms.MessageBox.Show(ex.ToString());
				if (System.Diagnostics.Debugger.IsAttached) {
					System.Diagnostics.Debugger.Break();
				}
			}
		}

		public static void ExecuteEditorCommand(string command, string args = "") {
			CodistPackage.DTE.TryExecuteCommand(command, args);
		}

		public static void OpenFile(this EnvDTE.DTE dte, string file, Action<EnvDTE.Document> action) {
			ThreadHelper.ThrowIfNotOnUIThread();
			if (String.IsNullOrEmpty(file)) {
				return;
			}
			file = System.IO.Path.GetFullPath(file);
			if (System.IO.File.Exists(file) == false) {
				return;
			}
			using (new NewDocumentStateScope(__VSNEWDOCUMENTSTATE.NDS_Provisional, Microsoft.VisualStudio.VSConstants.NewDocumentStateReason.Navigation)) {
				dte.ItemOperations.OpenFile(file);
				try {
					action(dte.ActiveDocument);
				}
				catch (NullReferenceException) { /* ignore */ }
			}
		}
		public static void OpenFile(this EnvDTE.DTE dte, string file, int line, int column) {
			dte.OpenFile(file, d => ((EnvDTE.TextSelection)d.Selection).MoveToLineAndOffset(line, column));
		}

		public static IWpfTextView GetActiveWpfDocumentView(this IServiceProvider service) {
			ThreadHelper.ThrowIfNotOnUIThread();
			var doc = CodistPackage.DTE.ActiveDocument;
			if (doc == null) {
				return null;
			}
			var textView = GetIVsTextView(service, doc.FullName);
			return textView == null ? null : GetWpfTextView(textView);
		}

		static VsTextView GetIVsTextView(IServiceProvider service, string filePath) {
			IVsWindowFrame windowFrame;
			return VsShellUtilities.IsDocumentOpen(service, filePath, Guid.Empty, out var uiHierarchy, out uint itemID, out windowFrame)
				? VsShellUtilities.GetTextView(windowFrame)
				: null;
		}
		static IWpfTextView GetWpfTextView(VsTextView vTextView) {
			var userData = vTextView as VsUserData;
			if (userData == null) {
				return null;
			}
			var guidViewHost = guidIWpfTextViewHost;
			userData.GetData(ref guidViewHost, out object holder);
			return ((IWpfTextViewHost)holder).TextView;
		}
	}

	[Flags]
	public enum TokenType
	{
		None,
		Letter = 1,
		Digit = 2,
		Dot = 4,
		Underscore = 8,
		Guid = 16,
		GuidPlaceHolder = 32
	}
}
