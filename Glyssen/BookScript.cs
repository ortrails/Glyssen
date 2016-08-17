﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Glyssen.Character;
using Glyssen.Dialogs;
using Glyssen.Quote;
using SIL.ObjectModel;
using SIL.Scripture;
using SIL.Unicode;
using ScrVers = Paratext.ScrVers;

namespace Glyssen
{
	[XmlRoot("book")]
	public class BookScript : IScrBook
	{
		public const int kSplitAtEndOfVerse = -999;
		private Dictionary<int, int> m_chapterStartBlockIndices;
		private int m_blockCount;
		private List<Block> m_blocks;
		private List<List<Block>> m_unappliedSplitBlocks = new List<List<Block>>();
		private ScrStylesheetAdapter m_styleSheet;

		public BookScript()
		{
			// Needed for deserialization
		}

		public BookScript(string bookId, IEnumerable<Block> blocks)
		{
			BookId = bookId;
			Blocks = blocks.ToList();
		}

		[XmlAttribute("id")]
		public string BookId { get; set; }

		[XmlAttribute("singlevoice")]
		public bool SingleVoice { get; set; }

		[XmlAttribute("pageheader")]
		public string PageHeader { get; set; }

		[XmlAttribute("maintitle")]
		public string MainTitle { get; set; }

		/// <summary>
		/// Don't use this getter in production code. It is intended ONLY for use by the XML serializer!
		/// This is to prevent accidentally leaking the actual list and risking modification by calling code.
		/// </summary>
		[XmlElement(ElementName = "block")]
		public List<Block> Blocks
		{
			get { return m_blocks; }
			set
			{
				m_blocks = value;
				m_chapterStartBlockIndices = new Dictionary<int, int>();
				m_blockCount = m_blocks.Count;
			}
		}

		public Block this[int i]
		{
			get { return m_blocks[i]; }
		}

		public bool HasScriptBlocks
		{
			get { return m_blocks.Any(); }
		}

		/// <summary>
		/// Don't use this getter in production code. It is intended ONLY for use by the XML serializer!
		/// This is to prevent accidentally leaking the actual list and risking modification by calling code.
		/// </summary>
		[XmlArray("UnappliedSplits")]
		[XmlArrayItem("Split")]
		public List<List<Block>> UnappliedBlockSplits_DoNotUse
		{
			get { return m_unappliedSplitBlocks; }
			set { m_unappliedSplitBlocks = value; }
		}

		public System.Collections.Generic.IReadOnlyList<IEnumerable<Block>> UnappliedSplits
		{
			get { return m_unappliedSplitBlocks; }
		}

		public BookScript Clone(bool join)
		{
			BookScript newBook = (BookScript) MemberwiseClone();
			newBook.Blocks = new List<Block>(GetScriptBlocks(join).Select(b => b.Clone()));
			newBook.m_unappliedSplitBlocks = new List<List<Block>>(m_unappliedSplitBlocks.Select(l => l.Select(b => b.Clone()).ToList()));
			return newBook;
		}

		public System.Collections.Generic.IReadOnlyList<Block> GetScriptBlocks(bool join = false)
		{
			EnsureBlockCount();

			if (!join || m_blockCount == 0)
				return m_blocks;

			var list = new List<Block>(m_blockCount) {m_blocks[0]};

			if (m_styleSheet == null)
				m_styleSheet = SfmLoader.GetUsfmStylesheet();

			for (var i = 1; i < m_blockCount; i++)
			{
				var block = m_blocks[i];
				var prevBlock = list.Last();
				var style = (StyleAdapter)m_styleSheet.GetStyle(block.StyleTag);

				if (!block.IsParagraphStart
					|| (style.IsPoetic && !CharacterUtils.EndsWithSentenceFinalPunctuation(prevBlock.GetText(false)))
					)
				{
					
					if (block.CharacterIdInScript == prevBlock.CharacterIdInScript && (block.Delivery ?? string.Empty) == (prevBlock.Delivery ?? string.Empty))
					{
						list[list.Count - 1] = CombineBlockWithPreviousBlock(block, prevBlock);
						continue;
					}
				}

				list.Add(block);
			}
			return list;
		}

