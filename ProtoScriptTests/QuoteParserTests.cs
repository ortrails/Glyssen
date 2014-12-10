﻿using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProtoScript;

namespace ProtoScriptTests
{
	[TestFixture]
	public class QuoteParserTests
	{
		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			// Use a test version of the file so the tests won't break every time we fix a problem in the production control file.
			CharacterVerseData.TabDelimitedCharacterVerseData = Properties.Resources.TestCharacterVerse;
		}

		[Test]
		public void Parse_OneBlockBecomesTwo_QuoteAtEnd()
		{
			var block = new Block("p", 7, 6);
			block.BlockElements.Add(new ScriptText("He replied, «Isaiah was right when he prophesied about you.»"));
			var input = new List<Block> { block };
			IList<Block> output = new QuoteParser(CharacterVerseData.Singleton, "MRK", input).Parse().ToList();
			Assert.AreEqual(2, output.Count);
			Assert.AreEqual("He replied, ", output[0].GetText(false));
			Assert.AreEqual(7, output[0].ChapterNumber);
			Assert.AreEqual(6, output[0].InitialStartVerseNumber);
			Assert.IsTrue(output[0].CharacterIs("MRK", CharacterVerseData.StandardCharacter.Narrator));
			Assert.AreEqual("«Isaiah was right when he prophesied about you.»", output[1].GetText(false));
			Assert.AreEqual("Jesus", output[1].CharacterId);
			Assert.AreEqual("rebuking", output[1].Delivery);
			Assert.AreEqual(7, output[1].ChapterNumber);
			Assert.AreEqual(6, output[1].InitialStartVerseNumber);
		}

		[Test]
		public void Parse_OneBlockBecomesTwo_UnclosedQuoteAtEnd()
		{
			var block = new Block("p", 2, 10);
			block.BlockElements.Add(new ScriptText("But the angel said to them, «Do not be afraid!"));
			var input = new List<Block> { block };
			IList<Block> output = new QuoteParser(CharacterVerseData.Singleton, "LUK", input).Parse().ToList();
			Assert.AreEqual(2, output.Count);
			Assert.AreEqual("But the angel said to them, ", output[0].GetText(false));
			Assert.AreEqual(2, output[0].ChapterNumber);
			Assert.AreEqual(10, output[0].InitialStartVerseNumber);
			Assert.IsTrue(output[0].CharacterIs("LUK", CharacterVerseData.StandardCharacter.Narrator));
			Assert.AreEqual("«Do not be afraid!", output[1].GetText(false));
			Assert.AreEqual("angel of the LORD, an", output[1].CharacterId);
			Assert.AreEqual(2, output[1].ChapterNumber);
			Assert.AreEqual(10, output[1].InitialStartVerseNumber);
		}

		[Test]
		public void Parse_OneBlockBecomesTwo_QuoteAtBeginning()
		{
			var block = new Block("p");
			block.BlockElements.Add(new ScriptText("«Go!» he said."));
			var input = new List<Block> { block };
			IList<Block> output = new QuoteParser(CharacterVerseData.Singleton, "LUK", input).Parse().ToList();
			Assert.AreEqual(2, output.Count);
			Assert.AreEqual("«Go!» ", output[0].GetText(false));
			Assert.IsFalse(CharacterVerseData.IsCharacterOfType(output[0].CharacterId, CharacterVerseData.StandardCharacter.Narrator));
			Assert.AreEqual("he said.", output[1].GetText(false));
			Assert.IsTrue(output[1].CharacterIs("LUK", CharacterVerseData.StandardCharacter.Narrator));
		}
		[Test]
		public void Parse_OneBlockBecomesThree_TwoQuotes()
		{
			var block = new Block("p");
			block.BlockElements.Add(new ScriptText("He said, «Go!»  «Make me!»"));
			var input = new List<Block> { block };
			IList<Block> output = new QuoteParser(CharacterVerseData.Singleton, "LUK", input).Parse().ToList();
			Assert.AreEqual(3, output.Count);
			Assert.AreEqual("He said, ", output[0].GetText(false));
			Assert.IsTrue(output[0].CharacterIs("LUK", CharacterVerseData.StandardCharacter.Narrator));
			Assert.AreEqual("«Go!»  ", output[1].GetText(false));
			Assert.IsFalse(CharacterVerseData.IsCharacterOfType(output[1].CharacterId, CharacterVerseData.StandardCharacter.Narrator));
			Assert.AreEqual("«Make me!»", output[2].GetText(false));
			Assert.IsFalse(CharacterVerseData.IsCharacterOfType(output[2].CharacterId, CharacterVerseData.StandardCharacter.Narrator));
		}
		[Test]
		public void Parse_OneBlockBecomesThree_QuoteInMiddle()
		{
			var block = new Block("p");
			block.BlockElements.Add(new ScriptText("He said, «Go!» quietly."));
			var input = new List<Block> { block };
			IList<Block> output = new QuoteParser(CharacterVerseData.Singleton, "LUK", input).Parse().ToList();
			Assert.AreEqual(3, output.Count);
			Assert.AreEqual("He said, ", output[0].GetText(false));
			Assert.IsTrue(output[0].CharacterIs("LUK", CharacterVerseData.StandardCharacter.Narrator));
			Assert.AreEqual("«Go!» ", output[1].GetText(false));
			Assert.IsFalse(CharacterVerseData.IsCharacterOfType(output[1].CharacterId, CharacterVerseData.StandardCharacter.Narrator));
			Assert.AreEqual("quietly.", output[2].GetText(false));
			Assert.IsTrue(output[2].CharacterIs("LUK", CharacterVerseData.StandardCharacter.Narrator));
		}

