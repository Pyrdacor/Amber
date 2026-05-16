using Amber.Common;
using Amber.Renderer;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.UI;

public enum ItemContainerClickMode
{
	Event,
	DragAndDrop
}

internal class ItemContainer
{
	public const int Width = 16;
	public const int Height = 16;
	public const int TwoHandedSecondSlotMarker = -1;
	readonly Game game;
	Position position;
    ISprite? sprite;
	ISprite? brokenOverlay; // TODO
	// TODO: Item count display
	byte paletteIndex;
	byte displayLayer;
	static ItemContainer? draggedSourceSlot;
    static ItemContainer? DraggedItem { get; set; }
	public static ItemContainerClickMode ClickMode { get; set; } = ItemContainerClickMode.Event;

    public event Action<MouseButtons, KeyModifiers>? Clicked;
    public event Action<IItem, int>? Dragged;
    public event Action<IItem, int>? Dropped;
	public event Action? SlotChanged;
    public static event Action? DraggingStarted;
    public static event Action? DraggingEnded;

    public byte PaletteIndex
	{
		get => paletteIndex;
		set
		{
			paletteIndex = value;

			if (sprite != null)
				sprite.PaletteIndex = value;
			if (brokenOverlay != null)
                brokenOverlay.PaletteIndex = value;
		}
	}

	public bool Empty => ItemCount == 0 || Item == null;

	public bool TwoHandedSecondSlot => ItemCount == TwoHandedSecondSlotMarker && Item != null;

    public bool Broken
	{
		get => brokenOverlay?.Visible ?? false;
		set
		{
			if (value && ItemCount <= 0)
                return;

            if (brokenOverlay != null)
				brokenOverlay.Visible = value;
			else if (value)
                CreateBrokenOverlay();
        }
	}

	public bool Draggable { get; set; }

	public Position Position
	{
		get => position;
		private set
		{
			if (position == value)
                return;

			position = value;

			UpdateRenderPosition(position);
        }
    }

	public bool Visible
	{
		get => sprite?.Visible ?? false;
		set
		{
			if (sprite != null)
				sprite.Visible = value;
			if (brokenOverlay != null)
				brokenOverlay.Visible = value;
		}
    }

    public int ItemCount { get; private set; }

	public IItem? Item { get; private set; }

    public ItemContainer(Game game, Position position, int itemCount, IItem? item, byte displayLayer)
	{
		this.game = game;
		this.position = position;
		this.paletteIndex = game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.Item];

        if (displayLayer > byte.MaxValue - 2)
            displayLayer = byte.MaxValue - 2;

		this.displayLayer = displayLayer;

        if (itemCount > 0 && item == null)
			throw new AmberException(ExceptionScope.Application, "Item count is greater than 0 but item is null.");
		if (itemCount <= 0 && item != null)
            throw new AmberException(ExceptionScope.Application, "Item count is less than 1 but item is not null.");