		private Block CombineBlockWithPreviousBlock(Block block, Block prevBlock)
		{
			var newBlock = prevBlock.Clone();
			var skip = 0;
			if (prevBlock.BlockElements.Last() is ScriptText && block.BlockElements.First() is ScriptText)
			{
				var lastScriptText = (ScriptText)newBlock.BlockElements.Last();
				lastScriptText.Content += ((ScriptText)block.BlockElements.First()).Content;
				skip = 1;
			}
			foreach (var blockElement in block.BlockElements.Skip(skip))
				newBlock.BlockElements.Add(blockElement.Clone());
			newBlock.UserConfirmed &= block.UserConfirmed;

			return newBlock;
		}

		public string GetVerseText(int chapter, int verse)
		{
			var iFirstBlockToExamine = GetIndexOfFirstBlockForVerse(chapter, verse);
			if (iFirstBlockToExamine < 0)
				return String.Empty;
			StringBuilder bldr = new StringBuilder();
			bool foundVerseStart = false;
			for (int index = iFirstBlockToExamine; index < m_blockCount; index++)
			{
				var block = m_blocks[index];
				if (block.ChapterNumber != chapter)
					break;
				foreach (var element in block.BlockElements)
				{
					Verse verseElement = element as Verse;
					if (verseElement != null)
					{
						var endVerse = verseElement.EndVerse;
						if (verse > endVerse)
							continue;
						if (verse >= verseElement.StartVerse && verse <= endVerse)
							foundVerseStart = true;
						else if (foundVerseStart)
							return bldr.ToString();
					}
					else if (foundVerseStart)
					{
						if (index > iFirstBlockToExamine)
							bldr.Append(Environment.NewLine);
						var textElement = (ScriptText) element;
						bldr.Append(textElement.Content);
					}
				}
			}
			return bldr.ToString();
		}

		public IEnumerable<Block> GetBlocksForVerse(int chapter, int verse)
		{
			var iFirstBlockToExamine = GetIndexOfFirstBlockForVerse(chapter, verse);
			if (iFirstBlockToExamine >= 0)
			{
				for (int index = iFirstBlockToExamine; index < m_blockCount; index++)
				{
					var block = m_blocks[index];
					if (block.ChapterNumber != chapter)
						break;
					if (block.BlockElements.OfType<Verse>().Any(v => verse >= v.StartVerse && verse <= v.EndVerse))
						yield return block;
					else if (block.InitialStartVerseNumber == verse || (block.InitialEndVerseNumber != 0 && block.InitialStartVerseNumber < verse && block.InitialEndVerseNumber >= verse))
						yield return block;
					else
						break;
				}
			}
		}

		public Block GetFirstBlockForVerse(int chapter, int verse)
		{
			var iFirstBlockToExamine = GetIndexOfFirstBlockForVerse(chapter, verse);
			if (iFirstBlockToExamine < 0)
				return null;

			var block = m_blocks[iFirstBlockToExamine];
			foreach (var verseElement in block.BlockElements.OfType<Verse>().SkipWhile(v => verse > v.EndVerse))
			{
				if (verse >= verseElement.StartVerse && verse <= verseElement.EndVerse)
					return block;
				break;
			}
			return null;
		}

