﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtoScript
{
	public interface IStylesheet
	{
		IStyle GetStyle(string styleId);
		string FontFamily { get; }
		int FontSizeInPoints { get; }
	}

	public interface IStyle
	{
		string Id { get; }
		bool IsVerseText { get; }
		bool IsPublishable { get; }
		bool IsChapterLabel { get; }
		bool IsParallelPassageReference { get; }
		bool HoldsBookNameOrAbbreviation { get; }
	}
}