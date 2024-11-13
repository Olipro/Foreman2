﻿using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Windows.Forms;

namespace Foreman
{
	partial class NativeMethods
	{
		private const int LVM_FIRST = 0x1000;
		private const int LVM_SETITEMSTATE = LVM_FIRST + 43;

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public unsafe struct LVITEM
		{
			public int mask;
			public int iItem;
			public int iSubItem;
			public int state;
			public int stateMask;
			public ushort* pszText;
			public int cchTextMax;
			public int iImage;
			public IntPtr lParam;
			public int iIndent;
			public int iGroupId;
			public int cColumns;
			public IntPtr puColumns;
		};
		[LibraryImport("user32.dll")]
		private static partial IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);

		[LibraryImport("user32.dll", EntryPoint = "SendMessage")]
		public static partial IntPtr SendMessageLVItem(IntPtr hWnd, int msg, int wParam, ref LVITEM lvi);

		[Flags]
		public enum DwmWindowAttribute : uint {
			DWMWA_NCRENDERING_ENABLED = 1,
			DWMWA_NCRENDERING_POLICY,
			DWMWA_TRANSITIONS_FORCEDISABLED,
			DWMWA_ALLOW_NCPAINT,
			DWMWA_CAPTION_BUTTON_BOUNDS,
			DWMWA_NONCLIENT_RTL_LAYOUT,
			DWMWA_FORCE_ICONIC_REPRESENTATION,
			DWMWA_FLIP3D_POLICY,
			DWMWA_EXTENDED_FRAME_BOUNDS,
			DWMWA_HAS_ICONIC_BITMAP,
			DWMWA_DISALLOW_PEEK,
			DWMWA_EXCLUDED_FROM_PEEK,
			DWMWA_CLOAK,
			DWMWA_CLOAKED,
			DWMWA_FREEZE_REPRESENTATION,
			DWMWA_PASSIVE_UPDATE_MODE,
			DWMWA_USE_HOSTBACKDROPBRUSH,
			DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
			DWMWA_WINDOW_CORNER_PREFERENCE = 33,
			DWMWA_BORDER_COLOR,
			DWMWA_CAPTION_COLOR,
			DWMWA_TEXT_COLOR,
			DWMWA_VISIBLE_FRAME_BORDER_THICKNESS,
			DWMWA_SYSTEMBACKDROP_TYPE,
			DWMWA_LAST
		}

		[LibraryImport("dwmapi.dll")]
		public static partial int DwmSetWindowAttribute(IntPtr hwnd, DwmWindowAttribute dwAttribute, ref int pvAttribute, int cbAttribute);

		/// <summary>
		/// Select all rows on the given listview
		/// </summary>
		/// <param name="list">The listview whose items are to be selected</param>
		public static void SelectAllItems(ListView list)
		{
			NativeMethods.SetItemState(list, -1, 2, 2);
		}

		/// <summary>
		/// Deselect all rows on the given listview
		/// </summary>
		/// <param name="list">The listview whose items are to be deselected</param>
		public static void DeselectAllItems(ListView list)
		{
			NativeMethods.SetItemState(list, -1, 2, 0);
		}

		/// <summary>
		/// Set the item state on the given item
		/// </summary>
		/// <param name="list">The listview whose item's state is to be changed</param>
		/// <param name="itemIndex">The index of the item to be changed</param>
		/// <param name="mask">Which bits of the value are to be set?</param>
		/// <param name="value">The value to be set</param>
		public static void SetItemState(ListView list, int itemIndex, int mask, int value)
		{
			LVITEM lvItem = new() {
				stateMask = mask,
				state = value
			};
			SendMessageLVItem(list.Handle, LVM_SETITEMSTATE, itemIndex, ref lvItem);
		}
	}
}