		[Test]
		public void Parse_TwoBlocksRemainTwo_NoQuotes()
		{
			var block = new Block("p");
			block.BlockElements.Add(new ScriptText("See Spot run. "));
			var block2 = new Block("p");
			block2.BlockElements.Add(new ScriptText("See Jane see Spot run."));
			var input = new List<Block> { block, block2 };
			IList<Block> output = new QuoteParser(CharacterVerseData.Singleton, "LUK", input).Parse().ToList();
			Assert.AreEqual(2, output.Count);
			Assert.AreEqual("See Spot run. ", output[0].GetText(false));
			Assert.IsTrue(output[0].CharacterIs("LUK", CharacterVerseData.StandardCharacter.Narrator));
			Assert.AreEqual("See Jane see Spot run.", output[1].GetText(false));
			Assert.IsTrue(output[1].CharacterIs("LUK", CharacterVerseData.StandardCharacter.Narrator));
		}
		[Test]
		public void Parse_TwoBlocksRemainTwo_NoQuotesAndQuotes()
		{
			var block = new Block("p");
			block.BlockElements.Add(new ScriptText("He said, "));
			var block2 = new Block("p");
			block2.BlockElements.Add(new ScriptText("«Go!»"));
			var input = new List<Block> { block, block2 };
			IList<Block> output = new QuoteParser(CharacterVerseData.Singleton, "LUK", input).Parse().ToList();
			Assert.AreEqual(2, output.Count);
			Assert.AreEqual("He said, ", output[0].GetText(false));
			Assert.IsTrue(output[0].CharacterIs("LUK", CharacterVerseData.StandardCharacter.Narrator));
			Assert.AreEqual("«Go!»", output[1].GetText(false));
			Assert.IsFalse(CharacterVerseData.IsCharacterOfType(output[1].CharacterId, CharacterVerseData.StandardCharacter.Narrator));
		}

		[Test]
		public void Parse_TwoBlocksBecomeThree_NoQuotesAndQuotes()
		{
			var block = new Block("p");
			block.BlockElements.Add(new ScriptText("He said, «Go!» "));
			var block2 = new Block("p");
			block2.BlockElements.Add(new ScriptText("«Make me!»"));
			var input = new List<Block> { block, block2 };
			IList<Block> output = new QuoteParser(CharacterVerseData.Singleton, "LUK", input).Parse().ToList();
			Assert.AreEqual(3, output.Count);
			Assert.AreEqual("He said, ", output[0].GetText(false));
			Assert.AreEqual("«Go!» ", output[1].GetText(false));
			Assert.AreEqual("«Make me!»", output[2].GetText(false));
		}
		[Test]
		public void Parse_TwoBlocksBecomeThree_AlreadyBrokenInMiddleOfQuote()
		{
			var block = new Block("p");
			block.BlockElements.Add(new ScriptText("He said, «Go "));
			var block2 = new Block("p");
			block2.BlockElements.Add(new ScriptText("west!»"));
			var input = new List<Block> { block, block2 };
			IList<Block> output = new QuoteParser(CharacterVerseData.Singleton, "LUK", input).Parse().ToList();
			Assert.AreEqual(3, output.Count);
			Assert.AreEqual("He said, ", output[0].GetText(false));
			Assert.AreEqual("«Go ", output[1].GetText(false));
			Assert.AreEqual("west!»", output[2].GetText(false));
		}

