﻿using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibGGPK3.Records {
	/// <summary>
	/// A free record represents space in the pack file that has been marked as deleted. It's much cheaper to just
	/// mark areas as free and append data to the end of the pack file than it is to rebuild the entire pack file just
	/// to remove a piece of data.
	/// </summary>
	public class FreeRecord : BaseRecord {
		public static readonly byte[] Tag = Encoding.ASCII.GetBytes("FREE");

		/// <summary>
		/// Offset of next FreeRecord
		/// </summary>
		protected internal long NextFreeOffset;

		protected internal FreeRecord(int length, GGPK ggpk) : base(length, ggpk) {
			Offset = ggpk.FileStream.Position - 8;
			NextFreeOffset = ggpk.FileStream.ReadInt64();
			ggpk.FileStream.Seek(Length - 16, SeekOrigin.Current);
		}

		protected internal FreeRecord(int length, GGPK ggpk, long nextFreeOffset, long recordBegin) : base(length, ggpk) {
			Offset = recordBegin;
			NextFreeOffset = nextFreeOffset;
		}

		protected internal override void Write(Stream? writeTo = null) {
			writeTo ??= Ggpk.FileStream;
			Offset = writeTo.Position;
			writeTo.Write(Length);
			writeTo.Write(Tag);
			writeTo.Write(NextFreeOffset);
			writeTo.Seek(Length - 16, SeekOrigin.Current);
		}

		/// <summary>
		/// Remove this FreeRecord from the Linked FreeRecord List
		/// </summary>
		/// <param name="node">Node in <see cref="GGPK.FreeRecords"/> to remove</param>
		public virtual void Remove(LinkedListNode<FreeRecord>? node = null) {
			var s = Ggpk.FileStream;
			node ??= Ggpk.FreeRecords.Find(this);
			if (node is null)
				return;
			var previous = node.Previous?.Value;
			var next = node.Next?.Value;
			if (next == null)
				if (previous == null) {
					Ggpk.GgpkRecord.FirstFreeRecordOffset = 0;
					s.Seek(Ggpk.GgpkRecord.Offset + 20, SeekOrigin.Begin);
					s.Write((long)0);
				} else {
					previous.NextFreeOffset = 0;
					s.Seek(previous.Offset + 8, SeekOrigin.Begin);
					s.Write((long)0);
				}
			else
				if (previous == null) {
				Ggpk.GgpkRecord.FirstFreeRecordOffset = next.Offset;
				s.Seek(Ggpk.GgpkRecord.Offset + 20, SeekOrigin.Begin);
				s.Write(next.Offset);
			} else {
				previous.NextFreeOffset = next.Offset;
				s.Seek(previous.Offset + 8, SeekOrigin.Begin);
				s.Write(next.Offset);
			}
			Ggpk.FreeRecords.Remove(node);
		}
	}
}