		private int GetIndexOfFirstBlockForVerse(int chapter, int verse)
		{
			EnsureBlockCount();
			if (m_blockCount == 0)
				return -1;
			int chapterStartBlock;
			bool chapterStartFound = m_chapterStartBlockIndices.TryGetValue(chapter, out chapterStartBlock);

			if (!chapterStartFound && m_chapterStartBlockIndices.Any())
			{
				int fallBackChapter = chapter;
				while (fallBackChapter > 1)
				{
					if (m_chapterStartBlockIndices.TryGetValue(--fallBackChapter, out chapterStartBlock))
						break;
				}
			}
			int iFirstBlockToExamine = -1;
			for (int index = chapterStartBlock; index < m_blockCount; index++)
			{
				var block = m_blocks[index];
				if (block.ChapterNumber < chapter)
					continue;
				if (block.ChapterNumber > chapter)
				{
					if (chapterStartFound)
						iFirstBlockToExamine = index - 1;
					break;
				}
				if (!chapterStartFound)
				{
					m_chapterStartBlockIndices[chapter] = index;
					chapterStartFound = true;
				}
				if (block.InitialStartVerseNumber < verse && block.InitialEndVerseNumber < verse)
					continue;
				iFirstBlockToExamine = index;
				if (block.InitialStartVerseNumber > verse ||
					(iFirstBlockToExamine > 0 && !(block.BlockElements.First() is Verse) && m_blocks[iFirstBlockToExamine - 1].LastVerse == verse))
				{
					iFirstBlockToExamine--;
				}
				break;
			}

			if (iFirstBlockToExamine < 0)
			{
				if (!chapterStartFound)
					return -1;
				iFirstBlockToExamine = m_blockCount - 1;
			}
			return iFirstBlockToExamine;
		}

		private int GetIndexOfFirstBlockThatStartsWithVerse(int chapter, int verse)
		{
			var i = GetIndexOfFirstBlockForVerse(chapter, verse);
			while (m_blocks[i].InitialStartVerseNumber < verse)
				i++;
			return i;
		}

		/// <summary>
		/// Admittedly, this isn't the best way to prevent changes, but it is easier than doing custom
		/// serialization or trying to encapsulate the class to allow XML serialization but not expose
		/// the Blocks getter.
		/// </summary>
		private void EnsureBlockCount()
		{
			if (m_blockCount == 0)
				m_blockCount = m_blocks.Count;
			else if (m_blockCount != m_blocks.Count)
				throw new InvalidOperationException(
					"Blocks collection changed. Blocks getter should not be used to add or remove blocks to the list. Use setter instead.");
		}

		public void ApplyUserDecisions(BookScript sourceBookScript, ScrVers versification = null)
		{
			foreach (var sourceUnappliedSplit in sourceBookScript.UnappliedSplits)
			{
				List<Block> targetUnappliedSplit = sourceUnappliedSplit.Select(splitPart => splitPart.Clone()).ToList();
				m_unappliedSplitBlocks.Add(targetUnappliedSplit);
			}
			ApplyUserSplits(sourceBookScript);
			ApplyUserAssignments(sourceBookScript, versification);
			CleanUpMultiBlockQuotes(versification);
		}

		private void ApplyUserAssignments(BookScript sourceBookScript, ScrVers versification)
		{
			var comparer = new BlockElementContentsComparer();
			int iTarget = 0;
			var bookNum = BCVRef.BookToNumber(sourceBookScript.BookId);
			foreach (var sourceBlock in sourceBookScript.m_blocks.Where(b => b.UserConfirmed))
			{
				if (iTarget == m_blocks.Count)
					return;

				if (m_blocks[iTarget].ChapterNumber < sourceBlock.ChapterNumber)
					iTarget = GetIndexOfFirstBlockForVerse(sourceBlock.ChapterNumber, sourceBlock.InitialStartVerseNumber);
				else
				{
					while (m_blocks[iTarget].InitialStartVerseNumber < sourceBlock.InitialStartVerseNumber)
					{
						iTarget++;
						if (iTarget == m_blocks.Count)
							return;
					}
				}
				do
				{
					if (m_blocks[iTarget].StyleTag == sourceBlock.StyleTag &&
						m_blocks[iTarget].IsParagraphStart == sourceBlock.IsParagraphStart &&
						m_blocks[iTarget].BlockElements.SequenceEqual(sourceBlock.BlockElements, comparer))
					{
						if (sourceBlock.CharacterIdOverrideForScript == null)
							m_blocks[iTarget].SetCharacterAndCharacterIdInScript(sourceBlock.CharacterId, bookNum, versification);
						else
						{
							m_blocks[iTarget].CharacterId = sourceBlock.CharacterId;
							m_blocks[iTarget].CharacterIdOverrideForScript = sourceBlock.CharacterIdOverrideForScript;
						}
						m_blocks[iTarget].Delivery = sourceBlock.Delivery;
						m_blocks[iTarget].UserConfirmed = true;
						iTarget++;
						if (iTarget == m_blocks.Count)
							return;
						break;
					}
				} while (++iTarget < m_blocks.Count &&
					m_blocks[iTarget].ChapterNumber == sourceBlock.ChapterNumber &&
					m_blocks[iTarget].InitialStartVerseNumber == sourceBlock.InitialStartVerseNumber);
			}
		}