		[Test]
		public void Parse_ContinuingQuote_UsingStartingMarker()
		{
			var block = new Block("p");
			block.BlockElements.Add(new ScriptText("He said, «Go!"));
			var block2 = new Block("p");
			block2.BlockElements.Add(new ScriptText("«Get!»"));
			var input = new List<Block> { block, block2 };
			IList<Block> output = new QuoteParser(CharacterVerseData.Singleton, "LUK", input).Parse().ToList();
			Assert.AreEqual(3, output.Count);
			Assert.AreEqual("He said, ", output[0].GetText(false));
			Assert.AreEqual("«Go!", output[1].GetText(false));
			Assert.AreEqual("«Get!»", output[2].GetText(false));
		}

		[Test, Ignore("TODO")]
		public void Parse_ContinuingQuote_UsingEndingMarker()
		{
			var block = new Block("p");
			block.BlockElements.Add(new ScriptText("He said, «Go!"));
			var block2 = new Block("p");
			block2.BlockElements.Add(new ScriptText("»Get!»"));
			var input = new List<Block> { block, block2 };
			IList<Block> output = new QuoteParser(CharacterVerseData.Singleton, "LUK", input).Parse().ToList();
			Assert.AreEqual(3, output.Count);
			Assert.AreEqual("He said, ", output[0].GetText(false));
			Assert.AreEqual("«Go!", output[1].GetText(false));
			Assert.AreEqual("»Get!»", output[2].GetText(false));
		}

		[Test]
		public void Parse_StartEndSame_QuoteAtEnd()
		{
			var options = new QuoteSystem { StartQuoteMarker = "\"", EndQuoteMarker = "\"" };
			var block = new Block("p");
			block.BlockElements.Add(new ScriptText("He said, \"Go!\""));
			var input = new List<Block> { block };
			IList<Block> output = new QuoteParser(CharacterVerseData.Singleton, "MRK", input, options).Parse().ToList();
			Assert.AreEqual(2, output.Count);
			Assert.AreEqual("He said, ", output[0].GetText(false));
			Assert.AreEqual("\"Go!\"", output[1].GetText(false));
		}

		[Test]
		public void Parse_StartEndSame_QuoteAtBeginning()
		{
			var options = new QuoteSystem { StartQuoteMarker = "\"", EndQuoteMarker = "\"" };
			var block = new Block("p");
			block.BlockElements.Add(new ScriptText("\"Go!\" he said."));
			var input = new List<Block> { block };
			IList<Block> output = new QuoteParser(CharacterVerseData.Singleton, "MRK", input, options).Parse().ToList();
			Assert.AreEqual(2, output.Count);
			Assert.AreEqual("\"Go!\" ", output[0].GetText(false));
			Assert.AreEqual("he said.", output[1].GetText(false));
		}

		[Test, Ignore("TODO")]
		public void Parse_StartEndSame_ThreeLevels()
		{
			var options = new QuoteSystem { StartQuoteMarker = "\"", EndQuoteMarker = "\"" };
			var block = new Block("p");
			block.BlockElements.Add(new ScriptText("He said, \"She said, 'They said, \"No way.\"'\""));
			var input = new List<Block> { block };
			IList<Block> output = new QuoteParser(CharacterVerseData.Singleton, "MRK", input, options).Parse().ToList();
			Assert.AreEqual(2, output.Count);
			Assert.AreEqual("He said, ", output[0].GetText(true));
			Assert.AreEqual("\"She said, 'They said, \"No way.\"'\"", output[1].GetText(true));
		}