        if (item != null)
			SetItem(itemCount, item);
	}

	private ItemContainer Clone(bool? draggable = null)
	{
        return new ItemContainer(game, position, ItemCount, Item, displayLayer)
		{
			Draggable = draggable ?? Draggable
        };
    }

	private void CreateBrokenOverlay()
    {
		// TODO

        /*var layer = game.GetRenderLayer(Layer.UI);
        var textureAtlas = layer.Config.Texture!;

        brokenOverlay = layer.SpriteFactory!.Create();
        brokenOverlay.Position = Position;
        brokenOverlay.Size = new(Width, Height);
		// TODO
        //brokenOverlay.TextureOffset = textureAtlas.GetOffset(game.GraphicIndexProvider.GetUIGraphicIndex(UIGraphic.BrokenIcon));
        brokenOverlay.DisplayLayer = (byte)(sprite.DisplayLayer + 1);
        brokenOverlay.PaletteIndex = paletteIndex;
        brokenOverlay.Visible = true;*/
    }

    /// <summary>
    /// Tries to drop the given items.
    /// 
    /// Note: This will not exchange items, only drop them.
	/// So if there is already an item, and the given item
	/// cannot be dropped, it will return the given count.
    /// 
    /// Returns the amount of remaining dragged items.
    /// </summary>
    public int DropItem(int count, IItem item)
	{
		if (ItemCount == 0)
		{
			SetItem(count, item);
			return 0;
		}
		else if (ItemCount == TwoHandedSecondSlotMarker)
		{
			// TODO? Drop on second two-handed slot.
			return count;
		}
		else if (Item!.Index == item.Index)
		{
			if (!Item.IsStackable())
				return count;

			int dropCount = Math.Min(count, 99 - ItemCount);

			SetItem(ItemCount + dropCount, Item);

			count -= dropCount;

            return count;
		}
		else
		{
			// Item is different, cannot drop.
			return count;
		}
	}

	public bool ExchangeItem(int count, IItem item)
	{
		// TODO
		return false;
	}

    public void SetItem(int count, IItem item)
	{
        if (count > 0 && item == null)
            throw new AmberException(ExceptionScope.Application, "Item count is greater than 0 but item is null.");
        if (count <= 0 && item != null)
            throw new AmberException(ExceptionScope.Application, "Item count is less than 1 but item is not null.");

        ItemCount = count;
        Item = item;

        if (count == 0)
		{
			ClearItem();
		}
		else
		{
            var layer = game.GetRenderLayer(Layer.UI);
            var textureAtlas = layer.Config.Texture!;

            var itemGraphicIndex = game!.GraphicIndexProvider.GetItemGraphicIndex(item!.GraphicIndex);

			if (sprite == null)
			{
				sprite = layer.SpriteFactory!.Create();
				sprite.Position = Position;
				sprite.Size = new(Width, Height);
                sprite.DisplayLayer = displayLayer;
                sprite.PaletteIndex = paletteIndex;
            }

            sprite.TextureOffset = textureAtlas.GetOffset(itemGraphicIndex);            
            sprite.Visible = true;

            // TODO: item count display, broken overlay

            SlotChanged?.Invoke();
        }
    }

	public void AddItem(int amount, IItem item)
	{
		if (Item != null && Item.Index != item.Index)
			throw new AmberException(ExceptionScope.Application, "Items with different index were put on same slot.");

		if (ItemCount + amount > 99)
            throw new AmberException(ExceptionScope.Application, "Item count exceeds maximum.");

		if (Empty)
			SetItem(amount, item);
		else
		{
			ItemCount += amount;

            // TODO: update item count display

            SlotChanged?.Invoke();
        }
    }

    public void ReduceItemCount(int amount)
    {
        if (amount >= ItemCount)
        {
            ClearItem();
            return;
        }

        ItemCount -= amount;

        // TODO: update item count display

        SlotChanged?.Invoke();
    }

    public void ClearItem()
	{
		Item = null;
		ItemCount = 0;

		if (sprite != null)
        {
            sprite.Visible = false;
            sprite = null;
        }

		if (brokenOverlay != null)
		{
            brokenOverlay.Visible = false;
            brokenOverlay = null;
        }

        // TODO: item count display

        SlotChanged?.Invoke();
    }

	public static void UpdateDragPosition(Game game, Position position)
	{
		if (DraggedItem == null)
			return;

		DraggedItem.UpdateRenderPosition(position);
    }

    public static void AbortDrag()
    {
        if (DraggedItem == null)
            return;

		var item = DraggedItem.Item!;
		int count = DraggedItem.ItemCount;

        DraggedItem.ClearItem();
        DraggedItem = null;

        DraggingEnded?.Invoke();

        draggedSourceSlot!.DropItem(count, item); // TODO: if exchanged, this is not possible
        draggedSourceSlot = null;
    }

    public static void ConsumeDragged()
    {
        if (DraggedItem == null)
            return;

		DraggedItem.ClearItem();
        DraggedItem = null;
        draggedSourceSlot = null;

        DraggingEnded?.Invoke();
    }

    private void UpdateRenderPosition(Position position)
	{
		// Center cursor pointer in the center of the item sprite.
		position = new(position.X - Width / 2, position.Y - Height / 2);

        if (sprite != null)
			sprite.Position = position;

        if (brokenOverlay != null)
            brokenOverlay.Position = position;

        // TODO: item count display
    }

    public bool MouseClick(Position position, MouseButtons mouseButtons, KeyModifiers keyModifiers)
	{
        var upperLeft = Position;
        var lowerRight = new Position(upperLeft.X + Width, upperLeft.Y + Height);

        if (position.X < upperLeft.X || position.Y < upperLeft.Y || position.X >= lowerRight.X || position.Y >= lowerRight.Y)
            return false; // Not hit

		if (ClickMode == ItemContainerClickMode.Event)
		{
			Clicked?.Invoke(mouseButtons, keyModifiers);
            return true;
		}

        if (DraggedItem == null && ItemCount <= 0)
			return false; // No drop and no item in slot

		if (DraggedItem == null)
		{
			if (!Draggable)
				return false; // Can't pick up

			// TODO: if right mouse button, take all, else only one or show amount box

			// Drag the item
			DraggedItem = Clone(draggable: false);

            if (!game.IsOptionSet(GameOption.UnmaskedDraggedItem))
            {
                DraggedItem.sprite!.MaskColorIndex = 0xf;
            }

            draggedSourceSlot = this;
            Dragged?.Invoke(Item!, 1); // TODO: count
			ClearItem(); // TODO: Some items may remain

            UpdateDragPosition(game, position);

			DraggingStarted?.Invoke();

            return true;
		}
		else
		{
			// Try to drop
			int previousCount = DraggedItem.ItemCount;
            DraggedItem.ItemCount = DropItem(DraggedItem.ItemCount, DraggedItem.Item!);

			if (DraggedItem.ItemCount == 0)
			{
				// Fully dropped
				ConsumeDragged();
				return true;
            }

            if (previousCount != DraggedItem.ItemCount)
			{
				// Some were dropped
				return true; // Nothing else to do
            }

            // Try to exchange item
			if (ItemCount == TwoHandedSecondSlotMarker)
			{
                // TODO: Replace the main slot!
                return false;
            }
			else
			{
				return ExchangeItem(DraggedItem.ItemCount, DraggedItem.Item!);
			}
		}
	}

	public void Destroy()
	{
		ClearItem();
	}
}
