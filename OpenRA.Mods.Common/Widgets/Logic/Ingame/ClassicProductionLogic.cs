#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ClassicProductionLogic : ChromeLogic
	{
		readonly ProductionPaletteWidget palette;
		readonly World world;

		readonly ProductionQueue[] allQueues;

		static void SetupProductionGroupButton(World world, ProductionPaletteWidget palette, ProductionTypeButtonWidget button)
		{
			if (button == null)
				return;

			// Classic production queues are initialized at game start, and then never change.
			var allQueues = world.GetAllProductionQueues()
				.Where(q => (q.Info.Group ?? q.Info.Type) == button.ProductionGroup)
				.ToArray();

			Action<bool> selectTab = reverse =>
			{
				palette.CurrentQueue = allQueues.FirstOrDefault(q => q.Enabled);

				// When a tab is selected, scroll to the top because the current row position may be invalid for the new tab
				palette.ScrollToTop();

				// Attempt to pick up a completed building (if there is one) so it can be placed
				palette.PickUpCompletedBuilding();
			};

			button.IsDisabled = () => !allQueues.Any(q => q.BuildableItems().Any());
			button.OnMouseUp = mi => selectTab(mi.Modifiers.HasModifier(Modifiers.Shift));
			button.OnKeyPress = e => selectTab(e.Modifiers.HasModifier(Modifiers.Shift));
			button.OnClick = () => selectTab(false);
			button.IsHighlighted = () => allQueues.Contains(palette.CurrentQueue);

			var chromeName = button.ProductionGroup.ToLowerInvariant();
			var icon = button.Get<ImageWidget>("ICON");
			icon.GetImageName = () =>
			{
				return button.IsDisabled() ?
					chromeName + "-disabled" :
					allQueues.Any(q => q.AllQueued().Any(i => i.Done)) ? chromeName + "-alert" : chromeName;
			};
		}

		[ObjectCreator.UseCtor]
		public ClassicProductionLogic(Widget widget, World world)
		{
			if (widget is null) throw new ArgumentNullException( nameof( widget ) );
			if (world  is null) throw new ArgumentNullException( nameof( world  ) );

			if (widget.Id != "SIDEBAR_PRODUCTION") throw new ArgumentException( message: "widget.Id != \"SIDEBAR_PRODUCTION\"", paramName: nameof( widget ) );

			//

			this.world = world;
			palette = widget.Get<ProductionPaletteWidget>("PRODUCTION_PALETTE");

			var background = widget.GetOrNull("PALETTE_BACKGROUND");
			var foreground = widget.GetOrNull("PALETTE_FOREGROUND");
			if (background != null || foreground != null)
			{
				Widget backgroundTemplate = null;
				Widget backgroundBottom = null;
				Widget foregroundTemplate = null;

				if (background != null)
				{
					backgroundTemplate = background.Get("ROW_TEMPLATE");
					backgroundBottom = background.GetOrNull("BOTTOM_CAP");
				}

				if (foreground != null)
					foregroundTemplate = foreground.Get("ROW_TEMPLATE");

				Action<int, int> updateBackground = (_, icons) =>
				{
					var rows = Math.Max(palette.MinimumRows, (icons + palette.Columns - 1) / palette.Columns);
					rows = Math.Min(rows, palette.MaximumRows);

					if (background != null)
					{
						background.RemoveChildren();

						var rowHeight = backgroundTemplate.Bounds.Height;
						for (var i = 0; i < rows; i++)
						{
							var row = backgroundTemplate.Clone();
							row.Bounds.Y = i * rowHeight;
							background.AddChild(row);
						}

						if (backgroundBottom == null)
							return;

						backgroundBottom.Bounds.Y = rows * rowHeight;
						background.AddChild(backgroundBottom);
					}

					if (foreground != null)
					{
						foreground.RemoveChildren();

						var rowHeight = foregroundTemplate.Bounds.Height;
						for (var i = 0; i < rows; i++)
						{
							var row = foregroundTemplate.Clone();
							row.Bounds.Y = i * rowHeight;
							foreground.AddChild(row);
						}
					}
				};

				palette.OnIconCountChanged += updateBackground;

				// Set the initial palette state
				updateBackground(0, 0);
			}

			var typesContainer = widget.Get("PRODUCTION_TYPES");
			foreach (var button in typesContainer.Children.OfType<ProductionTypeButtonWidget>())
			{
				SetupProductionGroupButton(world, this.palette, button);
			}

			var ticker = widget.Get<LogicTickerWidget>("PRODUCTION_TICKER");
			ticker.OnTick = () =>
			{
				if (palette.CurrentQueue == null || palette.DisplayedIconCount == 0)
				{
					// Select the first active tab
					foreach (var b in typesContainer.Children)
					{
						if (!(b is ProductionTypeButtonWidget button) || button.IsDisabled())
							continue;

						button.OnClick();
						break;
					}
				}
			};

			// Hook up scroll up and down buttons on the palette
			var scrollDown = widget.GetOrNull<ButtonWidget>("SCROLL_DOWN_BUTTON");

			if (scrollDown != null)
			{
				scrollDown.OnClick = palette.ScrollDown;
				scrollDown.IsVisible = () => palette.TotalIconCount > (palette.MaxIconRowOffset * palette.Columns);
				scrollDown.IsDisabled = () => !palette.CanScrollDown;
			}

			var scrollUp = widget.GetOrNull<ButtonWidget>("SCROLL_UP_BUTTON");

			if (scrollUp != null)
			{
				scrollUp.OnClick = palette.ScrollUp;
				scrollUp.IsVisible = () => palette.TotalIconCount > (palette.MaxIconRowOffset * palette.Columns);
				scrollUp.IsDisabled = () => !palette.CanScrollUp;
			}

			SetMaximumVisibleRows(palette);
		}

		static void SetMaximumVisibleRows(ProductionPaletteWidget productionPalette)
		{
			var screenHeight = Game.Renderer.Resolution.Height;

			// Get height of currently displayed icons
			var containerWidget = Ui.Root.GetOrNull<ContainerWidget>("SIDEBAR_PRODUCTION");

			if (containerWidget == null)
				return;

			var sidebarProductionHeight = containerWidget.Bounds.Y;

			// Check if icon heights exceed y resolution
			var maxItemsHeight = screenHeight - sidebarProductionHeight;

			var maxIconRowOffest = (maxItemsHeight / productionPalette.IconSize.Y) - 1;
			productionPalette.MaxIconRowOffset = Math.Min(maxIconRowOffest, productionPalette.MaximumRows);
		}
	}
}