		[Test]
		public void Parse_VerseAtBeginning()
		{
			var block = new Block("p", 5);
			block.BlockElements.Add(new Verse("3"));
			block.BlockElements.Add(new ScriptText("He said, «Go!»"));
			var input = new List<Block> { block };
			Assert.AreEqual(1, input.Count);
			Assert.AreEqual("[3]He said, «Go!»", input[0].GetText(true));
			Assert.AreEqual(5, input[0].ChapterNumber);
			Assert.AreEqual(3, input[0].InitialStartVerseNumber);

			IList<Block> output = new QuoteParser(CharacterVerseData.Singleton, "LUK", input).Parse().ToList();
			Assert.AreEqual(2, output.Count);
			Assert.AreEqual("[3]He said, ", output[0].GetText(true));
			Assert.AreEqual(5, output[0].ChapterNumber);
			Assert.AreEqual(3, output[0].InitialStartVerseNumber);
			Assert.AreEqual("«Go!»", output[1].GetText(true));
			Assert.AreEqual(5, output[1].ChapterNumber);
			Assert.AreEqual(3, output[1].InitialStartVerseNumber);
		}

		[Test]
		public void Parse_MultipleVersesBeforeQuote()
		{
			var block = new Block("p", 5);
			block.BlockElements.Add(new Verse("3"));
			block.BlockElements.Add(new ScriptText("Matthew tried to learn to fish, but Peter was upset. "));
			block.BlockElements.Add(new Verse("4"));
			block.BlockElements.Add(new ScriptText("He said, «Go back to your tax booth!»"));
			var input = new List<Block> { block };
			Assert.AreEqual(1, input.Count);
			Assert.AreEqual("[3]Matthew tried to learn to fish, but Peter was upset. [4]He said, «Go back to your tax booth!»", input[0].GetText(true));
			Assert.AreEqual(5, input[0].ChapterNumber);
			Assert.AreEqual(3, input[0].InitialStartVerseNumber);

			IList<Block> output = new QuoteParser(CharacterVerseData.Singleton, "LUK", input).Parse().ToList();
			Assert.AreEqual(2, output.Count);
			Assert.AreEqual("[3]Matthew tried to learn to fish, but Peter was upset. [4]He said, ", output[0].GetText(true));
			Assert.AreEqual(5, output[0].ChapterNumber);
			Assert.AreEqual(3, output[0].InitialStartVerseNumber);
			Assert.AreEqual("«Go back to your tax booth!»", output[1].GetText(true));
			Assert.AreEqual(5, output[1].ChapterNumber);
			Assert.AreEqual(4, output[1].InitialStartVerseNumber);
		}

		[Test]
		public void Parse_VerseBeforeQuote()
		{
			var block = new Block("p", 6, 2);
			block.BlockElements.Add(new ScriptText("He said, "));
			block.BlockElements.Add(new Verse("3"));
			block.BlockElements.Add(new ScriptText("«Go!»"));
			var input = new List<Block> { block };
			Assert.AreEqual(1, input.Count);
			Assert.AreEqual("He said, [3]«Go!»", input[0].GetText(true));
			Assert.AreEqual(6, input[0].ChapterNumber);
			Assert.AreEqual(2, input[0].InitialStartVerseNumber);

			IList<Block> output = new QuoteParser(CharacterVerseData.Singleton, "LUK", input).Parse().ToList();
			Assert.AreEqual(2, output.Count);
			Assert.AreEqual("He said, ", output[0].GetText(false));
			Assert.AreEqual(6, output[0].ChapterNumber);
			Assert.AreEqual(2, output[0].InitialStartVerseNumber);
			Assert.AreEqual("«Go!»", output[1].GetText(false));
			Assert.AreEqual(6, output[1].ChapterNumber);
			Assert.AreEqual(3, output[1].InitialStartVerseNumber);

			Assert.AreEqual("He said, ", output[0].GetText(true));
			Assert.AreEqual("[3]«Go!»", output[1].GetText(true));
		}

		[Test]
		public void Parse_VerseAfterQuote()
		{
			var block = new Block("p");
			block.BlockElements.Add(new ScriptText("«Go!» "));
			block.BlockElements.Add(new Verse("3"));
			block.BlockElements.Add(new ScriptText("he said."));
			var input = new List<Block> { block };
			Assert.AreEqual(1, input.Count);
			Assert.AreEqual("«Go!» [3]he said.", input[0].GetText(true));

			IList<Block> output = new QuoteParser(CharacterVerseData.Singleton, "LUK", input).Parse().ToList();
			Assert.AreEqual(2, output.Count);
			Assert.AreEqual("«Go!» ", output[0].GetText(false));
			Assert.AreEqual("he said.", output[1].GetText(false));

			Assert.AreEqual("«Go!» ", output[0].GetText(true));
			Assert.AreEqual("[3]he said.", output[1].GetText(true));
		}