		private void ApplyUserSplits(BookScript sourceBookScript)
		{
			int splitId = Block.kNotSplit;
			List<Block> split = null;
			foreach (var block in sourceBookScript.Blocks.Where(b => b.SplitId != Block.kNotSplit))
			{
				if (block.SplitId != splitId)
				{
					if (split != null)
						m_unappliedSplitBlocks.Add(split);
					split = new List<Block>();
					splitId = block.SplitId;
				}
				split.Add(block);
			}
			if (split != null)
				m_unappliedSplitBlocks.Add(split);

			var comparer = new SplitBlockComparer();

			for (int index = 0; index < m_unappliedSplitBlocks.Count; index++)
			{
				var unappliedSplit = m_unappliedSplitBlocks[index];
				var firstBlockOfSplit = unappliedSplit.First();
				var i = GetIndexOfFirstBlockThatStartsWithVerse(firstBlockOfSplit.ChapterNumber, firstBlockOfSplit.InitialStartVerseNumber);
				var iFirstMatchingBlock = i;
				var iUnapplied = 0;
				bool blocksMatch;
				do
				{
					var splitBlock = unappliedSplit[iUnapplied];
					var parsedBlock = m_blocks[i++];
					blocksMatch = comparer.Equals(splitBlock, parsedBlock);
					if (iUnapplied > 0 || blocksMatch)
					{
						if (!blocksMatch)
							break;
						if (iUnapplied == 0)
							iFirstMatchingBlock = i;
						iUnapplied++;
					}
				} while (i < m_blocks.Count && iUnapplied < unappliedSplit.Count);
				if (blocksMatch)
				{
					m_unappliedSplitBlocks.RemoveAt(index--);
				}
				else
				{
					var combinedBlock = CombineBlocks(unappliedSplit);
					for (int iBlock = iFirstMatchingBlock; iBlock < m_blocks.Count && m_blocks[iBlock].InitialStartVerseNumber == combinedBlock.InitialStartVerseNumber; iBlock++)
					{
						if (comparer.Equals(combinedBlock, m_blocks[iBlock]))
						{
							i = iBlock;
							for (iUnapplied = 1; iUnapplied < unappliedSplit.Count; iUnapplied++)
							{
								var elementsOfBlockPrecedingSplit = unappliedSplit[iUnapplied - 1].BlockElements;
								var textElementAtEndOfBlockPrecedingSplit = elementsOfBlockPrecedingSplit.Last() as ScriptText;
								int offset = textElementAtEndOfBlockPrecedingSplit != null ? textElementAtEndOfBlockPrecedingSplit.Content.Length : 0;
								string verse;
								if (unappliedSplit[iUnapplied].BlockElements.First() is Verse)
								{
									var lastVerseInPrecedingBlock = elementsOfBlockPrecedingSplit.OfType<Verse>().LastOrDefault();
									if (lastVerseInPrecedingBlock != null)
										verse = lastVerseInPrecedingBlock.Number;
									else
										verse = m_blocks[i].InitialVerseNumberOrBridge;
								}
								else
								{
									verse = unappliedSplit[iUnapplied].InitialVerseNumberOrBridge;
								}
								SplitBlock(m_blocks[i++], verse, offset);
							}
							m_unappliedSplitBlocks.RemoveAt(index--);
							break;
						}
					}
				}
			}
		}

