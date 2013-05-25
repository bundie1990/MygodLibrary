using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using Mygod.Windows.Controls;

namespace Mygod.Windows.Dialogs
{
	public partial class TaskDialog
	{
		private const string HtmlHyperlinkPattern = "<a href=\".+\">.+</a>";
		private const string HtmlHyperlinkCapturePattern = "<a href=\"(?<link>.+)\">(?<text>.+)</a>";

		private static readonly Regex _hyperlinkRegex = new Regex(HtmlHyperlinkPattern);
		private static readonly Regex _hyperlinkCaptureRegex = new Regex(HtmlHyperlinkCapturePattern);

		internal const int CommandButtonIDOffset = 2000;
		internal const int RadioButtonIDOffset = 1000;
		internal const int CustomButtonIDOffset = 500;

		/// <summary>
		/// Forces the WPF-based TaskDialog window instead of using native calls.
		/// </summary>
		public static bool ForceEmulationMode { get; set; }

		/// <summary>
		/// Occurs when a task dialog is about to show.
		/// </summary>
		/// <remarks>
		/// Use this event for both notification and modification of all task
		/// dialog showings. Changes made to the configuration options will be
		/// persisted.
		/// </remarks>
		public static event TaskDialogShowingEventHandler Showing;
		/// <summary>
		/// Occurs when a task dialog has been closed.
		/// </summary>
		public static new event TaskDialogClosedEventHandler Closed;

		/// <summary>
		/// Displays a task dialog with the given configuration options.
		/// </summary>
		/// <param name="options">
		/// A <see cref="T:TaskDialogInterop.TaskDialogOptions"/> that specifies the
		/// configuration options for the dialog.
		/// </param>
		/// <returns>
		/// A <see cref="T:TaskDialogInterop.TaskDialogResult"/> value that specifies
		/// which button is clicked by the user.
		/// </returns>
		public static TaskDialogResult Show(TaskDialogOptions options)
		{
			TaskDialogResult result;

			// Make a copy since we'll let Showing event possibly modify them
			var configOptions = options;

			OnShowing(new TaskDialogShowingEventArgs(ref configOptions));

			if (VistaTaskDialog.IsAvailableOnThisOS && !ForceEmulationMode)
			{
				try
				{
					result = ShowTaskDialog(configOptions);
				}
				catch (EntryPointNotFoundException)
				{
				    Trace.WriteLine("Warning: Please add an app.manifest to prevent the force emulation mode.");
					ForceEmulationMode = true;
					result = ShowEmulatedTaskDialog(configOptions);
				}
			}
			else
			{
				result = ShowEmulatedTaskDialog(configOptions);
			}

			OnClosed(new TaskDialogClosedEventArgs(result));

			return result;
		}

		/// <summary>
		/// Displays a task dialog that has a message and that returns a result.
		/// </summary>
		/// <param name="owner">
		/// The <see cref="T:System.Windows.Window"/> that owns this dialog.
		/// </param>
		/// <param name="title">
		/// A <see cref="T:System.String"/> that specifies the title bar
		/// caption to display.
		/// </param>
		/// <param name="mainInstruction">
		/// A <see cref="T:System.String"/> that specifies the main text to display.
		/// </param>
		/// <param name="content">
		/// A <see cref="T:System.String"/> that specifies the body text to display.
		/// </param>
		/// <param name="expandedInfo">
		/// A <see cref="T:System.String"/> that specifies the expanded text to display when toggled.
		/// </param>
		/// <param name="verificationText">
		/// A <see cref="T:System.String"/> that specifies the text to display next to a checkbox.
		/// </param>
		/// <param name="footerText">
		/// A <see cref="T:System.String"/> that specifies the footer text to display.
		/// </param>
		/// <param name="buttons">
		/// A <see cref="T:TaskDialogInterop.TaskDialogCommonButtons"/> value that
		/// specifies which button or buttons to display.
		/// </param>
		/// <param name="mainIcon">
		/// A <see cref="T:TaskDialogInterop.TaskDialogIcon"/> that specifies the
		/// main icon to display.
		/// </param>
		/// <param name="footerIcon">
		/// A <see cref="T:TaskDialogInterop.TaskDialogIcon"/> that specifies the
		/// footer icon to display.
		/// </param>
		/// <returns></returns>
        public static TaskDialogSimpleResult Show(Window owner = null, string title = null, string mainInstruction = null, string content = null, string expandedInfo = null, string verificationText = null, string footerText = null, TaskDialogButtons buttons = TaskDialogButtons.Default, int? defaultButtonIndex = null, TaskDialogIcon mainIcon = TaskDialogIcon.Default, TaskDialogIcon footerIcon = TaskDialogIcon.Default, TaskDialogType type = TaskDialogType.None)
		{
            TaskDialogOptions options = new TaskDialogOptions();

            options.Owner = owner;
		    options.Title = string.IsNullOrEmpty(title) ? TaskDialogTypeHelper.GetTitle(type) : title;
			options.MainInstruction = mainInstruction;
			options.Content = content;
			options.ExpandedInfo = expandedInfo;
			options.VerificationText = verificationText;
			options.Buttons = buttons == TaskDialogButtons.Default ? TaskDialogTypeHelper.GetButtons(type) : buttons;
			options.MainIcon = mainIcon == TaskDialogIcon.Default ? TaskDialogTypeHelper.GetIcon(type) : mainIcon;
		    options.DefaultButtonIndex = defaultButtonIndex;
            if (!string.IsNullOrEmpty(footerText))
            {
                options.FooterText = footerText;
                options.FooterIcon = footerIcon == TaskDialogIcon.Default ? TaskDialogTypeHelper.GetIcon(type) : footerIcon;
            }

			return Show(options).Result;
		}