		[Test]
		public void Parse_VerseWithinQuote()
		{
			var block = new Block("p", 6, 2);
			block.BlockElements.Add(new ScriptText("He said, «Go "));
			block.BlockElements.Add(new Verse("3"));
			block.BlockElements.Add(new ScriptText("west!»"));
			var input = new List<Block> { block };
			Assert.AreEqual(1, input.Count);
			Assert.AreEqual("He said, «Go [3]west!»", input[0].GetText(true));
			Assert.AreEqual(6, input[0].ChapterNumber);
			Assert.AreEqual(2, input[0].InitialStartVerseNumber);

			IList<Block> output = new QuoteParser(CharacterVerseData.Singleton, "LUK", input).Parse().ToList();
			Assert.AreEqual(2, output.Count);
			Assert.AreEqual("He said, ", output[0].GetText(false));
			Assert.AreEqual(6, output[0].ChapterNumber);
			Assert.AreEqual(2, output[0].InitialStartVerseNumber);
			Assert.AreEqual("«Go west!»", output[1].GetText(false));
			Assert.AreEqual(6, output[1].ChapterNumber);
			Assert.AreEqual(2, output[1].InitialStartVerseNumber);

			Assert.AreEqual("He said, ", output[0].GetText(true));
			Assert.AreEqual("«Go [3]west!»", output[1].GetText(true));
		}

		[Test]
		public void Parse_SpaceStaysWithPriorBlock()
		{
			var block = new Block("p");
			block.BlockElements.Add(new ScriptText("He said, «Go!»"));
			var input = new List<Block> { block };
			IList<Block> output = new QuoteParser(CharacterVerseData.Singleton, "LUK", input).Parse().ToList();
			Assert.AreEqual(2, output.Count);
			Assert.AreEqual("He said, ", output[0].GetText(true));
			Assert.AreEqual("«Go!»", output[1].GetText(true));
		}
		[Test]
		public void Parse_PunctuationStaysWithPriorBlock()
		{
			var block = new Block("p");
			block.BlockElements.Add(new ScriptText("«Go»!! he said."));
			var input = new List<Block> { block };
			IList<Block> output = new QuoteParser(CharacterVerseData.Singleton, "LUK", input).Parse().ToList();
			Assert.AreEqual(2, output.Count);
			Assert.AreEqual("«Go»!! ", output[0].GetText(true));
			Assert.AreEqual("he said.", output[1].GetText(true));
		}
		[Test]
		public void Parse_PunctuationStaysWithPriorBlock_AtBlockEnd()
		{
			var block = new Block("p");
			block.BlockElements.Add(new ScriptText("He said, «Go»!"));
			var input = new List<Block> { block };
			IList<Block> output = new QuoteParser(CharacterVerseData.Singleton, "LUK", input).Parse().ToList();
			Assert.AreEqual(2, output.Count);
			Assert.AreEqual("He said, ", output[0].GetText(true));
			Assert.AreEqual("«Go»!", output[1].GetText(true));
		}

		[Test]
		public void Parse_UsingDifferentQuoteMarks()
		{
			var options = new QuoteSystem { StartQuoteMarker = "“", EndQuoteMarker = "”" };
			var block = new Block("p");
			block.BlockElements.Add(new ScriptText("“Go!” he said."));
			var input = new List<Block> { block };
			IList<Block> output = new QuoteParser(CharacterVerseData.Singleton, "MRK", input, options).Parse().ToList();
			Assert.AreEqual(2, output.Count);
			Assert.AreEqual("“Go!” ", output[0].GetText(true));
			Assert.AreEqual("he said.", output[1].GetText(true));
		}

		[Test]
		public void Parse_BreakOnFirstLevelQuoteOnly_HasTwoLevels()
		{
			var options = new QuoteSystem { StartQuoteMarker = "“", EndQuoteMarker = "”" };
			var block = new Block("p");
			block.BlockElements.Add(new ScriptText("He said, “She said, ‘Get lost.’”"));
			var input = new List<Block> { block };
			IList<Block> output = new QuoteParser(CharacterVerseData.Singleton, "MRK", input, options).Parse().ToList();
			Assert.AreEqual(2, output.Count);
			Assert.AreEqual("He said, ", output[0].GetText(true));
			Assert.AreEqual("“She said, ‘Get lost.’”", output[1].GetText(true));
		}