		public void CleanUpMultiBlockQuotes(ScrVers versification)
		{
			var model = new BlockNavigatorViewModel(new ReadOnlyList<BookScript>(new[] { this }), versification);
			foreach (IEnumerable<Block> multiBlock in GetScriptBlocks()
				.Where(b => b.MultiBlockQuote == MultiBlockQuote.Start)
				.Select(block => model.GetAllBlocksWhichContinueTheQuoteStartedByBlock(block)))
			{
				ProcessAssignmentForMultiBlockQuote(BCVRef.BookToNumber(BookId), multiBlock.ToList(), versification);
			}
		}

		private Block CombineBlocks(List<Block> blocks)
		{
			Block combinedBlock = blocks.First().Clone();
			for (int i = 1; i < blocks.Count; i++)
			{
				int skip = 0;
				if ((combinedBlock.BlockElements.Last() is ScriptText) && (blocks[i].BlockElements.First() is ScriptText))
				{
					((ScriptText)combinedBlock.BlockElements.Last()).Content += ((ScriptText)blocks[i].BlockElements.First()).Content;
					skip = 1;
				}
				foreach (var blockElement in blocks[i].BlockElements.Skip(skip))
					combinedBlock.BlockElements.Add(blockElement.Clone());
			}
			return combinedBlock;
		}

		public Block UnsplitBlocks(List<Block> blocks)
		{
			var insertHere = m_blocks.IndexOf(blocks[0]);
			var userConfirmed = false;

			var unsplitBlock = CombineBlocks(blocks);

			// remove the old blocks
			for (var i = blocks.Count - 1; i > -1; i--)
			{
				if (blocks[i].UserConfirmed)
					userConfirmed = true;
				m_blocks.Remove(blocks[i]);
				m_blockCount--;
			}
			
			// insert the new block
			unsplitBlock.UserConfirmed = userConfirmed;
			m_blocks.Insert(insertHere, unsplitBlock);
			m_blockCount++;

			// are there still multiple blocks with this split id?
			var splitBlocks = m_blocks.Where(b => b.SplitId == unsplitBlock.SplitId);
			if (splitBlocks.Count() == 1)
			{
				// if not, clear the split id
				unsplitBlock.SplitId = -1;
			}

			return unsplitBlock;
		}

