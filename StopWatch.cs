using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace StopWatch
{
	// Please read https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Modding-Guide#mod-skeleton-contents for more information about the various files in a mod.
	public class StopWatch : Mod
	{
		public Stopwatch drawWatch = new();
		public Stopwatch updateWatch = new();

		public List<long> drawTimes = new();
		public List<long> updateTimes = new();

		public List<long> drawTimes2 = new();
		public List<long> updateTimes2 = new();
		public List<long> memoryTotals = new();

		public override void Load()
		{
			On_Main.DoDraw += TrackDraw;
			On_Main.DoUpdate += TrackUpdate;
		}

		private void TrackDraw(On_Main.orig_DoDraw orig, Main self, GameTime gameTime)
		{
			drawWatch.Reset();
			drawWatch.Start();
			orig(self, gameTime);
			drawWatch.Stop();

			drawTimes.Add(drawWatch.ElapsedTicks);

			DrawGraphs(Main.spriteBatch);
		}

		private void TrackUpdate(On_Main.orig_DoUpdate orig, Main self, ref GameTime gameTime)
		{
			updateWatch.Reset();
			updateWatch.Start();
			orig(self, ref gameTime);
			updateWatch.Stop();

			updateTimes.Add(updateWatch.ElapsedTicks);

			if (updateTimes.Count >= 60)
			{
				long avgDraw = drawTimes.Sum() / drawTimes.Count;
				long avgUpdate = updateTimes.Sum() / updateTimes.Count;

				drawTimes2.Add(avgDraw);
				updateTimes2.Add(avgUpdate);
				memoryTotals.Add(Process.GetCurrentProcess().PrivateMemorySize64 / 1000);

				drawTimes.Clear();
				updateTimes.Clear();

				if (drawTimes2.Count > 60)
					drawTimes2.RemoveRange(0, drawTimes2.Count - 60);

				if (updateTimes2.Count > 60)
					updateTimes2.RemoveRange(0, updateTimes2.Count - 60);

				if (memoryTotals.Count > 60)
					memoryTotals.RemoveRange(0, memoryTotals.Count - 60);
			}
		}	

		private void DrawGraphs(SpriteBatch spriteBatch)
		{
			int baseX = 20;

			if (!Main.gameMenu && !Main.mapFullscreen)
				baseX = Main.screenWidth - 600;

			spriteBatch.Begin();
			DrawGraph(spriteBatch, updateTimes2, new Vector2(baseX, 40), Color.Lime, "Update Time");
			DrawGraph(spriteBatch, drawTimes2, new Vector2(baseX, 230), Color.Cyan, "Render Time");
			DrawGraph(spriteBatch, memoryTotals, new Vector2(baseX, 420), Color.Yellow, "Memory (Kb)");
			spriteBatch.End();
		}

		private void DrawGraph(SpriteBatch spriteBatch, List<long> values, Vector2 pos, Color lineColor, string label)
		{
			var tex = Terraria.GameContent.TextureAssets.MagicPixel.Value;

			int width = 260;
			int height = 160;

			var backRect = new Rectangle((int)pos.X, (int)pos.Y,width, height);
			spriteBatch.Draw(tex, backRect, Color.Black * 0.25f);

			var labelRect = new Rectangle((int)pos.X, (int)pos.Y - 20, width, 20);
			spriteBatch.Draw(tex, labelRect, Color.Black * 0.35f);

			if (values.Count <= 0)
				return;

			var maxVal = values.Max();
			Vector2 last = new Vector2(0, (1 - values[0] / (float)maxVal) * height);
			for (int k = 1; k < values.Count; k++)
			{
				float x = k / (float)values.Count * width;
				float y = (1 - values[k] / (float)maxVal) * height;
				Vector2 next = new Vector2(x, y);

				var target = new Rectangle((int)last.X, (int)last.Y, (int)Vector2.Distance(last, next), 1);
				target.Offset(backRect.TopLeft().ToPoint());

				var rot = last.DirectionTo(next).ToRotation();

				spriteBatch.Draw(tex, target, null, lineColor, rot, Vector2.Zero, 0, 0);
				last = next;
			}

			var avg = values.Sum() / values.Count;
			spriteBatch.Draw(tex, new Rectangle((int)pos.X, (int)(pos.Y + (1 - avg / (float)maxVal) * height), width, 1), lineColor * 0.25f);

			Utils.DrawBorderString(spriteBatch, $"{label}: {values.Last()}", pos + new Vector2(6, -16), lineColor, 0.65f);
			Utils.DrawBorderString(spriteBatch, $"Avg: {avg}", pos + new Vector2(width / 2 + 40, -16), lineColor, 0.65f);

			Utils.DrawBorderString(spriteBatch, $"{maxVal}", pos + new Vector2(width - Terraria.GameContent.FontAssets.ItemStack.Value.MeasureString($"{maxVal}").X * 0.65f - 8, 4), lineColor, 0.65f);
			Utils.DrawBorderString(spriteBatch, "0", pos + new Vector2(width - 12, height - 16), lineColor, 0.65f);
		}
	}
}