		[Test]
		public void Parse_BreakOnFirstLevelQuoteOnly_HasThreeLevels()
		{
			var options = new QuoteSystem { StartQuoteMarker = "“", EndQuoteMarker = "”" };
			var block = new Block("p");
			block.BlockElements.Add(new ScriptText("He said, “She said, ‘They said, “No way.”’”"));
			var input = new List<Block> { block };
			IList<Block> output = new QuoteParser(CharacterVerseData.Singleton, "MRK", input, options).Parse().ToList();
			Assert.AreEqual(2, output.Count);
			Assert.AreEqual("He said, ", output[0].GetText(true));
			Assert.AreEqual("“She said, ‘They said, “No way.”’”", output[1].GetText(true));
		}

		[Test]
		public void Parse_TitleIntrosChaptersAndExtraBiblicalMaterial_OnlyVerseTextGetsParsedForQuotes()
		{
			var options = new QuoteSystem { StartQuoteMarker = "“", EndQuoteMarker = "”" };
			var titleBlock = new Block("mt");
			titleBlock.BlockElements.Add(new ScriptText("Gospel of Mark"));
			titleBlock.SetStandardCharacter("MRK", CharacterVerseData.StandardCharacter.BookOrChapter);
			var introBlock1 = new Block("is");
			introBlock1.BlockElements.Add(new ScriptText("All about Mark"));
			var introBlock2 = new Block("ip");
			introBlock1.SetStandardCharacter("MRK", CharacterVerseData.StandardCharacter.Intro);
			introBlock2.BlockElements.Add(new ScriptText("Some people say, “Mark is way to short,” but I disagree."));
			introBlock2.SetStandardCharacter("MRK", CharacterVerseData.StandardCharacter.Intro);
			var chapterBlock = new Block("c");
			chapterBlock.BlockElements.Add(new ScriptText("Chapter 1"));
			chapterBlock.SetStandardCharacter("MRK", CharacterVerseData.StandardCharacter.BookOrChapter);
			var sectionHeadBlock = new Block("s");
			sectionHeadBlock.BlockElements.Add(new ScriptText("John tells everyone: “The Kingdom of Heaven is at hand”"));
			sectionHeadBlock.SetStandardCharacter("MRK", CharacterVerseData.StandardCharacter.ExtraBiblical);
			var paraBlock = new Block("p");
			paraBlock.BlockElements.Add(new Verse("1"));
			paraBlock.BlockElements.Add(new ScriptText("Jesus said, “Is that John?”"));
			var input = new List<Block> { titleBlock, introBlock1, introBlock2, chapterBlock, sectionHeadBlock, paraBlock };
			IList<Block> output = new QuoteParser(CharacterVerseData.Singleton, "MRK", input, options).Parse().ToList();
			Assert.AreEqual(7, output.Count);
			Assert.AreEqual("Gospel of Mark", output[0].GetText(true));
			Assert.IsTrue(output[0].CharacterIs("MRK", CharacterVerseData.StandardCharacter.BookOrChapter));
			Assert.AreEqual("All about Mark", output[1].GetText(true));
			Assert.IsTrue(output[1].CharacterIs("MRK", CharacterVerseData.StandardCharacter.Intro));
			Assert.AreEqual("Some people say, “Mark is way to short,” but I disagree.", output[2].GetText(true));
			Assert.IsTrue(output[2].CharacterIs("MRK", CharacterVerseData.StandardCharacter.Intro));
			Assert.AreEqual("Chapter 1", output[3].GetText(true));
			Assert.IsTrue(output[3].CharacterIs("MRK", CharacterVerseData.StandardCharacter.BookOrChapter));
			Assert.AreEqual("John tells everyone: “The Kingdom of Heaven is at hand”", output[4].GetText(true));
			Assert.IsTrue(output[4].CharacterIs("MRK", CharacterVerseData.StandardCharacter.ExtraBiblical));
			Assert.AreEqual("[1]Jesus said, ", output[5].GetText(true));
			Assert.IsTrue(output[5].CharacterIs("MRK", CharacterVerseData.StandardCharacter.Narrator));
			Assert.AreEqual("“Is that John?”", output[6].GetText(true));
			Assert.AreEqual(CharacterVerseData.UnknownCharacter, output[6].CharacterId);
		}