		public Block SplitBlock(Block blockToSplit, string verseToSplit, int characterOffsetToSplit, bool userSplit = true,
			string characterId = null, ScrVers versification = null)
		{
			var iBlock = m_blocks.IndexOf(blockToSplit);

			if (iBlock < 0)
				throw new ArgumentException(@"Block not found in the list for " + BookId, "blockToSplit");

			int splitId;
			if (blockToSplit.SplitId != Block.kNotSplit)
				splitId = blockToSplit.SplitId;
			else
				splitId = m_blocks.Max(b => b.SplitId) + 1;

			if (verseToSplit == null && characterOffsetToSplit == 0)
			{
				SplitBeforeBlock(iBlock, splitId);
				return blockToSplit;
			}

			Block newBlock = blockToSplit.SplitBlock(verseToSplit, characterOffsetToSplit);

			if (newBlock == null)
			{
				SplitBeforeBlock(iBlock + 1, splitId);
				return m_blocks[iBlock + 1];
			}

			m_blocks.Insert(iBlock + 1, newBlock);
			var chapterNumbersToIncrement = m_chapterStartBlockIndices.Keys.Where(chapterNum => chapterNum > blockToSplit.ChapterNumber).ToList();
			foreach (var chapterNum in chapterNumbersToIncrement)
				m_chapterStartBlockIndices[chapterNum]++;

			m_blockCount++;

			if (userSplit)
			{
				newBlock.Delivery = null;
				if (string.IsNullOrEmpty(characterId))
				{
					newBlock.CharacterId = CharacterVerseData.kUnknownCharacter;
					newBlock.CharacterIdOverrideForScript = null;
					newBlock.UserConfirmed = false;
				}
				else
				{
					if (versification == null)
						throw new ArgumentNullException("versification");
					newBlock.SetCharacterAndCharacterIdInScript(characterId, BCVRef.BookToNumber(BookId), versification);
					newBlock.UserConfirmed = true;
				}

				if (blockToSplit.MultiBlockQuote == MultiBlockQuote.Start)
				{
					blockToSplit.MultiBlockQuote = MultiBlockQuote.None;
					newBlock.MultiBlockQuote = MultiBlockQuote.Start;
				}
				else if ((blockToSplit.MultiBlockQuote == MultiBlockQuote.Continuation || blockToSplit.MultiBlockQuote == MultiBlockQuote.ChangeOfDelivery) &&
					iBlock < m_blockCount - 2 &&
					(m_blocks[iBlock + 2].MultiBlockQuote == MultiBlockQuote.Continuation || m_blocks[iBlock + 2].MultiBlockQuote == MultiBlockQuote.ChangeOfDelivery))
				{
					newBlock.MultiBlockQuote = MultiBlockQuote.Start;
				}

				blockToSplit.SplitId = newBlock.SplitId = splitId;
			}
			else if (blockToSplit.MultiBlockQuote != MultiBlockQuote.None)
				newBlock.MultiBlockQuote = MultiBlockQuote.Continuation;
			//TODO handle splitId already exists but userSplit == false

			return newBlock;
		}

		public bool TrySplitBlockAtEndOfVerse(Block vernBlock, int verseNum)
		{
			var firstVerseElement = vernBlock.BlockElements.OfType<Verse>().FirstOrDefault();
			if (firstVerseElement == null)
				return false;
			var blockBeginsWithVerse = vernBlock.BlockElements.First() is Verse;
			var verseString = verseNum.ToString();

			if (vernBlock.InitialEndVerseNumber == verseNum)
			{
				verseString = vernBlock.InitialVerseNumberOrBridge;
				if (firstVerseElement.Number != verseString && blockBeginsWithVerse)
				{
					var secondPartOfVerse = vernBlock.BlockElements.Skip(2).OfType<Verse>().FirstOrDefault();
					if (secondPartOfVerse == null)
						return false;
					verseString = secondPartOfVerse.Number;
				}
			}
			else if (blockBeginsWithVerse ||
				!(vernBlock.InitialEndVerseNumber == 0 && vernBlock.InitialStartVerseNumber == verseNum))
			{
				foreach (var verse in vernBlock.BlockElements.OfType<Verse>())
				{
					if (verse.Number == verseString)
						break;
					if (verse.EndVerse == verseNum)
					{
						verseString = verse.Number;
						break;
					}
					if (verse.StartVerse >= verseNum)
						return false;
				}
			}
			try
			{
				var newBlock = SplitBlock(vernBlock, verseString, kSplitAtEndOfVerse, false);
				if (vernBlock.MatchesReferenceText)
				{
					try
					{
						// REVIEW: Should this be First or Single, or do we need to possibly handle the case of a sequence?
						newBlock.SetMatchedReferenceBlock(vernBlock.ReferenceBlocks.First().SplitBlock(verseString, kSplitAtEndOfVerse));
					}
					catch (ArgumentException)
					{
						// TODO: Handle English Reference block with different verse number from primary reference block
					}
				}
			}
			catch (ArgumentException)
			{
				return false;
			}

			return true;
		}

