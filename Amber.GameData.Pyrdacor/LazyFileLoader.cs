﻿using Amber.GameData.Pyrdacor.FileSpecs;
using Amber.Serialization;

namespace Amber.GameData.Pyrdacor
{
    public class LazyFileLoader<T, U> where T : class, IFileSpec, new()
    {
        readonly Func<T> loader;
        readonly Func<T, U> valueProvider;
        T? item = null;

        public LazyFileLoader(IDataReader dataReader, GameData gameData, Func<T, U> valueProvider)
        {
            loader = () =>
            {
                var item = PADF.Read(dataReader, gameData);

                if (item is T typedItem)
                    return typedItem;
                else
                    throw new AmberException(ExceptionScope.Data, "Read file had wrong file spec.");
            };
            this.valueProvider = valueProvider;
        }

        public U Load()
        {
            return valueProvider(item ??= loader());
        }
    }

    public class LazyContainerLoader<T, U> where T : IFileSpec, new()
    {
        readonly Func<Dictionary<ushort, T>> loader;
        readonly Func<T, U> valueProvider;
        Dictionary<ushort, T>? items = null;

        public LazyContainerLoader(IDataReader dataReader, GameData gameData, Func<T, U> valueProvider)
        {
            loader = () => PADP.Read<T>(dataReader, gameData);
            this.valueProvider = valueProvider;
        }

        public U Load(ushort key)
        {
            items ??= loader();

            return valueProvider(items[key]);
        }

        public Dictionary<uint, U> LoadAll()
        {
            items ??= loader();

            return items.ToDictionary(i => (uint)i.Key, i => valueProvider(i.Value));
        }
    }
}