		[Test]
		public void Parse_IsParagraphStart()
		{
			var chapterBlock = new Block("c") { IsParagraphStart = true };
			chapterBlock.BlockElements.Add(new ScriptText("Chapter 1"));
			var block = new Block("p") { IsParagraphStart = true };
			block.BlockElements.Add(new ScriptText("He said, «Go»!"));
			var input = new List<Block> { chapterBlock, block };
			IList<Block> output = new QuoteParser(CharacterVerseData.Singleton, "LUK", input).Parse().ToList();
			Assert.AreEqual(3, output.Count);
			Assert.AreEqual("Chapter 1", output[0].GetText(true));
			Assert.AreEqual("He said, ", output[1].GetText(true));
			Assert.AreEqual("«Go»!", output[2].GetText(true));
			Assert.IsTrue(output[0].IsParagraphStart);
			Assert.IsTrue(output[1].IsParagraphStart);
			Assert.IsFalse(output[2].IsParagraphStart);
		}

		[Test]
		public void Parse_QuoteInNewParagraphWithinVerseBridge_NarratorAndOther()
		{
			var block1 = new Block("p", 17, 3, 4);
			block1.BlockElements.Add(new Verse("3-4"));
			block1.BlockElements.Add(new ScriptText("Then Peter said, "));
			var block2 = new Block("q1", 17, 3, 4);
			block2.BlockElements.Add(new ScriptText("«What verse is this?»"));
			var input = new List<Block> { block1, block2 };

			CharacterVerseData.TabDelimitedCharacterVerseData = Properties.Resources.TestCharacterVerse;
			IList<Block> output = new QuoteParser(CharacterVerseData.Singleton, "MAT", input).Parse().ToList();
			Assert.AreEqual(2, output.Count);
			Assert.AreEqual("narrator-MAT", output[0].CharacterId);
			Assert.AreEqual("Peter (Simon)", output[1].CharacterId);
		}

		[Test]
		public void Parse_QuoteInNewParagraphWithinVerseBridge_DifferentCharacters_MarksBothAsAmbiguous()
		{
			var block1 = new Block("p", 6, 7, 9);
			block1.BlockElements.Add(new Verse("7-9"));
			block1.BlockElements.Add(new ScriptText("Philip said, «Surely you can't be serious.»"));
			block1.BlockElements.Add(new ScriptText("Andrew said, «I am serious.»"));
			var block2 = new Block("q1", 6, 7, 9);
			block2.BlockElements.Add(new ScriptText("«And don't call me Shirley.»"));
			var input = new List<Block> { block1, block2 };

			CharacterVerseData.TabDelimitedCharacterVerseData = Properties.Resources.TestCharacterVerse;
			IList<Block> output = new QuoteParser(CharacterVerseData.Singleton, "JHN", input).Parse().ToList();
			Assert.AreEqual(5, output.Count);
			Assert.AreEqual("narrator-JHN", output[0].CharacterId);
			Assert.AreEqual("Ambiguous", output[1].CharacterId);
			Assert.AreEqual("narrator-JHN", output[2].CharacterId);
			Assert.AreEqual("Ambiguous", output[3].CharacterId);
			Assert.AreEqual("Ambiguous", output[4].CharacterId);
		}

		[Test]
		public void Parse_QuoteInNewParagraphWithinVerseBridge_SameCharacter_MarksBothAsCorrectCharacter()
		{
			var block1 = new Block("p", 1, 19, 20);
			block1.BlockElements.Add(new Verse("19-20"));
			block1.BlockElements.Add(new ScriptText("Peter said, «They don't call him the son of thunder for nothing.»"));
			var block2 = new Block("q1", 1, 19, 20);
			block2.BlockElements.Add(new ScriptText("«Oh, and his brother, too.»"));
			var input = new List<Block> { block1, block2 };

			CharacterVerseData.TabDelimitedCharacterVerseData = Properties.Resources.TestCharacterVerse;
			IList<Block> output = new QuoteParser(CharacterVerseData.Singleton, "ACT", input).Parse().ToList();
			Assert.AreEqual(3, output.Count);
			Assert.AreEqual("narrator-ACT", output[0].CharacterId);
			Assert.AreEqual("Peter (Simon)", output[1].CharacterId);
			Assert.AreEqual("Peter (Simon)", output[2].CharacterId);
		}
	}
}