		private void SplitBeforeBlock(int indexOfBlockToSplit, int splitId)
		{
			if (indexOfBlockToSplit == 0 || m_blocks[indexOfBlockToSplit].MultiBlockQuote == MultiBlockQuote.None || m_blocks[indexOfBlockToSplit - 1].MultiBlockQuote == MultiBlockQuote.None)
				throw new ArgumentException("Split allowed only between blocks that are part of a multi-block quote");

			if (m_blocks[indexOfBlockToSplit - 1].MultiBlockQuote == MultiBlockQuote.Start)
				m_blocks[indexOfBlockToSplit - 1].MultiBlockQuote = MultiBlockQuote.None;

			if (indexOfBlockToSplit < m_blockCount - 1 && m_blocks[indexOfBlockToSplit + 1].MultiBlockQuote == MultiBlockQuote.Continuation)
				m_blocks[indexOfBlockToSplit].MultiBlockQuote = MultiBlockQuote.Start;
			else
				m_blocks[indexOfBlockToSplit].MultiBlockQuote = MultiBlockQuote.None;

			m_blocks[indexOfBlockToSplit - 1].SplitId = m_blocks[indexOfBlockToSplit].SplitId = splitId;
		}

		public void ClearUnappliedSplits()
		{
			m_unappliedSplitBlocks.Clear();
		}

		public static void ProcessAssignmentForMultiBlockQuote(int bookNum, List<Block> multiBlockQuote, ScrVers versification)
		{
			var uniqueCharacters = multiBlockQuote.Select(b => b.CharacterId).Distinct().ToList();
			int numUniqueCharacters = uniqueCharacters.Count;
			var uniqueCharacterDeliveries = multiBlockQuote.Select(b => new QuoteParser.CharacterDelivery(b.CharacterId, b.Delivery)).Distinct(QuoteParser.CharacterDelivery.CharacterDeliveryComparer).ToList();
			int numUniqueCharacterDeliveries = uniqueCharacterDeliveries.Count;
			if (numUniqueCharacterDeliveries > 1)
			{
				var unclearCharacters = new[] { CharacterVerseData.kAmbiguousCharacter, CharacterVerseData.kUnknownCharacter };
				if (numUniqueCharacters > unclearCharacters.Count(uniqueCharacters.Contains) + 1)
				{
					// More than one real character. Set to Ambiguous.
					SetCharacterAndDeliveryForMultipleBlocks(bookNum, multiBlockQuote, CharacterVerseData.kAmbiguousCharacter, null, versification);
				}
				else if (numUniqueCharacters == 2 && unclearCharacters.All(uniqueCharacters.Contains))
				{
					// Only values are Ambiguous and Unique. Set to Ambiguous.
					SetCharacterAndDeliveryForMultipleBlocks(bookNum, multiBlockQuote, CharacterVerseData.kAmbiguousCharacter, null, versification);
				}
				else if (numUniqueCharacterDeliveries > numUniqueCharacters)
				{
					// Multiple deliveries for the same character
					string delivery = "";
					bool first = true;
					foreach (Block block in multiBlockQuote)
					{
						if (first)
							first = false;
						else if (block.Delivery != delivery)
							block.MultiBlockQuote = MultiBlockQuote.ChangeOfDelivery;
						delivery = block.Delivery;
					}
				}
				else
				{
					// Only one real character (and delivery). Set to that character (and delivery).
					var realCharacter = uniqueCharacterDeliveries.Single(c => c.Character != CharacterVerseData.kAmbiguousCharacter && c.Character != CharacterVerseData.kUnknownCharacter);
					SetCharacterAndDeliveryForMultipleBlocks(bookNum, multiBlockQuote, realCharacter.Character, realCharacter.Delivery, versification);
				}
			}
		}

		private static void SetCharacterAndDeliveryForMultipleBlocks(int bookNum, IEnumerable<Block> blocks, string character, string delivery, ScrVers versification)
		{
			foreach (Block block in blocks)
			{
				block.SetCharacterAndCharacterIdInScript(character, bookNum, versification);
				block.Delivery = delivery;

				if (character == CharacterVerseData.kAmbiguousCharacter || character == CharacterVerseData.kUnknownCharacter)
					block.UserConfirmed = false;
			}
		}
	}
}