		internal static TaskDialogButtonData ConvertCommonButton(TaskDialogButtons commonButton, System.Windows.Input.ICommand command = null, bool isDefault = false, bool isCancel = false)
		{
			int id = 0;

			switch (commonButton)
			{
				default:
				case TaskDialogButtons.None:
					id = (int)TaskDialogSimpleResult.None;
					break;
				case TaskDialogButtons.OK:
					id = (int)TaskDialogSimpleResult.Ok;
					break;
				case TaskDialogButtons.Yes:
					id = (int)TaskDialogSimpleResult.Yes;
					break;
				case TaskDialogButtons.No:
					id = (int)TaskDialogSimpleResult.No;
					break;
				case TaskDialogButtons.Cancel:
					id = (int)TaskDialogSimpleResult.Cancel;
					break;
				case TaskDialogButtons.Retry:
					id = (int)TaskDialogSimpleResult.Retry;
					break;
				case TaskDialogButtons.Close:
					id = (int)TaskDialogSimpleResult.Close;
					break;
			}

			return new TaskDialogButtonData(id, "_" + commonButton.ToString(), command, isDefault, isCancel);
		}

		/// <summary>
		/// Raises the <see cref="E:Showing"/> event.
		/// </summary>
		/// <param name="e">The <see cref="TaskDialogShowingEventArgs"/> instance containing the event data.</param>
		private static void OnShowing(TaskDialogShowingEventArgs e)
		{
			if (Showing != null)
			{
				Showing(null, e);
			}
		}
		/// <summary>
		/// Raises the <see cref="E:Closed"/> event.
		/// </summary>
		/// <param name="e">The <see cref="TaskDialogClosedEventArgs"/> instance containing the event data.</param>
		private static void OnClosed(TaskDialogClosedEventArgs e)
		{
			if (Closed != null)
			{
				Closed(null, e);
			}
		}
		private static TaskDialogResult ShowTaskDialog(TaskDialogOptions options)
		{
			var vtd = new VistaTaskDialog
			{
			    WindowTitle = options.Title, MainInstruction = options.MainInstruction, Content = options.Content,
			    ExpandedInformation = options.ExpandedInfo, Footer = options.FooterText
			};

		    if (options.CommandButtons != null && options.CommandButtons.Length > 0)
			{
				List<VistaTaskDialogButton> lst = new List<VistaTaskDialogButton>();
				for (int i = 0; i < options.CommandButtons.Length; i++)
				{
					try
					{
						VistaTaskDialogButton button = new VistaTaskDialogButton();
						button.ButtonId = CommandButtonIDOffset + i;
                        button.ButtonText = options.CommandButtons[i].Replace('_', '&');
						lst.Add(button);
					}
					catch (FormatException)
					{
					}
				}
				vtd.Buttons = lst.ToArray();
				if (options.DefaultButtonIndex.HasValue
					&& options.DefaultButtonIndex >= 0
					&& options.DefaultButtonIndex.Value < vtd.Buttons.Length)
					vtd.DefaultButton = vtd.Buttons[options.DefaultButtonIndex.Value].ButtonId;
			}
			else if (options.RadioButtons != null && options.RadioButtons.Length > 0)
			{
				List<VistaTaskDialogButton> lst = new List<VistaTaskDialogButton>();
				for (int i = 0; i < options.RadioButtons.Length; i++)
				{
					try
					{
						VistaTaskDialogButton button = new VistaTaskDialogButton();
						button.ButtonId = RadioButtonIDOffset + i;
                        button.ButtonText = options.RadioButtons[i].Replace('_', '&');
						lst.Add(button);
					}
					catch (FormatException)
					{
					}
				}
				vtd.RadioButtons = lst.ToArray();
				vtd.NoDefaultRadioButton = (!options.DefaultButtonIndex.HasValue || options.DefaultButtonIndex.Value == -1);
				if (options.DefaultButtonIndex.HasValue
					&& options.DefaultButtonIndex >= 0
					&& options.DefaultButtonIndex.Value < vtd.RadioButtons.Length)
					vtd.DefaultButton = vtd.RadioButtons[options.DefaultButtonIndex.Value].ButtonId;
			}

			bool hasCustomCancel = false;

			if (options.CustomButtons != null && options.CustomButtons.Length > 0)
			{
				List<VistaTaskDialogButton> lst = new List<VistaTaskDialogButton>();
				for (int i = 0; i < options.CustomButtons.Length; i++)
				{
					try
					{
						VistaTaskDialogButton button = new VistaTaskDialogButton();
						button.ButtonId = CustomButtonIDOffset + i;
                        button.ButtonText = options.CustomButtons[i].Replace('_', '&');

						if (!hasCustomCancel)
						{
							hasCustomCancel =
								(button.ButtonText == "Close"
								|| button.ButtonText == "Cancel");
						}

						lst.Add(button);
					}
					catch (FormatException)
					{
					}
				}

				vtd.Buttons = lst.ToArray();
				if (options.DefaultButtonIndex.HasValue
					&& options.DefaultButtonIndex.Value >= 0
					&& options.DefaultButtonIndex.Value < vtd.Buttons.Length)
					vtd.DefaultButton = vtd.Buttons[options.DefaultButtonIndex.Value].ButtonId;
				vtd.CommonButtons = TaskDialogButtons.None;
			}
			else
			{
				vtd.CommonButtons = options.Buttons;

				if (options.DefaultButtonIndex.HasValue
					&& options.DefaultButtonIndex >= 0)
                    vtd.DefaultButton = options.DefaultButtonIndex.Value;
			}

			vtd.MainIcon = options.MainIcon;
			vtd.CustomMainIcon = options.CustomMainIcon;
			vtd.FooterIcon = options.FooterIcon;
			vtd.CustomFooterIcon = options.CustomFooterIcon;
			vtd.EnableHyperlinks = DetectHyperlinks(options.Content, options.ExpandedInfo, options.FooterText);
			vtd.AllowDialogCancellation =
				(options.AllowDialogCancellation
				|| hasCustomCancel
				|| (options.Buttons & (TaskDialogButtons.Close | TaskDialogButtons.Cancel)) != TaskDialogButtons.None);
			vtd.CallbackTimer = options.EnableCallbackTimer;
			vtd.ExpandedByDefault = options.ExpandedByDefault;
			vtd.ExpandFooterArea = options.ExpandToFooter;
			vtd.PositionRelativeToWindow = true;
			vtd.RightToLeftLayout = false;
			vtd.NoDefaultRadioButton = false;
			vtd.CanBeMinimized = false;
			vtd.ShowProgressBar = options.ShowProgressBar;
			vtd.ShowMarqueeProgressBar = options.ShowMarqueeProgressBar;
			vtd.UseCommandLinks = (options.CommandButtons != null && options.CommandButtons.Length > 0);
			vtd.UseCommandLinksNoIcon = false;
			vtd.VerificationText = options.VerificationText;
			vtd.VerificationFlagChecked = options.VerificationByDefault;
			vtd.ExpandedControlText = options.ExpandedControlText.Replace('_', '&');
            vtd.CollapsedControlText = options.CollapsedControlText.Replace('_', '&');
			vtd.Callback = options.Callback;
			vtd.CallbackData = options.CallbackData;
			vtd.Config = options;

			TaskDialogResult result = null;
			int diagResult = 0;
			TaskDialogSimpleResult simpResult;
			bool verificationChecked = false;
			int radioButtonResult = -1;
			int? commandButtonResult = null;
			int? customButtonResult = null;

			diagResult = vtd.Show((vtd.CanBeMinimized ? null : options.Owner), out verificationChecked, out radioButtonResult);

			if (diagResult >= CommandButtonIDOffset)
			{
				simpResult = TaskDialogSimpleResult.Command;
				commandButtonResult = diagResult - CommandButtonIDOffset;
			}
			else if (radioButtonResult >= RadioButtonIDOffset)
			{
				simpResult = (TaskDialogSimpleResult)diagResult;
				radioButtonResult -= RadioButtonIDOffset;
			}
			else if (diagResult >= CustomButtonIDOffset)
			{
				simpResult = TaskDialogSimpleResult.Custom;
				customButtonResult = diagResult - CustomButtonIDOffset;
			}
			else
			{
				simpResult = (TaskDialogSimpleResult)diagResult;
			}

			result = new TaskDialogResult(
				simpResult,
				(String.IsNullOrEmpty(options.VerificationText) ? null : (bool?)verificationChecked),
				((options.RadioButtons == null || options.RadioButtons.Length == 0) ? null : (int?)radioButtonResult),
				((options.CommandButtons == null || options.CommandButtons.Length == 0) ? null : commandButtonResult),
				((options.CustomButtons == null || options.CustomButtons.Length == 0) ? null : customButtonResult));

			return result;
		}
		private static TaskDialogResult ShowEmulatedTaskDialog(TaskDialogOptions options)
		{
			TaskDialog td = new TaskDialog();
			TaskDialogViewModel tdvm = new TaskDialogViewModel(options);

			td.DataContext = tdvm;

			if (options.Owner != null)
			{
				td.Owner = options.Owner;
			}

			td.ShowDialog();

			TaskDialogResult result = null;
			int diagResult = -1;
			TaskDialogSimpleResult simpResult = TaskDialogSimpleResult.None;
			bool verificationChecked = false;
			int radioButtonResult = -1;
			int? commandButtonResult = null;
			int? customButtonResult = null;

			diagResult = tdvm.DialogResult;
			radioButtonResult = tdvm.RadioResult - RadioButtonIDOffset;
			verificationChecked = tdvm.VerificationChecked;

			if (diagResult >= CommandButtonIDOffset)
			{
				simpResult = TaskDialogSimpleResult.Command;
				commandButtonResult = diagResult - CommandButtonIDOffset;
			}
			//else if (diagResult >= RadioButtonIDOffset)
			//{
			//    simpResult = (TaskDialogSimpleResult)diagResult;
			//    radioButtonResult = diagResult - RadioButtonIDOffset;
			//}
			else if (diagResult >= CustomButtonIDOffset)
			{
				simpResult = TaskDialogSimpleResult.Custom;
				customButtonResult = diagResult - CustomButtonIDOffset;
			}
			// This occurs usually when the red X button is clicked
			else if (diagResult == -1)
			{
				simpResult = TaskDialogSimpleResult.Cancel;
			}
			else
			{
				simpResult = (TaskDialogSimpleResult)diagResult;
			}

			result = new TaskDialogResult(
				simpResult,
				(String.IsNullOrEmpty(options.VerificationText) ? null : (bool?)verificationChecked),
				((options.RadioButtons == null || options.RadioButtons.Length == 0) ? null : (int?)radioButtonResult),
				((options.CommandButtons == null || options.CommandButtons.Length == 0) ? null : commandButtonResult),
				((options.CustomButtons == null || options.CustomButtons.Length == 0) ? null : customButtonResult));

			return result;
		}
		private static bool DetectHyperlinks(string content, string expandedInfo, string footerText)
		{
			return DetectHyperlinks(content) || DetectHyperlinks(expandedInfo) || DetectHyperlinks(footerText);
		}
		private static bool DetectHyperlinks(string text)
		{
			if (String.IsNullOrEmpty(text))
				return false;
			return _hyperlinkRegex.IsMatch(text);
		}
	}
}
